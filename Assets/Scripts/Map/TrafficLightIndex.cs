using UnityEngine;

using System.Collections.Generic;
using System.Linq;

public class TrafficLightIndex
{
	private static List<TrafficLightLogic> TrafficLights = new List<TrafficLightLogic>();

	public static void AddTrafficLight (TrafficLightLogic trafficLight) {
		TrafficLights.Add (trafficLight);
	}

	public static void AutosetTrafficLightProperties () {
		foreach (TrafficLightLogic trafficLight in TrafficLights) {
			// Get rotation, angles 45-135 & 225-315 should have same color, and the rest the other color
			float lightRotation = trafficLight.getRotation () % 180f;
			TrafficLightLogic.State state = lightRotation > 45f && lightRotation <= 135f ? TrafficLightLogic.State.GREEN : TrafficLightLogic.State.RED;
			trafficLight.setState(state);
		}
	}
}
