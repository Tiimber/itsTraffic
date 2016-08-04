using System.Collections;
using System.Xml;
using System;

public class Randomizer {

	public bool enabled = true;
	public float delay;
	public float interval;
	public float variation;
	public float minInterval;
	public float intervalDecreaseRate;

	public Randomizer(XmlNode randomizerNode, string type) {
		bool isHuman = type == "human";
		XmlAttributeCollection randomizerAttributes = randomizerNode.Attributes;
		enabled = Misc.xmlBool(randomizerAttributes.GetNamedItem ("enabled"), true);
		delay = Misc.xmlFloat(randomizerAttributes.GetNamedItem ("delay"), ObjectRandomizer.DEFAULT_DELAY);
		interval = Misc.xmlFloat(randomizerAttributes.GetNamedItem ("interval"), isHuman ? HumanRandomizer.START_INTERVAL : VehicleRandomizer.START_INTERVAL);
		variation = Misc.xmlFloat(randomizerAttributes.GetNamedItem ("variation"), isHuman ? HumanRandomizer.RANDOM_VARIATION : VehicleRandomizer.RANDOM_VARIATION);
		minInterval = Misc.xmlFloat(randomizerAttributes.GetNamedItem ("minInterval"), isHuman ? HumanRandomizer.MIN_INTERVAL : VehicleRandomizer.MIN_INTERVAL);
		intervalDecreaseRate = Misc.xmlFloat(randomizerAttributes.GetNamedItem ("intervalDecreaseRate"), isHuman ? HumanRandomizer.INTERVAL_DECREASE_RATE : VehicleRandomizer.INTERVAL_DECREASE_RATE);
	}
}
