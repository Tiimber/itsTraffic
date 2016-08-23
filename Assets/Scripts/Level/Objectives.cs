﻿using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System;

public class Objectives {

	public List<List<long>> winCombos = new List<List<long>>();
	public List<List<long>> loseCombos = new List<List<long>> ();
	public List<Objective> winObjectives = new List<Objective>();
	public List<Objective> loseObjectives = new List<Objective>();


	public Objectives(XmlNode objectivesNode) {
		XmlNodeList winNodes = objectivesNode.SelectNodes("win");
		foreach (XmlNode winNode in winNodes) {
			XmlAttributeCollection winAttributes = winNode.Attributes;
			long id = Misc.xmlLong(winAttributes.GetNamedItem ("id"));
			string type = Misc.xmlString(winAttributes.GetNamedItem ("type"));
			long targetId = Misc.xmlLong (winAttributes.GetNamedItem ("targetId"));
			string key = Misc.xmlString (winAttributes.GetNamedItem ("key"));
			float value = Misc.xmlFloat(winAttributes.GetNamedItem ("value"));
			winObjectives.Add (new Objective (id, type, targetId, key, value));
		}

		XmlNodeList loseNodes = objectivesNode.SelectNodes("lose");
		foreach (XmlNode loseNode in loseNodes) {
			XmlAttributeCollection loseAttributes = loseNode.Attributes;
			long id = Misc.xmlLong(loseAttributes.GetNamedItem ("id"));
			string type = Misc.xmlString(loseAttributes.GetNamedItem ("type"));
			long targetId = Misc.xmlLong (loseAttributes.GetNamedItem ("targetId"));
			string key = Misc.xmlString (loseAttributes.GetNamedItem ("key"));
			float value = Misc.xmlFloat(loseAttributes.GetNamedItem ("value"));
			loseObjectives.Add (new Objective (id, type, targetId, key, value));
		}

		XmlAttributeCollection objectiveAttributes = objectivesNode.Attributes;
		if (objectiveAttributes.GetNamedItem ("winCombos") != null) {
			winCombos = Misc.parseLongMultiList (Misc.xmlString(objectiveAttributes.GetNamedItem ("winCombos")), ';', ',');
		} else {
			List<long> winCombo = new List<long> ();
			foreach (Objective winObjective in winObjectives) {
				winCombo.Add (winObjective.id);
			}
			winCombos.Add (winCombo);
		}

		if (objectiveAttributes.GetNamedItem ("loseCombos") != null) {
			loseCombos = Misc.parseLongMultiList (Misc.xmlString(objectiveAttributes.GetNamedItem ("loseCombos")), ';', ',');
		} else {
			foreach (Objective loseObjective in loseObjectives) {
				loseCombos.Add (new List<long> () {
					loseObjective.id
				});
			}
		}


		DataCollector.registerObjectiveReporter (this);
	}

	public class Objective {
		public long id;
		public string type;
		public long targetId;
		public string key;
		public float value;

		public Objective(long id, string type, long targetId, string key, float value) {
			this.id = id;
			this.type = type;
			this.targetId = targetId;
			this.key = key;
			this.value = value;
		}
	}

	public void reportChange() {
		bool haveWon = checkCombos(winCombos, "win");
		bool haveLost = checkCombos(loseCombos, "lose");

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

	private List<Objective> getObjectives (string type, List<long> ids) {
		return (type == "win" ? winObjectives : loseObjectives).FindAll(i => ids.Contains(i.id));
	}

	private bool checkCombos(List<List<long>> combos, string type) {
		Dictionary<string, DataCollector.InnerData> data = DataCollector.Data;
		foreach (List<long> combo in combos) {
			List<Objective> checkObjectives = getObjectives (type, combo);
			bool haveMetAll = true;
			foreach (Objective objective in checkObjectives) {
				if (SpecialObjectives.TYPES.Contains (objective.type)) {
					if (!SpecialObjectives.check (objective)) {
						haveMetAll = false;
						break;
					} else {
						continue;
					}
				}

				if (!data.ContainsKey (objective.type) || data [objective.type].value < objective.value) {
					haveMetAll = false;
					break;
				}
			}
			if (haveMetAll) {
				return true;
			}
		}
		return false;
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
			InformationHuman human = getInformationHuman (objective.targetId);
			if (human != null) {
				switch (objective.key) {
					case "distance":
						return human.distance >= objective.value;
					// TODO - More here - money, mood?
				}
			}
			return false;
		}

		private static InformationHuman getInformationHuman(long targetId) {
			if (!cachedHumans.ContainsKey (targetId)) {
				GameObject human = GameObject.Find ("Human (id:" + targetId + ")");
				if (human != null) {
					InformationHuman informationHuman = human.GetComponent<InformationHuman> ();
					cachedHumans.Add (targetId, informationHuman);
					informationHuman.getInformation ();
				}
			}
			if (cachedHumans.ContainsKey (targetId)) {
				return cachedHumans [targetId];
			}
			return null;
		}

		private static bool checkInformationVehicle(Objective objective) {
			InformationVehicle vehicle = getInformationVehicle (objective.targetId);
			if (vehicle != null) {
				switch (objective.key) {
					case "distance":
						return vehicle.distance >= objective.value;
					// TODO - More here - condition...?
				}
			}
			return false;
		}

		private static InformationVehicle getInformationVehicle(long targetId) {
			if (!cachedVehicles.ContainsKey (targetId)) {
				GameObject human = GameObject.Find ("Vehicle (id:" + targetId + ")");
				if (human != null) {
					InformationVehicle informationVehicle = human.GetComponent<InformationVehicle> ();
					cachedVehicles.Add (targetId, informationVehicle);
					informationVehicle.getInformation ();
				}
			}
			if (cachedVehicles.ContainsKey (targetId)) {
				return cachedVehicles [targetId];
			}
			return null;
		}
	}
}
