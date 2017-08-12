using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Vehicle: MonoBehaviour, FadeInterface, IPubSub, IExplodable, IReroute {

	public const float START_POSITION_Z = -0.17f;
	public Pos StartPos { set; get; }
	public Pos EndPos { set; get; }
	public Pos CurrentPosition { set; get; } 
	public Pos CurrentTarget { set; get; }
	public Pos PreviousTarget;
    public bool wayPointsLoop;
    public List<Pos> wayPoints;
	private List<Pos> currentPath { set; get; }
    private bool currentPathIsDefinite = false;
    private List<DrivePath> drivePath { set; get; }

	public Setup.VehicleSetup characteristics = null;

	private float SpeedFactor { set; get; }
	private float Acceleration { set; get; }
	private float StartSpeedFactor { set; get; }
	private float ImpatientThresholdNonTrafficLight { set; get; }
	private float ImpatientThresholdTrafficLight { set; get; }
	private float currentSpeed = 0f;
	private float timeOfLastMovement = 0f;

	public float startHealth = 10f;
	public float health = 10f;
	private float vapourStartColorLevel = 0.92f;
	private float vapourEndColorLevel = 0.32f;
	public float totalDrivingDistance = 0f;
	public bool destroying = false;
    private bool paused = false;

	private float EmissionFactor { set; get; }
	private float CollectedEmissionAmount = 0f;

	private const float THRESHOLD_EMISSION_PUFF = 0.030f;
	private const float KPH_TO_LONGLAT_SPEED = 30000f;

	private float AwarenessBreakFactor { set; get; }

	private HashSet<Vehicle> FacVehiclesInAwarenessArea { set; get; }
	private HashSet<Vehicle> PcVehiclesInAwarenessArea { set; get; }

	private HashSet<HumanLogic> FacHumansInAwarenessArea { set; get; }
	private HashSet<HumanLogic> PcHumansInAwarenessArea { set; get; }

	private HashSet<TrafficLightLogic> YellowTrafficLightPresence { set; get; }
	private HashSet<TrafficLightLogic> RedTrafficLightPresence { set; get; }

	private float backingCounterSeconds = 0f;

	public Camera vehicleCameraObj;
	private static Vehicle debug;
	public static Camera debugCamera = null;
    public bool isOwningCamera = false;
    public bool switchingCameraInProgress = false;

	public int vehicleId;
	private TrafficLightLogic upcomingTrafficLight = null;

	public static int numberOfCars = 0;
	public static int vehicleInstanceCount = 0;
	private static float MAP_SPEED_TO_KPH_FACTOR = 100f;
	private static float IMPATIENT_TRAFFIC_LIGHT_THRESHOLD = 17f;
	private static float IMPATIENT_NON_TRAFFIC_LIGHT_THRESHOLD = 8f;

    public static void Reset() {
        Vehicle.numberOfCars = 0;
        Vehicle.vehicleInstanceCount = 0;
    }

	public void setDebug() {
		grabCamera ();
		Vehicle.debug = this;
	}

	public static void detachCurrentCamera () {
		if (Vehicle.debugCamera != null) {
			Vehicle.debugCamera.enabled = false;
			Vehicle vehicle = Vehicle.debugCamera.transform.parent.GetComponent<Vehicle> ();
			vehicle.isOwningCamera = false;

            vehicle.switchFromToCamera(Vehicle.debugCamera, Game.instance.perspectiveCamera, true);
		}
	}

	public void grabCamera ()
	{
		Vehicle.detachCurrentCamera ();

        isOwningCamera = true;
		// Instantiate camera in vehicle
		Vehicle.debugCamera = Instantiate (vehicleCameraObj, Vector3.zero, Quaternion.identity) as Camera;
		Vehicle.debugCamera.transform.parent = this.transform;
		Vehicle.debugCamera.transform.localPosition = new Vector3(0f, 0f, transform.position.z + vehicleCameraObj.transform.position.z / transform.localScale.z);
        Vehicle.debugCamera.transform.localScale = Vector3.one;

        this.switchFromToCamera(Game.instance.perspectiveCamera, Vehicle.debugCamera);
    }

    private void switchFromToCamera (Camera from, Camera to, bool destroyFromCameraAfter = false) {
        StartCoroutine( animateBetweenCameras(from, to, destroyFromCameraAfter) );
	}

    private IEnumerator animateBetweenCameras(Camera from, Camera to, bool destroyFromCameraAfter) {
        float time = 0.3f;

        switchingCameraInProgress = true;

//        yield return ScreenWipe.use.CrossFadePro (from, to, time);
        yield return Singleton<CameraSwitch>.Instance.animate(from, to, time, !destroyFromCameraAfter);

        yield return new WaitForSeconds(time);

        switchingCameraInProgress = false;

        if (destroyFromCameraAfter) {
            Destroy (from.gameObject);
        }
    }

	// TODO - Carefulness (drunk level, tired, age...)
	// Use this for initialization
	void Start () {
        ExplosionHelper.Add(this);
		//debugPrint = true;
		initInformationVehicle ();
		initVehicleProfile ();
		updateCurrentTarget ();

        DrivePath startDrivePath = drivePath[0];
		// Set start speed for car
		// TODO - In the future, if coming from parking, start from 0
		float wayTargetSpeedKmH = startDrivePath.wayWidthFactor * MAP_SPEED_TO_KPH_FACTOR; 	// Eg 51 km/h
		// Car target speed
		float vehicleTargetSpeedKmH = wayTargetSpeedKmH * SpeedFactor;						// Eg 10% faster = 56.5 km/h
		// Car starting speed
		currentSpeed = StartSpeedFactor * vehicleTargetSpeedKmH / KPH_TO_LONGLAT_SPEED;			

		numberOfCars++;
		vehicleId = vehicleInstanceCount++;
		DataCollector.Add ("Total # of vehicles", 1f);

		// Report one more car
		GenericVehicleSounds.VehicleCountChange();

		transform.position = Misc.WithZ(startDrivePath.startVector, transform.position);
        Vector3 positionMovementVector = startDrivePath.endVector - startDrivePath.startVector;
        transform.rotation = Quaternion.FromToRotation (Vector3.right, positionMovementVector);

		StartCoroutine (reportStats ());

		PubSub.subscribe ("Click", this, 100);

		setUpcomingTrafficLight();
    }

	private void initInformationVehicle () {
		VehicleInfo vehicleInfo = GetComponent<VehicleInfo> ();

		if (characteristics != null) {
			vehicleInfo.numberOfPassengers = characteristics.passengerIds.Count;
			totalDrivingDistance = characteristics.distance;
			health = startHealth * characteristics.condition;
		}

		InformationVehicle informationVehicle = GetComponent<InformationVehicle> ();
		// TODO - Special logic if backer - randomize backers?!
		informationVehicle.driver = gameObject.AddComponent<InformationHuman> ();
		informationVehicle.driver.passive = true;

		// TODO - Passengers to be family of driver? (Same last name in x% of cases)
		for (int i = 0; i < vehicleInfo.getNumberOfPassengers (); i++) {
			InformationHuman passenger = gameObject.AddComponent<InformationHuman> ();
			passenger.passive = true;
			passenger.passengerIndex = i;
			informationVehicle.passengers.Add (passenger);
		}
	}

	public void setCharacteristics(Setup.VehicleSetup characteristics) {
		this.characteristics = characteristics;
        if (characteristics != null && characteristics.specialIcon != null) {
            SpecialIcon specialIcon = gameObject.GetComponent<SpecialIcon>();
            specialIcon.setIcon(characteristics.specialIcon);
        }
	}

	private static float GetAccForKmh(float currentSpeed, float targetSpeed) {
		float x = currentSpeed;
		if (currentSpeed < targetSpeed) {
			x = Mathf.Max (x, 0f);
			float a=10f, b=2.782511f, c=-0.05497385f, d=0.0003783534f, e=-8.548685e-7f;
//			float a = 0.001696185f, b = 0.02523364f, c = -0.000497608f, d = 0.000003406148f, e = -7.872413E-9f;
			return a + b * x + c * Mathf.Pow (x, 2) + d * Mathf.Pow (x, 3) + e * Mathf.Pow (x, 4);
		} else {
			if (x >= 0) {
				// If breaking
				float a = 29f, b = 0.005330882f, c = -0.0005330882f;
				return -6 * (a + b * x + c * Mathf.Pow (x, 2));
			} else {
				// Backing
				return -GetAccForKmh(-x, -targetSpeed) / 15f;
			}
		}
	}

	// Update is called once per frame
	void Update () {
        if (isOwningCamera && Vehicle.debugCamera != null) {
            Vehicle.debugCamera.transform.rotation = Quaternion.identity;
        }
		if (Game.isMovementEnabled()) {
			if (!paused && !destroying) {
                if (drivePath.Count > 0) {
                    DrivePath currentDrivePath = drivePath[0];

					// Way target speed
                    float wayTargetSpeedKmH = currentDrivePath.wayWidthFactor * MAP_SPEED_TO_KPH_FACTOR; 	// Eg 51 km/h
					// Car target speed
                    float vehicleTargetSpeedKmH = wayTargetSpeedKmH * SpeedFactor;							// Eg 10% faster = 56.5 km/h

					// Current car speed
                    float currentSpeedKmH = currentSpeed * KPH_TO_LONGLAT_SPEED;

					// Lowest break factor (decides how fast the car currently want to go)
                    float breakFactor = Mathf.Min (currentDrivePath.breakFactor, AwarenessBreakFactor);

					// Car target after break factor
                    float vehicleTargetSpeedAfterBreakFactorKmH = breakFactor * vehicleTargetSpeedKmH;

					// Acceleration this "second" at current car speed
                    float speedChangeKmh = GetAccForKmh (currentSpeedKmH, vehicleTargetSpeedAfterBreakFactorKmH);

					// Speed change this delta time
                    float speedChangeInFrameKmh = speedChangeKmh * Time.deltaTime;

					// Car speed change for this current frame
                    float speedChangeInFrame = speedChangeInFrameKmh / KPH_TO_LONGLAT_SPEED;

                    float speedChangeInFrameNoBacking;

					// If in backing state, allow backing and count down the time to be backing
                    if (backingCounterSeconds > 0f) {
                        backingCounterSeconds -= Time.deltaTime;
                        startBacklights ();
                        if (backingCounterSeconds <= 0f) {
                            stopBacklights ();
                            autosetAwarenessBreakFactor ();
                        }

                        speedChangeInFrameNoBacking = speedChangeInFrame;
                    } else {
						// No backing
                        speedChangeInFrameNoBacking = Mathf.Max (speedChangeInFrame, -currentSpeed);
                    }

					// Apply speed change
                    currentSpeed += speedChangeInFrameNoBacking;

					// React to standing still or moving this frame
                    if (breakFactor == 0f) {
                        stats [STAT_WAITING_TIME].add (Time.deltaTime);

                        if (Time.time > timeOfLastMovement + ImpatientThresholdTrafficLight) {
                            performIrritationAction ();
							// Make sure to not honk directly again
                            timeOfLastMovement = Time.time - 5f * Misc.randomRange (0.8f, 1.2f);
                        }
					// TODO - ImpatientThresholdNonTrafficLight as well, if traffic light is NOT the cause of vehicle standing still
                    } else {
                        timeOfLastMovement = Time.time;
                        performIrritationAction (false);
                        stats [STAT_DRIVING_TIME].add (Time.deltaTime);
                    }

                    if (currentSpeed != 0f) {
                        float metersDriven = Mathf.Abs (Misc.kmhToMps (currentSpeed * KPH_TO_LONGLAT_SPEED) * Time.deltaTime);
                        stats [STAT_DRIVING_DISTANCE].add (metersDriven);
                        totalDrivingDistance += metersDriven;
                    }

                    calculateCollectedEmission (speedChangeInFrame);

                    adjustColliders ();

                    float driveLengthLeft = currentSpeed * 10;

                    while (driveLengthLeft > 0) {
						Vector3 currentPos = new Vector3 (transform.position.x, transform.position.y, 0f);
						Vector3 currentTargetInPath = currentDrivePath.endVector;

						if (driveLengthLeft > currentDrivePath.fullLength) {
							// We drive more than current target...
							// How long is left?
							driveLengthLeft -= currentDrivePath.fullLength;
							// Rotate car to current position
							Vector3 positionMovementVector = currentDrivePath.endVector - currentDrivePath.startVector;
							Quaternion vehicleRotation = Quaternion.FromToRotation (Vector3.right, positionMovementVector);
							transform.rotation = vehicleRotation;
							// Move car to target position
							transform.position = Misc.WithZ(currentTargetInPath, transform.position);

							// Remove current drive path
							if (drivePath.Count > 0) {
								drivePath.RemoveAt(0);
								if (drivePath.Count > 0) {
									// We should continue driving on next road
									currentDrivePath = drivePath[0];
                                }
							}
						} else {
							currentDrivePath.fullLength -= driveLengthLeft;
							// Rotate car
							Vector3 positionMovementVector = currentTargetInPath - currentPos;
							Quaternion vehicleRotation = Quaternion.FromToRotation (Vector3.right, positionMovementVector);
							transform.rotation = vehicleRotation;
							// Move car
							transform.position = transform.position + (currentTargetInPath - currentPos).normalized * driveLengthLeft;
 							// No more distance to drive...
                            driveLengthLeft = 0;
						}
                    }

                    if (shouldBlink(currentDrivePath)) {
                        if (currentDrivePath.blinkDirection == "left") {
                            startBlinkersLeft();
                        } else {
                            startBlinkersRight();
                        }
                    } else {
                        stopBlinkers();
                    }

					// TODO - OLD COLLISION logic reported "statReportPossibleCrossing" - do this somewhere in new logic as well
                    // stats [STAT_PASSED_CROSSINGS].add (1f);

                    // TODO - Need to flag which traffic light we consider
//                    setUpcomingTrafficLight();

                } else if (health > 0f) {
					// TODO - We've probably reached the end of the road, what to do?
					//			Debug.Log ("No movement");
					fadeOutAndDestroy ();
				}

				// Emit gas depending on health
				if (health < startHealth / 2f) {
					if (UnityEngine.Random.value < (1f - getHealthLevel ()) * 0.001f) {
						PubSub.publish ("Vehicle:emitVapour", this);
					}
				}
            }
		}
	}

    public bool shouldBlink (DrivePath dp) {
        if (dp.blinkDirection != null && dp.blinkStart != -1f) {
            return dp.blinkStart == 0 || dp.fullLength <= dp.blinkStart;
        }
        return false;
    }

	public bool hasSpeed () {
		return currentSpeed != 0f;
	}

	private void performIrritationAction (bool startAction = true) {
		VehicleSounds vehicleSounds = GetComponent<VehicleSounds> ();
		float frustrationLevel = vehicleSounds.getFrustrationLevel ();
		// TODO - This is the real level where the vehicle should back
//		if (frustrationLevel < 8f) {
		if (frustrationLevel < 2f || !startAction) {
			// TODO - Maybe base honking/blinking depending on frustration level
			if (!startAction) {
				vehicleSounds.honk (startAction);
			} else {
				if (UnityEngine.Random.value < 0.5f) {
					vehicleSounds.honk (startAction);
                    DataCollector.Add("Vehicle:honk", 1.0f);
				} else {
					flashHeadlights ();
                    DataCollector.Add("Vehicle:flash headlight", 1.0f);
				}

				// If seeing human and no other slowdown - send signal to Human
				if (onlyHumansInPanicCollider ()) {
					foreach (HumanLogic human in PcHumansInAwarenessArea) {
						human.vehicleIrritationAction (this);
					}
				}
				// Humans subscribing to the car, send signal to Human
				PubSub.publish("Vehicle#" + vehicleId + ":Irritation", this);
			}
		} else {
			AwarenessBreakFactor = -0.15f;
			backingCounterSeconds = 0.8f;
			stopBreaklights ();
            DataCollector.Add("Vehicle:backing", 1.0f);
		}
	}

    // TODO - Don't run every frame. Calculate only when necessary.
	private void adjustColliders () {
		VehicleCollider[] vehicleColliders = GetComponentsInChildren<VehicleCollider> ();

//		float forwardColliders = 1f;
		float forwardColliders = 1.5f;
		float pc = 1f / 3f;
		float fac = 2f / 3f;

		float minSpeedThreshold = 0.0005f;
		float maxSpeedThreshold = 0.005f;

		float totalColliderSize = forwardColliders;
		float maxColliderAdditionFactor = 3f;

		if (currentSpeed > maxSpeedThreshold) {
			totalColliderSize = forwardColliders + maxColliderAdditionFactor * forwardColliders;
		} else if (currentSpeed > minSpeedThreshold) {
			float max = maxSpeedThreshold - minSpeedThreshold;
			float current = currentSpeed - minSpeedThreshold;
			totalColliderSize = forwardColliders + maxColliderAdditionFactor * current / max * forwardColliders;
		}

		BoxCollider carCollider = vehicleColliders [2].GetComponent<BoxCollider> ();
		float carColliderOffset = carCollider.size.x / 2f;

		float widthFac = fac * totalColliderSize;
		float midFac = carColliderOffset + (pc * totalColliderSize) + widthFac / 2f;

		BoxCollider facCollider = vehicleColliders [0].GetComponent<BoxCollider> ();
		facCollider.center = new Vector3 (midFac, 0f, 0f);
		facCollider.size = new Vector3 (widthFac, 1f, 1f);

		float widthPc = pc * totalColliderSize;
		float midPc = carColliderOffset + widthPc / 2f;

		BoxCollider pcCollider = vehicleColliders [1].GetComponent<BoxCollider> ();
		pcCollider.center = new Vector3 (midPc, 0f, 0f);
		pcCollider.size = new Vector3 (widthPc, 1f, 1f);

		// TODO - When backing - calculate back collider
		float backColliders = 0.2f;
	}

	void initVehicleProfile () {
		float minSpeedFactor = 0.8f;
		float maxSpeedFactor = 1.2f;
		float speedFactorInterval = maxSpeedFactor - minSpeedFactor;

		if (characteristics != null && characteristics.speedFactor > 0f) {
			SpeedFactor = characteristics.speedFactor;
		} else {
			SpeedFactor = Misc.randomRange (minSpeedFactor, maxSpeedFactor);
		}

		if (characteristics != null && characteristics.acceleration > 0f) {
			Acceleration = characteristics.acceleration;
		} else {
			Acceleration = Misc.randomRange (2f, 3f);
		}

		if (characteristics != null && characteristics.startSpeedFactor > 0f) {
			StartSpeedFactor = characteristics.startSpeedFactor;
		} else {
			StartSpeedFactor = Misc.randomRange (0.5f, 1f);
		}

		float minImpatientFactor = 0.8f;
		float maxImpatientFactor = 1.2f;
		float impatientFactorInterval = maxImpatientFactor - minImpatientFactor;

		float impatientFactor = (speedFactorInterval - ((SpeedFactor - minSpeedFactor) / speedFactorInterval)) * impatientFactorInterval + minImpatientFactor;

		if (characteristics != null && characteristics.impatientThresholdNonTrafficLight > 0f) {
			ImpatientThresholdNonTrafficLight = characteristics.impatientThresholdNonTrafficLight;
		} else {
			ImpatientThresholdNonTrafficLight = IMPATIENT_NON_TRAFFIC_LIGHT_THRESHOLD * impatientFactor;
		}

		if (characteristics != null && characteristics.impatientThresholdTrafficLight > 0f) {
			ImpatientThresholdTrafficLight = characteristics.impatientThresholdTrafficLight;
		} else {
			ImpatientThresholdTrafficLight = IMPATIENT_TRAFFIC_LIGHT_THRESHOLD * impatientFactor;
		}

        if (characteristics != null && characteristics.wayPoints != null) {
            this.wayPoints = NodeIndex.getPosById(characteristics.wayPoints);
            this.wayPointsLoop = characteristics.wayPointsLoop;
        } else {
            this.wayPoints = new List<Pos>();
        }

        if (characteristics != null) {
            gameObject.GetComponent<Mood>().init(characteristics.mood[0], characteristics.angrySpeed, characteristics.happySpeed, characteristics.mood[1], characteristics.mood[2]);
        } else {
            gameObject.GetComponent<Mood>().init();
        }

		AwarenessBreakFactor = 1.0f;
		timeOfLastMovement = Time.time;
		EmissionFactor = Misc.randomRange (0.1f, 1.0f);
		CollectedEmissionAmount = Misc.randomRange (0.000f, 0.002f);

		FacVehiclesInAwarenessArea = new HashSet<Vehicle> ();
		PcVehiclesInAwarenessArea = new HashSet<Vehicle> ();

		FacHumansInAwarenessArea = new HashSet<HumanLogic> ();
		PcHumansInAwarenessArea = new HashSet<HumanLogic> ();

		YellowTrafficLightPresence = new HashSet<TrafficLightLogic> ();
		RedTrafficLightPresence = new HashSet<TrafficLightLogic> ();
	}

	public void reportColliderExit (Collider col, string colliderName) {
		if (!destroying) {
			CollisionObj rawCollisionObj = getColliderType (col);
			if (rawCollisionObj != null) {
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				TrafficLightCollisionObj trafficLightCollisionObj = rawCollisionObj.typeName == TrafficLightCollisionObj.NAME ? (TrafficLightCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				string otherColliderName = rawCollisionObj.CollisionObjType;
				if (vehicleCollisionObj != null) {
					if (otherColliderName == CollisionObj.VEHICLE_COLLIDER) {
						Vehicle otherVehicle = vehicleCollisionObj.Vehicle;
						if (colliderName == "FAC") {
							// Front collider discovered car, slow down
							removeVehicleInAwarenessArea (colliderName, otherVehicle);
							autosetAwarenessBreakFactor ();
						} else if (colliderName == "PC") {
							// Panic collider discovered car, break hard
							removeVehicleInAwarenessArea (colliderName, otherVehicle);
							autosetAwarenessBreakFactor ();
						}
					}
				} else if (trafficLightCollisionObj != null && colliderName == "CAR") {
					TrafficLightLogic trafficLightLogic = trafficLightCollisionObj.TrafficLightLogic;
					// Car is in either yellow or red traffic light, slow down or break hard
					removeTrafficLightPresence (otherColliderName, trafficLightLogic);
					autosetAwarenessBreakFactor ();
				} else if (humanCollisionObj != null) {
					if (colliderName == "FAC") {
						// Human leaving Front collider
						removeHumanInAwarenessArea (colliderName, humanCollisionObj.Human);
						autosetAwarenessBreakFactor ();
					} else if (colliderName == "PC") {
						// Human leaving Panic collider
						removeHumanInAwarenessArea (colliderName, humanCollisionObj.Human);
						autosetAwarenessBreakFactor ();
					}
				}
			}
		}
	}

	public void reportCollision (Collider col, string colliderName) {
		// Colliders for car:
		//								-----
		// Front aware collider	(FAC)	|	|
		//								|	|
		// 								+---+
		// Panic collider (PC)			|	|
		// 								/---\
		//								|	|
		//								|	|
		// Car (CAR)					|	|
		//								|	|
		//								\---/
		// Backing collider	(BC)		|	|
		// 								-----
		//
		// Scenarios for collision:
		//
		// * FAC with other car FAC							- Driving towards each other, risk of collision, depending on vehicle behaviour profile either hard break, steer away, honk...
		// * FAC with other car CAR							- Driving close to other car, decelerate
		// * PC with other car CAR (currentSpeed > 0)		- Driving close to other car, decelerate harder
		// * CAR with other car CAR (currentSpeed > 0		- Collision with other car, crash depends on speed; bump -> HONK and get angry, possibly drive aside and discuss/swear/scream/fight; crash -> Injury, possible police / ambulance
		// * PC with other car CAR (currentSpeed <= 0)		- Other car is backing, depending on vehicle characteristics, honk and back
		// * BC ...
		if (!destroying) {
			CollisionObj rawCollisionObj = getColliderType (col);
			if (rawCollisionObj != null) {
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				TrafficLightCollisionObj trafficLightCollisionObj = rawCollisionObj.typeName == TrafficLightCollisionObj.NAME ? (TrafficLightCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				// Logic for other vehicle awareness
				string otherColliderName = rawCollisionObj.CollisionObjType;
				if (vehicleCollisionObj != null) {
					if (otherColliderName == CollisionObj.VEHICLE_COLLIDER) {
						Vehicle otherVehicle = vehicleCollisionObj.Vehicle;
						// Awareness for other CAR
						if (colliderName == "FAC") {
							// Front collider discovered car, slow down
							addVehicleInAwarenessArea (colliderName, otherVehicle);
							autosetAwarenessBreakFactor ();
						} else if (colliderName == "PC") {
							// Panic collider discovered car, break hard
							addVehicleInAwarenessArea (colliderName, otherVehicle);
							autosetAwarenessBreakFactor ();
						}

						// Crashing our "CAR" with other CAR
						if (colliderName == "CAR") {
							float speedKmh = currentSpeed * KPH_TO_LONGLAT_SPEED;
							Vector3 speedVector = transform.rotation * new Vector3 (speedKmh, 0f, 0f);

							float otherVehicleSpeedKmh = otherVehicle.currentSpeed * KPH_TO_LONGLAT_SPEED;
							Vector3 otherVehicleSpeedVector = otherVehicle.transform.rotation * new Vector3 (otherVehicleSpeedKmh, 0f, 0f);

							Vector3 collissionDiff = speedVector - otherVehicleSpeedVector;
							float collissionAmount = collissionDiff.magnitude;

							stats [STAT_TOTAL_COLLISSION_AMOUNT].add (collissionAmount / 2f);
							if (collissionAmount < 10f) {
								stats [STAT_MINOR_COLLISSIONS].add (0.5f);
							} else {
								stats [STAT_MAJOR_COLLISSIONS].add (0.5f);
							}

							registerCollissionAmount (collissionAmount, otherVehicle);
						}
					}


				} else if (trafficLightCollisionObj != null && colliderName == "CAR") {
					TrafficLightLogic trafficLightLogic = trafficLightCollisionObj.TrafficLightLogic;
					// Car is in either yellow or red traffic light, slow down or break hard
					addTrafficLightPresence (otherColliderName, trafficLightLogic);
					autosetAwarenessBreakFactor ();
				} else if (humanCollisionObj != null) {
					if (colliderName == "FAC") {
						// Front collider discovered human, slow down
						addHumanInAwarenessArea (colliderName, humanCollisionObj.Human);
						autosetAwarenessBreakFactor ();
					} else if (colliderName == "PC") {
						// Panic collider discovered human, break hard
						addHumanInAwarenessArea (colliderName, humanCollisionObj.Human);
						autosetAwarenessBreakFactor ();
					}
				}
			}
		}
	}

	private float getHealthLevel () {
		return health / (startHealth / 2f);
	}

	public Color getVapourColor() {
		float healthLevel = getHealthLevel();
		float colorSpan = vapourStartColorLevel - vapourEndColorLevel;
		float colorLevel = Mathf.Min(vapourStartColorLevel, vapourEndColorLevel + healthLevel * colorSpan);
		return new Color (colorLevel, colorLevel, colorLevel);
	}

	private void registerCollissionAmount (float amount, Vehicle otherVehicle) {
		bool shouldPlayCrashSound = vehicleId < otherVehicle.vehicleId;
		health -= amount;
		if (health <= 0f) {
			VehicleLights lights = GetComponentInChildren<VehicleLights> ();
			lights.turnAllOff ();
			startWarningBlinkers ();

			createDangerHalo ();
			blinkUntilClickedAndDestroy ();

			stats [STAT_CRASHES].add (0.5f);

			if (shouldPlayCrashSound) {
				// Big crash - play sound
				VehicleSounds vehicleSounds = GetComponent<VehicleSounds> ();
				vehicleSounds.playMajorCrashSound ();
			}
		} else if (shouldPlayCrashSound) {
			// Minor crash - play sound
			VehicleSounds vehicleSounds = GetComponent<VehicleSounds> ();
			vehicleSounds.playMinorCrashSound ();
		}
	}

	private void createDangerHalo () {
		PubSub.publish ("Vehicle:createDangerHalo", this);
	}

	private void autosetAwarenessBreakFactor () {
		if (backingCounterSeconds <= 0f) {
			bool hasVehicleInFac = FacVehiclesInAwarenessArea.Any (); 
			bool hasVehicleInPc = PcVehiclesInAwarenessArea.Any (); 
			bool hasHumanInFac = FacHumansInAwarenessArea.Any ();
			bool hasHumanInPc = PcHumansInAwarenessArea.Any ();
			bool isYellowLightPresent = YellowTrafficLightPresence.Any (); 
			bool isRedLightPresent = RedTrafficLightPresence.Any (); 
			if (hasVehicleInPc || hasHumanInPc || isRedLightPresent) {
				AwarenessBreakFactor = 0.0f;
				startBreaklights ();
			} else if (hasVehicleInFac || hasHumanInFac || isYellowLightPresent) {
				AwarenessBreakFactor = 0.25f;
				stopBreaklights ();
			} else {
				AwarenessBreakFactor = 1f;
				stopBreaklights ();
			}
		}
	}
	private void addVehicleInAwarenessArea (string colliderName, Vehicle otherVehicle) {
		HashSet<Vehicle> vehiclesInAwarenessArea = colliderName == "FAC" ? FacVehiclesInAwarenessArea : PcVehiclesInAwarenessArea;
		vehiclesInAwarenessArea.Add (otherVehicle);
	}

	private void removeVehicleInAwarenessArea (string colliderName, Vehicle otherVehicle) {
		HashSet<Vehicle> vehiclesInAwarenessArea = colliderName == "FAC" ? FacVehiclesInAwarenessArea : PcVehiclesInAwarenessArea;
		vehiclesInAwarenessArea.Remove (otherVehicle);
	}

	private void addHumanInAwarenessArea (string colliderName, HumanLogic human) {
		HashSet<HumanLogic> humansInAwarenessArea = colliderName == "FAC" ? FacHumansInAwarenessArea : PcHumansInAwarenessArea;
		humansInAwarenessArea.Add (human);
	}

	private void removeHumanInAwarenessArea (string colliderName, HumanLogic human) {
		HashSet<HumanLogic> humansInAwarenessArea = colliderName == "FAC" ? FacHumansInAwarenessArea : PcHumansInAwarenessArea;
		humansInAwarenessArea.Remove (human);
	}

	private void addTrafficLightPresence (string colliderName, TrafficLightLogic trafficLightLogic) {
		if (upcomingTrafficLight == trafficLightLogic) {
			if (colliderName == CollisionObj.TRAFFIC_LIGHT_GREEN) {
				upcomingTrafficLight = null;
                RedTrafficLightPresence.Clear();
                YellowTrafficLightPresence.Clear();
			} else {
				HashSet<TrafficLightLogic> trafficLightPresence = colliderName == CollisionObj.TRAFFIC_LIGHT_YELLOW ? YellowTrafficLightPresence : RedTrafficLightPresence;
				trafficLightPresence.Add(trafficLightLogic);
//		        Debug.Log ("Added to " + colliderName + ", length: " + trafficLightPresence.Count);
			}
		}
	}

	private void removeTrafficLightPresence (string colliderName, TrafficLightLogic trafficLightLogic) {
		HashSet<TrafficLightLogic> trafficLightPresence = colliderName == CollisionObj.TRAFFIC_LIGHT_YELLOW ? YellowTrafficLightPresence : RedTrafficLightPresence;
		if (trafficLightPresence.Contains(trafficLightLogic)) {
			trafficLightPresence.Remove(trafficLightLogic);
//	    	Debug.Log ("Removed from " + colliderName + ", length: " + trafficLightPresence.Count);
		}
	}

	private bool onlyHumansInPanicCollider () {
		return !PcVehiclesInAwarenessArea.Any () && !RedTrafficLightPresence.Any () && PcHumansInAwarenessArea.Any ();
	}

	private CollisionObj getColliderType (Collider col)
	{
		GameObject colliderGameObject = col.gameObject;
        if (colliderGameObject.GetComponentInParent<Vehicle> () != null) {
			// Car
			return new VehicleCollisionObj (colliderGameObject.GetComponentInParent<Vehicle> (), colliderGameObject.name);
		} else if (colliderGameObject.GetComponentInParent<TrafficLightLogic> () != null) {
			// Traffic Light
			return new TrafficLightCollisionObj (colliderGameObject.GetComponentInParent<TrafficLightLogic> (), colliderGameObject.name);
		} else if (colliderGameObject.GetComponentInParent<HumanLogic> () != null) {
			// Human (only "BODY" is interesting)
			if (HumanCollider.colliderNamesForGameObjectName.ContainsKey(colliderGameObject.name)) {
				string name = HumanCollider.colliderNamesForGameObjectName[colliderGameObject.name];
				if (name == "BODY") {
					return new HumanCollisionObj(colliderGameObject.GetComponentInParent<HumanLogic>(), name);
				}
			}
		}
		return null;
	}

	public void updateCurrentTarget () {
		// TODO - Needs to work with new drive logic
		if (CurrentTarget != null && TrafficLightIndex.TrafficLightsForPos.ContainsKey(CurrentTarget.Id)) {
			stats[STAT_PASSED_TRAFFICLIGHT].add(1f);
		}

        if (wayPointsLoop && CurrentTarget == wayPoints[0]) {
			// TODO - Needs to work with new drive logic
			// We have reached our first wayPoint and should loop, place first waypoint last and recalculate route
            wayPoints.RemoveAt(0);
            wayPoints.Add(CurrentTarget);
            currentPath = Game.calculateCurrentPaths (CurrentTarget, EndPos, PreviousTarget, wayPoints, true, false);
        }

		if (currentPathIsDefinite) {
            // TODO - Needs to work with new drive logic
            while (currentPath[0] != CurrentPosition) {
                currentPath.RemoveAt(0);
            }
        } else {
            // Calculate path once, set it as definite to not re-calculate at next crossing
            currentPath = Game.calculateCurrentPaths (CurrentPosition, EndPos, null, wayPoints, true);
            currentPathIsDefinite = true;

/*
			// TODO - START DEBUG

			// 660826508, 686773447, 660826508
            List<Pos> poses = new List<Pos>{
                NodeIndex.getPosById(660826508L),
                NodeIndex.getPosById(686773447L),
                NodeIndex.getPosById(747255967L)
            };
            List<Vector3> vectors = getVectorsForPath(poses);
            DebugFn.arrows(vectors);
            DebugFn.square(NodeIndex.getPosById(686773447L));

            // 686773447L, 747255967L
            WayReference way = NodeIndex.getWayReference(686773447L, 747255967L);
            Vector3 center = getCenterYOfField(way, NodeIndex.getPosById(686773447L));
//            DebugFn.print(center);
            DebugFn.temporaryOverride(Color.blue, 10f);
            DebugFn.arrow(vectors[vectors.Count - 2], vectors[vectors.Count - 2] + center);

            // TODO - END DEBUG
*/

            // TODO Calculate total path in vector. For future use if using other driving logic.
            List<Vector3> pathVectors = getVectorsForPath(currentPath);
            DebugFn.arrows(pathVectors);
            drivePath = DrivePath.Build(pathVectors, currentPath);
			foreach (DrivePath dp in drivePath) {
                DebugFn.arrow(dp.startVector, dp.endVector);
            }
//            DebugFn.temporaryOverride(Color.magenta, 2f);
//            DebugFn.DebugPath(pathVectors);
        }
	}

    private List<Vector3> getVectorsForPath(List<Pos> path) {
        List<Vector3> vectors = new List<Vector3>();

        Pos prevPos = path[0];
        for (int i = 1; i < path.Count; i++) {
            Pos currPos = path[i];
            if (i == 1) {
                vectors.Add(Game.getCameraPosition(prevPos) + getCenterYOfField(NodeIndex.getWayReference(prevPos.Id, currPos.Id), prevPos));
            }
            if (i < path.Count - 1) {
                Pos nextPos = path[i + 1];
                Vector3 centerOffsetCurr = getCenterYOfField(NodeIndex.getWayReference(prevPos.Id, currPos.Id), prevPos);
                Vector3 centerOffsetNext = getCenterYOfField(NodeIndex.getWayReference(currPos.Id, nextPos.Id), currPos);
                vectors.Add(Game.getCameraPosition(currPos) + (centerOffsetCurr + centerOffsetNext) / 2f);
            } else {
	            vectors.Add(Game.getCameraPosition(currPos) + getCenterYOfField(NodeIndex.getWayReference(prevPos.Id, currPos.Id), prevPos));
            }
            prevPos = currPos;
        }

        return vectors;
    }

	public Vector3 getCenterYOfField (WayReference wayReference, Pos fromPosition) {
		// TODO - this should be a variable
		bool inWayDirection = wayReference.isNode1 (fromPosition);
		float wayNumberOfFieldsOurDirection = wayReference.getNumberOfFieldsInDirection (fromPosition);
		// TODO - Logic for our car... where are we headed
		float field = wayNumberOfFieldsOurDirection;
//		Debug.Log ("field: " + field);
		float wayNumberOfFields = wayReference.getNumberOfFields ();
//		Debug.Log ("wayNumberOfFields: " + wayNumberOfFields);
//		Debug.Log ("wayNumberOfFieldsOurDirection: " + wayNumberOfFieldsOurDirection);
		float wayWidth = wayReference.transform.localScale.y;
//		Debug.Log ("wayWidth: " + wayWidth);
		float absoluteFieldNumber = wayNumberOfFields - wayNumberOfFieldsOurDirection + field;
//		Debug.Log ("absoluteFieldNumber: " + absoluteFieldNumber);
		float rightPosition = wayWidth / wayNumberOfFields * absoluteFieldNumber;
//		Debug.Log ("rightPosition: " + rightPosition);
		float eachFieldWidth = wayWidth / wayNumberOfFields;
//		Debug.Log ("eachFieldWidth: " + eachFieldWidth);
		float centerPosition = rightPosition - eachFieldWidth / 2;
//		Debug.Log ("centerPosition: " + centerPosition);
		float offsetFromMiddle = centerPosition - wayWidth / 2;
//		Debug.Log ("offsetFromMiddle: " + offsetFromMiddle);
		offsetFromMiddle = inWayDirection ? -offsetFromMiddle : offsetFromMiddle;
		return wayReference.transform.rotation * new Vector3 (0, offsetFromMiddle, 0);
	}

	private void calculateCollectedEmission (float speedChangeInFrame) {
		if (speedChangeInFrame > 0f) {
			CollectedEmissionAmount += speedChangeInFrame;
//			Debug.Log ("Gas amount: " + CollectedEmissionAmount);
			if (CollectedEmissionAmount > THRESHOLD_EMISSION_PUFF) {
				emitGas();
				CollectedEmissionAmount -= THRESHOLD_EMISSION_PUFF;
			}
		}
	}

	private void emitGas ()
	{
//		Debug.Log ("Emit gas");
		stats [STAT_EMITTEDGAS].add (1f);
		PubSub.publish ("Vehicle:emitGas", this);
//		Debug.Break ();
	}

	public Vector3 getEmitPosition () {
		return transform.position;
	}

	// TODO - Depending on vehicle, return different amounts
	public float getEmissionAmount () {
		return 1f;
	}

	private void blinkUntilClickedAndDestroy(bool fadeDirectionOut = true) {
		if (!destroying) {
//			FadeObjectInOut fadeObject = GetComponent<FadeObjectInOut>();
//			if (fadeDirectionOut) {
//				fadeObject.DoneMessage = "fadeOut";
//				fadeObject.FadeOut (0.2f);
//			} else {
//				fadeObject.DoneMessage = "fadeIn";
//				fadeObject.FadeIn (0.2f);
//			}
		}
	}

	public void fadeOutAndDestroy () {
		destroying = true;
		PubSub.unsubscribe ("Click", this);

		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.turnAllOff ();

        // If camera attached, detatch
        if (isOwningCamera) {
            detachCurrentCamera();
        }

		FadeObjectInOut fadeObject = GetComponent<FadeObjectInOut>();
		fadeObject.DoneMessage = "destroy";
		fadeObject.FadeOut (0.5f);
	}

    void OnDestroy() {
        ExplosionHelper.Remove(this);
        PubSub.unsubscribe ("Click", this);
    }

	public void onFadeMessage (string message) {
		if (message == "destroy") {
			StartCoroutine ("destroyVehicle");
		} else if (message == "fadeOut") {
			blinkUntilClickedAndDestroy (false);
		} else if (message == "fadeIn") {
			blinkUntilClickedAndDestroy (true);
		}
	}

	private IEnumerator destroyVehicle() {
		VehicleCollider[] vehicleColliders = GetComponentsInChildren<VehicleCollider> ();
		foreach (VehicleCollider collider in vehicleColliders) {
			collider.GetComponent<BoxCollider> ().center = new Vector3 (0f, 0f, 1000f);
		}
		yield return null;
		Destroy (this.gameObject);
		if (health > 0f) {
			PubSub.publish ("points:inc", PointCalculator.vehicleDestinationPoints);
			DataCollector.Add ("Vehicles reached goal", 1f);
		} else {
			DataCollector.Add ("Vehicles:destroy", 1f);
		}
		numberOfCars--;
		GenericVehicleSounds.VehicleCountChange();
	}

	public PROPAGATION onMessage (string message, object data) {
		if (!destroying) {
			if (message == "Click") {
				if (health <= 0f) {
					// Get click position (x,y) in a plane of the objects' Z position
					Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
					Vector2 clickPos = Game.instance.screenToWorldPosInPlane((Vector3) data, plane);
					CircleTouch vehicleTouch = new CircleTouch (transform.position, 0.1f * 3f); // Click 0.1 (vehicle length) multiplied by three
					if (vehicleTouch.isInside (clickPos)) {
						fadeOutAndDestroy ();
						PubSub.publish ("Vehicle:removeDangerHalo", this);
						return PROPAGATION.STOP_AFTER_SAME_TYPE;
					}
				} else if (!hasSpeed ()) {
					Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
					Vector2 clickPos = Game.instance.screenToWorldPosInPlane((Vector3) data, plane);
					CircleTouch vehicleTouch = new CircleTouch (transform.position, 0.1f * 1.5f); // Click 0.1 (vehicle length) multiplied by 1.5
					if (vehicleTouch.isInside (clickPos)) {
						performIrritationAction ();
						VehicleSounds vehicleSounds = GetComponent<VehicleSounds> ();
						vehicleSounds.decreaseFrustrationLevelOnManualHonk ();

						return PROPAGATION.STOP_AFTER_SAME_TYPE;
					}
				}
			}
		}
		return PROPAGATION.DEFAULT;
	}

	private static string STAT_DRIVING_TIME = "Vehicles driving time";
	private static string STAT_WAITING_TIME = "Vehicles waiting time";
	private static string STAT_DRIVING_DISTANCE = "Vehicles driving distance";
	private static string STAT_PASSED_CROSSINGS = "Vehicles passed crossings";
	private static string STAT_PASSED_TRAFFICLIGHT = "Vehicles passed traffic lights";
	private static string STAT_EMITTEDGAS = "Vehicle:emission";
	private static string STAT_MINOR_COLLISSIONS = "Vehicle minor collissions";
	private static string STAT_MAJOR_COLLISSIONS = "Vehicle major collissions";
	private static string STAT_TOTAL_COLLISSION_AMOUNT = "Vehicle total collission force";
	private static string STAT_CRASHES = "Vehicle crashes";
	private Dictionary<string, DataCollector.InnerData> stats = new Dictionary<string, DataCollector.InnerData> {
		{STAT_DRIVING_TIME, new DataCollector.InnerData()},
		{STAT_WAITING_TIME, new DataCollector.InnerData()},
		{STAT_DRIVING_DISTANCE, new DataCollector.InnerData()},
		{STAT_PASSED_CROSSINGS, new DataCollector.InnerData()},
		{STAT_PASSED_TRAFFICLIGHT, new DataCollector.InnerData()},
		{STAT_EMITTEDGAS, new DataCollector.InnerData()},
		{STAT_MINOR_COLLISSIONS, new DataCollector.InnerData()},
		{STAT_MAJOR_COLLISSIONS, new DataCollector.InnerData()},
		{STAT_TOTAL_COLLISSION_AMOUNT, new DataCollector.InnerData()},
		{STAT_CRASHES, new DataCollector.InnerData()}
	};
	private IEnumerator reportStats () {
		do {
			yield return new WaitForSeconds (1f);
			foreach (KeyValuePair<string, DataCollector.InnerData> stat in stats) {
				DataCollector.Add (stat.Key, stat.Value);
				stat.Value.reset ();
			}
		} while (this.gameObject != null);
	}

	private void startBacklights() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.setTaillightsState (true);
	}

	private void stopBacklights() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.toggleTaillights (false);
	}

	private void startBreaklights() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.setTaillightsState (false);
	}

	private void stopBreaklights() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.toggleTaillights (false);
	}

	private void flashHeadlights() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.flashHeadlights ();
	}

	private void startWarningBlinkers() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.startWarningBlinkers ();
	}

	private void stopWarningBlinkers() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.stopWarningBlinkers ();
	}

	private void startBlinkersLeft() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.startBlinkersLeft ();
	}

	private void startBlinkersRight() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.startBlinkersRight ();	
	}

	private void stopBlinkers() {
		VehicleLights lights = GetComponentInChildren<VehicleLights> ();
		lights.stopBlinkers ();
	}

    public void turnOnExplodable() {
        Misc.SetGravityState (gameObject, true);
        pauseVehicle();
    }

    private void pauseVehicle() {
        paused = true;
        currentSpeed = 0f;
    }

    // IReroute - for pause, re-routing and resuming
    public void pauseMovement() {
        pauseVehicle();
    }

    public List<Pos> getPath() {
        return currentPath;
    }

    public void setPath(List<Pos> path, bool isDefinite = true) {
        currentPath = path;
        currentPathIsDefinite = isDefinite;
        // TODO - If adding possibilities to re-route with a vehicle looping, below need to change
        wayPointsLoop = false;
    }

    public void resumeMovement() {
        paused = false;
    }

    public bool isRerouteOk() {
        return characteristics == null || characteristics.rerouteOK;
    }
	// IReroute - end

    public void OnGUI () {
		if (Vehicle.debug == this) {
			int y = 200;
			// CurrentSpeed, CurrentPos, CurrentTarget, EndPos
			GUI.Label (new Rect (0, y += 20, 500, 20), "Speed: "+currentSpeed * MAP_SPEED_TO_KPH_FACTOR);
			GUI.Label (new Rect (0, y += 20, 500, 20), "StartPos: " + StartPos.Id + "(" + NodeIndex.endPointIndex[StartPos.Id][0].Id + ")");
			GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentPos: " + CurrentPosition.Id);
			if (CurrentTarget != null) {
				GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentTarget: " + CurrentTarget.Id);
			}
			GUI.Label (new Rect (0, y += 20, 500, 20), "EndPos: " + EndPos.Id + "(" + NodeIndex.endPointIndex[EndPos.Id][0].Id + ")");
		}
	}

	private void setUpcomingTrafficLight() {
        upcomingTrafficLight = null;
        for (int i = 1; i < currentPath.Count; i++) {
            long currTargetId = currentPath[i].Id;
            long prevTargetId = currentPath[i-1].Id;
			if (TrafficLightIndex.TrafficLightsForPos.ContainsKey(currTargetId)) {
				upcomingTrafficLight = TrafficLightIndex.TrafficLightsForPos[currTargetId].Find(trafficLight => trafficLight.getOtherPos().Id == prevTargetId);
                break;
			}
		}
    }
}

