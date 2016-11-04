using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class VehicleColors {

    private List<VehicleColors.Vehicle> vehicles = new List<VehicleColors.Vehicle> ();

    public VehicleColors(XmlNode vehicleColors) {
        vehicles.Clear ();
        XmlNodeList vehicleNodes = vehicleColors.SelectNodes ("vehicle");
        foreach (XmlNode vehicleNode in vehicleNodes) {
            vehicles.Add (new VehicleColors.Vehicle (vehicleNode));
        }
    }

    public string getRandomColorForBrand(string brand) {
        string randomSelectedColor = null;
        VehicleColors.Vehicle vehicleColors = vehicles.Find (vehicle => vehicle.brand == brand);
        if (vehicleColors != null) {
            randomSelectedColor = vehicleColors.getRandomColor ();
        }
        return randomSelectedColor;
    }

    private class Vehicle {

        public string brand;
        private int totalFrequencyAmount;
        private SortedDictionary<int, VehicleColors.Vehicle.Color> colors;

        public Vehicle(XmlNode vehicleNode) {
            totalFrequencyAmount = 0;
            colors = new SortedDictionary<int, VehicleColors.Vehicle.Color> ();

            brand = Misc.xmlString (vehicleNode.Attributes.GetNamedItem ("brand"));
            XmlNodeList colorNodes = vehicleNode.SelectNodes ("color");
            foreach (XmlNode colorNode in colorNodes) {
                VehicleColors.Vehicle.Color color = new VehicleColors.Vehicle.Color (colorNode);
                totalFrequencyAmount += color.frequency;
                colors.Add (totalFrequencyAmount, color);
            }
        }

        public string getRandomColor() {
            string randomColor = null;
            if (colors.Count > 0) {
                int randomPosition = Misc.randomRange (0, totalFrequencyAmount);
                foreach (KeyValuePair<int, VehicleColors.Vehicle.Color> colorKeyValuePair in colors) {
                    if (randomPosition < colorKeyValuePair.Key) {
                        randomColor = colorKeyValuePair.Value.getRandomVariant ();
                        break;
                    }
                }
            }
            return randomColor;
        }

        private class Color {

            private string color;
            private string span;
            public int frequency;

            public Color(XmlNode colorNode) {
                color = Misc.xmlString (colorNode.Attributes.GetNamedItem ("value"));
                span = Misc.xmlString (colorNode.Attributes.GetNamedItem ("span"));
                frequency = Misc.xmlInt (colorNode.Attributes.GetNamedItem ("frequency"));
            }

            public string getRandomVariant() {
                List<int> colorRGB = Misc.splitInts (color);
                List<int> colorSpanRBG = Misc.splitInts (span);
                int rMod = colorSpanRBG [0] > 0 ? Misc.randomRange (-colorSpanRBG [0] + 1, colorSpanRBG [0]) : 0;
                int gMod = colorSpanRBG [1] > 0 ? Misc.randomRange (-colorSpanRBG [1] + 1, colorSpanRBG [1]) : 0;
                int bMod = colorSpanRBG [2] > 0 ? Misc.randomRange (-colorSpanRBG [2] + 1, colorSpanRBG [2]) : 0;
                int r = Mathf.Clamp (colorRGB [0] + rMod, 0, 255);
                int g = Mathf.Clamp (colorRGB [1] + gMod, 0, 255);
                int b = Mathf.Clamp (colorRGB [2] + bMod, 0, 255);
                string randomColor = r + "," + g + "," + b;
                return randomColor;
            }

        }

    }

}
