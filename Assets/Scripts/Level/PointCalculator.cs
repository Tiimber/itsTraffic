using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class PointCalculator {

    private const int VEHICLE_DESTINATION_DEFAULT_POINTS = 100;
    private const int HUMAN_DESTINATION_DEFAULT_POINTS = 50;

    public static int vehicleDestinationPoints = VEHICLE_DESTINATION_DEFAULT_POINTS;
    public static int humanDestinationPoints = HUMAN_DESTINATION_DEFAULT_POINTS;

    public int oneStar;
    public int twoStars;
    public int threeStars;
    public List<Point> points;
    public Point intervalTimePoint = null;

    public PointCalculator(XmlNode pointCalculatorNode) {
        XmlAttributeCollection pointCalculatorAttributes = pointCalculatorNode.Attributes;
        oneStar = Misc.xmlInt (pointCalculatorAttributes.GetNamedItem ("oneStar"), 1000);
        twoStars = Misc.xmlInt (pointCalculatorAttributes.GetNamedItem ("twoStars"), 2000);
        threeStars = Misc.xmlInt (pointCalculatorAttributes.GetNamedItem ("threeStars"), 3000);

        // Reset to default points
        vehicleDestinationPoints = VEHICLE_DESTINATION_DEFAULT_POINTS;
        humanDestinationPoints = HUMAN_DESTINATION_DEFAULT_POINTS;
        DataCollector.registerPointCalculator (null);

        points = new List<Point> ();
        XmlNodeList pointNodes = pointCalculatorNode.SelectNodes ("point");
        foreach (XmlNode pointNode in pointNodes) {
            var point = new Point (pointNode);
            points.Add (point);

            if (point.type == Point.TYPE_VEHICLE) {
                PointCalculator.vehicleDestinationPoints = point.value;
            } else if (point.type == Point.TYPE_HUMAN) {
                PointCalculator.humanDestinationPoints = point.value;
            } else if (point.type == Point.TYPE_TIME) {
                intervalTimePoint = point;
                DataCollector.registerPointCalculator (this);
            }
        }
    }

    public void reportElapsedTime(float time) {
        float previousTime = time - Time.deltaTime;
        if (previousTime % intervalTimePoint.threshold > time % intervalTimePoint.threshold) {
            PubSub.publish ("points:inc", intervalTimePoint.value);
        }
    }

    public List<Point> getPoints(bool includedPoints, Objectives objectives = null) {
        List<Point> pointsFiltered = points.FindAll(i => includedPoints == Point.includedPoints.Contains(i.type));
        pointsFiltered.ForEach(i => i.calculate(objectives));
        pointsFiltered.Sort((a, b) => a.value - b.value);
        return pointsFiltered;
    }

    public int getNumberOfStars(int points) {
        // TODO - Logic (for non custom levels) - 4 & 5 stars
        if (points >= threeStars) {
            return 3;
        } else if (points >= twoStars) {
            return 2;
        } else if (points >= oneStar) {
            return 1;
        }
        return 0;
    }

    public class Point {

        public const string TYPE_VEHICLE = "Vehicle";
        public const string TYPE_HUMAN = "Human";
        public const string TYPE_TIME = "Time";
        public const string TYPE_VEHICLE_DESTROY = "Vehicle:destroy";
        public const string TYPE_EMISSION = "Vehicle:emission";
        public const string TYPE_OBJECTIVE = "Objective";
        public const string TYPE_SUMMARY_TIME = "SummaryTime";

        public const string TYPE_TOTAL_POINTS = "Total Points";

        public static List<string> includedPoints = new List<string>() {
            TYPE_VEHICLE,
            TYPE_HUMAN,
            TYPE_TIME
        };

        public string type;
        public string label;
        public long id;
        public long threshold;
        public int value;

        public float amount;
        public int calculatedValue;

        public Point(XmlNode pointNode) {
            XmlAttributeCollection pointAttributes = pointNode.Attributes;
            type = Misc.xmlString (pointAttributes.GetNamedItem ("type"));
            label = Misc.xmlString (pointAttributes.GetNamedItem ("label"));
            id = Misc.xmlLong (pointAttributes.GetNamedItem ("id"), -1L);
            threshold = Misc.xmlLong (pointAttributes.GetNamedItem ("threshold"), -1L);
            value = Misc.xmlInt (pointAttributes.GetNamedItem ("value"));
        }

        public Point(string type, string label, int calculatedValue) {
            this.type = type;
            this.label = label;
            this.calculatedValue = calculatedValue;
        }

        public bool hasOwnAmountCalculation() {
            switch(type) {
                case TYPE_SUMMARY_TIME:
                case TYPE_TOTAL_POINTS:
                    return true;
                default:
                    return false;
            }
        }

        public float getOwnAmount() {
            float ownAmount = 0f;
            switch(type) {
                case TYPE_SUMMARY_TIME:
                    bool isNegative = value < 0f;
                    if (isNegative) {
                        ownAmount = amount > threshold ? (amount - threshold) : 0f;
                    } else {
                        ownAmount = amount < threshold ? (threshold - amount) : 0f;
                    }
                    break;
                case TYPE_TOTAL_POINTS:
                    ownAmount = -1f;
                    break;
            }
            return ownAmount;
        }

        public void calculate(Objectives objectives) {
            switch(type) {
                case TYPE_TIME:
                    amount = DataCollector.GetValue ("Elapsed Time");
                    calculatedValue = Mathf.FloorToInt(amount / threshold) * value;
                    break;
                case TYPE_VEHICLE:
                    amount = DataCollector.GetValue ("Vehicles reached goal");
                    calculatedValue = (int) amount * value;
                    break;
                case TYPE_HUMAN:
                    amount = DataCollector.GetValue ("Humans reached goal");
                    calculatedValue = (int) amount * value;
                    break;
                case TYPE_SUMMARY_TIME:
                    bool isNegative = value < 0f;
                    amount = DataCollector.GetValue("Elapsed Time");
                    if (isNegative) {
                        calculatedValue = amount > threshold ? Mathf.FloorToInt((amount - threshold) * value) : 0;
                    } else {
                        calculatedValue = amount < threshold ? Mathf.FloorToInt((threshold - amount) * value) : 0;
                    }
                    break;
                case TYPE_OBJECTIVE:
                    if (objectives != null) {
                        calculatedValue = objectives.get(id).isMet ? value : 0;
                    }
                    break;
                case TYPE_EMISSION:
                    amount = DataCollector.GetValue("Vehicle:emission");
                    calculatedValue = Mathf.FloorToInt(amount * value);
                    break;
                case TYPE_VEHICLE_DESTROY:
                    amount = DataCollector.GetValue("Vehicles:destroy");
                    calculatedValue = Mathf.FloorToInt(amount * value);
                    break;
            }
        }
    }
}
