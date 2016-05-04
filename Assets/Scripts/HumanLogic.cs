using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HumanLogic : MonoBehaviour {

	public static System.Random HumanRNG = new System.Random ((int)Game.randomSeed);

	// Use this for initialization
	void Start () {
		DataCollector.Add ("Total # of people", 1f);
	}
	
	// Update is called once per frame
	void Update () {
	}
		
	public void setStartAndEndInfo (Tuple3<Pos, WayReference, Vector3> startInfo, Tuple3<Pos, WayReference, Vector3> endInfo) {
		List<Pos> path = Game.calculateCurrentPath (startInfo.First, endInfo.First, false);
		List<Vector3> walkPath = Misc.posToVector3 (path);

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
		path.Insert (0, Game.createTmpPos(startInfo.Third));

		WayReference endWay = endInfo.Second;
		if (endWay.hasNodes (secondToLastPos, lastPos)) {
			walkPath.RemoveAt (walkPath.Count - 1);
			path.RemoveAt (path.Count - 1);
		}
		walkPath.Add (endInfo.Third);
		path.Add (Game.createTmpPos(endInfo.Third));

		DebugFn.DebugPath (walkPath);

		Vector3 vec1 = walkPath [0];
		Vector3 vec2 = walkPath [1];

		Quaternion humanRotation = Quaternion.FromToRotation (Vector3.right, vec2 - vec1);
		transform.rotation = humanRotation;
	}
}
