using UnityEngine;
using System.Collections;

public class TrafficLightLogic : MonoBehaviour {
	private float timeToSwitch = 0f;
	private float timeBetweenSwitches = 5f;
	private bool switching = false;
	private State state = State.GREEN;
	private Light lightObj = null;

	private Pos pos = null;
	private float rotation = 0f;

	private Color lightGreen = new Color(0f, 0.45f, 0f);
	private Color lightYellow = new Color(0.45f, 0.45f, 0f);
	private Color lightRed = new Color(1f, 0f, 0f);

	public void setProperties (Pos pos, float rotation) {
		setPos (pos);
		setRotation (rotation);
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

	public void setState (State state) {
		this.state = state;
	}

	public State getState () {
		return state;
	}

	// Use this for initialization
	void Start () {
		lightObj = GetComponent<Light> ();
		// TODO - Set this depending on other lights in crossing, and user setting
		lightObj.color = state == State.RED ? lightRed : lightGreen;
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
		lightObj.color = lightYellow;
		state = State.YELLOW;
		yield return new WaitForSeconds (2f);
		lightObj.color = endState == State.RED ? lightRed : lightGreen;
		state = endState;
		switching = false;
	}

	public enum State {
		GREEN,
		YELLOW,
		RED
	}
}
