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
	
	public static void Add (long posId, Vector2 center, float radius) {
		getInstance().toggles.Add (new CircleTouchWithPosId(posId, center, radius));
	}


	List<CircleTouchWithPosId> toggles = new List<CircleTouchWithPosId>();

	public PROPAGATION onMessage(string message, System.Object obj) {
		if (message == "Click") {
			Vector2 clickPos = (Vector3) obj;
			CircleTouchWithPosId touchArea = toggles.Find(i => i.isInside(clickPos));
			if (touchArea != null) {
				TrafficLightIndex.toggleLightsForPos(touchArea.posId);
			}
		}
		return PROPAGATION.DEFAULT;
	}

	private class CircleTouchWithPosId : CircleTouch {
		public long posId;

		public CircleTouchWithPosId(long posId, Vector2 center, float radius) : base(center, radius) {
			this.posId = posId;
		}
	}

}
