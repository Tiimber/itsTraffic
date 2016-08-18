using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SkinLogic : MonoBehaviour {

	private static readonly Color whiteish = new Color(1f, 0.91f, 0.66f);
	private static readonly Color brownish = new Color(0.62f, 0.47f, 0.2f);
	private static readonly Color yellowish = new Color(0.95f, 0.89f, 0.6f);
	private static readonly Color blackish = new Color(0.25f, 0.13f, 0.08f);

	private Dictionary<Color, float> skinColors = new Dictionary<Color, float> {
		{whiteish, 0.7f},  // "white"
		{brownish, 0.2f},  // "brown"
		{yellowish, 0.1f}, // "yellow"
		{blackish, 0.05f}  // "black"
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

	private Color getBaseColorForCountry(string countryCode, out bool haveBaseColor) {
		if (baseSkinColorByNationality.ContainsKey (countryCode)) {
			haveBaseColor = true;
			return baseSkinColorByNationality [countryCode];
		}
		haveBaseColor = false;
		return whiteish;
	}

	private void setSkinColor() {
		Color skinColor;

		Setup.PersonSetup personality = GetComponentInParent<HumanLogic> ().personality;
		if (personality != null && personality.skinColor != null) {
			// Set specific skin color
			skinColor = Misc.parseColor (personality.skinColor);
		} else {
			// Check if we should lookup get specific base color
			Color baseColor = Color.magenta;
			
			bool haveBaseColor = false;
			if (personality != null && personality.country != null) {
				baseColor = getBaseColorForCountry (personality.country, out haveBaseColor);
			} else if (Game.instance.loadedLevel != null && Game.instance.loadedLevel.country != null) {
				baseColor = getBaseColorForCountry (Game.instance.loadedLevel.country, out haveBaseColor);
			}

			if (!haveBaseColor) {
				baseColor = getBaseSkinColor ();
			}

			float r = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;
			float g = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;
			float b = ((float) HumanLogic.HumanRNG.NextDouble ()) * 0.06f - 0.03f;

			float skinRed = Mathf.Clamp(baseColor.r + r, 0f, 1f);
			float skinGreen = Mathf.Clamp(baseColor.g + g, 0f, 1f);
			float skinBlue = Mathf.Clamp(baseColor.b + b, 0f, 1f);

			skinColor = new Color (skinRed, skinGreen, skinBlue, 0f);
		}

		Renderer[] renderers = GetComponentsInChildren<Renderer> ();
		foreach (Renderer renderer in renderers) {
			if (renderer.gameObject.name == "skinmesh") {
				foreach (Material material in renderer.materials) {
					material.SetColor ("_Color", skinColor);
				}
			}
		}
	}

	// Update is called once per frame
	void Update () {

	}

	// Tried mapping with sources:
	// https://upload.wikimedia.org/wikipedia/commons/6/6a/Unlabeled_Renatto_Luschan_Skin_color_map.svg
	// http://www.targetmap.com/viewer.aspx?reportId=7301
	public static Dictionary<string, Color> baseSkinColorByNationality = new Dictionary<string, Color> {
		{"ar", whiteish},
		{"at", whiteish},
		{"au", brownish},
		{"ba", whiteish},
		{"be", whiteish},
		{"ca", whiteish},
		{"ch", yellowish},
		{"cl", whiteish},
		{"cz", whiteish},
		{"de", whiteish},
		{"es", whiteish},
		{"fi", whiteish},
		{"fr", whiteish},
		{"gb", whiteish},
		{"ge", whiteish},
		{"ie", whiteish},
		{"jp", yellowish},
		{"ke", blackish},
		{"ph", yellowish},
		{"pl", whiteish},
		{"pt", whiteish},
		{"ru", whiteish},
		{"se", whiteish},
		{"ua", whiteish},
		{"us", whiteish},
		{"za", whiteish}
	};
}
