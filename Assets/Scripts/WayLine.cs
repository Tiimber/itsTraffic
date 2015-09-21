using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WayLine : MonoBehaviour {

	private static float LINE_WIDTH = 0.002f;
	private static float LIMIT_WAYWIDTH = WayTypeEnum.PEDESTRIAN;

	public void create (WayReference reference) {
		if (reference.way.WayWidthFactor >= LIMIT_WAYWIDTH) {
			createOuterLines (reference);
			createMiddleLine (reference);
			createDashedLines (reference);
		}
	}

	private void createOuterLines (WayReference reference) {
		GameObject way = reference.gameObject;
		float wayHeight = way.transform.localScale.y;

		GameObject lineUpper = getLineForWay (way);
		setWhiteMaterial (lineUpper);
		lineUpper.transform.SetParent (transform);
		lineUpper.transform.localPosition = new Vector3(0, -wayHeight/2f, -0.01f);

		GameObject lineLower = getLineForWay (way);
		setWhiteMaterial (lineLower);
		lineLower.transform.SetParent (transform);
		lineLower.transform.localPosition = new Vector3(0, wayHeight/2f - LINE_WIDTH * Settings.currentMapWidthFactor, -0.01f);
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
		float wayHeight = way.transform.localScale.y - LINE_WIDTH*2f;


		GameObject lineMiddle = getLineForWay (way);
		setWhiteMaterial (lineMiddle);
		lineMiddle.transform.SetParent (transform);
		lineMiddle.transform.localPosition = new Vector3(0, LINE_WIDTH + percentualPositionY * wayHeight - wayHeight / 2f - LINE_WIDTH / 2f * Settings.currentMapWidthFactor, -0.01f);
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
		float wayHeight = way.transform.localScale.y - LINE_WIDTH*2f;
		foreach (float field in dashedLineFields) {
			// Percentual position of way width, where to put middle line
			float percentualPositionY = field / numberOfFields;

			GameObject lineMiddle = getLineForWay (way);
			setWhiteMaterial (lineMiddle);
			lineMiddle.transform.SetParent (transform);
			lineMiddle.transform.localPosition = new Vector3(0, LINE_WIDTH + percentualPositionY * wayHeight - wayHeight / 2f - LINE_WIDTH / 2f * Settings.currentMapWidthFactor, -0.01f);
		}

	}

	private GameObject getLineForWay (GameObject way, bool dashed = false) {
		Vector3 fromPos = -way.transform.localScale / 2;
		Vector3 toPos = way.transform.localScale / 2;
		
		toPos = new Vector3(toPos.x, fromPos.y + LINE_WIDTH * Settings.currentMapWidthFactor, toPos.z); 
		
		return MapSurface.createPlaneMeshForPoints (fromPos, toPos);
	}

	private void setWhiteMaterial (GameObject line) {
		MeshRenderer meshRenderer = line.GetComponent<MeshRenderer> ();
		meshRenderer.material.color = new Color (1f, 1f, 1f);
	}
}
