using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// TODO - Rename since fitting for straight and intersections
public class WayObjectStraight {

	private static bool off = false;

	private static Vector3 DEGREES_90 = new Vector3 (0f, 0f, 90f);
	private static Vector3 DEGREES_270 = new Vector3 (0f, 0f, 270f);

	public static void create (long key, List<WayReference> wayReferences, string materialId) {
		if (off) {
			return;
		}
		Pos pos = NodeIndex.getPosById (key);

		// Sort based on rotation in the point closest to the intersection
		wayReferences.Sort (delegate(WayReference x, WayReference y) {
			float angleDiff = AngleAroundNode(pos, x) - AngleAroundNode(pos, y);
			// Ignoring float to int issues (if there are any)
			int angleDiffInt = (int) angleDiff;
			return angleDiffInt;
		});

		// Gather our way bounds (used for checking if ways intersects each other)
		List<Bounds> wayBounds = getWayBounds (wayReferences);

		// List of positions where to draw the mesh
		List<Vector3> meshPoints;

		List<WayReference> intersectionList = Misc.CloneBaseNodeList (wayReferences);
		bool isComplex = false;
		if (intersectionList.Count == 2 && wayBounds [0].Intersects (wayBounds [1])) {
			// Only two ways and they intersect, special logic
			meshPoints = getMeshPointsForComplexTwoWay(intersectionList, wayBounds, pos);
			isComplex = true;
		} else {
			meshPoints = getMeshPointsForNonComplex(intersectionList, wayBounds, pos);
		}

//		Debug.Log ("Intersection");
		GameObject intersectionObj = MapSurface.createPlaneMeshForPoints (meshPoints);
		intersectionObj.name = "Intersection " + (isComplex ? "complex " : "") + (intersectionList.Count - 1) + "-way (" + key + ")";
		Vector3 zOffset = intersectionList[0].way.CarWay ? new Vector3 (0, 0, -0.1f) : new Vector3 (0, 0, -0.099f);
		intersectionObj.transform.position = intersectionObj.transform.position + zOffset;
		AutomaticMaterialObject intersectionMaterialObject = intersectionObj.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
		intersectionMaterialObject.requestMaterial (materialId, null); // TODO - Should have same material as connecting way(s)

		// Need waylines for all straight ways
		if (wayReferences.Count == 2) {
			bool wayQualifiedForCrossing = wayReferences[0].way.WayWidthFactor >= WayHelper.LIMIT_WAYWIDTH && wayReferences[0].way.CarWay;
			if (pos.getTagValue("highway") == "crossing" && wayQualifiedForCrossing) {
				DebugFn.square(Game.getCameraPosition(pos));
				WayCrossing.Create (intersectionObj, key, wayReferences);
			} else {
				WayLine.CreateCurved(intersectionObj, key, wayReferences);
			}
		}
	}

	private static float AngleAroundNode(Pos pos, WayReference wayReference) {
		bool isNode1 = wayReference.isNode1(pos);
		float rotation = wayReference.gameObject.transform.rotation.eulerAngles.z;
		float adjustedRotatiton = Math3d.Mod((rotation + (isNode1 ? 0f : 180f)), 360f);
		return adjustedRotatiton;
	}

	static List<Bounds> getWayBounds (List<WayReference> wayReferences)
	{
		List<Bounds> wayBounds = new List<Bounds> ();
		foreach (WayReference wayReference in wayReferences) {
			GameObject wayMeshObj = GameObject.Find ("Plane Mesh for " + wayReference.gameObject.name);
			Renderer wayRenderer = wayMeshObj.GetComponent<Renderer>();
			Bounds wayBound = wayRenderer.bounds;
			wayBounds.Add (wayBound);
		}

		return wayBounds;
	}

	static List<Vector3> getMeshPointsForComplexTwoWay (List<WayReference> wayReferences, List<Bounds> wayBounds, Pos pos)
	{
		List<Vector3> meshPoints = new List<Vector3> ();

		// middle of intersection "pos"
		Vector3 intersectionPos = Game.getCameraPosition (pos);

		WayReference way1 = wayReferences [0];
		Bounds way1Bounds = wayBounds [0];
		bool way1IsNode1 = way1.isNode1(pos);
		Quaternion way1Rotation = way1IsNode1 ? way1.transform.rotation : Quaternion.Euler (way1.transform.rotation.eulerAngles + new Vector3(0f, 0f, 180f));

		Vector3 way1Left = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, -way1.transform.localScale.y / 2f, 0f);
		Bounds leftCheckPoint = new Bounds(way1Left + (way1Rotation * new Vector3(way1.transform.localScale.y / 20f, way1.transform.localScale.y / 20f, 0f)) - new Vector3(0f, 0f, 0.1f), new Vector3(way1.transform.localScale.y / 10f, way1.transform.localScale.y / 10f, way1.transform.localScale.y / 10f));

//		DebugFn.DrawBounds (leftCheckPoint);

		WayReference way2;
		Bounds way2Bounds = wayBounds [1];
		bool way2IsNode1;
		Quaternion way2Rotation;

		if (way2Bounds.Intersects (leftCheckPoint)) {
			way1 = wayReferences [1];
			way1Bounds = wayBounds [1];
			way1IsNode1 = way1.isNode1 (pos);
	        way1Rotation = way1IsNode1 ? way1.transform.rotation : Quaternion.Euler (way1.transform.rotation.eulerAngles + new Vector3 (0f, 0f, 180f));
			way1Left = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, -way1.transform.localScale.y / 2f, 0f);

			way2 = wayReferences [0];
			way2Bounds = wayBounds [0];
			way2IsNode1 = way2.isNode1 (pos);
			way2Rotation = way2IsNode1 ? way2.transform.rotation : Quaternion.Euler (way2.transform.rotation.eulerAngles + new Vector3 (0f, 0f, 180f));
		} else {
			way2 = wayReferences [1];
			way2Bounds = wayBounds [1];
			way2IsNode1 = way2.isNode1 (pos);
			way2Rotation = way2IsNode1 ? way2.transform.rotation : Quaternion.Euler (way2.transform.rotation.eulerAngles + new Vector3 (0f, 0f, 180f));
		}


		Vector3 way2Right = intersectionPos + way2Rotation * new Vector3(way2.transform.localScale.y / 2f, way2.transform.localScale.y / 2f , 0f);

		// Angles looking towards intersection of ways
		Vector3 way1IntersectionAngle = (way1IsNode1 ? DEGREES_90 : DEGREES_270) + way1.transform.rotation.eulerAngles;
		Vector3 way2IntersectionAngle = (way2IsNode1 ? DEGREES_270 : DEGREES_90) + way2.transform.rotation.eulerAngles;
		
//		// TODO DEBUG ONLY
//		DebugFn.arrow (way1Left, way1IntersectionAngle, new Vector3(0.1f, 0f, 0f));
//		DebugFn.arrow (way2Right, way2IntersectionAngle, new Vector3(0.1f, 0f, 0f));
		
		// Get intersection point
		Vector3 intersectionPoint;
		bool intersectionFound = Math3d.LineLineIntersection(out intersectionPoint, way1Left, Quaternion.Euler(way1IntersectionAngle) * Vector3.right, way2Right, Quaternion.Euler(way2IntersectionAngle) * Vector3.right);  
		
//		DebugFn.square (intersectionPoint);
		
		// TODO DEBUG ONLY
		if (!intersectionFound) {
			// TODO DEBUG ONLY
			DebugFn.arrow (way1Left, way1IntersectionAngle, new Vector3(0.1f, 0f, 0f));
			DebugFn.arrow (way2Right, way2IntersectionAngle, new Vector3(0.1f, 0f, 0f));
			Debug.Log ("Complex Intersection point not found");
//			Debug.Break ();
		}
		
		meshPoints.Add (intersectionPoint);
		meshPoints.Add (way2Right);

		// TODO - Can this use GetBezeirPoints below?
		// Add bezier points between the ways, from "right" point in way2 to "left" point in way1
		Vector3 intersectionPointBezier;
		bool intersectionFoundBezier = Math3d.LineLineIntersection(out intersectionPointBezier, way2Right, way2.transform.rotation * Vector3.right, way1Left, way1.transform.rotation * Vector3.right);  
		
		if (!intersectionFoundBezier) {
			intersectionFoundBezier = Math3d.LineLineIntersection(out intersectionPointBezier, way2Right, Quaternion.Euler(new Vector3(0, 0, 180f) + way2.transform.rotation.eulerAngles) * Vector3.right, way1Left, Quaternion.Euler(new Vector3(0, 0, 180f) + way1.transform.rotation.eulerAngles) * Vector3.right);
		}

//		DebugFn.arrow (way1Left, way1.transform.rotation.eulerAngles, new Vector3(0.1f, 0f, 0f));
//		DebugFn.arrow (way2Right, way2.transform.rotation.eulerAngles, new Vector3(0.1f, 0f, 0f));
//
//		DebugFn.square (intersectionPointBezier);

		// Intersection found, draw the bezier curve
		float bezierLength = Math3d.GetBezierLength(way2Right, intersectionPointBezier, way1Left);
		float numberOfPoints = bezierLength * WayHelper.BEZIER_RESOLUTION;
		
		float step = 1.0f / numberOfPoints;
		bool doBreak = false;
		for (float time = step; time < 1.0f + step; time += step) {
			if (time > 1f) {
				time = 1f;
				doBreak = true;
			}
			Vector3 bezierPoint = Math3d.GetVectorInBezierAtTime (time, way2Right, intersectionPointBezier, way1Left);
			meshPoints.Add (bezierPoint);
			if (doBreak) {
				break;
			}
		}

		return meshPoints;
	}

	static List<Vector3> getMeshPointsForNonComplex (List<WayReference> wayReferences, List<Bounds> wayBounds, Pos pos)
	{
		List<Vector3> meshPoints = new List<Vector3> ();

		// Add first one last as well, so we can iterate them as pairs
		wayReferences.Add (wayReferences [0]);
		wayBounds.Add (wayBounds [0]);
		
		// middle of intersection "pos"
		Vector3 intersectionPos = Game.getCameraPosition (pos);
		
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
				// Ways intersects, no bezier, instead take the intersection point along the border
				lastWaysIntersected = true;
				
				// Left pos of way1 and right pos of way2
				Vector3 way1Left = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, -way1.transform.localScale.y / 2f , 0f);
				Vector3 way2Right = intersectionPos + way2Rotation * new Vector3(way2.transform.localScale.y / 2f, way2.transform.localScale.y / 2f , 0f);
				
				// Angles looking towards intersection of ways
				Vector3 way1IntersectionAngle = (way1IsNode1 ? DEGREES_90 : DEGREES_270) + way1.transform.rotation.eulerAngles;
				Vector3 way2IntersectionAngle = (way2IsNode1 ? DEGREES_270 : DEGREES_90) + way2.transform.rotation.eulerAngles;
				
//				// TODO DEBUG ONLY
//				DebugFn.arrow (way1Left, way1IntersectionAngle, new Vector3(0.1f, 0f, 0f));
//				DebugFn.arrow (way2Right, way2IntersectionAngle, new Vector3(0.1f, 0f, 0f));
				
				// Get intersection point
				Vector3 intersectionPoint;
				bool intersectionFound = Math3d.LineLineIntersection(out intersectionPoint, way1Left, Quaternion.Euler(way1IntersectionAngle) * Vector3.right, way2Right, Quaternion.Euler(way2IntersectionAngle) * Vector3.right);  
				
//				DebugFn.square (intersectionPoint);
				
//				// TODO DEBUG ONLY
//				if (!intersectionFound) {
//					Debug.Log ("Intersection point not found");
//				}
				
				meshPoints.Add (intersectionPoint);
				
				if (i == wayReferences.Count - 2) {
					meshPoints.RemoveAt (0);
					meshPoints.Insert (0, meshPoints[meshPoints.Count-1]);
				}
			} else {
				// Add "right" point
				Vector3 right = intersectionPos + way1Rotation * new Vector3(way1.transform.localScale.y / 2f, way1.transform.localScale.y / 2f , 0f);
				meshPoints.Add(right);
				
				// "Left" point in next way
				Vector3 leftWay2 = intersectionPos + way2Rotation * new Vector3(way2.transform.localScale.y / 2f, -way2.transform.localScale.y / 2f , 0f);
				
				GetBezierPoints (meshPoints, way1, way2, right, leftWay2);
				
				
			}
		}

		meshPoints.RemoveAt (meshPoints.Count - 1);


		return meshPoints;
	}

	private static void GetBezierPoints (List<Vector3> meshPoints, WayReference way1, WayReference way2, Vector3 right, Vector3 leftWay2)
	{
		// Add bezier points between this ways "right" and next ways "left" point
		Vector3 intersectionPoint;
		bool intersectionFound = Math3d.LineLineIntersection (out intersectionPoint, right, way1.transform.rotation * Vector3.right, leftWay2, way2.transform.rotation * Vector3.right);
		if (!intersectionFound) {
			intersectionFound = Math3d.LineLineIntersection (out intersectionPoint, right, Quaternion.Euler (new Vector3 (0, 0, 180f) + way1.transform.rotation.eulerAngles) * Vector3.right, leftWay2, Quaternion.Euler (new Vector3 (0, 0, 180f) + way2.transform.rotation.eulerAngles) * Vector3.right);
		}
		if (intersectionFound) {
			// Intersection found, draw the bezier curve
			float bezierLength = Math3d.GetBezierLength (right, intersectionPoint, leftWay2);
			float numberOfPoints = bezierLength * WayHelper.BEZIER_RESOLUTION;
			float step = 1.0f / numberOfPoints;
			bool doBreak = false;
			for (float time = step; time < 1.0f + step; time += step) {
				if (time > 1f) {
					time = 1f;
					doBreak = true;
				}
				Vector3 bezierPoint = Math3d.GetVectorInBezierAtTime (time, right, intersectionPoint, leftWay2);
				meshPoints.Add (bezierPoint);
				if (doBreak) {
					break;
				}
			}
		}
		else {
			// No intersection found for way points, just draw a straight line
			meshPoints.Add (leftWay2);
		}
	}
}
