using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

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
		xmlDoc.LoadXml(www.text);

		Dictionary<long, Pos> nodes = new Dictionary<long, Pos> ();

		XmlNode boundsNode = xmlDoc.SelectSingleNode ("/osm/bounds");
		XmlAttributeCollection boundsAttributes = boundsNode.Attributes;
		decimal minlat = Convert.ToDecimal (boundsAttributes.GetNamedItem ("minlat").Value);
		decimal maxlat = Convert.ToDecimal (boundsAttributes.GetNamedItem ("maxlat").Value);
		decimal minlon = Convert.ToDecimal (boundsAttributes.GetNamedItem ("minlon").Value);
		decimal maxlon = Convert.ToDecimal (boundsAttributes.GetNamedItem ("maxlon").Value);

		Camera mainCamera = Camera.main;
		mainCamera.rect = new Rect ((float)minlon, (float)maxlon, (float)minlat, (float)maxlat);

		XmlNodeList nodeNodes = xmlDoc.SelectNodes("/osm/node");
		foreach (XmlNode xmlNode in nodeNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long id = Convert.ToInt64(attributes.GetNamedItem("id").Value);
			Pos node = new Pos(id, Convert.ToDecimal(attributes.GetNamedItem("lon").Value), Convert.ToDecimal(attributes.GetNamedItem("lat").Value));
			addTags(node, xmlNode);
			nodes.Add (id, node);
		}
		Map.Nodes = nodes.Values.ToList();

		XmlNodeList wayNodes = xmlDoc.SelectNodes("/osm/way");
		foreach (XmlNode xmlNode in wayNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			Way way = new Way(Convert.ToInt64(attributes.GetNamedItem("id").Value));
			addTags(way, xmlNode);
			addNodes(way, xmlNode, nodes);

			Map.Ways.Add(way);
		}
	}

	private void addTags (NodeWithTags node, XmlNode xmlNode)
	{
		XmlNodeList tagNodes = xmlNode.SelectNodes ("tag");
		foreach (XmlNode tagNode in tagNodes) {
			XmlAttributeCollection attributes = tagNode.Attributes;
			node.addTag(new Tag(attributes.GetNamedItem("k").Value, attributes.GetNamedItem("v").Value));
		}
	}

	private void addNodes (Way way, XmlNode xmlNode, Dictionary<long, Pos> nodes)
	{
		XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
		foreach (XmlAttribute refAttribute in nodeRefs) {
			way.addPos(nodes[Convert.ToInt64(refAttribute.Value)]);
		}
	}
}
