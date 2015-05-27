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
	private Vector3 PreviousMovementVector { set; get; }
	private float currentSpeed = 0f;

	private const float MaxRotation = 20f;
	private float DesiredRotation { set; get; }
	private float BreakFactor { set; get; }

	private Vector3 TargetPoint { set; get; }
	private WayReference TurnToRoad { set; get; }
	private TurnState turnState = TurnState.NONE;

	public Camera vehicleCameraObj;
	private static Vehicle debug;
	private static Camera debugCamera = null;

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
		transform.position = new Vector3 (transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z);
	}

	// Update is called once per frame
	void Update () {
		if (TurnToRoad != null) {
			// TODO - Try to make this better
			// The vehicles desired speed per second on this specific road
			float wayTargetSpeed = CurrentWayReference.way.WayWidthFactor * Settings.playbackSpeed;
			float vehicleTargetSpeed = (wayTargetSpeed * SpeedFactor * (1f - BreakFactor)) / 10f;
			// Calculated movement for current frame
			float currentAcceleration = (vehicleTargetSpeed - currentSpeed) / vehicleTargetSpeed * Acceleration;
			// Adjust with speedfactor
			currentAcceleration /= Settings.speedFactor;
			currentSpeed += currentAcceleration * Time.deltaTime;

			adjustColliders ();

//			Debug.Log ("Current speed: " + currentSpeed + ", Vehicle target speed: " + vehicleTargetSpeed + ", Acceleration: " + currentAcceleration);

			Vector3 currentPos = new Vector3 (transform.position.x, transform.position.y, 0f);
			Vector3 intersection = Vector3.zero;
			Vector3 toTarget;

			// We have a target point that we want to move towards - check if we intersect the target point (which means we need to turn)
			Vector3 vehicleMovement = transform.rotation * Vector3.right;
			Vector3 wayDirection = TurnToRoad.gameObject.transform.rotation * Vector3.right;
			bool intersects = Math3d.LineLineIntersection (out intersection, currentPos, vehicleMovement, TargetPoint, wayDirection);

			// TODO - Adjust in aspect of how close to target we are
			float time = 0.1f;
			Vector3 currentTargetPoint = Math3d.GetVectorInBezierAtTime(time, currentPos, intersects ? intersection : TargetPoint, TargetPoint);

			Vector3 positionMovementVector = currentTargetPoint - currentPos;
			transform.rotation = Quaternion.FromToRotation(Vector3.right, positionMovementVector);

			float movementPct = (currentSpeed / positionMovementVector.magnitude) * Settings.wayLengthFactor;
			Vector3 movementVector = positionMovementVector * movementPct;
			transform.position += new Vector3 (movementVector.x, movementVector.y, 0);

			toTarget = TargetPoint - transform.position;
			// Calculate how much we need to break in
			// TODO - See if this is still correct
//			BreakFactor = getBreakFactorForDegrees (Mathf.Abs (desiredRotation));

			toTarget.z = 0;
			if (PreviousMovementVector != Vector3.zero && Vector3.Angle (toTarget, PreviousMovementVector) > 150f) {
				CurrentPosition = CurrentTarget;
				updateCurrentTarget ();
				PreviousMovementVector = Vector3.zero;
			} else {
				PreviousMovementVector = toTarget;
			}
		} else {
			// TODO - We've probably reached the end of the road, what to do?
			Debug.Log ("No movement");
		}
	}

	void adjustColliders () {
		VehicleCollider[] vehicleCollders = GetComponentsInChildren<VehicleCollider> ();

		float forwardColliders = 0.75f;
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

//	private const float a = 1f;
	private const float b = -0.01648148f;
	private const float c = 0.0001049383f;
	private const float d = -2.286237e-7f;
	private float getBreakFactorForDegrees (float x)
	{
		// TODO - Make break factor working smoothly
//		return a + b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3);
		return - (b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3));
	}

	void initVehicleProfile () {
		// Set more vehicle profile properties here
		SpeedFactor = Random.Range (0.8f, 1.2f);
		Acceleration = Random.Range (2f, 3f);
		BreakFactor = 1.0f;
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
					BreakFactor = getBreakFactorForDegrees (Mathf.Abs (desiredRotation));
					Debug.Log (desiredRotation + " - " + BreakFactor);
//					BreakFactor = 0.9f;
				}
			} else if (possitilities.Count > 1) {
				currentPath = Game.calculateCurrentPath (CurrentPosition, EndPos);
				Pos nextTarget = currentPath [2];
				TurnToRoad = NodeIndex.getWayReference(CurrentTarget.Id, nextTarget.Id);
				TargetPoint = getTargetPoint(TurnToRoad);
				if (turnState != TurnState.BC) {
					float desiredRotation = Quaternion.Angle(CurrentWayReference.transform.rotation, TurnToRoad.transform.rotation);
					BreakFactor = getBreakFactorForDegrees (Mathf.Abs (desiredRotation));
					Debug.Log (desiredRotation + " - " + BreakFactor);
//					BreakFactor = 0.9f;
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
	}

	private Vector3 getTargetPoint (WayReference turnToRoad, Pos endNode = null)
	{
		Quaternion turnToRoadQuaternion = turnToRoad.gameObject.transform.rotation;
		float halfWayWidth = turnToRoad.gameObject.transform.localScale.y / 2f;
		bool isNode1 = endNode == null ? turnToRoad.isNode1 (CurrentTarget) : !turnToRoad.isNode1(endNode);

		Vector3 offset = getCenterYOfField (turnToRoad, endNode == null ? CurrentTarget : turnToRoad.getOtherNode (endNode));
		return endVector + turnToRoadQuaternion * new Vector3((isNode1 ? halfWayWidth : -halfWayWidth), 0, 0) + offset;
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
		} else if (colliderGameObject.GetComponent<Vehicle> () != null) {
			// Car
			return new CollisionObj<Pos>(null, colliderGameObject.GetComponent<Vehicle> (), colliderName, null);
		}
		return null;
	}

	public void updateCurrentTarget () {
		Debug.Log ("UpdateCurrentTarget");
		BreakFactor = 0f;
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
				TargetPoint = getTargetPoint(TurnToRoad);
			} else {
				TurnToRoad = CurrentWayReference;
				TargetPoint = getTargetPoint(CurrentWayReference, CurrentTarget);
			}
		} else {
			CurrentTarget = null;
			CurrentWayReference = null;

			// TODO - We've reached our destination - drive off map and despawn
			TurnToRoad = null;
			TargetPoint = Vector3.zero;
		}
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
			// BreakFactor, CurrentSpeed, CurrentPos, CurrentTarget, EndPos
			GUI.Label (new Rect (0, y += 20, 500, 20), "Speed: "+currentSpeed);
			GUI.Label (new Rect (0, y += 20, 500, 20), "BreakFactor: "+BreakFactor);
			GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentPos: " + CurrentPosition.Id);
			if (CurrentTarget != null) {
				GUI.Label (new Rect (0, y += 20, 500, 20), "CurrentTarget: " + CurrentTarget.Id);
			}
			GUI.Label (new Rect (0, y += 20, 500, 20), "EndPos: " + EndPos.Id);
		}
	}
}
