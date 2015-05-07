using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Vehicle: MonoBehaviour {

	public Pos StartPos { set; get; }
	public Pos EndPos { set; get; }
	public Pos CurrentPosition { set; get; } 
	public Pos CurrentTarget { set; get; }

	private Vector3 endVector;
	private Vector3 startVector;
	
	private WayReference CurrentWayReference { set; get; }
	private float SpeedFactor { set; get; }
	private float Acceleration { set; get; }
	private float currentSpeed = 0f;

	// TODO - Carefulness (drunk level, tired, age...)

	// Use this for initialization
	void Start () {
		WayReference wayReference = NodeIndex.endPointIndex [StartPos.Id][0];
		transform.rotation = wayReference.transform.rotation;
		initVehicleProfile ();
		updateCurrentTarget ();
	}
	
	// Update is called once per frame
	void Update () {
		// The vehicles desired speed per second on this specific road
		float wayTargetSpeed = CurrentWayReference.way.WayWidthFactor;
		float vehicleTargetSpeed = wayTargetSpeed * SpeedFactor;
		// Calculated movement for current frame
		float currentAcceleration = (vehicleTargetSpeed - currentSpeed) / vehicleTargetSpeed * Acceleration;
		// Adjust with speedfactor
		currentAcceleration /= Settings.speedFactor;
		currentSpeed += currentAcceleration * Time.deltaTime;

		// Update pos, move closer to EndPos
		Vector3 positionMovementVector = endVector - startVector;
		float movementPct = (currentSpeed / positionMovementVector.magnitude) * Settings.wayLengthFactor;
		transform.position += positionMovementVector * movementPct;
	}

	void initVehicleProfile () {
		// Set more vehicle profile properties here
		SpeedFactor = Random.Range (0.8f, 1.2f);
		Acceleration = Random.Range (0.2f, 0.3f);
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
		if (collisionObj.WayReference != null && collisionObj.WayReference == CurrentWayReference && collisionObj.ExtraData == CurrentTarget) {
			Debug.Log (colliderName);
		}
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
		List<Pos> path = Game.calculateCurrentPath (CurrentPosition, EndPos);
		if (path.Count > 1) {
			CurrentTarget = path [1];
			CurrentWayReference = NodeIndex.getWayReference(CurrentPosition.Id, CurrentTarget.Id);
			endVector = Game.getCameraPosition (CurrentTarget);
			startVector = Game.getCameraPosition (CurrentPosition);
		} else {
			CurrentTarget = null;
			CurrentWayReference = null;
			// TODO - We've reached our destination - drive off map and despawn
		}
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
}
