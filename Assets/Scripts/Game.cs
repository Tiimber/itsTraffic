using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

// Longitude = x
// Latitude = y

public class Game : MonoBehaviour {

	private string mapFile = "/testmap01.osm";

	public GameObject partOfWay;
	public GameObject partOfNonCarWay;

	// These are not really rects, just four positions minX, minY, maxX, maxY
	private Rect cameraBounds;
	private Rect mapBounds;

	private Vector3 oneVector = new Vector3(1F, 0F, 0F);
	private float wayLengthFactor = 10f;

	private float currentLevel = WayTypeEnum.WayTypes.First<float>();
	private bool showOnlyCurrentLevel = false;

	private int debugIndex = 0;
	private List<string> debugIndexNodes = new List<string> () {
		"none", "endpoint", "straightWay", "intersections"
	};

	// Use this for initialization
	void Start () {
		StartCoroutine (loadXML ());
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Plus)) {
			currentLevel = WayTypeEnum.getLower (currentLevel);
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.Minus)) {
			currentLevel = WayTypeEnum.getHigher (currentLevel);
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.Space)) {
			showOnlyCurrentLevel ^= true;
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.LeftShift)) {
			debugIndex = ++debugIndex % debugIndexNodes.Count;
			WayReference[] wayReferences = FindObjectsOfType<WayReference> ();
			foreach (WayReference wayReference in wayReferences) {
				if (wayReference.OriginalColor != Color.magenta) {
					wayReference.gameObject.GetComponent<Renderer>().material.color = wayReference.OriginalColor;
					wayReference.OriginalColor = Color.magenta;
				}
			}
		}

		// Draw debugIndex stuff
		Dictionary<long, List<Way>> debugWayIndex;
		if (debugIndex > 0) {
			switch (debugIndexNodes[debugIndex]) {
			case "endpoint": debugWayIndex = NodeIndex.endPointIndex; break;
			case "straightWay": debugWayIndex = NodeIndex.straightWayIndex; break;
			case "intersections": debugWayIndex = NodeIndex.intersectionWayIndex; break;
			default: debugWayIndex = null; break;
			}
		} else {
			debugWayIndex = null;
		}
		if (debugWayIndex != null) {
			foreach (long key in debugWayIndex.Keys.ToList()) {
				foreach (Way way in debugWayIndex[key]) {
					WayReference wayReference = way.WayReference;
					GameObject wayObject = wayReference.gameObject;
					Debug.DrawLine(wayObject.transform.position - new Vector3(-.2f, -.2f, 0), wayObject.transform.position + new Vector3(-.2f, -.2f, 0), Color.yellow);
					Debug.DrawLine(wayObject.transform.position - new Vector3(.2f, -.2f, 0), wayObject.transform.position + new Vector3(.2f, -.2f, 0), Color.yellow);
					if (wayReference.OriginalColor == Color.magenta) {
						wayReference.OriginalColor = wayObject.GetComponent<Renderer>().material.color;
					}
					wayObject.GetComponent<Renderer>().material.color = Color.blue;
				}
			}
		}

	}

	private IEnumerator loadXML () {
		string configFile = "file://" + Application.streamingAssetsPath + mapFile; 
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
		mapBounds = new Rect ((float)minlon, (float)minlat, (float)(maxlon - minlon), (float)(maxlat - minlat));

//		Camera mainCamera = Camera.main;
		// TODO - Take these out from the camera
		float cameraMinX = -5F;
		float cameraMinY = -5F;
		float cameraMaxX = 5F;
		float cameraMaxY = 5F;
		cameraBounds = new Rect (cameraMinX, cameraMinY, cameraMaxX - cameraMinX, cameraMaxY - cameraMinY);

		XmlNodeList nodeNodes = xmlDoc.SelectNodes("/osm/node");
		foreach (XmlNode xmlNode in nodeNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long id = Convert.ToInt64(attributes.GetNamedItem("id").Value);
			Pos node = new Pos(id, (float)Convert.ToDecimal(attributes.GetNamedItem("lon").Value), (float)Convert.ToDecimal(attributes.GetNamedItem("lat").Value));
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

		plotMap ();

		NodeIndex.calculateIndexes ();
		Debug.Log (NodeIndex.nodeWayIndex);
	}

	private void addTags (NodeWithTags node, XmlNode xmlNode)
	{
		XmlNodeList tagNodes = xmlNode.SelectNodes ("tag");
		foreach (XmlNode tagNode in tagNodes) {
			XmlAttributeCollection attributes = tagNode.Attributes;
			node.addTag(new Tag(attributes.GetNamedItem("k").Value, attributes.GetNamedItem("v").Value));
		}
//		node.processTags ();
	}

	private void addNodes (Way way, XmlNode xmlNode, Dictionary<long, Pos> nodes)
	{
		XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
		foreach (XmlAttribute refAttribute in nodeRefs) {
			Pos node = nodes[Convert.ToInt64(refAttribute.Value)];
			way.addPos (node);
			NodeIndex.addWayToNode(node.Id, way);
		}
	}

	private void plotMap () {
		foreach (Way way in Map.Ways) {
			Pos prev = null;
			foreach (Pos pos in way.getPoses ()) {
				if (prev != null) {
					Vector3 position = getCameraPosition(pos);
					Vector3 prevPosition = getCameraPosition(prev);
					createPartOfWay(prevPosition, position, way);
				}
				prev = pos;
			}
		}
	}

	private void createPartOfWay (Vector3 position1, Vector3 position2, Way wayObject)
	{
		Vector3 wayVector = position2 - position1;
		Vector3 position = getMidPoint(position1, position2);

		GameObject way;
		Vector3 originalScale;
		if (wayObject.CarWay) {
			way = Instantiate (partOfWay, position, Quaternion.FromToRotation (oneVector, wayVector)) as GameObject;
			originalScale = partOfWay.transform.localScale;
		} else {
			way = Instantiate (partOfNonCarWay, position, Quaternion.FromToRotation (oneVector, wayVector)) as GameObject;
			originalScale = partOfNonCarWay.transform.localScale;
		}
		WayReference wayReference = way.GetComponent<WayReference> ();
		wayReference.way = wayObject;
		wayObject.WayReference = wayReference;
		float currentMapWidthFactor = 5f;
		way.transform.localScale = new Vector3 (Vector3.Magnitude(wayVector) * wayLengthFactor * originalScale.x, 1f * originalScale.y * wayObject.WayWidthFactor * currentMapWidthFactor, 1f * originalScale.z);
	}

	private Vector3 getMidPoint (Vector3 position1, Vector3 position2)
	{
		return ((position2 - position1) / 2) + position1;
	}

	private Vector3 getCameraPosition (Pos pos)
	{
		float posX = pos.Lon;
		float posY = pos.Lat;

		float cameraPosX = ((posX - mapBounds.x) / mapBounds.width) * cameraBounds.width + cameraBounds.x;
		float cameraPosY = ((posY - mapBounds.y) / mapBounds.height) * cameraBounds.height + cameraBounds.y;

		return new Vector3 (cameraPosX, cameraPosY, 0);
	}

//	private void getWaysWithLevel(

	public void OnGUI () {
		GUI.Label(new Rect(0, 0, 100, 20), debugIndexNodes[debugIndex]);
	}

	private void filterWays() 
	{
		Debug.Log ("Current way width level: " + currentLevel);
		WayReference[] wayReferences = FindObjectsOfType<WayReference> ();
		foreach (WayReference wayReference in wayReferences) {
			float currentWayWidthFactor = wayReference.way.WayWidthFactor;
			if (showOnlyCurrentLevel && currentWayWidthFactor == currentLevel) {
				wayReference.GetComponent<Renderer>().enabled = true;
			} else if (!showOnlyCurrentLevel && currentWayWidthFactor >= currentLevel) {
				wayReference.GetComponent<Renderer>().enabled = true;
			} else {
				wayReference.GetComponent<Renderer>().enabled = false;
			}
		}
	}
}
