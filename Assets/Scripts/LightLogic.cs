using UnityEngine;
using System.Collections;

public class LightLogic : MonoBehaviour {
	private TrafficLightLogic.State color;

	private static readonly float TRAFFIC_COLLIDER_CENTER_Z = -4.5f;
	private static float TRAFFIC_COLLIDER_CENTER_INACTIVE_Z = -20f;

	// Use this for initialization
	void Start () {
		color = (gameObject.name == "Green" ? TrafficLightLogic.State.GREEN : (gameObject.name == "Red" ? TrafficLightLogic.State.RED : TrafficLightLogic.State.YELLOW));
	}

	public void setState(TrafficLightLogic.State state) {
		setLight(state == color);
		switch (color) {
			case TrafficLightLogic.State.YELLOW: 
				setCollider(state == TrafficLightLogic.State.YELLOW || state == TrafficLightLogic.State.RED);
				break;
			case TrafficLightLogic.State.RED: 
				setCollider(state == TrafficLightLogic.State.RED);
				break;
			default:
				break;
		}
	}

	void setLight (bool render) {
		Renderer renderer = GetComponent<Renderer> ();
		renderer.enabled = render;
	}

	void setCollider (bool enable) {
		BoxCollider collider = GetComponent<BoxCollider> ();
		if (enable) {
			collider.center = new Vector3 (collider.center.x, collider.center.y, TRAFFIC_COLLIDER_CENTER_Z);
		} else {
			collider.center = new Vector3 (collider.center.x, collider.center.y, TRAFFIC_COLLIDER_CENTER_INACTIVE_Z);
		}
//		collider.enabled = enable;
	}
}
