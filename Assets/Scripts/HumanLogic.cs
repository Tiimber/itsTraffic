using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HumanLogic : MonoBehaviour, FadeInterface, IPubSub {

	public static System.Random HumanRNG = new System.Random ((int)Game.randomSeed);
	private static Vector3 INVALID_POINT = new Vector3(0f, 0f, float.MinValue);
	private static Vector3 BODY_WIDTH = Vector3.right * 0.022f;

	private const float TARGET_WALKING_SPEED_KMH = 4.5f;
	private const float KPH_TO_LONGLAT_SPEED = 100f;

	private List<Pos> path;
	private List<Vector3> walkPath;
	private Vector3 deviationTarget = INVALID_POINT;
	private TimedDeviationTarget timedDeviationTarget = null;

	private Dictionary<Vehicle, string> vehiclesInVision = new Dictionary<Vehicle, string> ();
	private HashSet<HumanLogic> waitForHumans = new HashSet<HumanLogic> ();

	public float totalWalkingDistance;

	private float speedFactor;
	private float waitTime = 0f;
	private bool destroying = false;
	public int humanId;
	public Setup.PersonSetup personality = null;

	public static int numberOfHumans = 0;
	public static int humanInstanceCount = 0;

	// TODO - This is for debugging (press T to increase, Shift+T to reset)
	public static float TURBO = 1f;

	public void setPersonality (Setup.PersonSetup personality) {
		this.personality = personality;
	}

	// Use this for initialization
	void Start () {
		numberOfHumans++;
		humanId = humanInstanceCount++;
		DataCollector.Add ("Total # of people", 1f);

		GenericHumanSounds.HumanCountChange ();
		initHumanProfile ();

		StartCoroutine (reportStats ());
	}

	private void initHumanProfile () {
		if (personality != null && personality.speedFactor != 0f) {
			speedFactor = personality.speedFactor;
		} else {
			float minSpeedFactor = 0.8f;
			float maxSpeedFactor = 1.2f;

			speedFactor = Misc.randomRange (minSpeedFactor, maxSpeedFactor);
		}
	}

	// Update is called once per frame
	void Update () {
		if (destroying) {
			return;
		}
			
		if (Game.isMovementEnabled ()) {
			if (waitTime > 0f) {
				waitTime -= Time.deltaTime;
				stats [STAT_WAITING_TIME].add (Time.deltaTime);
			} else if (waitForHumans.Any ()) {
				// Do nothing
				stats [STAT_WAITING_TIME].add (Time.deltaTime);
			} else {
				if (!vehiclesInVision.Any () || (deviationTarget != INVALID_POINT && vehiclesInVision.Any ())) {
					// Go to deviation target if set, otherwise according to walkPath
					Vector3 targetPoint = getTargetPoint ();
					Vector3 currentPoint = transform.position;
					rotateHuman (targetPoint, currentPoint);

					float travelDistance = Misc.getDistance (currentPoint, targetPoint);

					float targetSpeedKmH = TARGET_WALKING_SPEED_KMH * speedFactor;
					// TODO - Debug only - TURBO
					targetSpeedKmH *= HumanLogic.TURBO;
					float travelLengthThisFrame = (targetSpeedKmH * Time.deltaTime) / KPH_TO_LONGLAT_SPEED;

					if (travelLengthThisFrame >= travelDistance) {
						transform.position = targetPoint;
						if (deviationTarget != INVALID_POINT) {
							deviationTarget = INVALID_POINT;
						} else if (timedDeviationTarget != null) {
							timedDeviationTarget = null;
						} else {
							walkPath.RemoveAt (0);
							if (walkPath.Count == 0) {
								fadeOutAndDestroy ();
							}
						}
					} else {
						Vector3 movement = transform.rotation * (Vector3.right * travelLengthThisFrame);
						transform.position = transform.position + movement;
					}

					float metersWalked = Mathf.Abs (Misc.kmhToMps (targetSpeedKmH) * Time.deltaTime);
					totalWalkingDistance += metersWalked;

					stats [STAT_WALKING_TIME].add (Time.deltaTime);
					stats [STAT_WALKING_DISTANCE].add (metersWalked);
				}
			}
		}
	}

	private Vector3 getTargetPoint () {
		if (deviationTarget != INVALID_POINT) {
			return deviationTarget;
		} else if (timedDeviationTarget != null) {
			timedDeviationTarget.time -= Time.deltaTime;
			Vector3 point = timedDeviationTarget.point;
			if (timedDeviationTarget.time <= 0f) {
				timedDeviationTarget = null;
			}
			return point;
		}
		return walkPath [0];
	}

	private void positionHuman (Vector3 pos) {
		if (personality != null && personality.startVector != null) {
			transform.position = Misc.parseVector (personality.startVector) + new Vector3 (0, 0, pos.z);
		} else {
			transform.position = pos;
		}
	}

	private void rotateHuman (Vector3 target, Vector3 current) {
		Quaternion humanRotation = Quaternion.FromToRotation (Vector3.right, target - current);
		transform.rotation = humanRotation;
	}

	public void fadeOutAndDestroy () {
		destroying = true;
		FadeObjectInOut fadeObject = GetComponent<FadeObjectInOut>();
		fadeObject.DoneMessage = "destroy";
		removeAllVehiclesInVision ();
		fadeObject.FadeOut (0.5f);
	}

	public void onFadeMessage (string message) {
		if (message == "destroy") {
			StartCoroutine ("humanReachedGoal");
		}
	}

	private IEnumerator humanReachedGoal() {
		HumanCollider[] humanColliders = GetComponentsInChildren<HumanCollider> ();
		foreach (HumanCollider collider in humanColliders) {
			collider.GetComponent<BoxCollider> ().center = new Vector3 (0f, 0f, 1000f);
		}

		yield return null;

		Destroy (this.gameObject);
//		if (health > 0f) {
//			// TODO - Calculate points based on time, distance, or whatever...
			PubSub.publish ("points:inc", 50);
			DataCollector.Add ("Humans reached goal", 1f);
//		}
		numberOfHumans--;
		GenericHumanSounds.HumanCountChange ();
	}

		
	public void setStartAndEndInfo (Tuple3<Pos, WayReference, Vector3> startInfo, Tuple3<Pos, WayReference, Vector3> endInfo) {
		path = Game.calculateCurrentPath (startInfo.First, endInfo.First, false);
		walkPath = Misc.posToVector3 (path);

		if (path.Count == 1) {
			Destroy (gameObject);
			return;
		}
		// Rotate human...
		Pos pos1 = path [0];
		Pos pos2 = path [1];

		Pos lastPos = path [path.Count - 1];
		Pos secondToLastPos = path [path.Count - 2];

		WayReference startWay = startInfo.Second;
		if (personality != null && personality.startVector != null || startWay.hasNodes (pos1, pos2)) {
			walkPath.RemoveAt (0);
			path.RemoveAt (0);
		}
		walkPath.Insert (0, startInfo.Third);
		path.Insert (0, Game.createTmpPos (startInfo.Third));

		WayReference endWay = endInfo.Second;
		if (endWay.hasNodes (secondToLastPos, lastPos)) {
			walkPath.RemoveAt (walkPath.Count - 1);
			path.RemoveAt (path.Count - 1);
		}
		walkPath.Add (endInfo.Third);
		path.Add (Game.createTmpPos(endInfo.Third));

		// Adjust position to side of bigger ways
		adjustPositionsOnBiggerWays(path, walkPath, startWay, endWay);

		DebugFn.DebugPath (walkPath);

		Vector3 vec1 = walkPath [0];
		Vector3 vec2 = walkPath [1];

		positionHuman (vec1);
		rotateHuman (vec2, vec1);

		walkPath.RemoveAt (0);
	}

	private void adjustPositionsOnBiggerWays (List<Pos> path, List<Vector3> walkPath, WayReference startWay, WayReference endWay) {
		WayReference currentWayReference = startWay;
		for (int i = 0; i < path.Count; i++) {
			Pos currentPos = path [i];
			if (i == path.Count - 1) {
				currentWayReference = endWay;
			} else if (i > 0) {
				Pos previousPos = path [i-1];
				if (currentPos.Id != -1L && previousPos.Id != -1L) {
					currentWayReference = NodeIndex.getWayReference (currentPos.Id, previousPos.Id);
				}
			}

			// We now have the wayReference that our point should be offset on, if it's a way where cars normally drive
//			if (currentWayReference.way.WayWidthFactor >= WayHelper.MINIMUM_DRIVE_WAY) {
				Vector3 point = walkPath [i];

				// TODO - Position on correct side of way
				float offsetWayWidth = +(currentWayReference.transform.localScale.y / 2f) - 0.05f;
				Vector3 humanOffsetOnWay = currentWayReference.transform.rotation * new Vector3(0f, offsetWayWidth, 0f);
				walkPath[i] = new Vector3(point.x + humanOffsetOnWay.x, point.y + humanOffsetOnWay.y, point.z);
//			}
		}
	}

	private void waitForAWhile() {
		waitTime = 0.3f;
	}

	public void reportCollision (Collider col, string colliderName) {
		// ColliderName = "BODY" or "VISION"
		if (!destroying) {
			CollisionObj rawCollisionObj = getColliderType (col);
			if (rawCollisionObj != null) {
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				if (vehicleCollisionObj != null) {
					setVehicleInVision (vehicleCollisionObj);
				} else if (humanCollisionObj != null) {
					HumanLogic otherHuman = humanCollisionObj.Human;
					bool shouldDecide = humanId < otherHuman.humanId;
					if (colliderName == "VISION") {
						// Scenarios:
						// Meeting in intersection, moving towards same point
						// One walking faster, moving in same direction (towards same point)
						// Meeting, not moving towards same point
						// One or both have already deviated from target, meeting on the way to temporary point
						float angle = Quaternion.Angle (transform.rotation, otherHuman.transform.rotation);
						if (shouldDecide || angle < 20f) {
							if (angle >= 20f && angle <= 160f) {
								// Walking with an angle towards each other - one should wait
								otherHuman.waitForAWhile ();
							} else if (angle < 20f) {
								// Walking alongside, they divide their speed, the one ahead goes a tiny bit quicker
								float totalWalkingSpeed = speedFactor + otherHuman.speedFactor;
								speedFactor = totalWalkingSpeed * 0.45f;
								otherHuman.speedFactor = totalWalkingSpeed * 0.55f;
							} else {
								// Walking towards each other, decide one deviation point for each
								Vector3 otherPosition = otherHuman.transform.position;
								Quaternion otherRotation = otherHuman.transform.rotation;

								Vector3 otherHumanToMe = transform.position - otherPosition;
								Vector3 otherPersonMovement = otherRotation * Vector3.right;
								bool isToTheRight = Vector3.Cross (otherPersonMovement, otherHumanToMe).z < 0;

								Vector3 meetingPosition = transform.position + (otherPosition - transform.position) / 2f;

								deviationTarget = meetingPosition + Quaternion.Euler (0f, 0f, transform.rotation.eulerAngles.z + (isToTheRight ? 90f : -90f)) * BODY_WIDTH;
								timedDeviationTarget = null;
								otherHuman.deviationTarget = meetingPosition + Quaternion.Euler (0f, 0f, otherRotation.eulerAngles.z + (isToTheRight ? 90f : -90f)) * BODY_WIDTH;
								otherHuman.timedDeviationTarget = null;
							}
						}
					} else if (colliderName == "BODY") {
						if (walkPath.Count > 0 && otherHuman.walkPath.Count > 0) {
							float ourDistance = Misc.getDistance (transform.position, walkPath [0]);
							float otherDistance = Misc.getDistance (otherHuman.transform.position, otherHuman.walkPath [0]);
							if (ourDistance > otherDistance || (ourDistance == otherDistance && !shouldDecide)) {
								if (!otherHuman.waitForHumans.Contains (this)) {
									waitForHumans.Add (otherHuman);
								}
							}
						}
					}
				}
			}
		}
	}

	public void reportColliderExit (Collider col, string colliderName) {
		if (!destroying) {
			CollisionObj rawCollisionObj = getColliderType (col);
			if (rawCollisionObj != null) {
				VehicleCollisionObj vehicleCollisionObj = rawCollisionObj.typeName == VehicleCollisionObj.NAME ? (VehicleCollisionObj)rawCollisionObj : null;
				HumanCollisionObj humanCollisionObj = rawCollisionObj.typeName == HumanCollisionObj.NAME ? (HumanCollisionObj)rawCollisionObj : null;

				if (vehicleCollisionObj != null) {
					removeVehicleInVision (vehicleCollisionObj);
				} else if (humanCollisionObj != null) {
					HumanLogic otherHuman = humanCollisionObj.Human;
					waitForHumans.Remove (otherHuman);
				}
			}
		}
	}

	private CollisionObj getColliderType (Collider col)
	{
		GameObject colliderGameObject = col.gameObject;
		if (colliderGameObject.GetComponentInParent<Vehicle> () != null) {
			// Car
			if (colliderGameObject.name == "CAR") {
				return new VehicleCollisionObj (colliderGameObject.GetComponentInParent<Vehicle> (), colliderGameObject.name);
			}
		} else if (colliderGameObject.GetComponentInParent<HumanLogic> () != null) {
			if (colliderGameObject.GetComponentInParent<HumanLogic> ().humanId != humanId) {
				string otherColliderName = HumanCollider.colliderNamesForGameObjectName [colliderGameObject.name];
				if (otherColliderName == "BODY") {
					return new HumanCollisionObj (colliderGameObject.GetComponentInParent<HumanLogic> (), otherColliderName);
				}
			}
		}
		return null;
	}

	private void setVehicleInVision (VehicleCollisionObj vehicleCollisionObj) {
		string subscriptionId = "Vehicle#" + vehicleCollisionObj.Vehicle.vehicleId + ":Irritation";
		vehiclesInVision.Add (vehicleCollisionObj.Vehicle, subscriptionId);
		PubSub.subscribe (subscriptionId, this);
	}

	private void removeVehicleInVision (VehicleCollisionObj vehicleCollisionObj) {
		string subscriptionId = vehiclesInVision [vehicleCollisionObj.Vehicle];
		vehiclesInVision.Remove (vehicleCollisionObj.Vehicle);
		PubSub.unsubscribe (subscriptionId, this);
	}

	private void removeAllVehiclesInVision () {
		PubSub.unsubscribeAllForSubscriber (this);
		vehiclesInVision.Clear ();
	}

	public PROPAGATION onMessage (string message, object data) {
		vehicleIrritationAction ((Vehicle)data);
		return PROPAGATION.DEFAULT;
	}
		
	public void vehicleIrritationAction (Vehicle vehicle) {
		Vector3 vehiclePosition = vehicle.gameObject.transform.position;
		Quaternion vehicleRotation = vehicle.gameObject.transform.rotation;

		Vector3 vehicleToHuman = transform.position - vehiclePosition;
		Vector3 vehicleMovement = vehicleRotation * Vector3.right;
		bool isToTheRight = Vector3.Cross (vehicleMovement, vehicleToHuman).z <= 0;

		Quaternion humanMovementRotation = Quaternion.Euler (0f, 0f, vehicleRotation.eulerAngles.z + (isToTheRight ? -90f : 90f));
		Vector3 offsetPosition = humanMovementRotation * (Vector3.right * vehicle.GetComponent<VehicleInfo> ().vehicleWidth);
		deviationTarget = transform.position + offsetPosition;
		timedDeviationTarget = new TimedDeviationTarget(4f, walkPath [0] + offsetPosition);
	}

	private class TimedDeviationTarget {
		public float time;
		public Vector3 point;

		public TimedDeviationTarget (float time, Vector3 point) {
			this.time = time;
			this.point = point;
		}
	}

	private static string STAT_WALKING_TIME = "People walking time";
	private static string STAT_WAITING_TIME = "People waiting time";
	private static string STAT_WALKING_DISTANCE = "People walking distance";
	private Dictionary<string, DataCollector.InnerData> stats = new Dictionary<string, DataCollector.InnerData> {
		{STAT_WALKING_TIME, new DataCollector.InnerData()},
		{STAT_WAITING_TIME, new DataCollector.InnerData()},
		{STAT_WALKING_DISTANCE, new DataCollector.InnerData()}
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
}
