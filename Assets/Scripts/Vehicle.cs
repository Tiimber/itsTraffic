using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Vehicle: MonoBehaviour {

	public Pos StartPos { set; get; }
	public Pos EndPos { set; get; }
	public Pos CurrentPosition { set; get; } 
	public Pos CurrentTarget { set; get; }
	private List<Pos> currentPath { set; get; }

	private Vector3 endVector;
	private Vector3 startVector;

	private WayReference CurrentWayReference { set; get; }
	private float SpeedFactor { set; get; }
	private float Acceleration { set; get; }
//	private Vector3 PreviousMovementVector { set; get; }
	private float currentSpeed = 0f;

	private float EmissionFactor { set; get; }
	private float CollectedEmissionAmount = 0f;

	private const float THRESHOLD_EMISSION_PUFF = 0.002f;

//	private const float MaxRotation = 20f;
	private float DesiredRotation { set; get; }
	private float TurnBreakFactor { set; get; }
	private float AwarenessBreakFactor { set; get; }

	private List<Vehicle> FacVehiclesInAwarenessArea { set; get; }
	private List<Vehicle> PcVehiclesInAwarenessArea { set; get; }
	
	private Vector3 TargetPoint { set; get; }
	private WayReference TurnToRoad { set; get; }
	private bool isStraightWay { set; get; }
	private TurnState turnState = TurnState.NONE;
	private float BezierLength { set; get; }
	private float AccumulatedBezierDistance { set; get; }

	public Camera vehicleCameraObj;
	private static Vehicle debug;
	private static Camera debugCamera = null;
	
	private Vector3 vehicleMovement;

	public void setDebug() {
		grabCamera ();
		Vehicle.debug = this;
	}

	public static void detachCurrentCamera () {
		if (Vehicle.debugCamera != null) {
			Vehicle.debugCamera.enabled = false;
			Destroy (Vehicle.debugCamera.gameObject);
		}
	}

	public void grabCamera ()
	{
		Vehicle.detachCurrentCamera ();

		// Instantiate camera in vehicle
		Vehicle.debugCamera = Instantiate (vehicleCameraObj, Vector3.zero, vehicleCameraObj.transform.rotation) as Camera;
		Vehicle.debugCamera.transform.parent = this.transform;
		Vehicle.debugCamera.transform.localPosition = new Vector3(0f, 0f, -1f);
		Vehicle.debugCamera.enabled = true;
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
		initVehicleProfile ();
		updateCurrentTarget ();


//		transform.rotation = Quaternion.Euler(0, 0, 97.97565f);

//		float currentYOffset = getCenterYOfField (CurrentWayReference, CurrentPosition);
		Vector3 offset = getCenterYOfField (CurrentWayReference, CurrentPosition);
//		Debug.Log (offset.x + ", " + offset.y);
		transform.position = new Vector3 (transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z);
		vehicleMovement = transform.rotation * Vector3.right;
	}

	// Update is called once per frame
	int i = 0;
	void Update () {
		i++;
//		Debug.Log (i++);
		if (TurnToRoad != null) {
			// TODO - Try to make this better
			// The vehicles desired speed per second on this specific road
			float wayTargetSpeed = CurrentWayReference.way.WayWidthFactor * Settings.playbackSpeed;
			float breakFactor = Mathf.Min (TurnBreakFactor, AwarenessBreakFactor);
			float vehicleTargetSpeed = (wayTargetSpeed * SpeedFactor * breakFactor / 2) / 10f;
			// Calculated movement for current frame
			float currentAcceleration = (vehicleTargetSpeed - currentSpeed) / vehicleTargetSpeed * Acceleration;
			// Adjust with speedfactor
			currentAcceleration /= Settings.speedFactor;
			float speedChangeInFrame = currentAcceleration * Time.deltaTime;
			currentSpeed += speedChangeInFrame;

			calculateCollectedEmission(speedChangeInFrame);

			adjustColliders ();

//			Debug.Log ("Current speed: " + currentSpeed + ", Vehicle target speed: " + vehicleTargetSpeed + ", Acceleration: " + currentAcceleration);

			Vector3 currentPos = new Vector3 (transform.position.x, transform.position.y, 0f);
			Vector3 intersection = Vector3.zero;
//			Vector3 toTarget;

			// We have a target point that we want to move towards - check if we intersect the target point (which means we need to turn)
			Vector3 wayDirection = TurnToRoad.gameObject.transform.rotation * Vector3.right;

			Vector3 currentWayDirection = CurrentWayReference.gameObject.transform.rotation * (CurrentWayReference.isNode1(CurrentTarget) ? Vector3.left : Vector3.right);
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
			float time = TurnToRoad.SmallWay && isStraightWay ? 1.0f : Mathf.Max (Mathf.Min(1f, AccumulatedBezierDistance / BezierLength), 0.05f);
//			Debug.Log ("Time: " + time);
			Vector3 currentTargetPoint = Math3d.GetVectorInBezierAtTime(time, currentPos, intersects ? intersection : TargetPoint, TargetPoint);

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
			if (positionMovementVector.magnitude > 0.0001f) {
				Quaternion vehicleRotation = Quaternion.FromToRotation(Vector3.right, positionMovementVector);
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
				Update();
				return;
			}
			Vector3 positionMovement = new Vector3 (movementVector.x, movementVector.y, 0);
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
		} else {
			// TODO - We've probably reached the end of the road, what to do?
//			Debug.Log ("No movement");
		}
	}

	private void adjustColliders () {
		VehicleCollider[] vehicleCollders = GetComponentsInChildren<VehicleCollider> ();

		float forwardColliders = 1f;
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

		float widthFac = fac * totalColliderSize;
		float midFac = 0.5f + (pc * totalColliderSize) + widthFac / 2f;

		BoxCollider facCollider = vehicleCollders [0].GetComponent<BoxCollider> ();
		facCollider.center = new Vector3 (midFac, 0f, 0f);
		facCollider.size = new Vector3 (widthFac, 1f, 1f);

		float widthPc = pc * totalColliderSize;
		float midPc = 0.5f + widthPc / 2f;

		BoxCollider pcCollider = vehicleCollders [1].GetComponent<BoxCollider> ();
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
		// Set more vehicle profile properties here
		SpeedFactor = Random.Range (0.8f, 1.2f);
		Acceleration = Random.Range (2f, 3f);
		TurnBreakFactor = 1.0f;
		AwarenessBreakFactor = 1.0f;
		EmissionFactor = Random.Range (0.1f, 1.0f);

		FacVehiclesInAwarenessArea = new List<Vehicle> ();
		PcVehiclesInAwarenessArea = new List<Vehicle> ();
	}

	public void reportColliderExit (Collider col, string colliderName) {
		CollisionObj<Pos> collisionObj = getColliderType (col, colliderName);
		// If we're turning and our Panic Collider have left the target collider
		if (colliderName == "PC" && turnState != TurnState.NONE) {
			// If the collisionObj is our current TurnToRoad and the collider we're leaving is the target
			if (collisionObj != null && collisionObj.WayReference != null && collisionObj.WayReference == TurnToRoad && collisionObj.ExtraData == CurrentTarget) {
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
				float angleDiff = Mathf.Abs (vehicleAngle - wayAngle);
//				Debug.Log ("Diff: " + angleDiff);
				if (angleDiff < acceptableAngleDiff) {
					CurrentPosition = CurrentTarget;
					updateCurrentTarget ();
				}
			}
		} else if (TurnToRoad.SmallWay && colliderName == "BC" && turnState != TurnState.NONE) {
			if (collisionObj != null && collisionObj.WayReference != null && collisionObj.WayReference == TurnToRoad && collisionObj.ExtraData == TurnToRoad.getOtherNode(CurrentTarget)) {
				CurrentPosition = collisionObj.ExtraData;
				updateCurrentTarget ();
			}
		} else if (colliderName == "BC" && turnState != TurnState.NONE) {
			if (collisionObj != null && collisionObj.WayReference != null && collisionObj.WayReference == TurnToRoad && collisionObj.ExtraData == CurrentTarget) {
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
				float angleDiff = Mathf.Abs (vehicleAngle - wayAngle);
//				Debug.Log ("Diff: " + angleDiff);
				if (angleDiff < acceptableAngleDiff) {
					CurrentPosition = CurrentTarget;
					updateCurrentTarget ();
				}
			}
		}

		if (collisionObj != null && collisionObj.Vehicle != null) {
			string otherColliderName = collisionObj.CollisionObjType;
			if (otherColliderName == CollisionObj<Pos>.VEHICLE_COLLIDER) {
				Vehicle otherVehicle = collisionObj.Vehicle;
				if (colliderName == "FAC") {
					// Front collider discovered car, slow down
					removeVehicleInAwarenessArea(colliderName, otherVehicle);
				} else if (colliderName == "PC") {
					// Panic collider discovered car, break hard
					removeVehicleInAwarenessArea(colliderName, otherVehicle);
				}
				autosetAwarenessBreakFactor();
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
		CollisionObj<Pos> collisionObj = getColliderType (col, colliderName);

		// Logic for upcoming wayreference end node collission
		if (collisionObj != null && collisionObj.WayReference != null && collisionObj.WayReference == CurrentWayReference && collisionObj.ExtraData == CurrentTarget) {
			// We know that this is the currentTarget - we want to know our options
			List<WayReference> possitilities = NodeIndex.nodeWayIndex [CurrentTarget.Id].Where (p => p != CurrentWayReference && p.way.WayWidthFactor >= WayTypeEnum.MINIMUM_DRIVE_WAY).ToList ();
			if (colliderName == "FAC") {
				turnState = TurnState.FAC;
			} else if (colliderName == "PC") {
				turnState = TurnState.PC;
			} else if (colliderName == "CAR") {
				turnState = TurnState.CAR;
			} else if (colliderName == "BC") {
				turnState = TurnState.BC;
			}

			if (possitilities.Count == 1) {
				if (turnState != TurnState.BC) {
					float desiredRotation = Quaternion.Angle(CurrentWayReference.transform.rotation, TurnToRoad.transform.rotation);
					bool areBothSameDirection = CurrentWayReference.isNode1(CurrentTarget) != TurnToRoad.isNode1(CurrentTarget);
					if (!areBothSameDirection) {
						desiredRotation = 180f - desiredRotation;
					}
					TurnBreakFactor = getTurnBreakFactorForDegrees (Mathf.Abs (desiredRotation));
				}
			} else if (possitilities.Count > 1) {
				currentPath = Game.calculateCurrentPath (CurrentPosition, EndPos);
				Pos nextTarget = currentPath [2];
				if (turnState != TurnState.BC) {
					WayReference otherWayReference = NodeIndex.getWayReference(CurrentTarget.Id, nextTarget.Id);
					float desiredRotation = Quaternion.Angle(CurrentWayReference.transform.rotation, otherWayReference.transform.rotation);
					bool areBothSameDirection = CurrentWayReference.isNode1(CurrentTarget) != otherWayReference.isNode1(CurrentTarget);
					if (!areBothSameDirection) {
						desiredRotation = 180f - desiredRotation;
					}
					TurnBreakFactor = getTurnBreakFactorForDegrees (Mathf.Abs (desiredRotation));
//					Debug.Log ("breakFactor: " + TurnBreakFactor + ", for degrees: " + desiredRotation); 
				}
				if (turnState == TurnState.CAR) {
					TurnToRoad = NodeIndex.getWayReference(CurrentTarget.Id, nextTarget.Id);
					BezierLength = 0f;
					TargetPoint = getTargetPoint(TurnToRoad, null, true);
					isStraightWay = false;
					vehicleMovement = transform.rotation * Vector3.right;
				}
			} else {
				// TODO - Temporary only - stop on endpoint
				if (colliderName == "CAR") {
					// Endpoint
					currentSpeed = 0;
					Acceleration = 0;
				}
			}
		}

		// Logic for other vehicle awareness
		if (collisionObj != null && collisionObj.Vehicle != null) {
			string otherColliderName = collisionObj.CollisionObjType;
			if (otherColliderName == CollisionObj<Pos>.VEHICLE_COLLIDER) {
				Vehicle otherVehicle = collisionObj.Vehicle;
				if (colliderName == "FAC") {
					// Front collider discovered car, slow down
					addVehicleInAwarenessArea(colliderName, otherVehicle);
				} else if (colliderName == "PC") {
					// Panic collider discovered car, break hard
					addVehicleInAwarenessArea(colliderName, otherVehicle);
				}
				autosetAwarenessBreakFactor();
			}
		}

	}

	private void autosetAwarenessBreakFactor ()
	{
		bool hasVehicleInFac = FacVehiclesInAwarenessArea.Any (); 
		bool hasVehicleInPc = PcVehiclesInAwarenessArea.Any (); 
		if (hasVehicleInPc) {
			AwarenessBreakFactor = 0.1f;
		} else if (hasVehicleInFac) {
			AwarenessBreakFactor = 0.5f;
		} else {
			AwarenessBreakFactor = 1f;
		}
	}

	private void addVehicleInAwarenessArea (string colliderName, Vehicle otherVehicle) {
		List<Vehicle> vehiclesInAwarenessArea = colliderName == "FAC" ? FacVehiclesInAwarenessArea : PcVehiclesInAwarenessArea;
		if (!vehiclesInAwarenessArea.Contains (otherVehicle)) {
			vehiclesInAwarenessArea.Add (otherVehicle);
		}
//		Debug.Log ("Added to " + colliderName + ", length: " + vehiclesInAwarenessArea.Count);
	}

	private void removeVehicleInAwarenessArea (string colliderName, Vehicle otherVehicle) {
		List<Vehicle> vehiclesInAwarenessArea = colliderName == "FAC" ? FacVehiclesInAwarenessArea : PcVehiclesInAwarenessArea;
		if (vehiclesInAwarenessArea.Contains (otherVehicle)) {
			vehiclesInAwarenessArea.Remove (otherVehicle);
		}
//		Debug.Log ("Removed from " + colliderName + ", length: " + vehiclesInAwarenessArea.Count);
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

	private CollisionObj<Pos> getColliderType (Collider col, string colliderName)
	{
		GameObject colliderGameObject = col.gameObject;
		if (colliderGameObject.GetComponent<WayReference> () != null) {
			// Way reference
			int colliderIndex = colliderGameObject.GetComponents<Collider>().ToList().IndexOf(col);
			bool isNode1 = colliderIndex % 2 == 0;
			WayReference wayReference = colliderGameObject.GetComponent<WayReference> ();
			return new CollisionObj<Pos>(wayReference, null, CollisionObj<Pos>.WAY_COLLIDER, isNode1 ? wayReference.node1 : wayReference.node2);
		} else if (colliderGameObject.transform.parent != null && colliderGameObject.transform.parent.gameObject.GetComponent<Vehicle> () != null) {
			// Car
			return new CollisionObj<Pos>(null, colliderGameObject.transform.parent.gameObject.GetComponent<Vehicle> (), colliderGameObject.name, null);
		}
		return null;
	}

	public void updateCurrentTarget () {
		TurnBreakFactor = 1.0f;
//		Time.timeScale = TurnBreakFactor;
		turnState = TurnState.NONE;
		currentPath = Game.calculateCurrentPath (CurrentPosition, EndPos);
		if (currentPath.Count > 1) {
			CurrentTarget = currentPath [1];
			CurrentWayReference = NodeIndex.getWayReference(CurrentPosition.Id, CurrentTarget.Id);
			// TODO - Can remove?
			endVector = Game.getCameraPosition (CurrentTarget);
			startVector = Game.getCameraPosition (CurrentPosition);

			List<WayReference> possitilities = NodeIndex.nodeWayIndex [CurrentTarget.Id].Where (p => p != CurrentWayReference && p.way.WayWidthFactor >= WayTypeEnum.MINIMUM_DRIVE_WAY).ToList ();
			if (possitilities.Count == 1) {
				TurnToRoad = possitilities[0];
				BezierLength = 0f;
				TargetPoint = getTargetPoint(TurnToRoad);
				isStraightWay = true;
			} else {
				TurnToRoad = CurrentWayReference;
				BezierLength = 0f;
				TargetPoint = getTargetPoint(CurrentWayReference, CurrentTarget);
				isStraightWay = true;
			}
		} else {
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
		Debug.Log ("Emit gas");
		PubSub.publish ("Vehicle:emitGas", this);
//		Debug.Break ();
	}

	public Vector3 getEmitPosition () {
//		Transform carObjectTransform = transform.FindChild ("CarObject");
//		Vector3 localPos = carObjectTransform.localPosition;
//
//		return transform.position + localPos;
		return transform.position;
	}

	private class CollisionObj<T> {
		public WayReference WayReference { get; set; }
		public Vehicle Vehicle { get; set; }
		public string CollisionObjType { get; set; }
		public T ExtraData { get; set; }

		public const string WAY_COLLIDER = "WC";
		public const string VEHICLE_FRONT_AWARE_COLLIDER = "FAC";
		public const string VEHICLE_PANIC_COLLIDER = "PC";
		public const string VEHICLE_COLLIDER = "CAR";
		public const string VEHICLE_BACK_COLLIDER = "BC";

		public CollisionObj (WayReference wayReference, Vehicle vehicle, string collisionObjType, T extraData) {
			this.WayReference = wayReference;
			this.Vehicle = vehicle;
			this.CollisionObjType = collisionObjType;
			this.ExtraData = extraData;
		}
	}
	
	public void OnGUI () {
		if (Vehicle.debug == this) {
			int y = 200;
			// TurnBreakFactor, CurrentSpeed, CurrentPos, CurrentTarget, EndPos
			GUI.Label (new Rect (0, y += 20, 500, 20), "Speed: "+currentSpeed);
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
