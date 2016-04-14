using UnityEngine;
using System.Collections;

public class ShirtLogic : MonoBehaviour {

	private System.Random random;

	[InspectorButton("OnButtonClicked")]
	public bool debugPrint;

	private void OnButtonClicked() {
		setShirtColor ();
	}

	// Use this for initialization
	void Start () {
		random = new System.Random ((int)Game.randomSeed);
		setShirtColor ();
	}

	private void setShirtColor() {
		float r = (float) random.NextDouble ();
		float g = (float) random.NextDouble ();
		float b = (float) random.NextDouble ();
		Color shirtColor = new Color (r, g, b);
		Renderer renderer = GetComponent<Renderer> ();
		renderer.material.SetColor ("_Color", shirtColor);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
