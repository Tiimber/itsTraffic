using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SkinLogic : MonoBehaviour {

	private Dictionary<Color, float> skinColors = new Dictionary<Color, float> {
		{new Color(1f, 0.91f, 0.66f), 0.7f},     // "white"
		{new Color(0.62f, 0.47f, 0.2f), 0.2f},   // "brown"
		{new Color(0.95f, 0.89f, 0.6f), 0.1f},   // "yellow"
		{new Color(0.25f, 0.13f, 0.08f), 0.05f}  // "black"
	};

	private float totalRange = 0f;

	[InspectorButton("OnButtonClicked")]
	public bool debugPrint;

	private void OnButtonClicked() {
		setSkinColor ();
	}

	// Use this for initialization
	void Start () {
		foreach (float value in skinColors.Values) {
			totalRange += value;
		}
		setSkinColor ();
	}

	private Color getFirstColor() {
		return skinColors.Keys.First ();
	}

	private Color getBaseSkinColor () {
		Color baseColor = getFirstColor();
		float randomVal = ((float) HumanLogic.HumanRNG.NextDouble ()) * totalRange;
		foreach (KeyValuePair<Color, float> color in skinColors) {
			baseColor = color.Key;
			randomVal -= color.Value;
			if (randomVal <= 0f) {
				break;
			}
		}
		return baseColor;
	}

	private void setSkinColor() {
		Color baseColor = getBaseSkinColor ();

		float r = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;
		float g = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;
		float b = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;

		float skinRed = Mathf.Clamp(baseColor.r + r, 0f, 1f);
		float skinGreen = Mathf.Clamp(baseColor.g + g, 0f, 1f);
		float skinBlue = Mathf.Clamp(baseColor.b + b, 0f, 1f);

		Color skinColor = new Color (skinRed, skinGreen, skinBlue);

		Renderer[] renderers = GetComponentsInChildren<Renderer> ();
		foreach (Renderer renderer in renderers) {
			if (renderer.gameObject.name == "skinmesh") {
				renderer.material.SetColor ("_Color", skinColor);
			}
		}
	}

	// Update is called once per frame
	void Update () {

	}
}
