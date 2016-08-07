using UnityEngine;
using System.Collections;

public class ShirtLogic : MonoBehaviour {

	[InspectorButton("OnButtonClicked")]
	public bool debugPrint;

	private void OnButtonClicked() {
		setShirtColor ();
	}

	// Use this for initialization
	void Start () {
		setShirtColor ();
	}

	private void setShirtColor() {
		Color shirtColor;

		Setup.PersonSetup personality = GetComponentInParent<HumanLogic> ().personality;
		if (personality != null && personality.shirtColor != null) {
			shirtColor = Misc.parseColor (personality.shirtColor);
		} else {
			float r = (float) HumanLogic.HumanRNG.NextDouble ();
			float g = (float) HumanLogic.HumanRNG.NextDouble ();
			float b = (float) HumanLogic.HumanRNG.NextDouble ();
			shirtColor = new Color (r, g, b, 0f);
		}
		Renderer renderer = GetComponent<Renderer> ();
		renderer.material.SetColor ("_Color", shirtColor);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
