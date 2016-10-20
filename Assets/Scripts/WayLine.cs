using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WayLine : MonoBehaviour {

	private const float LINE_HEIGHT = 0.002f;

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

		if (reference.way.WayWidthFactor >= WayHelper.LIMIT_WAYWIDTH) {
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

    private static Material whiteMaterial = null;
	private static void SetWhiteMaterial (GameObject line) {
        MeshRenderer meshRenderer = line.GetComponent<MeshRenderer> ();
		if (whiteMaterial == null) {
            meshRenderer.material.color = new Color (1f, 1f, 1f);
            whiteMaterial = meshRenderer.material;
        } else {
            meshRenderer.material = whiteMaterial;
        }
	}

	public static void CreateCurved (GameObject parent, long key, List<WayReference> wayReferences) {
		// Make sure to only make dashed lines if the number of fields are the same in the way direction
		WayReference firstWayReference = wayReferences [0];
		WayReference secondWayReference = wayReferences [1];

		bool firstIsNode1 = firstWayReference.isNode1 (NodeIndex.getPosById (key));
		bool secondIsNode1 = secondWayReference.isNode1 (NodeIndex.getPosById (key));

		float firstFieldsTowardsPos = firstWayReference.getNumberOfFieldsInDirection(!firstIsNode1);
		float secondFieldsFromPos = secondWayReference.getNumberOfFieldsInDirection(secondIsNode1);

		float secondFieldsTowardsPos = secondWayReference.getNumberOfFieldsInDirection(!secondIsNode1);
		float firstFieldsFromPos = firstWayReference.getNumberOfFieldsInDirection(firstIsNode1);
		bool makeDashedLines = firstFieldsTowardsPos == secondFieldsFromPos && secondFieldsTowardsPos == firstFieldsFromPos;

		if (firstWayReference.way.WayWidthFactor >= WayHelper.LIMIT_WAYWIDTH && firstWayReference.way.CarWay) {
//			createOuterLines (reference);
			CreateCurvedMiddleLine (parent, key, wayReferences);
			if (makeDashedLines) {
				CreateCurvedDashedLines (parent, key, wayReferences);
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
		lineMiddle.name = "Curved middle line";
		WayLine.SetWhiteMaterial (lineMiddle);
		lineMiddle.transform.SetParent (parent.transform);
		lineMiddle.transform.localPosition = new Vector3(0, 0, -0.01f);
	}

	private static void CreateCurvedDashedLines (GameObject parent, long key, List<WayReference> wayReferences)
	{
		GameObject curveDashedLines = new GameObject ();
		curveDashedLines.name = "Curved dashed lines";
		curveDashedLines.transform.SetParent(parent.transform);
		curveDashedLines.transform.localPosition = Vector3.zero;

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
		
		// Number of fields in own direction
		float fieldsFromPos1 = firstReference.getNumberOfFieldsInDirection (true);
		
		// Number of fields in total
		float numberOfFields = firstReference.getNumberOfFields ();
		
		List<float> dashedLineFields = new List<float> ();
		for (float field = 1; field < fieldsFromPos2; field++) {
			dashedLineFields.Add (field);
		}
		
		for (float field = 1; field < fieldsFromPos1; field++) {
			dashedLineFields.Add (fieldsFromPos2 + field);
		}

		// If ways have same "isNode1", invert y-axis for second way
		bool isSameNode1 = firstIsNode1 == secondIsNode1;

		// Way width
		float wayHeight = wayFirst.transform.localScale.y;
		float wayHeightCompensated = wayHeight - GetLineHeight()*2f;
		foreach (float field in dashedLineFields) {
			GameObject curveDashes = new GameObject ();
			curveDashes.name = "Curved dashes";
			curveDashes.transform.SetParent(curveDashedLines.transform);
			curveDashes.transform.localPosition = Vector3.zero;

			// Percentual position of way width, where to put middle line
			float percentualPositionY = field / numberOfFields;

			// Get our points (center in y-axis)
			float yPositionInWay = percentualPositionY * wayHeightCompensated;
			float dashYMiddle = -wayHeightCompensated / 2f + GetLineHeight() + yPositionInWay - GetDashedLineHeight() / 2f;
			float dashYMiddleSameNode1 = wayHeightCompensated / 2f - yPositionInWay + GetDashedLineHeight() / 2f;

			Vector3 firstPosMiddle = posPosition + wayFirst.transform.rotation * new Vector3((firstIsNode1 ? 1 : -1) * wayHeight / 2f, dashYMiddle, 0);
			Vector3 secondPosMiddle = posPosition + waySecond.transform.rotation * new Vector3((secondIsNode1 ? 1 : -1) * wayHeight / 2f, isSameNode1 ? dashYMiddleSameNode1 : dashYMiddle, 0);

			Vector3 halfDashedLineHeight = new Vector3(0, GetDashedLineHeight() / 2f, 0);

			// Get our points (top in y-axis)
			Vector3 wayFirstHalfDashedHeight = wayFirst.transform.rotation * halfDashedLineHeight;
			Vector3 waySecondHalfDashedHeight = waySecond.transform.rotation * halfDashedLineHeight;
			Vector3 firstPosTop = firstPosMiddle - wayFirstHalfDashedHeight;
			Vector3 secondPosTop = secondPosMiddle + (isSameNode1 ? 1 : -1) * waySecondHalfDashedHeight;

			// Get our points (bottom in y-axis)
			Vector3 firstPosBottom = firstPosMiddle + wayFirstHalfDashedHeight;
			Vector3 secondPosBottom = secondPosMiddle + (isSameNode1 ? -1 : 1) *  waySecondHalfDashedHeight;

			Quaternion firstRotation = firstIsNode1 ? WayHelper.ONEEIGHTY_DEGREES * wayFirst.transform.rotation : wayFirst.transform.rotation;
			Quaternion secondRotation = secondIsNode1 ? WayHelper.ONEEIGHTY_DEGREES * waySecond.transform.rotation : waySecond.transform.rotation;
			Vector3 firstDirection = firstRotation * Vector3.right;
			Vector3 secondDirection = secondRotation * Vector3.right;

			Vector3 intersectionPoint;
			Vector3 intersectionPointTop;
			Vector3 intersectionPointBottom;
			bool intersectionFound = Math3d.LineLineIntersection (out intersectionPoint, firstPosMiddle, firstDirection, secondPosMiddle, secondDirection);
			if (!intersectionFound && firstRotation.eulerAngles.z == secondRotation.eulerAngles.z) {
				intersectionFound = true;
				intersectionPoint = firstPosMiddle + ((secondPosMiddle - firstPosMiddle) / 2);
				intersectionPointTop = firstPosTop + ((secondPosTop - firstPosTop) / 2);
				intersectionPointBottom = firstPosBottom + ((secondPosBottom - firstPosBottom) / 2);
			} else {
				Math3d.LineLineIntersection (out intersectionPointTop, firstPosTop, firstDirection, secondPosTop, secondDirection);
				Math3d.LineLineIntersection (out intersectionPointBottom, firstPosBottom, firstDirection, secondPosBottom, secondDirection);

			}

			// TODO - Shouldn't be needed - debug only
			if (!intersectionFound) {
				Debug.Log ("ERR: " + key);
				return;
			}

			// 1. Get bezier length for curve
			float bezierLength = Math3d.GetBezierLength (firstPosMiddle, intersectionPoint, secondPosMiddle);
			
			// 2. Decide how many dashes to fit, with gaps (also calculate each dash and gap length)
			// If only one line
			float numberOfLines = 1f; 
			float dashedLineWidth = bezierLength;
			float dashedLineGap = 0f;
			// If more lines
			if (bezierLength > DASHED_LINE_WIDTH + CITY_DASHED_LINE_GAP) {
				float totalWidth = 0f;
				for ( numberOfLines = 2f ;  ; numberOfLines++) {
					totalWidth = DASHED_LINE_WIDTH + (DASHED_LINE_WIDTH + CITY_DASHED_LINE_GAP) * (numberOfLines - 1);
					if (totalWidth >= bezierLength) {
						break;
					}
				}
				dashedLineWidth = DASHED_LINE_WIDTH * bezierLength / totalWidth;
				dashedLineGap = CITY_DASHED_LINE_GAP * bezierLength / totalWidth;
			}

			
			// 3. Calculate each dash along the line t (time) on bezier curve 
			List<KeyValuePair<float, float>> dashTimes = new List<KeyValuePair<float, float>> ();
			if (numberOfLines == 1f) {
				dashTimes.Add(new KeyValuePair<float, float>(0f, 1f));
			} else {
				dashTimes.Add(new KeyValuePair<float, float>(0f, dashedLineWidth / bezierLength));
				for (float lineStart = dashedLineWidth + dashedLineGap; lineStart < bezierLength; lineStart += dashedLineWidth + dashedLineGap) {
					float lineStartTime = lineStart / bezierLength;
					dashTimes.Add(new KeyValuePair<float, float>(lineStartTime, lineStartTime + dashedLineWidth / bezierLength));
				}
			}

			foreach (KeyValuePair<float, float> dashTime in dashTimes) {
				float startTime = dashTime.Key;
				float endTime = dashTime.Value;
				float dashLengthPercent = endTime - startTime;
				float numberOfPoints = Mathf.Max(bezierLength / dashLengthPercent * WayHelper.BEZIER_RESOLUTION, 4f);
				float eachPointTime = dashLengthPercent / numberOfPoints;

				List<Vector3> dashPoints = new List<Vector3>();

				// Top line
				for (float t = startTime; t <= endTime; t += eachPointTime) {
					dashPoints.Add (Math3d.GetVectorInBezierAtTime (t, firstPosTop, intersectionPointTop, secondPosTop));
				}

				// Bottom line
				for (float t = endTime; t >= startTime; t -= eachPointTime) {
					dashPoints.Add (Math3d.GetVectorInBezierAtTime (t, firstPosBottom, intersectionPointBottom, secondPosBottom));
				}

				GameObject lineMiddle = MapSurface.createPlaneMeshForPoints (dashPoints);
				lineMiddle.name = "Curved dash";
				WayLine.SetWhiteMaterial (lineMiddle);
				lineMiddle.transform.SetParent (curveDashes.transform);
				lineMiddle.transform.localPosition = new Vector3(0, 0, -0.01f);
			}
		}
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
