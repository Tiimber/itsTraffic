using UnityEngine;
using System.Collections;
using System;

public class PointAndClock : MonoBehaviour, IPubSub {

	private Clock clock;

	void Start() {
		// For Clock
		clock = GetComponentInChildren<Clock> ();
		PubSub.subscribe ("clock:setTime", this);
		PubSub.subscribe ("clock:setSpeed", this);
		PubSub.subscribe ("clock:setDisplaySeconds", this);
		PubSub.subscribe ("clock:start", this);
		PubSub.subscribe ("clock:stop", this);

		clock.hour = 0;
		clock.minutes = 0;
		clock.clockSpeed = 1f;
        clock.running = false;
	}

	#region IPubSub implementation
	public PROPAGATION onMessage (string message, object data) {
		if (message == "clock:setTime") {
			string time = (string)data;
			string[] timeParts = time.Split (':');
			clock.hour = Convert.ToInt32 (timeParts [0]);
			clock.minutes = Convert.ToInt32 (timeParts [1]);
            clock.Restart();
		} else if (message == "clock:setSpeed") {
            clock.clockSpeed = (int)data;
        } else if (message == "clock:setDisplaySeconds") {
            clock.showSeconds((bool)data);
        } else if (message == "clock:start") {
            clock.running = true;
        } else if (message == "clock:stop") {
            clock.running = false;
        }
		return PROPAGATION.DEFAULT;
	}
	#endregion

	void OnDestroy() {
		PubSub.unsubscribeAllForSubscriber (this);
	}
}
