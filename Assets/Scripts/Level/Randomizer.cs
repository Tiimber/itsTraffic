using System.Collections;
using System.Xml;
using System;

public class Randomizer {

	public bool enabled = true;
	public string time;

	public Randomizer(XmlNode randomizerNode) {
		XmlAttributeCollection randomizerAttributes = randomizerNode.Attributes;
		enabled = Misc.xmlBool(randomizerAttributes.GetNamedItem ("enabled"));
		time = Misc.xmlString(randomizerAttributes.GetNamedItem ("time"));
	}
}
