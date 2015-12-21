using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Points : MonoBehaviour, IPubSub {

	private int points = 0;
	private int showNumbers = 6;

	private float charWidth = 0.30f;

	public GameObject digit;
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
		GameObject numberObj = Instantiate (digit);
		TextMesh text = numberObj.GetComponent <TextMesh> ();
		text.text = "" + value;
		numberObj.AddComponent <PointDigit> ();
		numberObj.transform.SetParent (transform);
		numberObj.transform.localPosition = new Vector3 (-charWidth * position, 0f, 0f);
		parts.Add (numberObj);
	}

	void updatePart (int position, int value) {
		parts [position].GetComponent<PointDigit> ().setDigit (value);
	}

	void destroyPart (int i) {
		parts [i].GetComponent<PointDigit> ().remove ();
		parts.RemoveAt (i);
	}

	#region IPubSub implementation
	public void onMessage (string message, object data)
	{
		if (message == "points:clear") {
			points = 0;
			return;
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
	}
	#endregion
}
