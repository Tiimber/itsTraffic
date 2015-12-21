using UnityEngine;
using System.Collections;

public class LightLogic : MonoBehaviour {
	private TrafficLightLogic.State color;

	// Use this for initialization
	void Start () {
		color = (gameObject.name == "Green" ? TrafficLightLogic.State.GREEN : (gameObject.name == "Red" ? TrafficLightLogic.State.RED : TrafficLightLogic.State.YELLOW));
	}

	public void setState(TrafficLightLogic.State state) {
		string colorStr = color.ToString ();
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
		Collider collider = GetComponent<Collider> ();
		collider.enabled = enable;
	}
}
