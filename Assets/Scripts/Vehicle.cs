using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Vehicle: MonoBehaviour, FadeInterface, IPubSub, IExplodable, IReroute {

//	[InspectorButton("OnButtonClicked")]
//	public bool debugPrint;
//
//	private void OnButtonClicked()
//	{
//		debugPrint ^= true;
//		if (debugPrint) {
//			createDangerHalo ();
//			DebugFn.square (TargetPoint, 3f);
//			List<Pos> drivePoints = Game.calculateCurrentPath (StartPos, EndPos);
//			WayReference wayReferenceStart = NodeIndex.getWayReference (StartPos.Id, drivePoints[1].Id);
//			WayReference wayReferenceEnd = NodeIndex.getWayReference (drivePoints[drivePoints.Count - 2].Id, EndPos.Id);
//			Debug.Log ("Start: " + wayReferenceStart.Id);
//			Debug.Log ("End: " + wayReferenceEnd.Id);
//			Debug.Log ("Turn state: " + turnState);
//		}
//	}

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

	private Vector3 endVector;
	private Vector3 startVector;
	public Setup.VehicleSetup characteristics = null;

	private WayReference CurrentWayReference { set; get; }
	private float SpeedFactor { set; get; }
	private float Acceleration { set; get; }
	private float StartSpeedFactor { set; get; }
	private float ImpatientThresholdNonTrafficLight { set; get; }
	private float ImpatientThresholdTrafficLight { set; get; }
//	private Vector3 PreviousMovementVector { set; get; }
	private float currentSpeed = 0f;
	private float timeOfLastMovement = 0f;
	private bool isBigTurn = false;

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

//	private const float MaxRotation = 20f;
	private float DesiredRotation { set; get; }
	private float TurnBreakFactor { set; get; }
	private float AwarenessBreakFactor { set; get; }

	private HashSet<Vehicle> FacVehiclesInAwarenessArea { set; get; }
	private HashSet<Vehicle> PcVehiclesInAwarenessArea { set; get; }

	private HashSet<HumanLogic> FacHumansInAwarenessArea { set; get; }
	private HashSet<HumanLogic> PcHumansInAwarenessArea { set; get; }

	private HashSet<TrafficLightLogic> YellowTrafficLightPresence { set; get; }
	private HashSet<TrafficLightLogic> RedTrafficLightPresence { set; get; }

	private Vector3 TargetPoint { set; get; }
	private WayReference TurnToRoad { set; get; }
	private bool isStraightWay { set; get; }
	private bool isCurrentTargetCrossing = false;
	private TurnState turnState = TurnState.NONE;
	private float BezierLength { set; get; }
	private float AccumulatedBezierDistance { set; get; }
	private float backingCounterSeconds = 0f;

	public Camera vehicleCameraObj;
	private static Vehicle debug;
	public static Camera debugCamera = null;
    public bool isOwningCamera = false;
    public bool switchingCameraInProgress = false;

	private Vector3 vehicleMovement;
	public int vehicleId;

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

	private enum TurnState {
		NONE,
		FAC,
		PC,
		CAR, 
		BC
	}

	// TODO - Carefulness (drunk level, tired, age...)
	// Use this for initialization
	void Start () {
		//debugPrint = true;
		initInformationVehicle ();
		initVehicleProfile ();
		updateCurrentTarget ();

		// Set start speed for car
		// TODO - In the future, if coming from parking, start from 0
		float wayTargetSpeedKmH = CurrentWayReference.way.WayWidthFactor * MAP_SPEED_TO_KPH_FACTOR; 	// Eg 51 km/h
		// Car target speed
		float vehicleTargetSpeedKmH = wayTargetSpeedKmH * SpeedFactor;				// Eg 10% faster = 56.5 km/h
		// Car starting speed
		currentSpeed = StartSpeedFactor * vehicleTargetSpeedKmH / KPH_TO_LONGLAT_SPEED;			

		numberOfCars++;
		vehicleId = vehicleInstanceCount++;
		DataCollector.Add ("Total # of vehicles", 1f);

		// Report one more car
		GenericVehicleSounds.VehicleCountChange();

//		Transform car = transform.FindChild ("CarObject");
//		Renderer r = car.GetComponent<Renderer>();
//		Material m = r.material;
//		Color c = m.color;
//		c.a = 0.5f;
//		m.color = c;

//		transform.rotation = Quaternion.Euler(0, 0, 97.97565f);

//		float currentYOffset = getCenterYOfField (CurrentWayReference, CurrentPosition);
		Vector3 offset = getCenterYOfField (CurrentWayReference, CurrentPosition);
//		Debug.Log (offset.x + ", " + offset.y);
		transform.position = new Vector3 (transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z);
		vehicleMovement = transform.rotation * Vector3.right;

		StartCoroutine (reportStats ());

		PubSub.subscribe ("Click", this, 100);
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
				if (TurnToRoad != null && CurrentWayReference != null) {

					// Way target speed
					float wayTargetSpeedKmH = CurrentWayReference.way.WayWidthFactor * MAP_SPEED_TO_KPH_FACTOR; 	// Eg 51 km/h
					// Car target speed
					float vehicleTargetSpeedKmH = wayTargetSpeedKmH * SpeedFactor;									// Eg 10% faster = 56.5 km/h

					// Current car speed
					float currentSpeedKmH = currentSpeed * KPH_TO_LONGLAT_SPEED;

					// Lowest break factor (decides how fast the car currently want to go)
					float breakFactor = Mathf.Min (TurnBreakFactor, AwarenessBreakFactor);

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

					//			if (currentSpeed > 0f) {
					//				Debug.Log ("Current Speed: " + currentSpeedKmH);
					//				Debug.Log ("Target Speed: " + vehicleTargetSpeedAfterBreakFactorKmH);
					//				Debug.Log ("Speed Change: " + speedChangeKmh);
					//			}

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
						float metersDriven = Mathf.Abs (Misc.kmhToMps (currentSpeed * KPH_TO_LONGLAT_SPEED * Time.deltaTime));
						stats [STAT_DRIVING_DISTANCE].add (metersDriven);
						totalDrivingDistance += metersDriven;
					}

					//			// TODO - Try to make this better
					//			// The vehicles desired speed per second on this specific road
					//			float wayTargetSpeed = CurrentWayReference.way.WayWidthFactor * Settings.playbackSpeed;
					//			float breakFactor = Mathf.Min (TurnBreakFactor, AwarenessBreakFactor);
					//			float vehicleTargetSpeed = (wayTargetSpeed * SpeedFactor * breakFactor / 2) / 10f;
					//			// Calculated movement for current frame
					//			float currentAcceleration = (vehicleTargetSpeed - currentSpeed) / vehicleTargetSpeed * Acceleration;
					//			// Adjust with speedfactor
					//			currentAcceleration /= Settings.speedFactor;
					//			float speedChangeInFrame = currentAcceleration * Time.deltaTime;
					//			currentSpeed += speedChangeInFrame;

					calculateCollectedEmission (speedChangeInFrame);

					adjustColliders ();

					//			Debug.Log ("Current speed: " + currentSpeed + ", Vehicle target speed: " + vehicleTargetSpeed + ", Acceleration: " + currentAcceleration);

					Vector3 currentPos = new Vector3 (transform.position.x, transform.position.y, 0f);
					Vector3 intersection = Vector3.zero;
					//			Vector3 toTarget;

					// We have a target point that we want to move towards - check if we intersect the target point (which means we need to turn)
					Vector3 wayDirection = TurnToRoad.gameObject.transform.rotation * Vector3.right;

					Vector3 currentWayDirection = CurrentWayReference.gameObject.transform.rotation * (CurrentWayReference.isNode1 (CurrentTarget) ? Vector3.left : Vector3.right);
					Vector3 appropriateMovementVector = isStraightWay ? currentWayDirection : vehicleMovement;

					bool intersects = Math3d.LineLineIntersection (out intersection, currentPos, appropriateMovementVector, TargetPoint, wayDirection);

					//			Debug.DrawLine (transform.position, transform.position + appropriateMovementVector, Color.black, float.MaxValue);

					if (BezierLength == 0f) {
						BezierLength = Math3d.GetBezierLength (currentPos, intersects ? intersection : TargetPoint, TargetPoint);
						//				Debug.Log ("Bezier length: " + BezierLength);
						AccumulatedBezierDistance = 0f;
					}
					//			float time = turnState == TurnState.NONE ? 0.5f : TurnToRoad.SmallWay ? 1.0f : 0.1f;
					//			float time = turnState == TurnState.NONE ? 0.5f : TurnToRoad.SmallWay ? 1.0f : (Mathf.Max (Mathf.Min(1f, AccumulatedBezierDistance / BezierLength), 0.05f));
					float time = TurnToRoad.SmallWay && isStraightWay ? 1.0f : Mathf.Max (Mathf.Min (1f, AccumulatedBezierDistance / BezierLength), 0.05f);
					//			Debug.Log ("Time: " + time);
					Vector3 currentTargetPoint = Math3d.GetVectorInBezierAtTime (time, currentPos, intersects ? intersection : TargetPoint, TargetPoint);

					//			Vector3 prev = Vector3.zero;
					//			for (float t = 0.0f; t <= 1.0f; t+= TurnToRoad.SmallWay && isStraightWay ? 1.0f : 0.05f) {
					//				Vector3 curr = Math3d.GetVectorInBezierAtTime(t, currentPos, intersects ? intersection : TargetPoint, TargetPoint);
					//				if (prev != Vector3.zero) {
					////					Debug.DrawLine (prev, curr, Color.yellow, float.MaxValue); // Forever
					//					Debug.DrawLine (prev, curr, Color.yellow, 10f);
					//				}
					//				prev = curr;
					//			}

					Vector3 positionMovementVector = currentTargetPoint - currentPos;
					if (positionMovementVector.magnitude > 0.0001f && breakFactor >= 0f) {
						Quaternion vehicleRotation = Quaternion.FromToRotation (Vector3.right, positionMovementVector);
						//				float currentRotationDegrees = Mathf.Abs(vehicleRotation.eulerAngles.z - transform.rotation.eulerAngles.z);
						//				if (i > 1 && currentRotationDegrees > 90f) {
						////					Debug.Log ("Move forward");
						//					MoveTargetPointForward ();
						////					Update ();
						//					return;
						//				} else {
						//					Debug.Log ("Rotation: " + currentRotationDegrees);
						transform.rotation = vehicleRotation;
						//				}
						//			} else {
						//				Debug.Log (positionMovementVector.magnitude);
					}

					//			float movementPct = (currentSpeed / Mathf.Max(positionMovementVector.magnitude, 0.001f)) * Settings.wayLengthFactor;
					float movementPct = (currentSpeed / positionMovementVector.magnitude) * Settings.wayLengthFactor;
					Vector3 movementVector = positionMovementVector * movementPct;
					//			Debug.Log (BezierLength / positionMovementVector.magnitude);
					if (TurnToRoad.SmallWay && positionMovementVector.magnitude < 0.05f && positionMovementVector.magnitude < BezierLength / 40f) {
						// TODO - Try to get rid of SmallWays. Remove the connections to footways and merge with "non-intersecting" way 
						//				Debug.Log (movementVector);
						//				Debug.Log (positionMovementVector);
						//				Debug.Log (positionMovementVector.magnitude);
						//				Debug.Log (movementPct);
						//				Debug.Log (currentSpeed);
						//				Debug.Log (positionMovementVector.magnitude);

						// Panic mode, switch to next target
						//				Debug.Log ("Small way and very small movement vector, move to next road");
						//				if (turnState != TurnState.NONE) {
						//					CurrentPosition = CurrentTarget;
						//					updateCurrentTarget ();
						//				}
						CurrentPosition = CurrentTarget;
						updateCurrentTarget ();
						Update ();
						return;
					}
					Vector3 positionMovement = new Vector3 (movementVector.x, movementVector.y, 0);
					//			if (float.IsNaN(positionMovement.x) || float.IsInfinity(positionMovement.x)) {
					//				positionMovement = Vector3.zero;
					//			}
					//			Debug.Log (positionMovement);
					transform.position += positionMovement;
					AccumulatedBezierDistance += positionMovement.magnitude;

					//			toTarget = TargetPoint - transform.position;
					//			toTarget.z = 0;
					//			if (PreviousMovementVector != Vector3.zero && Vector3.Angle (toTarget, PreviousMovementVector) > 150f) {
					//				CurrentPosition = CurrentTarget;
					//				updateCurrentTarget ();
					////				Quaternion rotation;
					////				if (CurrentWayReference.isNode1(CurrentPosition)) {
					////					rotation = CurrentWayReference.transform.rotation;
					////				} else {
					////					rotation = Quaternion.Euler(0, 0, 180f) * CurrentWayReference.transform.rotation;
					////				}
					////				transform.rotation = rotation;
					//				PreviousMovementVector = Vector3.zero;
					//			} else {
					//				PreviousMovementVector = toTarget;
					//			}
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

//				if (debugPrint) {
//					DebugFn.square (TargetPoint, 0.0f);
//					//			Debug.Log ("Turn state: " + turnState);
//				}
			}
		}
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

	private const float a = 1f;
	private const float b = -0.01197f;
	private const float c = 3.65e-5f;
//	private const float d = 2.74e-7f;
	private float getTurnBreakFactorForDegrees (float x)
	{
		// TODO - Make break factor working smoothly
//		return - (b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3));
//		return a + b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3);
		return a + b * x + c * Mathf.Pow(x, 2);
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

		TurnBreakFactor = 1.0f;
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
				WayCollisionObj wayCollisionObj = rawCollisionObj.typeName == WayCollisionObj.NAME ? (WayCollisionObj)rawCollisionObj : null;
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				TrafficLightCollisionObj trafficLightCollisionObj = rawCollisionObj.typeName == TrafficLightCollisionObj.NAME ? (TrafficLightCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				// If we're turning and our Panic Collider have left the target collider
				if (colliderName == "PC" && turnState != TurnState.NONE) {
					// If the collisionObj is our current TurnToRoad and the collider we're leaving is the target
					if (wayCollisionObj != null && wayCollisionObj.WayReference == TurnToRoad && wayCollisionObj.Pos == CurrentTarget) {
						// Make sure the vehicle rotation is somewhat similar to the target way rotation
						float acceptableAngleDiff = 45f;
						float vehicleAngle = transform.rotation.eulerAngles.z;
						float wayAngle = TurnToRoad.transform.rotation.eulerAngles.z;
						if (CurrentTarget != null && !TurnToRoad.isNode1 (CurrentTarget)) {
							wayAngle = (wayAngle + 180) % 360;
							//					Debug.Log ("180");
						}
						//				Debug.Log ("Vehicle: " + vehicleAngle);
						//				Debug.Log ("Way: " + wayAngle);
						if (Misc.isAngleAccepted (vehicleAngle, wayAngle, acceptableAngleDiff)) {
							CurrentPosition = CurrentTarget;
							updateCurrentTarget ();
						}
					}
				} else if (TurnToRoad != null && TurnToRoad.SmallWay && colliderName == "BC" && turnState != TurnState.NONE) {
					if (wayCollisionObj != null && wayCollisionObj.WayReference == TurnToRoad && wayCollisionObj.Pos == TurnToRoad.getOtherNode (CurrentTarget)) {
						CurrentPosition = wayCollisionObj.Pos;
						updateCurrentTarget ();
					}
				} else if (colliderName == "BC" && turnState != TurnState.NONE) {
					if (wayCollisionObj != null && wayCollisionObj.WayReference == TurnToRoad && wayCollisionObj.Pos == CurrentTarget) {
						// Make sure the vehicle rotation is somewhat similar to the target way rotation
						float acceptableAngleDiff = 45f;
						float vehicleAngle = transform.rotation.eulerAngles.z;
						float wayAngle = TurnToRoad.transform.rotation.eulerAngles.z;
						if (!TurnToRoad.isNode1 (CurrentTarget)) {
							wayAngle = (wayAngle + 180) % 360;
							//					Debug.Log ("180");
						}
						//				Debug.Log ("Vehicle: " + vehicleAngle);
						//				Debug.Log ("Way: " + wayAngle);
						if (Misc.isAngleAccepted (vehicleAngle, wayAngle, acceptableAngleDiff)) {
							CurrentPosition = CurrentTarget;
							updateCurrentTarget ();
						}
					}
				}

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
		// * FAC with WayReference "intersection" collider	- Start slowing car down (if intersection - a bit more than if straight way depending how much the way turns)
		// * PC with WayReference "intersection" collider 	- Slow car down a bit harder (as above)
		// * CAR with WayReference "intersection" collider	- Should have stopped by now, initiate turn if intersection, wait for greenlight, proceed to CurrentTargetPos if straight, drive off map if endpoint, park if garage...
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
				WayCollisionObj wayCollisionObj = rawCollisionObj.typeName == WayCollisionObj.NAME ? (WayCollisionObj)rawCollisionObj : null;
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				TrafficLightCollisionObj trafficLightCollisionObj = rawCollisionObj.typeName == TrafficLightCollisionObj.NAME ? (TrafficLightCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				// Logic for upcoming wayreference end node collission
				if (wayCollisionObj != null && wayCollisionObj.WayReference == CurrentWayReference && wayCollisionObj.Pos == CurrentTarget) {
					// We know that this is the currentTarget - we want to know our options
					List<WayReference> possibilities = NodeIndex.nodeWayIndex [CurrentTarget.Id].Where (p => p != CurrentWayReference && p.way.WayWidthFactor >= WayHelper.MINIMUM_DRIVE_WAY).ToList ();
					if (colliderName == "FAC") {
						turnState = TurnState.FAC;
					} else if (colliderName == "PC") {
						turnState = TurnState.PC;
					} else if (colliderName == "CAR") {
						turnState = TurnState.CAR;
					} else if (colliderName == "BC") {
						turnState = TurnState.BC;
					}

					if (possibilities.Count == 1 && !isBigTurn) {
						if (turnState != TurnState.BC) {
							float desiredRotation = Quaternion.Angle (CurrentWayReference.transform.rotation, TurnToRoad.transform.rotation);
							bool areBothSameDirection = CurrentWayReference.isNode1 (CurrentTarget) != TurnToRoad.isNode1 (CurrentTarget);
							if (!areBothSameDirection) {
								desiredRotation = 180f - desiredRotation;
							}
							TurnBreakFactor = getTurnBreakFactorForDegrees (Mathf.Abs (desiredRotation));
						}
					} else if (possibilities.Count > 1 || isBigTurn) {
						Pos nextTarget = currentPath [2];
						if (turnState != TurnState.BC) {
							WayReference otherWayReference = NodeIndex.getWayReference (CurrentTarget.Id, nextTarget.Id);
							float desiredRotation = Quaternion.Angle (CurrentWayReference.transform.rotation, otherWayReference.transform.rotation);
							bool areBothSameDirection = CurrentWayReference.isNode1 (CurrentTarget) != otherWayReference.isNode1 (CurrentTarget);

							float currentWayAngle = CurrentWayReference.transform.rotation.eulerAngles.z;
							float realWayAngle;
							if (!areBothSameDirection) {
								desiredRotation = 180f - desiredRotation;
								realWayAngle = otherWayReference.transform.rotation.eulerAngles.z - 180f - currentWayAngle;
							} else {
								realWayAngle = otherWayReference.transform.rotation.eulerAngles.z - currentWayAngle;
							}

							TurnBreakFactor = getTurnBreakFactorForDegrees (Mathf.Abs (desiredRotation));
							//					Debug.Log ("breakFactor: " + TurnBreakFactor + ", for degrees: " + desiredRotation); 
							if (Mathf.Abs (desiredRotation) >= 45f) {
								if (realWayAngle < 0f) {
									realWayAngle = realWayAngle + 360f;
								}
								//							Debug.Log (realWayAngle);
								if (realWayAngle > 0f && realWayAngle <= 180f) {
									startBlinkersLeft ();
								} else {
									startBlinkersRight ();
								}
							}
						}
						if (turnState == TurnState.CAR || turnState == TurnState.BC) {
							TurnToRoad = NodeIndex.getWayReference (CurrentTarget.Id, nextTarget.Id);
							BezierLength = 0f;
							TargetPoint = getTargetPoint (TurnToRoad, null, true);
							isStraightWay = false;
							vehicleMovement = transform.rotation * Vector3.right;
							statReportPossibleCrossing ();
						}
					} else {
						// "Disappear" on endpoint
						if (turnState == TurnState.CAR || turnState == TurnState.BC) {
							// Endpoint
							currentSpeed = 0;
							Acceleration = 0;
							fadeOutAndDestroy ();
						}
					}
				}

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
//		Debug.Log ("Added to " + colliderName + ", length: " + vehiclesInAwarenessArea.Count);
	}

	private void removeVehicleInAwarenessArea (string colliderName, Vehicle otherVehicle) {
		HashSet<Vehicle> vehiclesInAwarenessArea = colliderName == "FAC" ? FacVehiclesInAwarenessArea : PcVehiclesInAwarenessArea;
		vehiclesInAwarenessArea.Remove (otherVehicle);
//		Debug.Log ("Removed from " + colliderName + ", length: " + vehiclesInAwarenessArea.Count);
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
		HashSet<TrafficLightLogic> trafficLightPresence = colliderName == CollisionObj.TRAFFIC_LIGHT_YELLOW ? YellowTrafficLightPresence : RedTrafficLightPresence;
		trafficLightPresence.Add (trafficLightLogic);
//		Debug.Log ("Added to " + colliderName + ", length: " + trafficLightPresence.Count);
	}

	private void removeTrafficLightPresence (string colliderName, TrafficLightLogic trafficLightLogic) {
		HashSet<TrafficLightLogic> trafficLightPresence = colliderName == CollisionObj.TRAFFIC_LIGHT_YELLOW ? YellowTrafficLightPresence : RedTrafficLightPresence;
		trafficLightPresence.Remove (trafficLightLogic);
//		Debug.Log ("Removed from " + colliderName + ", length: " + trafficLightPresence.Count);
	}

	private bool onlyHumansInPanicCollider () {
		return !PcVehiclesInAwarenessArea.Any () && !RedTrafficLightPresence.Any () && PcHumansInAwarenessArea.Any ();
	}

	private Vector3 getTargetPoint (WayReference turnToRoad, Pos endNode = null, bool furtherIn = false)
	{
		Quaternion turnToRoadQuaternion = turnToRoad.gameObject.transform.rotation;
		float halfWayWidth = turnToRoad.gameObject.transform.localScale.y / (furtherIn ? 0.75f : 1.5f);
		bool isNode1 = endNode == null ? turnToRoad.isNode1 (CurrentTarget) : !turnToRoad.isNode1(endNode);

		Vector3 offset = getCenterYOfField (turnToRoad, endNode == null ? CurrentTarget : turnToRoad.getOtherNode (endNode));
		return endVector + turnToRoadQuaternion * new Vector3((isNode1 ? halfWayWidth : -halfWayWidth), 0, 0) + offset;
	}

	private void MoveTargetPointForward ()
	{
//		Vector3 oneCarLengthMovement = transform.rotation * new Vector3(transform.localScale.x, 0f, 0f);
		Vector3 oneCarLengthMovement = (TurnToRoad.transform.rotation * (TurnToRoad.isNode1(CurrentTarget) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 0f, 180f))) * new Vector3(transform.localScale.x, 0f, 0f);
//		Debug.Log ("Old: " + TargetPoint);
		Vector3 newTarget = TargetPoint + oneCarLengthMovement;
//		Debug.Log ("newTarget: " + newTarget);
		TargetPoint = newTarget;
//		Debug.Log ("New: " + TargetPoint);
	}

	private CollisionObj getColliderType (Collider col)
	{
		GameObject colliderGameObject = col.gameObject;
		if (colliderGameObject.GetComponent<WayReference> () != null) {
			// Way reference
			int colliderIndex = colliderGameObject.GetComponents<Collider> ().ToList ().IndexOf (col);
			bool isNode1 = colliderIndex % 2 == 0;
			WayReference wayReference = colliderGameObject.GetComponent<WayReference> ();
			return new WayCollisionObj (wayReference, CollisionObj.WAY_COLLIDER, isNode1 ? wayReference.node1 : wayReference.node2);
		} else if (colliderGameObject.GetComponentInParent<Vehicle> () != null) {
			// Car
			return new VehicleCollisionObj (colliderGameObject.GetComponentInParent<Vehicle> (), colliderGameObject.name);
		} else if (colliderGameObject.GetComponentInParent<TrafficLightLogic> () != null) {
			// Traffic Light
			return new TrafficLightCollisionObj (colliderGameObject.GetComponentInParent<TrafficLightLogic> (), colliderGameObject.name);
		} else if (colliderGameObject.GetComponentInParent<HumanLogic> () != null) {
			// Human (only "BODY" is interesting)
			string name = HumanCollider.colliderNamesForGameObjectName[colliderGameObject.name];
			if (name == "BODY") {
				return new HumanCollisionObj (colliderGameObject.GetComponentInParent<HumanLogic> (), name);
			}
		}
		return null;
	}

	public void updateCurrentTarget () {
		if (CurrentTarget != null && TrafficLightIndex.TrafficLightsForPos.ContainsKey(CurrentTarget.Id)) {
			stats[STAT_PASSED_TRAFFICLIGHT].add(1f);
		}

        if (wayPointsLoop && CurrentTarget == wayPoints[0]) {
            // We have reached our first wayPoint and should loop, place first waypoint last and recalculate route
            wayPoints.RemoveAt(0);
            wayPoints.Add(CurrentTarget);
            currentPath = Game.calculateCurrentPaths (CurrentTarget, EndPos, PreviousTarget, wayPoints, true, false);
        }

		isBigTurn = false;
		TurnBreakFactor = 1.0f;
//		Time.timeScale = TurnBreakFactor;
		turnState = TurnState.NONE;
		stopBlinkers ();

		statReportPossibleCrossing ();
		if (currentPathIsDefinite) {
            while (currentPath[0] != CurrentPosition) {
                currentPath.RemoveAt(0);
            }
        } else {
            // Calculate path once, set it as definite to not re-calculate at next crossing
            currentPath = Game.calculateCurrentPaths (CurrentPosition, EndPos, null, wayPoints, true);
            currentPathIsDefinite = true;
        }
		if (currentPath.Count > 1) {
            PreviousTarget = CurrentTarget;
			CurrentTarget = currentPath [1];
			CurrentWayReference = NodeIndex.getWayReference(CurrentPosition.Id, CurrentTarget.Id);
			// TODO - Can remove?
			endVector = Game.getCameraPosition (CurrentTarget);
			startVector = Game.getCameraPosition (CurrentPosition);

			List<WayReference> possitilities = NodeIndex.nodeWayIndex [CurrentTarget.Id].Where (p => p != CurrentWayReference && p.way.WayWidthFactor >= WayHelper.MINIMUM_DRIVE_WAY).ToList ();
			if (possitilities.Count == 1) {
				if (TurnToRoad == null || Misc.isAngleAccepted (gameObject.transform.rotation.eulerAngles.z, possitilities [0].gameObject.transform.rotation.eulerAngles.z, 45f, 180f)) {
					TurnToRoad = possitilities [0];
					BezierLength = 0f;
					TargetPoint = getTargetPoint (TurnToRoad);
					isStraightWay = true;
				} else {
					isBigTurn = true;
					TurnToRoad = CurrentWayReference;
					BezierLength = 0f;
					TargetPoint = getTargetPoint(CurrentWayReference, CurrentTarget);
					isStraightWay = true;
				}
			} else {
				TurnToRoad = CurrentWayReference;
				BezierLength = 0f;
				TargetPoint = getTargetPoint(CurrentWayReference, CurrentTarget);
				isStraightWay = true;
				isCurrentTargetCrossing = true;
			}
		} else {
            PreviousTarget = null;
			CurrentTarget = null;
			CurrentWayReference = null;

			// TODO - We've reached our destination - drive off map and despawn
			TurnToRoad = null;
			BezierLength = 0f;
			TargetPoint = Vector3.zero;
			isStraightWay = true;
		}
		vehicleMovement = transform.rotation * Vector3.right;
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
			TurnToRoad = null;
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

	void statReportPossibleCrossing () {
		if (isCurrentTargetCrossing) {
			stats [STAT_PASSED_CROSSINGS].add (1f);
			isCurrentTargetCrossing = false;
		}
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
    }

    // IReroute - for pause, re-routing and resuming
    public void pauseMovement() {
        paused = true;
        currentSpeed = 0f;
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
			// TurnBreakFactor, CurrentSpeed, CurrentPos, CurrentTarget, EndPos
			GUI.Label (new Rect (0, y += 20, 500, 20), "Speed: "+currentSpeed * MAP_SPEED_TO_KPH_FACTOR);
			GUI.Label (new Rect (0, y += 20, 500, 20), "TurnBreakFactor: "+TurnBreakFactor);
			GUI.Label (new Rect (0, y += 20, 500, 20), "StartPos: " + StartPos.Id + "(" + NodeIndex.endPointIndex[StartPos.Id][0].Id + ")");
			GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentPos: " + CurrentPosition.Id);
			if (CurrentTarget != null) {
				GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentTarget: " + CurrentTarget.Id);
			}
			GUI.Label (new Rect (0, y += 20, 500, 20), "EndPos: " + EndPos.Id + "(" + NodeIndex.endPointIndex[EndPos.Id][0].Id + ")");
		}
	}
}

