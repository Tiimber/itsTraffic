using System;
using UnityEngine;
using System.Collections.Generic;

public class WayCrossing {
	public static void Create (GameObject parent, long key, List<WayReference> wayReferences) {
		WayReference firstWayReference = wayReferences[0];
		WayReference secondWayReference = wayReferences[1];
		Quaternion firstWayRotation = firstWayReference.transform.rotation;
		float crossingAngle = (secondWayReference.transform.rotation.eulerAngles.z - firstWayRotation.eulerAngles.z) / 2f;
		Vector3 crossingRotationVector = new Vector3 (0f, 0f, firstWayRotation.eulerAngles.z + crossingAngle);
		Quaternion crossingRotation = Quaternion.Euler (crossingRotationVector);
		Quaternion orthoCrossingRotation = Quaternion.Euler (crossingRotationVector + WayHelper.DEGREES_90_VECTOR);
		Pos pos = NodeIndex.getPosById (key);
		GameObject crossingLine = Game.instance.wayCrossing;
		float lineHeight = crossingLine.transform.localScale.y;
		float wayScale = Game.instance.partOfWay.transform.localScale.y * Settings.currentMapWidthFactor;
		float lineWidth = crossingLine.transform.localScale.x * Settings.currentMapWidthFactor * firstWayReference.way.WayWidthFactor;
		float wayHeight = firstWayReference.way.WayWidthFactor * wayScale;
		Vector3 startPosition = Game.getCameraPosition (pos) - orthoCrossingRotation * new Vector3 (wayHeight / 2f, 0, 0) + crossingLine.transform.position;
		float numberOfCrossingLines = Mathf.Floor ((wayHeight - WayHelper.CROSSING_LINE_PERCENTAGE * lineHeight) / (lineHeight + WayHelper.CROSSING_LINE_PERCENTAGE * lineHeight));
		float spaceHeight = (wayHeight - numberOfCrossingLines * lineHeight) / (numberOfCrossingLines + 1);
		float stepLength = spaceHeight + lineHeight;
		for (int i = 1; i <= numberOfCrossingLines; i++) {
			Vector3 lineCenter = startPosition + orthoCrossingRotation * new Vector3 (i * stepLength - lineHeight / 2f, 0, -0.01f);
			GameObject line = MonoBehaviour.Instantiate (crossingLine, lineCenter, crossingRotation) as GameObject;
			line.transform.localScale = new Vector3 (lineWidth, crossingLine.transform.localScale.y, crossingLine.transform.localScale.z);
			line.transform.SetParent(parent.transform);
		}
	}
}

