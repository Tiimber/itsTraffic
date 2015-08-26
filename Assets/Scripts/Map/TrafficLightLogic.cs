using UnityEngine;
using System.Collections;

public class TrafficLightLogic : MonoBehaviour {
	private float timeToSwitch = 0f;
	private float timeBetweenSwitches = 5f;
	private bool switching = false;
	private State state = State.NOT_INITIALISED;
	private Light lightObj = null;

	private GameObject redLightObject = null;
	private GameObject yellowLightObject = null;
	private GameObject greenLightObject = null;

	private Pos pos = null;
	private Pos otherPos = null;
	private float rotation = 0f;

	private Color lightGreen = new Color(0f, 0.45f, 0f);
	private Color lightYellow = new Color(0.45f, 0.45f, 0f);
	private Color lightRed = new Color(1f, 0f, 0f);

	public string Id { set; get; }

	public void setProperties (Pos pos, float rotation, Pos otherPos) {
		setPos (pos);
		setRotation (rotation);
		setOtherPos (otherPos);
		autosetName ();
	}

	private void setPos (Pos pos) {
		this.pos = pos;
	}
	
	public Pos getPos () {
		return pos;
	}
	
	public void setRotation (float rotation) {
		this.rotation = rotation;
	}

	public float getRotation () {
		return rotation;
	}

	private void setOtherPos (Pos otherPos) {
		this.otherPos = otherPos;
	}
	
	public Pos getOtherPos () {
		return otherPos;
	}
	

	public void setState (State state) {
		this.state = state;
		if (lightObj != null) {
			lightObj.color = state == State.RED ? lightRed : lightGreen;
		}
	}

	public State getState () {
		return state;
	}

	public void setTimeBetweenSwitches (float timeBetweenSwitches) {
		this.timeBetweenSwitches = timeBetweenSwitches;
	}

	private void autosetName () {
		name = "Traffic Light @ " + pos.Id + " (other node: " + otherPos.Id + ")";
		Id = pos.Id + "," + otherPos.Id;
	}

	// Use this for initialization
	void Start () {
		for (int i = 0; i < transform.childCount; i++) {
			GameObject child = transform.GetChild (i).gameObject;
			switch (child.name) {
				case "Red": redLightObject = child; break;
				case "Yellow": yellowLightObject = child; break;
				case "Green": greenLightObject = child; break;
				default: break;
			}
		}

		lightObj = GetComponentInChildren<Light> ();
		setLightState ();
		timeToSwitch = timeBetweenSwitches;
	}
	
	// Update is called once per frame
	void Update () {
		if (!switching) {
			timeToSwitch -= Time.deltaTime;
			if (timeToSwitch <= 0) {
				StartSwitchLights ();
			}
		}
	}

	private void StartSwitchLights () {
		switching = true;
		if (state == State.GREEN) {
			StartCoroutine (switchColors (State.RED));
		} else if (state == State.RED) {
			StartCoroutine (switchColors (State.GREEN));
		}
		timeToSwitch = timeBetweenSwitches;
	}

	private IEnumerator switchColors (State endState) {
		state = State.YELLOW;
		setLightState ();
		yield return new WaitForSeconds (2f);
		state = endState;
		setLightState ();
		switching = false;
	}

	private void setLightState () {
		lightObj.color = state == State.RED ? lightRed : lightGreen;
		redLightObject.SetActive (state == State.RED);
		yellowLightObject.SetActive (state == State.YELLOW);
		greenLightObject.SetActive (state == State.GREEN);
	}

	public enum State {
		GREEN,
		YELLOW,
		RED,
		NOT_INITIALISED
	}
}
