using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Collections;

public class TrafficLightIndex
{
	private static List<TrafficLightLogic> TrafficLights = new List<TrafficLightLogic>();
	private static Dictionary<TrafficLightLogic, Dictionary<TrafficLightIndex.RelationState, List<TrafficLightLogic>>> TrafficLightRelations = new Dictionary<TrafficLightLogic, Dictionary<TrafficLightIndex.RelationState, List<TrafficLightLogic>>> ();
	public static Dictionary<long, List<TrafficLightLogic>> TrafficLightsForPos = new Dictionary<long, List<TrafficLightLogic>> (); 

	public static void AddTrafficLight (TrafficLightLogic trafficLight) {
		TrafficLights.Add (trafficLight);
	}

	public static void AutoInitTrafficLights () {
		// Set properties depending on rotation
		AutosetTrafficLightProperties ();
		// Index them to keep a map with each traffic light, and know which are in same crossing, oppose or on the sides
		BuildTrafficLightIndex ();
		// Create a traffic light collider for interacting
		CreateTrafficLightColliders ();
		// Tell all traffic light to update their "lit" light status
		Singleton<Game>.Instance.StartCoroutine (SetTrafficLightStates ());
	}

	public static void ApplyConfig (XmlNode objectNode) {
		string id = objectNode.Attributes.GetNamedItem ("id").Value;
		TrafficLightLogic trafficLight = GetTrafficLightById (id);
		if (trafficLight) {
			foreach (XmlNode propertyNode in objectNode.ChildNodes) {
				switch (propertyNode.Name) {
					case "time": 
						float time = float.Parse (propertyNode.InnerText);
						ApplyTimeOnTrafficLight (trafficLight, time);
						break;
					case "state":
						TrafficLightLogic.State state = propertyNode.InnerText == "RED" ? TrafficLightLogic.State.RED : TrafficLightLogic.State.GREEN;
						ApplyStateOnTrafficLight (trafficLight, state);
						break;
				}
			}
		}
	}

	private static void AutosetTrafficLightProperties () {
		foreach (TrafficLightLogic trafficLight in TrafficLights) {
			if (trafficLight.getState () == TrafficLightLogic.State.NOT_INITIALISED) {
				// Get rotation, angles 45-135 & 225-315 should have same color, and the rest the other color
				float firstLightRotation = trafficLight.getRotation ();
				List<TrafficLightLogic> relatedTrafficLights = TrafficLights.Where (p => p.getPos () == trafficLight.getPos ()).ToList ();
				foreach (TrafficLightLogic relatedTrafficLight in relatedTrafficLights) {
					float lightRotation = Mathf.Abs((relatedTrafficLight.getRotation () - firstLightRotation) % 180f);
					TrafficLightLogic.State state = lightRotation > 45f && lightRotation <= 135f ? TrafficLightLogic.State.GREEN : TrafficLightLogic.State.RED;
					relatedTrafficLight.setState(state);
				}
			}
		}
	}

	private static void BuildTrafficLightIndex () {
		foreach (TrafficLightLogic trafficLight in TrafficLights) {
			// Add to our Pos.id -> [trafficLight] dictionary
			long posId = trafficLight.getPos().Id;
			if (!TrafficLightsForPos.ContainsKey (posId)) {
				TrafficLightsForPos.Add (posId, new List<TrafficLightLogic>());
			}
			TrafficLightsForPos [posId].Add(trafficLight);

			foreach (TrafficLightLogic otherTrafficLight in TrafficLights) {
				if (trafficLight != otherTrafficLight && trafficLight.getPos ().Equals (otherTrafficLight.getPos())) {
					// This traffic light is either same light or opposite to it
					bool isSameDirection = trafficLight.getState () == otherTrafficLight.getState ();
					RelationState relationState = isSameDirection ? RelationState.SAME_DIRECTION : RelationState.CROSSING_DIRECTION;

					if (!TrafficLightRelations.ContainsKey (trafficLight)) {
						TrafficLightRelations.Add (trafficLight, new Dictionary<RelationState, List<TrafficLightLogic>> ());
					}
					Dictionary<RelationState, List<TrafficLightLogic>> trafficLightEntry = TrafficLightRelations [trafficLight];

					if (!trafficLightEntry.ContainsKey (relationState)) {
						trafficLightEntry.Add (relationState, new List<TrafficLightLogic>());
					}
					List<TrafficLightLogic> linkedTrafficLightsList = trafficLightEntry [relationState];

					linkedTrafficLightsList.Add (otherTrafficLight);
				}
			}
		}
	}
	
	private static TrafficLightLogic GetTrafficLightById (string id) {
		TrafficLightLogic trafficLight = null;
		foreach (TrafficLightLogic light in TrafficLights) {
			if (light.Id == id) {
				trafficLight = light;
				break;
			}
		}
		return trafficLight;
	}

	private static void ApplyTimeOnTrafficLight (TrafficLightLogic trafficLight, float time) {
		trafficLight.setTimeBetweenSwitches (time);
		Dictionary<RelationState, List<TrafficLightLogic>> trafficLightEntry = TrafficLightRelations [trafficLight];
		if (trafficLightEntry.ContainsKey (RelationState.SAME_DIRECTION)) {
			foreach (TrafficLightLogic sameDirectionLight in trafficLightEntry [RelationState.SAME_DIRECTION]) {
				sameDirectionLight.setTimeBetweenSwitches (time);
			}
			foreach (TrafficLightLogic otherDirectionLight in trafficLightEntry [RelationState.CROSSING_DIRECTION]) {
				otherDirectionLight.setTimeBetweenSwitches (time);
			}
		}
	}

	private static void ApplyStateOnTrafficLight (TrafficLightLogic trafficLight, TrafficLightLogic.State state) {
		TrafficLightLogic.State otherState = state == TrafficLightLogic.State.GREEN ? TrafficLightLogic.State.RED : TrafficLightLogic.State.GREEN;
		trafficLight.setState (state);
		Dictionary<RelationState, List<TrafficLightLogic>> trafficLightEntry = TrafficLightRelations [trafficLight];
		if (trafficLightEntry.ContainsKey (RelationState.SAME_DIRECTION)) {
			foreach (TrafficLightLogic sameDirectionLight in trafficLightEntry [RelationState.SAME_DIRECTION]) {
				sameDirectionLight.setState (state);
			}
			foreach (TrafficLightLogic otherDirectionLight in trafficLightEntry [RelationState.CROSSING_DIRECTION]) {
				otherDirectionLight.setState (otherState);
			}
		}
	}

	private static void CreateTrafficLightColliders () {
		// For each pos in TrafficLightsForPos, register an entry in TrafficLightToggle, with position and radius
		foreach (KeyValuePair<long, List<TrafficLightLogic>> trafficLightGroup in TrafficLightsForPos) {
			long posId = trafficLightGroup.Key;
			Pos centerPos = NodeIndex.getPosById (posId);
			float maxDistance = 0f;
			Vector3 centerPosCameraPosition = Game.getCameraPosition (centerPos);
		
			// Get max distance from crossing
			foreach (TrafficLightLogic trafficLight in trafficLightGroup.Value) {
				maxDistance = Mathf.Max( Vector2.Distance (trafficLight.transform.position, centerPosCameraPosition), maxDistance);
			}

			// Double the maxDistance to get a somewhat bigger touch area
			TrafficLightToggle.Add (posId, centerPosCameraPosition, maxDistance * 2f);
		}
		TrafficLightToggle.Start ();
	}

	public static void toggleLightsForPos (long posId) {
		// Get lights for pos
		List<TrafficLightLogic> trafficLights = TrafficLightsForPos [posId];
		foreach (TrafficLightLogic trafficLight in trafficLights) {
			trafficLight.manualSwitch ();
		}
		DataCollector.Add ("Manual traffic light switches", 1f);
	}

	private static IEnumerator SetTrafficLightStates () {
		yield return new WaitForSeconds (0.1f);
		foreach (TrafficLightLogic trafficLight in TrafficLights) {
			trafficLight.manualStart ();
		}
	}
	
	private enum RelationState {
		SAME_DIRECTION,
		CROSSING_DIRECTION
	}
}
