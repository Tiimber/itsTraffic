using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Level {

    public string id;
    public string name;
    public string fileUrl;
    public string iconUrl;
    public string countryCode;
    public float lon;
    public float lat;

    public string brief;
    public string randomSeedStr;
    public int randomSeed;
    public DateTime dateTime;
    public string date;
    public string timeOfDay;
    public int timeProgressionFactor;
    public bool timeDisplaySeconds;
    public string country;
    public string mapUrl;
    public string configUrl;

    public Objectives objectives;
    public PointCalculator pointCalculator;
    public VehicleColors vehicleColors;
    public Randomizer humanRandomizer;
    public Randomizer vehicleRandomizer;
    public List<VehiclesDistribution> vehiclesDistribution;
    public Setup setup;

    public Level(XmlDocument xmlDoc) {
        XmlNode levelNode = xmlDoc.SelectSingleNode ("/level");
        extractLevelDetails (levelNode);

        XmlNode objectivesNode = xmlDoc.SelectSingleNode ("/level/objectives");
        objectives = new Objectives (objectivesNode);

        XmlNode pointCalculatorNode = xmlDoc.SelectSingleNode ("/level/pointCalculator");
        pointCalculator = new PointCalculator (pointCalculatorNode);

        XmlNode vehicleColorsNode = xmlDoc.SelectSingleNode ("/level/vehicleColors");
        vehicleColors = new VehicleColors (vehicleColorsNode);

        XmlNode humanRandomizerNode = xmlDoc.SelectSingleNode ("/level/humanRandomizer");
        humanRandomizer = new Randomizer (humanRandomizerNode, "human");

        XmlNode vehicleRandomizerNode = xmlDoc.SelectSingleNode ("/level/vehicleRandomizer");
        vehicleRandomizer = new Randomizer (vehicleRandomizerNode, "vehicle");

        XmlNode vehiclesDistributionNode = xmlDoc.SelectSingleNode ("/level/vehicleDistributions");
        extractVehiclesDistributions (vehiclesDistributionNode);

        XmlNode setupNode = xmlDoc.SelectSingleNode ("/level/setup");
        setup = new Setup (setupNode);
    }

    public void extractVehiclesDistributions(XmlNode vehiclesDistributionNode) {
        // Parse from XML
        vehiclesDistribution = new List<VehiclesDistribution> ();
        XmlNodeList vehiclesDistributionNodes = vehiclesDistributionNode.SelectNodes("vehicle");
        foreach (XmlNode vehicle in vehiclesDistributionNodes) {
            string brand = Misc.xmlString (vehicle.Attributes.GetNamedItem ("brand"));
            float frequency = Misc.xmlFloat (vehicle.Attributes.GetNamedItem ("frequency"));

            // Merge in Vehicle object from Game.cs
            VehiclesDistribution defaultVehicleDistribution = Game.instance.vehicles.Find (distributionVehicle => distributionVehicle.brand == brand);
            if (defaultVehicleDistribution != null) {
                // Add to the list (only valid in this case)
                vehiclesDistribution.Add (new VehiclesDistribution (brand, frequency, defaultVehicleDistribution.vehicle));
            }
        }
    }

    public Level(XmlNode levelNode) {
        extractLevelDetails (levelNode);
    }

    private void extractLevelDetails(XmlNode levelNode) {
        XmlAttributeCollection levelAttributes = levelNode.Attributes;
        id = Misc.xmlString (levelAttributes.GetNamedItem ("id"));
        name = Misc.xmlString (levelAttributes.GetNamedItem ("name"));
        fileUrl = Misc.xmlString (levelAttributes.GetNamedItem ("fileUrl"));
        iconUrl = Misc.xmlString (levelAttributes.GetNamedItem ("iconUrl"));
        countryCode = Misc.xmlString (levelAttributes.GetNamedItem ("countrycode"));
        lon = Misc.xmlFloat (levelAttributes.GetNamedItem ("lon"));
        lat = Misc.xmlFloat (levelAttributes.GetNamedItem ("lat"));

        brief = Misc.xmlString (levelAttributes.GetNamedItem ("brief"));
        randomSeedStr = Misc.xmlString (levelAttributes.GetNamedItem ("randomSeed"));
        if (randomSeedStr != null) {
            randomSeed = randomSeedStr.GetHashCode ();
            //		    DebugFn.print ("Random seed: " + randomSeedStr + ", hash: " + randomSeed);
        }
        date = Misc.xmlString (levelAttributes.GetNamedItem ("date"), DateTime.Now.ToString("yyyy-MM-dd"));
        timeOfDay = Misc.xmlString (levelAttributes.GetNamedItem ("timeOfDay"), "10:00");
        dateTime = Misc.parseDateTime(date, timeOfDay);
        timeProgressionFactor = Misc.xmlInt (levelAttributes.GetNamedItem ("timeProgressionFactor"), 1);
        timeDisplaySeconds = Misc.xmlBool (levelAttributes.GetNamedItem ("timeDisplaySeconds"), true);

        PubSub.publish ("clock:setTime", timeOfDay);
        PubSub.publish ("clock:setDisplaySeconds", timeDisplaySeconds);
        PubSub.publish ("clock:setSpeed", timeProgressionFactor);
        PubSub.publish ("clock:stop");

        country = Misc.xmlString (levelAttributes.GetNamedItem ("country"));
        mapUrl = Misc.xmlString (levelAttributes.GetNamedItem ("mapUrl"));
        configUrl = Misc.xmlString (levelAttributes.GetNamedItem ("configUrl"));
    }

}
