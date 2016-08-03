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
			string value = Misc.xmlString(winAttributes.GetNamedItem ("value"));
			winObjectives.Add (new Win (type, value));
		}

		XmlNodeList loseNodes = objectivesNode.SelectNodes("lose");
		foreach (XmlNode loseNode in loseNodes) {
			XmlAttributeCollection loseAttributes = loseNode.Attributes;
			string type = Misc.xmlString(loseAttributes.GetNamedItem ("type"));
			string value = Misc.xmlString(loseAttributes.GetNamedItem ("value"));
			loseObjectives.Add (new Lose (type, value));
		}
	}

	public class Objective {
		public string objectiveType;
		public string type;
		public string value;

		public Objective(string objectiveType, string type, string value) {
			this.objectiveType = objectiveType;
			this.type = type;
			this.value = value;
		}
	}

	class Win : Objective {
		private static string TYPE = "WIN";
		public Win(string type, string value) : base(TYPE, type, value) {}
	}

	class Lose : Objective {
		private static string TYPE = "LOSE";
		public Lose(string type, string value) : base(TYPE, type, value) {}
	}
}
