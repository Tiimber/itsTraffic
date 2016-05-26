using UnityEngine;
using System.Collections;

public class VehicleLights : MonoBehaviour {

	private static float headlightLowBeamIntensity = 3f;
	private static float headlightHighBeamIntensity = 5f;
	private static Quaternion headlightsLowBeamRotation = Quaternion.Euler(0f, 75f, 0f);
	private static Quaternion headlightsHighBeamRotation = Quaternion.Euler(0f, 80f, 0f);

	private static float backlightsIntensity = 1f;
	private static float breaklightsIntensity = 5f;
	private static Color backlightsColor = Color.white;
	private static Color breaklightsColor = Color.red;

	private GameObject headlightsGroup;
	private Light headlightLeft;
	private Light headlightRight;

	private GameObject blinkersLeft;
	private GameObject blinkersRight;

	private GameObject taillightsGroup;
	private Light taillightLeft;
	private Light taillightRight;

	private bool warningBlinkersOn = false;
	private bool blinkersOn = false;
	private bool lastBlinkLeft = false;
	private bool lastBlinkRight = false;

	// Use this for initialization
	void Start () {
		Light[] vehicleLights = GetComponentsInChildren<Light> (true);
		foreach (Light vehicleLight in vehicleLights) {
			switch (vehicleLight.name) {
				case "Headlight right":
					headlightRight = vehicleLight;
					break;
				case "Headlight left":
					headlightLeft = vehicleLight;
					break;
				case "Blinker right front":
					blinkersRight = vehicleLight.transform.parent.gameObject;
					break;
				case "Blinker left front":
					blinkersLeft = vehicleLight.transform.parent.gameObject;
					break;
				case "Taillight right":
					taillightRight = vehicleLight;
					break;
				case "Taillight left":
					taillightLeft = vehicleLight;
					break;
			}
		}

		if (headlightRight != null) {
			headlightsGroup = headlightRight.transform.parent.gameObject;
		}
		if (taillightRight != null) {
			taillightsGroup = taillightRight.transform.parent.gameObject;
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

	public void toggleWarningBlinkers(bool enable = true) {
		blinkersLeft.SetActive (enable);
		blinkersRight.SetActive (enable);
	}

	public void startWarningBlinkers() {
		if (!warningBlinkersOn) {
			warningBlinkersOn = true;
			StartCoroutine (startWarningBlinkersBlinking());
		}
	}

	private IEnumerator startWarningBlinkersBlinking() {
		while (warningBlinkersOn) {
			toggleWarningBlinkers (!blinkersRight.activeSelf);
			yield return new WaitForSeconds (0.8f);
		}
	}

	public void stopWarningBlinkers() {
		if (warningBlinkersOn) {
			warningBlinkersOn = false;
		}
	}

	public void flashHeadlights() {
		StopCoroutine ("startFlashHeadlight");
		StartCoroutine ("startFlashHeadlight");
	}

	private IEnumerator startFlashHeadlight() {
		setHeadlightIntensity (false);
		yield return new WaitForSeconds (1f);
		setHeadlightIntensity (true);
	}

	public void toggleTaillights(bool enable) {
		taillightsGroup.SetActive (enable);
	}

	public void startBlinkersLeft() {
		if (!blinkersOn || !lastBlinkLeft) {
			stopBlinkers ();
			blinkersOn = true;
			lastBlinkLeft = true;
			StartCoroutine ("startBlinkers", true);
		}
	}

	public void startBlinkersRight() {
		if (!blinkersOn || !lastBlinkRight) {
			stopBlinkers ();
			blinkersOn = true;
			lastBlinkRight = true;
			StartCoroutine ("startBlinkers", false);
		}
	}

	public void stopBlinkers() {
		if (blinkersLeft != null) {		
			blinkersOn = false;
			lastBlinkLeft = false;
			lastBlinkRight = false;
			StopCoroutine ("startBlinkers");
			blinkersLeft.SetActive (false);
			blinkersRight.SetActive (false);
		}
	}

	private IEnumerator startBlinkers(bool blinkLeft) {
		while (blinkersOn) {
			if (blinkLeft) {
				blinkersLeft.SetActive (!blinkersLeft.activeSelf);
			} else {
				blinkersRight.SetActive (!blinkersRight.activeSelf);
			}
			yield return new WaitForSeconds (0.5f);
		}
	}

	// backlights = true means lights to indicate backing
	// backlights = false means break lights
	public void setTaillightsState(bool backlights) {
		taillightsGroup.SetActive (true);
		taillightLeft.intensity = backlights ? backlightsIntensity : breaklightsIntensity;
		taillightRight.intensity = backlights ? backlightsIntensity : breaklightsIntensity;
		taillightLeft.color = backlights ? backlightsColor : breaklightsColor;
		taillightRight.color = backlights ? backlightsColor : breaklightsColor;
	}

	public void turnAllOff() {
		StopCoroutine ("startFlashHeadlight");
		stopWarningBlinkers ();
		headlightsGroup.SetActive (false);
		taillightsGroup.SetActive (false);
		blinkersLeft.SetActive (false);
		blinkersRight.SetActive (false);
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
	[InspectorButton("warningBlinkersToggleFn")]
	public bool warningBlinkersToggle;
	[InspectorButton("warningBlinkersCoroutineOnFn")]
	public bool warningBlinkersCoroutineOn;
	[InspectorButton("warningBlinkersCoroutineOffFn")]
	public bool warningBlinkersCoroutineOff;

	private void blinkersLeftToggleFn() {toggleBlinkersLeft (!blinkersLeft.activeSelf);}
	private void blinkersRightToggleFn() {toggleBlinkersRight (!blinkersRight.activeSelf);}
	private void warningBlinkersToggleFn() {toggleWarningBlinkers (!blinkersRight.activeSelf);}
	private void warningBlinkersCoroutineOnFn() {startWarningBlinkers ();}
	private void warningBlinkersCoroutineOffFn() {stopWarningBlinkers ();}

	[InspectorButton("taillightsOffFn")]
	public bool taillightsOff;
	[InspectorButton("taillightsOnFn")]
	public bool taillightsOn;
	[InspectorButton("taillightsBacklightsFn")]
	public bool taillightsBacklights;
	[InspectorButton("taillightsBreaklightsFn")]
	public bool taillightsBreaklights;

	private void taillightsOffFn() {toggleTaillights (false);}
	private void taillightsOnFn() {toggleTaillights (true);}
	private void taillightsBacklightsFn() {setTaillightsState (true);}
	private void taillightsBreaklightsFn() {setTaillightsState (false);}
}
