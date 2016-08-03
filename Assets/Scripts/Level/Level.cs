using System.Collections;
using System.Xml;

public class Level {

	public string name;
	public string randomSeed;
	public string timeOfDay;
	public string mapUrl;
	public string configUrl;

	public Objectives objectives;
	public Randomizer randomizer;
	public Setup setup;

	public Level(XmlDocument xmlDoc) {
		XmlNode levelNode = xmlDoc.SelectSingleNode ("/level");
		extractLevelDetails (levelNode);

		XmlNode objectivesNode = xmlDoc.SelectSingleNode ("/level/objectives");
		objectives = new Objectives (objectivesNode);

		XmlNode randomizerNode = xmlDoc.SelectSingleNode ("/level/randomizer");
		randomizer = new Randomizer (randomizerNode);

		XmlNode setupNode = xmlDoc.SelectSingleNode ("/level/setup");
		setup = new Setup (setupNode);

		string a = setup.ToString();
	}

	private void extractLevelDetails(XmlNode levelNode) {
		XmlAttributeCollection levelAttributes = levelNode.Attributes;
		name = Misc.xmlString(levelAttributes.GetNamedItem ("name"));
		randomSeed = Misc.xmlString(levelAttributes.GetNamedItem ("randomSeed"));
		timeOfDay = Misc.xmlString(levelAttributes.GetNamedItem ("timeOfDay"));
		mapUrl = Misc.xmlString(levelAttributes.GetNamedItem ("mapUrl"));
		configUrl = Misc.xmlString(levelAttributes.GetNamedItem ("configUrl"));
	}
}
