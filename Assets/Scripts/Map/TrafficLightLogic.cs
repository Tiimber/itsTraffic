using UnityEngine;
using System.Collections;

public class TrafficLightLogic : MonoBehaviour {
	private float timeToSwitch = 0f;
	private float timeBetweenSwitches = 5f;
	private bool switching = false;
	private State state = State.NOT_INITIALISED;
	private State endState = State.NOT_INITIALISED;
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

	private Coroutine currentCoroutine = null;

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

	void Start() {
		if (lightObj == null) {
			manualStart ();
		}
	}

	// Use this for initialization
	public void manualStart () {
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

	public void setColliders (WayReference wayReference, float colliderPercentageY, bool isNode1) {
		// Calculations for collider height
		float lightColliderHeightFactor = 20.3f / 0.255f;
		float wayHeight = wayReference.gameObject.transform.localScale.y;
		float percentageHeight = isNode1 ? colliderPercentageY : 1f - colliderPercentageY;
		float lightColliderAbsoluteHeight = wayHeight * percentageHeight;
		float lightColliderHeight = lightColliderAbsoluteHeight * lightColliderHeightFactor;

		// Calculations for collider length
		float waySpeed = wayReference.way.WayWidthFactor;
		float redLightColliderLength = (waySpeed / 0.85f) * 1200f; 
		float yellowLightColliderLength = redLightColliderLength * 2f;

		BoxCollider redCollider = transform.FindChild ("Red").GetComponent<BoxCollider> ();	
		Vector3 redColliderSize = new Vector3 (lightColliderHeight, redLightColliderLength, redCollider.size.z);
		redCollider.size = redColliderSize;
		Vector3 redColliderCenter = new Vector3 (lightColliderHeight / 2f, redLightColliderLength / 2f, redCollider.center.z);
		redCollider.center = redColliderCenter;
		BoxCollider yellowCollider = transform.FindChild ("Yellow").gameObject.GetComponent<BoxCollider> ();	
		Vector3 yellowColliderSize = new Vector3 (lightColliderHeight, yellowLightColliderLength, yellowCollider.size.z);
		yellowCollider.size = yellowColliderSize;
		Vector3 yellowColliderCenter = new Vector3 (lightColliderHeight / 2f, yellowLightColliderLength / 2f, yellowCollider.center.z);
		yellowCollider.center = yellowColliderCenter;
	}


	// Update is called once per frame
	void Update () {
		if (!switching) {
			timeToSwitch -= Time.deltaTime;
			if (timeToSwitch <= 0) {
//				StartSwitchLights ();
			}
		}
	}

	public void manualSwitch() {
		if (currentCoroutine != null) {
			StopCoroutine(currentCoroutine);
			state = endState;
		}
		StartSwitchLights ();
	}

	private void StartSwitchLights () {
		switching = true;
		if (state == State.GREEN) {
			currentCoroutine = StartCoroutine (switchColors (State.RED));
		} else if (state == State.RED) {
			currentCoroutine = StartCoroutine (switchColors (State.GREEN));
		}
		timeToSwitch = timeBetweenSwitches;
	}

	private IEnumerator switchColors (State endState) {
		this.endState = endState;
		state = State.YELLOW;
		setLightState ();
		yield return new WaitForSeconds (2f);
		state = endState;
		setLightState ();
		switching = false;
	}

	public void setLightState () {
		lightObj.color = (state == State.RED ? lightRed : (state == State.GREEN ? lightGreen : lightYellow));

		LightLogic redLight = redLightObject.GetComponent<LightLogic> ();
		LightLogic yellowLight = yellowLightObject.GetComponent<LightLogic> ();
		LightLogic greenLight = greenLightObject.GetComponent<LightLogic> ();

		redLight.setState (state);
		yellowLight.setState (state);
		greenLight.setState (state);
	}

	public enum State {
		GREEN,
		YELLOW,
		RED,
		NOT_INITIALISED
	}
}
