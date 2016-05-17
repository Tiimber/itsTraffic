using UnityEngine;
using System.Collections;

public class VehicleLights : MonoBehaviour {

	private static float headlightLowBeamIntensity = 3f;
	private static float headlightHighBeamIntensity = 5f;
	private static Quaternion headlightsLowBeamRotation = Quaternion.Euler(0f, 75f, 0f);
	private static Quaternion headlightsHighBeamRotation = Quaternion.Euler(0f, 80f, 0f);

	GameObject headlightsGroup;
	Light headlightRight;
	Light headlightLeft;

	GameObject blinkersLeft;
	GameObject blinkersRight;

	// Use this for initialization
	void Start () {
		Light[] vehicleLights = GetComponentsInChildren<Light> (true);
		foreach (Light vehicleLight in vehicleLights) {
			switch (vehicleLight.name) {
				case "headlight right":
					headlightRight = vehicleLight;
					break;
				case "headlight left":
					headlightLeft = vehicleLight;
					break;
				case "Blinker right front":
					blinkersRight = vehicleLight.transform.parent.gameObject;
					break;
				case "Blinker left front":
					blinkersLeft = vehicleLight.transform.parent.gameObject;
					break;
			}
		}

		if (headlightRight != null) {
			headlightsGroup = headlightRight.transform.parent.gameObject;
		}
	}

	public void toggleHeadlights(bool enable) {
		headlightsGroup.SetActive (enable);
	}

	public void setHeadlightIntensity(bool lowBeam = true) {
		headlightsGroup.SetActive (true);
		headlightsGroup.transform.localRotation = lowBeam ? headlightsLowBeamRotation : headlightsHighBeamRotation;
		headlightLeft.intensity = lowBeam ? headlightLowBeamIntensity : headlightHighBeamIntensity;
		headlightRight.intensity = lowBeam ? headlightLowBeamIntensity : headlightHighBeamIntensity;
	}

	public void toggleBlinkersLeft(bool enable) {
		blinkersLeft.SetActive (enable);
	}

	public void toggleBlinkersRight(bool enable) {
		blinkersRight.SetActive (enable);
	}

	/* TODO - Remove debug */
	[InspectorButton("headlightsOffFn")]
	public bool headlightsOff;
	[InspectorButton("headlightsOnFn")]
	public bool headlightsOn;
	[InspectorButton("headlightsLowBeamFn")]
	public bool headlightsLowBeam;
	[InspectorButton("headlightsHighBeamFn")]
	public bool headlightsHighBeam;

	private void headlightsOffFn() {toggleHeadlights (false);}
	private void headlightsOnFn() {toggleHeadlights (true);}
	private void headlightsLowBeamFn() {setHeadlightIntensity (true);}
	private void headlightsHighBeamFn() {setHeadlightIntensity (false);}

	[InspectorButton("blinkersLeftToggleFn")]
	public bool blinkersLeftToggle;
	[InspectorButton("blinkersRightToggleFn")]
	public bool blinkersRightToggle;

	private void blinkersLeftToggleFn() {toggleBlinkersLeft (!blinkersLeft.activeSelf);}
	private void blinkersRightToggleFn() {toggleBlinkersRight (!blinkersRight.activeSelf);}
}
