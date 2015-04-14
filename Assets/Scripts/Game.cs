using UnityEngine;
using System.Collections;
using System.Xml;

public class Game : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine (loadXML ());
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	private IEnumerator loadXML () {
		string configFile = "file://" + Application.streamingAssetsPath + "/testmap01.osm"; 
		WWW www = new WWW (configFile);
		
		yield return www;

		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(www.data);

		Debug.Log (xmlDoc.SelectNodes("osm/node"));
	}
}
