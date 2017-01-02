using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Cities {

    public List<City> cities = new List<City>();

	public Cities(XmlDocument xmlDoc) {
		XmlNodeList cityNodes = xmlDoc.SelectNodes ("/cities/city");
        foreach (XmlNode cityNode in cityNodes) {
            cities.Add (new City(cityNode));
        }

        // Set a "level" value, to classify them based on population (display on certain zoom level)
        int index = 0;
        foreach (City city in cities) {
            float pct = (float) index / cities.Count;
            city.level = city.capital ? 1 : Mathf.Max(Mathf.CeilToInt(pct * 10), 1);
            // Debug.Log(pct + " = " + city.level);
            index++;
        }
	}

    public class City {
        public string name;
        public bool capital;
        public float lon;
        public float lat;
        public int population;
        public int level;

        public City (XmlNode XmlNode) {
            XmlAttributeCollection cityAttributes = XmlNode.Attributes;
            name = Misc.xmlString(cityAttributes.GetNamedItem("name"));
            capital = Misc.xmlBool(cityAttributes.GetNamedItem("capital"), false);
            lon = Misc.xmlFloat(cityAttributes.GetNamedItem("lon"));
            lat = Misc.xmlFloat(cityAttributes.GetNamedItem("lat"));
            population = Misc.xmlInt(cityAttributes.GetNamedItem("population"));
        } 
    }

    public class CityObj : MonoBehaviour {
        public City city;

        public float orthoOrg = 5f;
        private float orthoCurr;
        private Vector3 scaleOrg;

        void Start() {
            scaleOrg  = transform.localScale;
            // Set city name
            Transform nameObject = transform.Find("CityNameContainer/CityName");
            nameObject.GetComponent<TextMesh>().text = name;
        }

        void Update() {
            float osize = Camera.main.orthographicSize;
            if (orthoCurr != osize) {
                transform.localScale = scaleOrg * osize / orthoOrg;
                orthoCurr = osize;
            }
        }

        public void setOriginalOrtho (float ortho) {
            orthoCurr = ortho;
        }

        public void setVisibleLevel(int level) {
            this.gameObject.SetActive(city.level <= level);
        }
    }
}
