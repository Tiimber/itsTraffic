using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HumanLogic : MonoBehaviour, FadeInterface {

	public static System.Random HumanRNG = new System.Random ((int)Game.randomSeed);

	private const float TARGET_WALKING_SPEED_KMH = 4.5f;
	private const float KPH_TO_LONGLAT_SPEED = 100f;

	private List<Pos> path;
	private List<Vector3> walkPath;

	private float speedFactor;
	private bool destroying = false;

	// Use this for initialization
	void Start () {
		DataCollector.Add ("Total # of people", 1f);
		initHumanProfile ();
	}

	private void initHumanProfile () {
		float minSpeedFactor = 0.8f;
		float maxSpeedFactor = 1.2f;

		speedFactor = Random.Range (minSpeedFactor, maxSpeedFactor);
	}

	// Update is called once per frame
	void Update () {
		if (destroying) {
			return;
		}

		Vector3 targetPoint = walkPath [0];
		Vector3 currentPoint = transform.position;
		rotateHuman (targetPoint, currentPoint);

		float travelDistance = (currentPoint - targetPoint).magnitude;

		float targetSpeedKmH = TARGET_WALKING_SPEED_KMH * speedFactor;
		float travelLengthThisFrame = (targetSpeedKmH * Time.deltaTime) / KPH_TO_LONGLAT_SPEED;

		if (travelLengthThisFrame >= travelDistance) {
			transform.position = targetPoint;
			walkPath.RemoveAt (0);
			if (walkPath.Count == 0) {
				fadeOutAndDestroy ();
			}
		} else {
			Vector3 movement = transform.rotation * (Vector3.right * travelLengthThisFrame);
			transform.position = transform.position + movement;
		}
	}

	private void positionHuman (Vector3 pos) {
		transform.position = pos;
	}

	private void rotateHuman (Vector3 target, Vector3 current) {
		Quaternion humanRotation = Quaternion.FromToRotation (Vector3.right, target - current);
		transform.rotation = humanRotation;
	}

	public void fadeOutAndDestroy () {
		destroying = true;
		FadeObjectInOut fadeObject = GetComponent<FadeObjectInOut>();
		fadeObject.DoneMessage = "destroy";
		fadeObject.FadeOut (0.5f);
	}

	public void onFadeMessage (string message) {
		if (message == "destroy") {
			StartCoroutine ("humanReachedGoal");
		}
	}

	private IEnumerator humanReachedGoal() {
//		VehicleCollider[] humanColliders = GetComponentsInChildren<HumanCollider> ();
//		foreach (VehicleCollider collider in humanColliders) {
//			collider.GetComponent<BoxCollider> ().center = new Vector3 (0f, 0f, 1000f);
//		}
		yield return null;
		Destroy (this.gameObject);
//		if (health > 0f) {
//			// TODO - Calculate points based on time, distance, or whatever...
			PubSub.publish ("points:inc", 50);
			DataCollector.Add ("Humans reached goal", 1f);
//		} else {
//			DataCollector.Add ("Vehicles destroyed", 1f);
//		}
//		numberOfCars--;
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
		if (startWay.hasNodes (pos1, pos2)) {
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
}
