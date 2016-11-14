using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Points3D : MonoBehaviour, IPubSub {

	public int points = 0;
	private int showNumbers = 6;

	private float charWidth = 0.30f;

	public List<GameObject> digits = new List<GameObject> ();
	public List<GameObject> parts = new List<GameObject> ();

	// Use this for initialization
	void Start () {
		PubSub.subscribe ("points:inc", this);
		PubSub.subscribe ("points:dec", this);
		PubSub.subscribe ("points:set", this);
		PubSub.subscribe ("points:clear", this);

		pointsUpdated ();
	}
	
	// Update is called once per frame
	void Update () {

	}
	
	private void pointsUpdated() {
		List<int> numberValues = getNumberValues ();
		updatePointObjects (numberValues);
	}

	private List<int> getNumberValues () {
		string pointsString = "" + points;
		while (pointsString.Length < showNumbers) {
			pointsString = "0" + pointsString;
		}
		int length = pointsString.Length;
	    List<int> numberValues = new List<int> (length);
		for (int i = 1; i <= length; i++) {
			numberValues.Add (int.Parse ("" + pointsString [length - i]));
		}
		return numberValues;
	}

	private void updatePointObjects (List<int> numberValues) {
		for (int i = 0; i < numberValues.Count; i++) {
			setNumberAtSlot (i, numberValues[i]);
		}
		while (parts.Count > numberValues.Count) {
			destroyPart (parts.Count - 1);
		}
	}

	private void setNumberAtSlot (int position, int value) {
		if (parts.Count > position) {
			updatePart (position, value);
		} else {
			createPart (position, value);
		}
	}

	void createPart (int position, int value) {
        GameObject numberObj = new GameObject("Digit3D_" + position);
		numberObj.AddComponent <PointDigit> ();
		numberObj.transform.SetParent (transform);
		numberObj.transform.localPosition = new Vector3 (-charWidth * position, 0f, 0f);
		parts.Add (numberObj);

		GameObject number3DObj = Instantiate (digits[value], numberObj.transform, false) as GameObject;
        number3DObj.transform.localPosition = Vector3.zero;
        number3DObj.transform.localScale = PointDigit.NumberScale;
	}

	void updatePart (int position, int value) {
		parts [position].GetComponent<PointDigit> ().setDigit (digits[value]);
	}

	void destroyPart (int i) {
		parts [i].GetComponent<PointDigit> ().remove ();
		parts.RemoveAt (i);
	}

	#region IPubSub implementation
	public PROPAGATION onMessage (string message, object data)
	{
		if (message == "points:clear") {
			points = 0;
            pointsUpdated ();
			return PROPAGATION.DEFAULT;
		}

		int number = (int)data;
		switch (message) {
			case "points:inc":
				points += number;
				break;
			case "points:dec":
				points -= number;
				break;
			case "points:set":
				points = number;
				break;
			default:
				break;
		}
		pointsUpdated ();
		return PROPAGATION.DEFAULT;
	}
	#endregion
}
