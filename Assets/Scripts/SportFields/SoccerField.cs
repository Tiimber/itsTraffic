using UnityEngine;
using System.Collections;

public class SoccerField : MonoBehaviour {

	private const float LINE_THICKNESS = 0.03f;

	public Vector3 center;
	public float rotation;
	public float width;
	public float height;

	// Use this for initialization
	void Start () {
		// Grab props from MapSurface
		MapSurface mapSurface = GetComponent<MapSurface>();
		center = mapSurface.calculatedCenter;
		rotation = mapSurface.calculatedRotation;
		width = mapSurface.calculatedWidth;
		height = mapSurface.calculatedHeight;

		// TODO - Doesn't really work right now - continue this later with a fresh mind...
		// createLines ();
	}

	private void createLines () {
		GameObject linesParent = new GameObject("Lines");
		linesParent.transform.parent = transform;
		linesParent.transform.localPosition = center + new Vector3(0f, 0f, -0.01f);

		// Side lines
		Vector3 sideLineOuter = new Vector3(LINE_THICKNESS, 0f, 0f);
		Vector3 sideLineInner = new Vector3(width - LINE_THICKNESS, LINE_THICKNESS, 0f);
		GameObject leftLine = MapSurface.createPlaneMeshForPoints(sideLineOuter, sideLineInner);
		GameObject rightLine = MapSurface.createPlaneMeshForPoints(sideLineOuter, sideLineInner);

		// Use WayLine for setting white material
		WayLine.SetWhiteMaterial(leftLine);
		WayLine.SetWhiteMaterial(rightLine);

		// Locate left line correctly
		leftLine.name = "Left outer";
		leftLine.transform.parent = linesParent.transform;
		leftLine.transform.localPosition = (Quaternion.Euler(0f, 0f, rotation) * new Vector3(-LINE_THICKNESS, -height / 2f + LINE_THICKNESS * 1.5f, 0f));
		leftLine.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

		// Locate right line correctly
		rightLine.name = "Right outer";
		rightLine.transform.parent = linesParent.transform;
		rightLine.transform.localPosition = (Quaternion.Euler(0f, 0f, rotation) * new Vector3(LINE_THICKNESS, height / 2f - LINE_THICKNESS * 1.5f, 0f));
		rightLine.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

		// Goal lines
		Vector3 goalLineOuter = new Vector3(0f, LINE_THICKNESS, 0f);
		Vector3 goalLineInner = new Vector3(LINE_THICKNESS, height - LINE_THICKNESS, 0f);
		GameObject goalLineClose = MapSurface.createPlaneMeshForPoints(goalLineOuter, goalLineInner);
		GameObject goalLineFar = MapSurface.createPlaneMeshForPoints(goalLineOuter, goalLineInner);

		// Use WayLine for setting white material
		WayLine.SetWhiteMaterial(goalLineClose);
		WayLine.SetWhiteMaterial(goalLineFar);

		// Locate close goal line correctly
		goalLineClose.name = "Close goal line";
		goalLineClose.transform.parent = linesParent.transform;
		goalLineClose.transform.localPosition = (Quaternion.Euler(0f, 0f, rotation) * new Vector3(-width / 2f + LINE_THICKNESS * 1.5f, -LINE_THICKNESS, 0f));
		goalLineClose.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

		// Locate far goal line correctly
		goalLineFar.name = "Far goal line";
		goalLineFar.transform.parent = linesParent.transform;
		goalLineFar.transform.localPosition = (Quaternion.Euler(0f, 0f, rotation) * new Vector3(width / 2f - LINE_THICKNESS * 1.5f, LINE_THICKNESS, 0f));
		goalLineFar.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
