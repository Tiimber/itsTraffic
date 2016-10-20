using System.Collections.Generic;
using System.Xml;

public class Levels {

    public List<Level> levels = new List<Level>();

	public Levels(XmlDocument xmlDoc) {
		XmlNodeList levelsNode = xmlDoc.SelectNodes ("/levels/level");
        foreach (XmlNode levelNode in levelsNode) {
            levels.Add (new Level(levelNode));
        }
	}
}
