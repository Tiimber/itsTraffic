using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Objectives {
	public List<Objective> winObjectives = new List<Objective>();
	public List<Objective> loseObjectives = new List<Objective>();


	public Objectives(XmlNode objectivesNode) {
		XmlNodeList winNodes = objectivesNode.SelectNodes("win");
		foreach (XmlNode winNode in winNodes) {
			XmlAttributeCollection winAttributes = winNode.Attributes;
			string type = Misc.xmlString(winAttributes.GetNamedItem ("type"));
			long id = Misc.xmlLong (winAttributes.GetNamedItem ("id"));
			string key = Misc.xmlString (winAttributes.GetNamedItem ("key"));
			float value = Misc.xmlFloat(winAttributes.GetNamedItem ("value"));
			winObjectives.Add (new Objective (type, id, key, value));
		}

		XmlNodeList loseNodes = objectivesNode.SelectNodes("lose");
		foreach (XmlNode loseNode in loseNodes) {
			XmlAttributeCollection loseAttributes = loseNode.Attributes;
			string type = Misc.xmlString(loseAttributes.GetNamedItem ("type"));
			long id = Misc.xmlLong (loseAttributes.GetNamedItem ("id"));
			string key = Misc.xmlString (loseAttributes.GetNamedItem ("key"));
			float value = Misc.xmlFloat(loseAttributes.GetNamedItem ("value"));
			loseObjectives.Add (new Objective (type, id, key, value));
		}

		DataCollector.registerObjectiveReporter (this);
	}

	public class Objective {
		public string type;
		public long id;
		public string key;
		public float value;

		public Objective(string type, long id, string key, float value) {
			this.type = type;
			this.id = id;
			this.key = key;
			this.value = value;
		}
	}

	public void reportChange() {
		Dictionary<string, DataCollector.InnerData> data = DataCollector.Data;
		bool haveWon = true;
		foreach (Objective win in winObjectives) {
			if (SpecialObjectives.TYPES.Contains (win.type)) {
				if (!SpecialObjectives.check (win)) {
					haveWon = false;
					break;
				} else {
					continue;
				}
			}

			if (!data.ContainsKey (win.type) || data [win.type].value < win.value) {
				haveWon = false;
				break;
			}
		}
		bool haveLost = false;
		foreach (Objective lose in loseObjectives) {
			if (SpecialObjectives.TYPES.Contains (lose.type)) {
				if (SpecialObjectives.check (lose)) {
					haveLost = true;
					break;
				} else {
					continue;
				}
			}

			if (data.ContainsKey (lose.type) && data [lose.type].value >= lose.value) {
				haveLost = true;
				break;
			}
		}

		if (haveLost && haveWon) {
			// Won AND lost same frame... do what?
			DebugFn.print ("You WON & LOST!");
		} else if (haveWon) {
			// Won the level!
			DebugFn.print ("You WON!");
		} else if (haveLost) {
			// Won the level!
			DebugFn.print ("You Lost!");
		}
	}

	private class SpecialObjectives {
		private const string OBJECTIVE_TYPE_TIME = "Time";
		private const string OBJECTIVE_TYPE_INFORMATION_HUMAN = "InformationHuman";
		private const string OBJECTIVE_TYPE_INFORMATION_VEHICLE = "InformationVehicle";

		public static List<string> TYPES = new List<string> {
			OBJECTIVE_TYPE_TIME,
			OBJECTIVE_TYPE_INFORMATION_HUMAN,
			OBJECTIVE_TYPE_INFORMATION_VEHICLE
		};

		private static Dictionary<long, InformationHuman> cachedHumans = new Dictionary<long, InformationHuman> ();
		private static Dictionary<long, InformationVehicle> cachedVehicles = new Dictionary<long, InformationVehicle> ();

		public static bool check(Objective objective) {
			switch (objective.type) {
				case OBJECTIVE_TYPE_TIME:
					return checkTime(objective.value);
				case OBJECTIVE_TYPE_INFORMATION_HUMAN:
					return checkInformationHuman (objective);
				case OBJECTIVE_TYPE_INFORMATION_VEHICLE:
					return checkInformationVehicle (objective);
			}
			return false;
		}

		private static bool checkTime(float time) {
			return GameTimer.elapsedTime() >= time;
		}

		private static bool checkInformationHuman(Objective objective) {
			InformationHuman human = getInformationHuman (objective.id);
			if (human != null) {
				switch (objective.key) {
					case "distance":
					return human.distance >= objective.value;
					// TODO - More here - money, mood?
				}
			}
			return false;
		}

		private static InformationHuman getInformationHuman(long id) {
			if (!cachedHumans.ContainsKey (id)) {
				GameObject human = GameObject.Find ("Human (id:" + id + ")");
				if (human != null) {
					InformationHuman informationHuman = human.GetComponent<InformationHuman> ();
					cachedHumans.Add (id, informationHuman);
					informationHuman.getInformation ();
				}
			}
			if (cachedHumans.ContainsKey (id)) {
				return cachedHumans [id];
			}
			return null;
		}

		private static bool checkInformationVehicle(Objective objective) {
			InformationVehicle vehicle = getInformationVehicle (objective.id);
			if (vehicle != null) {
				switch (objective.key) {
				case "distance":
					return vehicle.distance >= objective.value;
					// TODO - More here - condition...?
				}
			}
			return false;
		}

		private static InformationVehicle getInformationVehicle(long id) {
			if (!cachedVehicles.ContainsKey (id)) {
				GameObject human = GameObject.Find ("Vehicle (id:" + id + ")");
				if (human != null) {
					InformationVehicle informationVehicle = human.GetComponent<InformationVehicle> ();
					cachedVehicles.Add (id, informationVehicle);
					informationVehicle.getInformation ();
				}
			}
			if (cachedVehicles.ContainsKey (id)) {
				return cachedVehicles [id];
			}
			return null;
		}
	}
}
