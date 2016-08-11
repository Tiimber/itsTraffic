using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class Objectives {
	public List<Objective> winObjectives = new List<Objective>();
	public List<Objective> loseObjectives = new List<Objective>();

	public Objectives(XmlNode objectivesNode) {
		XmlNodeList winNodes = objectivesNode.SelectNodes("win");
		foreach (XmlNode winNode in winNodes) {
			XmlAttributeCollection winAttributes = winNode.Attributes;
			string type = Misc.xmlString(winAttributes.GetNamedItem ("type"));
			float value = Misc.xmlFloat(winAttributes.GetNamedItem ("value"));
			winObjectives.Add (new Objective (type, value));
		}

		XmlNodeList loseNodes = objectivesNode.SelectNodes("lose");
		foreach (XmlNode loseNode in loseNodes) {
			XmlAttributeCollection loseAttributes = loseNode.Attributes;
			string type = Misc.xmlString(loseAttributes.GetNamedItem ("type"));
			float value = Misc.xmlFloat(loseAttributes.GetNamedItem ("value"));
			loseObjectives.Add (new Objective (type, value));
		}

		DataCollector.registerObjectiveReporter (this);
	}

	public class Objective {
		public string type;
		public float value;

		public Objective(string type, float value) {
			this.type = type;
			this.value = value;
		}
	}

	public void reportChange() {
		Dictionary<string, DataCollector.InnerData> data = DataCollector.Data;
		bool haveWon = true;
		foreach (Objective win in winObjectives) {
			if (!data.ContainsKey (win.type) || data [win.type].value < win.value) {
				haveWon = false;
				break;
			}
		}
		bool haveLost = false;
		foreach (Objective lose in loseObjectives) {
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
}
