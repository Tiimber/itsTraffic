using UnityEngine;
using System.Collections.Generic;

public class PosHelper {
	public static Pos getClosestNode (Pos pos, List<Pos> nodes) {
		Pos closestNode = null;
		float minDistance = float.MaxValue;

		foreach (Pos node in nodes) {
			float distance = PosHelper.getNodeDistance(pos, node);
			if (distance < minDistance) {
				minDistance = distance;
				closestNode = node;
			}
		}

		return closestNode;
	}

	private static float getNodeDistance (Pos node1, Pos node2) {
		return Mathf.Pow (node1.Lon - node2.Lon, 2f) + Mathf.Pow (node1.Lat - node2.Lat, 2f); 
	}

	public static float getVectorDistance (Vector2 v1, Vector2 v2) {
		return Mathf.Pow (v1.x - v2.x, 2f) + Mathf.Pow (v1.y - v2.y, 2f);
	}
}
