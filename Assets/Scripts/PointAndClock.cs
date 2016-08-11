using UnityEngine;
using System.Collections;
using System;

public class PointAndClock : MonoBehaviour, IPubSub {

	private Clock clock;

	void Start() {
		// For Clock
		clock = GetComponentInChildren<Clock> ();
		PubSub.subscribe ("clock:setTime", this);

		clock.hour = 0;
		clock.minutes = 0;
		clock.clockSpeed = 0f;
	}

	#region IPubSub implementation
	public PROPAGATION onMessage (string message, object data) {
		if (message == "clock:setTime") {
			string time = (string)data;
			string[] timeParts = time.Split (':');
			clock.hour = Convert.ToInt32 (timeParts [0]);
			clock.minutes = Convert.ToInt32 (timeParts [1]);
			clock.clockSpeed = 1f;
		}
		return PROPAGATION.DEFAULT;
	}
	#endregion
}
