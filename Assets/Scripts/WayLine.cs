using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WayLine : MonoBehaviour {

	private const float LINE_HEIGHT = 0.002f;
	private static float LIMIT_WAYWIDTH = WayTypeEnum.PEDESTRIAN;

	private static float DASHED_LINE_WIDTH = 0.05f;
	private static float CITY_DASHED_LINE_GAP = 0.04f;
	private static float DASHED_LINE_HEIGHT = 0.001f;

	private Vector3 drawSize;

	private static float GetLineHeight() {
		return LINE_HEIGHT * Settings.currentMapWidthFactor;
	}

	private static float GetDashedLineHeight() {
		return DASHED_LINE_HEIGHT * Settings.currentMapWidthFactor;
	}

	public void create (WayReference reference) {

		if (reference.way.WayWidthFactor >= LIMIT_WAYWIDTH) {
			drawSize = reference.gameObject.transform.localScale;
			drawSize -= new Vector3(drawSize.y, 0f, 0f);

//			createOuterLines (reference);
			createMiddleLine (reference);
			createDashedLines (reference);

			drawSize = Vector3.zero;
		}
	}

	private void createOuterLines (WayReference reference) {
		GameObject way = reference.gameObject;
		float wayHeight = way.transform.localScale.y;

		GameObject lineUpper = getLineForWay (drawSize);
		lineUpper.name = "Line border 1";
		WayLine.SetWhiteMaterial (lineUpper);
		lineUpper.transform.SetParent (transform);
		lineUpper.transform.localPosition = new Vector3(0, -wayHeight/2f, -0.01f);

		GameObject lineLower = getLineForWay (drawSize);
		lineLower.name = "Line border 2";
		WayLine.SetWhiteMaterial (lineLower);
		lineLower.transform.SetParent (transform);
		lineLower.transform.localPosition = new Vector3(0, wayHeight/2f - GetLineHeight(), -0.01f);
	}

	private void createMiddleLine (WayReference reference) {
		// Number of fields in opposite direction
		float fieldsFromPos2 = reference.getNumberOfFieldsInDirection (false);

		// Number of fields in total
		float numberOfFields = reference.getNumberOfFields ();

		// Percentual position of way width, where to put middle line
		float percentualPositionY = fieldsFromPos2 / numberOfFields;

		// Way width
		GameObject way = reference.gameObject;
		float wayHeight = way.transform.localScale.y - GetLineHeight()*2f;


		GameObject lineMiddle = getLineForWay (drawSize);
		lineMiddle.name = "Middle line";
		WayLine.SetWhiteMaterial (lineMiddle);
		lineMiddle.transform.SetParent (transform);
		lineMiddle.transform.localPosition = new Vector3(0, GetLineHeight() + percentualPositionY * wayHeight - wayHeight / 2f - GetLineHeight() / 2f, -0.01f);
	}

	private void createDashedLines (WayReference reference)
	{
		// Number of fields in opposite direction
		float fieldsFromPos2 = reference.getNumberOfFieldsInDirection (false);

		// Number of fields in own direction
		float fieldsFromPos1 = reference.getNumberOfFieldsInDirection (true);

		// Number of fields in total
		float numberOfFields = reference.getNumberOfFields ();

		List<float> dashedLineFields = new List<float> ();
		for (float field = 1; field < fieldsFromPos2; field++) {
			dashedLineFields.Add (field);
		}

		for (float field = 1; field < fieldsFromPos1; field++) {
			dashedLineFields.Add (fieldsFromPos2 + field);
		}

		// Way width
		GameObject way = reference.gameObject;
		float wayHeight = way.transform.localScale.y - GetLineHeight()*2f;
		foreach (float field in dashedLineFields) {
			// Percentual position of way width, where to put middle line
			float percentualPositionY = field / numberOfFields;

			GameObject lineMiddle = getDashedLineForWay (drawSize);
			lineMiddle.transform.SetParent (transform);
			lineMiddle.transform.localPosition = new Vector3(0f, GetLineHeight() + percentualPositionY * wayHeight - wayHeight / 2f - GetDashedLineHeight() / 2f, -0.01f);
		}

	}

	private GameObject getDashedLineForWay (Vector3 way) {
		GameObject dashedLine = new GameObject ();
		dashedLine.name = "Dashed line";
		for (float xStart = CITY_DASHED_LINE_GAP; xStart <= way.x - DASHED_LINE_WIDTH; xStart += DASHED_LINE_WIDTH + CITY_DASHED_LINE_GAP) {
			Vector3 lineVector = new Vector3(DASHED_LINE_WIDTH, way.y, way.z);
			GameObject linePart = getLineForWay(lineVector, GetDashedLineHeight());
			linePart.name = "Dash";

			WayLine.SetWhiteMaterial (linePart);
			linePart.transform.SetParent (dashedLine.transform);
			linePart.transform.localPosition = new Vector3(xStart - way.x / 2f, 0f, 0f);
		}
		return dashedLine;
	}

	private GameObject getLineForWay (Vector3 way, float lineHeight = -1f) {
		Vector3 fromPos = -way / 2;
		Vector3 toPos = way / 2;

		if (lineHeight == -1f) {
			lineHeight = GetLineHeight();
		}
		
		toPos = new Vector3(toPos.x, fromPos.y + lineHeight, toPos.z); 
		
		return MapSurface.createPlaneMeshForPoints (fromPos, toPos);
	}

	private static void SetWhiteMaterial (GameObject line) {
		MeshRenderer meshRenderer = line.GetComponent<MeshRenderer> ();
		meshRenderer.material.color = new Color (1f, 1f, 1f);
	}

	public static void CreateCurved (GameObject parent, long key, List<WayReference> wayReferences) {
		WayReference firstReference = wayReferences [0];
	
		// Make sure to only make dashed lines if the number of fields are the same in the way direction
		WayReference firstWayReference = wayReferences [0];
		WayReference secondWayReference = wayReferences [1];
		bool makeDashedLines = firstWayReference.getNumberOfFieldsInDirection(true) == secondWayReference.getNumberOfFieldsInDirection(false);
//		Debug.Log (makeDashedLines);

		if (firstReference.way.WayWidthFactor >= LIMIT_WAYWIDTH && firstReference.way.CarWay) {
//			createOuterLines (reference);
			CreateCurvedMiddleLine (parent, key, wayReferences);
			if (makeDashedLines) {
//				createDashedLines (reference);
			}
		}
	}

	private static void CreateCurvedMiddleLine (GameObject parent, long key, List<WayReference> wayReferences) {

		Pos centerPos = NodeIndex.getPosById (key);
		Vector3 posPosition = Game.getCameraPosition (centerPos);

		WayReference firstReference = wayReferences [0];
		WayReference secondReference = wayReferences [1];

		bool firstIsNode1 = firstReference.isNode1 (centerPos);
		bool secondIsNode1 = secondReference.isNode1 (centerPos);

		GameObject wayFirst = firstReference.gameObject;
		GameObject waySecond = secondReference.gameObject;

		// Number of fields in opposite direction
		float fieldsFromPos2 = firstReference.getNumberOfFieldsInDirection (false);
		
		// Number of fields in total
		float numberOfFields = firstReference.getNumberOfFields ();
		
		// Percentual position of way width, where to put middle line
		float percentualPositionY = fieldsFromPos2 / numberOfFields;

		// Way width
		float wayHeight = wayFirst.transform.localScale.y - GetLineHeight()*2f;

		// TODO - What if the wayReference is REALLY short
		Vector3 wayFirstSize = wayFirst.transform.localScale;
		Quaternion wayFirstRotation = wayFirst.transform.rotation;
		float wayFirstWayWidth = (firstIsNode1 ? 1 : -1) * wayFirstSize.y;
		float lineFirstPositionAdjustment = percentualPositionY * wayHeight;
		Vector3 wayFirstMiddleLineTopPos = posPosition + wayFirstRotation * new Vector3 (wayFirstWayWidth / 2f, - wayHeight / 2f + lineFirstPositionAdjustment, 0f);
		Vector3 wayFirstMiddleLineBottomPos = posPosition + wayFirstRotation * new Vector3 (wayFirstWayWidth / 2f, - wayHeight / 2f + GetLineHeight() + lineFirstPositionAdjustment, 0f);

		Vector3 waySecondSize = waySecond.transform.localScale;
		Quaternion waySecondRotation = waySecond.transform.rotation;
		float waySecondWayWidth = (secondIsNode1 ? 1 : -1) * waySecondSize.y;
		float lineSecondPositionAdjustment = percentualPositionY * wayHeight;
		Vector3 waySecondMiddleLineTopPos = posPosition + waySecondRotation * new Vector3 (waySecondWayWidth / 2f, - wayHeight / 2f + lineSecondPositionAdjustment, 0f);
		Vector3 waySecondMiddleLineBottomPos = posPosition + waySecondRotation * new Vector3 (waySecondWayWidth / 2f, - wayHeight / 2f + GetLineHeight() + lineSecondPositionAdjustment, 0f);

		if (IsTopAndBottomCrossing(wayFirstMiddleLineTopPos, waySecondMiddleLineTopPos, wayFirstMiddleLineBottomPos, waySecondMiddleLineBottomPos)) {
			Vector3 tmp = wayFirstMiddleLineBottomPos;
			wayFirstMiddleLineBottomPos = wayFirstMiddleLineTopPos;
			wayFirstMiddleLineTopPos = tmp;
		}

//		DebugFn.square (wayFirstMiddleLineTopPos);
//		DebugFn.square (wayFirstMiddleLineBottomPos);
//		DebugFn.square (waySecondMiddleLineTopPos);
//		DebugFn.square (waySecondMiddleLineBottomPos);

		List<Vector3> linePoints = new List<Vector3> ();
		linePoints.Add (wayFirstMiddleLineTopPos);
		AddBezierPoints (linePoints, wayFirstMiddleLineTopPos, wayFirstRotation, waySecondMiddleLineTopPos, waySecondRotation);
		linePoints.Add (waySecondMiddleLineBottomPos);
		AddBezierPoints (linePoints, waySecondMiddleLineBottomPos, waySecondRotation, wayFirstMiddleLineBottomPos, wayFirstRotation);

		GameObject lineMiddle = MapSurface.createPlaneMeshForPoints (linePoints);
		lineMiddle.name = "Middle line";
		WayLine.SetWhiteMaterial (lineMiddle);
		lineMiddle.transform.SetParent (parent.transform);
		lineMiddle.transform.localPosition = new Vector3(0, 0, -0.01f);

	}

	private static void AddBezierPoints (List<Vector3> meshPoints, Vector3 start, Quaternion startRotation, Vector3 end, Quaternion endRotation)
	{
		// Add bezier points between "start" and "end"
		Vector3 intersectionPoint;
		bool intersectionFound = Math3d.LineLineIntersection (out intersectionPoint, start, startRotation * Vector3.right, end, endRotation * Vector3.right);
		if (!intersectionFound) {
			intersectionFound = Math3d.LineLineIntersection (out intersectionPoint, start, Quaternion.Euler (new Vector3 (0, 0, 180f) + startRotation.eulerAngles) * Vector3.right, end, Quaternion.Euler (new Vector3 (0, 0, 180f) + endRotation.eulerAngles) * Vector3.right);
		}
		if (intersectionFound) {
			// Intersection found, draw the bezier curve
			float bezierLength = Math3d.GetBezierLength (start, intersectionPoint, end);
			float numberOfPoints = bezierLength * WayHelper.BEZIER_RESOLUTION;
			float step = 1.0f / numberOfPoints;
			bool doBreak = false;
			for (float time = step; time < 1.0f + step; time += step) {
				if (time > 1f) {
					time = 1f;
					doBreak = true;
				}
				Vector3 bezierPoint = Math3d.GetVectorInBezierAtTime (time, start, intersectionPoint, end);
				meshPoints.Add (bezierPoint);
				if (doBreak) {
					break;
				}
			}
		}
		else {
			// No intersection found for way points, just draw a straight line
			meshPoints.Add (end);
		}
	}

	private static bool IsTopAndBottomCrossing (Vector3 wayFirstMiddleLineTopPos, Vector3 waySecondMiddleLineTopPos, Vector3 wayFirstMiddleLineBottomPos, Vector3 waySecondMiddleLineBottomPos)
	{
		Vector3 topVector = waySecondMiddleLineTopPos - wayFirstMiddleLineTopPos;
		Vector3 bottomVector = waySecondMiddleLineBottomPos - wayFirstMiddleLineBottomPos;
		Vector3 intersectionPoint = Vector3.zero;
		return Math3d.LineLineIntersection (out intersectionPoint, wayFirstMiddleLineTopPos, topVector, wayFirstMiddleLineBottomPos, bottomVector);
	}

	private static bool IsAnyEqual (Vector3 origin, params Vector3[] points)
	{
		foreach (Vector3 point in points) {
			if (point == origin) {
				return true;
			}
		}
		return false;
	}
}
