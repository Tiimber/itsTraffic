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
		setWhiteMaterial (lineUpper);
		lineUpper.transform.SetParent (transform);
		lineUpper.transform.localPosition = new Vector3(0, -wayHeight/2f, -0.01f);

		GameObject lineLower = getLineForWay (drawSize);
		lineLower.name = "Line border 2";
		setWhiteMaterial (lineLower);
		lineLower.transform.SetParent (transform);
		lineLower.transform.localPosition = new Vector3(0, wayHeight/2f - LINE_HEIGHT * Settings.currentMapWidthFactor, -0.01f);
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
		float wayHeight = way.transform.localScale.y - LINE_HEIGHT*2f;


		GameObject lineMiddle = getLineForWay (drawSize);
		lineMiddle.name = "Middle line";
		setWhiteMaterial (lineMiddle);
		lineMiddle.transform.SetParent (transform);
		lineMiddle.transform.localPosition = new Vector3(0, LINE_HEIGHT + percentualPositionY * wayHeight - wayHeight / 2f - LINE_HEIGHT / 2f * Settings.currentMapWidthFactor, -0.01f);
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
		float wayHeight = way.transform.localScale.y - LINE_HEIGHT*2f;
		foreach (float field in dashedLineFields) {
			// Percentual position of way width, where to put middle line
			float percentualPositionY = field / numberOfFields;

			GameObject lineMiddle = getDashedLineForWay (drawSize);
			lineMiddle.transform.SetParent (transform);
			lineMiddle.transform.localPosition = new Vector3(0f, LINE_HEIGHT + percentualPositionY * wayHeight - wayHeight / 2f - DASHED_LINE_HEIGHT / 2f * Settings.currentMapWidthFactor, -0.01f);
		}

	}

	private GameObject getDashedLineForWay (Vector3 way) {
		GameObject dashedLine = new GameObject ();
		dashedLine.name = "Dashed line";
		for (float xStart = 0; xStart <= way.x; xStart += DASHED_LINE_WIDTH + CITY_DASHED_LINE_GAP) {
			Vector3 lineVector = new Vector3(DASHED_LINE_WIDTH, way.y, way.z);
			GameObject linePart = getLineForWay(lineVector, DASHED_LINE_HEIGHT);
			linePart.name = "Dash";

			setWhiteMaterial (linePart);
			linePart.transform.SetParent (dashedLine.transform);
			linePart.transform.localPosition = new Vector3(xStart - way.x / 2f, 0f, 0f);
		}
		return dashedLine;
	}

	private GameObject getLineForWay (Vector3 way, float lineHeight = LINE_HEIGHT) {
		Vector3 fromPos = -way / 2;
		Vector3 toPos = way / 2;
		
		toPos = new Vector3(toPos.x, fromPos.y + lineHeight * Settings.currentMapWidthFactor, toPos.z); 
		
		return MapSurface.createPlaneMeshForPoints (fromPos, toPos);
	}

	private void setWhiteMaterial (GameObject line) {
		MeshRenderer meshRenderer = line.GetComponent<MeshRenderer> ();
		meshRenderer.material.color = new Color (1f, 1f, 1f);
	}
}
