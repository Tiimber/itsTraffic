using System.Collections.Generic;
using System.Xml;

public class Levels {

    public List<Level> levels = new List<Level>();
    public bool hasPrevious = false;
    public bool hasNext = false;

	public Levels(XmlDocument xmlDoc) {
        XmlAttributeCollection levelsAttributes = xmlDoc.SelectSingleNode("/levels").Attributes;
        hasPrevious = Misc.xmlBool(levelsAttributes.GetNamedItem("hasPrevious"), false);
        hasNext = Misc.xmlBool(levelsAttributes.GetNamedItem("hasNext"), false);
		XmlNodeList levelsNode = xmlDoc.SelectNodes ("/levels/level");
        foreach (XmlNode levelNode in levelsNode) {
            levels.Add (new Level(levelNode));
        }
	}
}
