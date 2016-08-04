using System.Collections;
using System.Xml;

public class Level {

	public string name;
	public int randomSeed;
	public string timeOfDay;
	public string mapUrl;
	public string configUrl;

	public Objectives objectives;
	public Randomizer humanRandomizer;
	public Randomizer vehicleRandomizer;
	public Setup setup;

	public Level(XmlDocument xmlDoc) {
		XmlNode levelNode = xmlDoc.SelectSingleNode ("/level");
		extractLevelDetails (levelNode);

		XmlNode objectivesNode = xmlDoc.SelectSingleNode ("/level/objectives");
		objectives = new Objectives (objectivesNode);

		XmlNode humanRandomizerNode = xmlDoc.SelectSingleNode ("/level/humanRandomizer");
		humanRandomizer = new Randomizer (humanRandomizerNode, "human");

		XmlNode vehicleRandomizerNode = xmlDoc.SelectSingleNode ("/level/vehicleRandomizer");
		vehicleRandomizer = new Randomizer (vehicleRandomizerNode, "vehicle");

		XmlNode setupNode = xmlDoc.SelectSingleNode ("/level/setup");
		setup = new Setup (setupNode);
	}

	private void extractLevelDetails(XmlNode levelNode) {
		XmlAttributeCollection levelAttributes = levelNode.Attributes;
		name = Misc.xmlString(levelAttributes.GetNamedItem ("name"));
		string randomSeedStr = Misc.xmlString(levelAttributes.GetNamedItem ("randomSeed"));
		randomSeed = randomSeedStr.GetHashCode ();
		timeOfDay = Misc.xmlString(levelAttributes.GetNamedItem ("timeOfDay"));
		mapUrl = Misc.xmlString(levelAttributes.GetNamedItem ("mapUrl"));
		configUrl = Misc.xmlString(levelAttributes.GetNamedItem ("configUrl"));
	}
}
