using System;
using UnityEngine;
using System.Collections.Generic;

public class WayCrossing {
	public static void Create (GameObject parent, long key, List<WayReference> wayReferences) {
		Pos pos = NodeIndex.getPosById (key);
		GameObject crossingLine = Game.instance.wayCrossing;
		Vector3 center = Game.getCameraPosition (pos) + crossingLine.transform.position;
		GameObject line = MonoBehaviour.Instantiate (crossingLine, center, Quaternion.identity) as GameObject;
	}
}

