using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightToggle : IPubSub {

	private static TrafficLightToggle instance = null;
	
	private static TrafficLightToggle getInstance() {
		if (instance == null) {
			instance = new TrafficLightToggle(); 
		}
		return instance;
	}

	public static void Start () {
		PubSub.subscribe ("Click", getInstance());
	}
	
	public static void Add (long posId, Vector3 center, float radius) {
		getInstance().toggles.Add (new CircleTouch(posId, center, radius));
	}


	List<CircleTouch> toggles = new List<CircleTouch>();

	public void onMessage(string message, System.Object obj) {
		if (message == "Click") {
			Vector3 clickPos = (Vector3) obj;
			Vector3 clickPosNoZ = new Vector3(clickPos.x, clickPos.y);
			CircleTouch touchArea = toggles.Find(i => i.isInside(clickPosNoZ));
			if (touchArea != null) {
				TrafficLightIndex.toggleLightsForPos(touchArea.posId);
			}
		}
	}


	private class CircleTouch {
		public long posId;
		private Vector3 center;
		private float radius;

		public CircleTouch(long posId, Vector3 center, float radius) {
			this.posId = posId;
			this.center = center;
			this.radius = radius;
		}

		public bool isInside(Vector3 pos) {
			return (center - pos).magnitude <= radius;
		}
	}
}
