using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO - Rename since fitting for straight and intersections
public class WayObjectStraight {

	private const float BEZIER_RESOLUTION = 20f;

	public static void create (long key, List<WayReference> wayReferences) {
		Pos pos = NodeIndex.getPosById (key);

		// Sort based on rotation in the point closest to the intersection
		wayReferences.Sort (delegate(WayReference x, WayReference y) {
			float angleDiff = AngleAroundNode(pos, x) - AngleAroundNode(pos, y);
			// Ignoring float to int issues (if there are any)
			int angleDiffInt = (int) angleDiff;
			return angleDiffInt;
		});

		// Add first one last as well, so we can iterate them as pairs
		wayReferences.Add (wayReferences [0]);

		// Gather our way bounds (used for checking if ways intersects each other)
		List<Bounds> wayBounds = new List<Bounds> ();
		foreach (WayReference wayReference in wayReferences) {
			GameObject wayMeshObj = GameObject.Find ("Plane Mesh for " + wayReference.gameObject.name);
			Renderer wayRenderer = wayMeshObj.GetComponent<Renderer>();
			Bounds wayBound = wayRenderer.bounds;
			wayBounds.Add (wayBound);
		}
				
		// middle of intersection "pos"
		Vector3 intersectionPos = Game.getCameraPosition (pos);

		// List of positions where to draw the mesh
		List<Vector3> meshPoints = new List<Vector3> ();

//		bool isNode1InFirst = wayReferences [0].isNode1 (pos);

		// Iterate over our wayReferences in pairs
		bool lastWaysIntersected = false;
		for (int i = 0; i < wayReferences.Count - 1; i++) {
			WayReference way1 = wayReferences[i];
			WayReference way2 = wayReferences[i+1];

			Bounds way1Bounds = wayBounds[i];
			Bounds way2Bounds = wayBounds[i+1];

			bool way1IsNode1 = way1.isNode1(pos);
			bool way2IsNode1 = way2.isNode1(pos);

			Quaternion way1Rotation = way1IsNode1 ? way1.transform.rotation : Quaternion.Euler(way1.transform.rotation.eulerAngles + new Vector3(0f, 0f, 180f));
			Quaternion way2Rotation = way2IsNode1 ? way2.transform.rotation : Quaternion.Euler(way2.transform.rotation.eulerAngles + new Vector3(0f, 0f, 180f));

			if (!lastWaysIntersected && i == 0) {
				// Add "left" point of first way only
				Vector3 left = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, -way1.transform.localScale.y / 2f , 0f);
				meshPoints.Add(left);
			}
			lastWaysIntersected = false;

			if (way1Bounds.Intersects(way2Bounds)) {
				// TODO Ways intersects, no bezier, instead take the intersection point along the border
				lastWaysIntersected = true;

				if (i == wayReferences.Count - 2) {
					// TODO - Remove first mesh point, and add the intersecting point instead
				}
			} else {
				// Add "right" point
				Vector3 right = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, way1.transform.localScale.y / 2f , 0f);
				meshPoints.Add(right);

				// "Left" point in next way
				Vector3 leftWay2 = intersectionPos + way2Rotation * new Vector3(way2.transform.localScale.y / 2f, -way2.transform.localScale.y / 2f , 0f);

				// Add bezier points between this ways "right" and next ways "left" point
				Vector3 intersectionPoint;
				bool intersectionFound = Math3d.LineLineIntersection(out intersectionPoint, right, way1.transform.rotation * Vector3.right, leftWay2, way2.transform.rotation * Vector3.right);  

//				Debug.Log (i + ": " + intersectionFound);
				if (!intersectionFound) {
					intersectionFound = Math3d.LineLineIntersection(out intersectionPoint, right, Quaternion.Euler(new Vector3(0, 0, 180f) + way1.transform.rotation.eulerAngles) * Vector3.right, leftWay2, Quaternion.Euler(new Vector3(0, 0, 180f) + way2.transform.rotation.eulerAngles) * Vector3.right);  
//					Debug.Log ("Retry: " + intersectionFound2);
				}

				if (intersectionFound) {
					// Intersection found, draw the bezier curve
					float bezierLength = Math3d.GetBezierLength(right, intersectionPoint, leftWay2);
//					Debug.Log (bezierLength);
					float numberOfPoints = bezierLength * BEZIER_RESOLUTION;

//					meshPoints.Add (leftWay2);
//					Debug.Log ("Bezier:");
					float step = 1.0f / numberOfPoints;
					bool doBreak = false;
					for (float time = step; time < 1.0f + step; time += step) {
						if (time > 1f) {
							time = 1f;
							doBreak = true;
						}
						Vector3 bezierPoint = Math3d.GetVectorInBezierAtTime (time, right, intersectionPoint, leftWay2);
						meshPoints.Add (bezierPoint);
//						DebugFn.print (bezierPoint);
						Debug.Log (time);
						if (doBreak) {
							break;
						}
					}
				} else {
					// No intersection found for way points, just draw a straight line
	                meshPoints.Add (leftWay2);
				}
			}
		}

//		DebugFn.print (meshPoints);

		meshPoints.RemoveAt (meshPoints.Count - 1);

//		Debug.Log ("Intersection");
		GameObject intersectionObj = MapSurface.createPlaneMeshForPoints (meshPoints);
		intersectionObj.name = "Intersection";
		intersectionObj.transform.position = intersectionObj.transform.position - new Vector3 (0, 0, 0.1f);
		AutomaticMaterialObject intersectionMaterialObject = intersectionObj.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
		intersectionMaterialObject.requestMaterial ("2002-Street", null); // TODO - Should have same material as connecting way(s)
	}

	private static float AngleAroundNode(Pos pos, WayReference wayReference) {
		bool isNode1 = wayReference.isNode1(pos);
		float rotation = wayReference.gameObject.transform.rotation.eulerAngles.z;
		float adjustedRotatiton = Math3d.Mod((rotation + (isNode1 ? 0f : 180f)), 360f);
		return adjustedRotatiton;
	}
}
