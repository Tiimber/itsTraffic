using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

// Longitude = x
// Latitude = y
using UnityStandardAssets.Cameras;

public class Game : MonoBehaviour, IPubSub {

	public Camera mainCamera;
	public Camera introCamera;
	public Camera pointsCamera;

	public static Game instance;
	private int animationItemsQueue = 0;
	private float cameraEmission = 0f;

	public static long randomSeed;
	private static bool running = false;

	public static KeyValuePair<Pos, WayReference> CurrentWayReference { set; get; }
	public static KeyValuePair<Pos, WayReference> CurrentTarget { set; get; }
	public static List<Pos> CurrentPath { set; get; }

//	private string mapFileName = "http://samlingar.com/itsTraffic/testmap01.osm";
//	private string mapFileName = "file:///home/anders/Programmering/itsTraffic/Assets/StreamingAssets/testmap08.osm";
//	private string mapFileName = "file:///home/anders/Programmering/itsTraffic/Assets/StreamingAssets/testmap01.osm";
	private string mapFileName = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/testmap01.osm";
	private string configFileName = "http://samlingar.com/itsTraffic/testmap03-config.xml";
//	private string configFileName = "file:///home/anders/Programmering/itsTraffic/Assets/StreamingAssets/testmap08-config.xml";

	public GameObject partOfWay;
	public GameObject partOfNonCarWay;
	public GameObject vehicle;
	public GameObject buildingObject;
	public GameObject landuseObject;
	public GameObject trafficLight;
	public GameObject treeObject;
	public GameObject vehicleEmission;
	public GameObject wayCrossing;

	// These are not really rects, just four positions minX, minY, maxX, maxY
	private static Rect cameraBounds;
	private static Rect mapBounds;

	// TODO - When switched to ortographic camera, set this in those objects
	public static float cameraOrtographicSize = 5f;
	public static float heightFactor;

	private Vector3 oneVector = Vector3.right;
	
	private float currentLevel = WayTypeEnum.WayTypes.First<float>();
	private bool showOnlyCurrentLevel = false;
	private bool followCar = false;
	private Vector3 prevMousePosition;

	private Dictionary<long, Dictionary<string, string>> objectProperties = new Dictionary<long, Dictionary<string, string>>();

	private int debugIndex = 0;
	private List<string> debugIndexNodes = new List<string> () {
		"none", "endpoint", "straightWay", "intersections", "all"
	};

	// Use this for initialization
	void Start () {
		Game.instance = this;
		Game.randomSeed = Misc.currentTimeMillis ();

		StartCoroutine (MaterialManager.Init ());
		StartCoroutine (loadXML ());

		CameraHandler.SetMinZoom (cameraOrtographicSize);
		CameraHandler.SetMainCamera (mainCamera);
		PubSub.subscribe ("mainCameraActivated", this);

//		Time.timeScale = 0.1f;
		// Subscribe to when emission is let out from vehicles
		PubSub.subscribe ("Vehicle:emitGas", this);

		Game.running = true;
		new VehicleRandomizer ();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Plus) || Input.GetKeyDown (KeyCode.P)) {
			currentLevel = WayTypeEnum.getLower (currentLevel);
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.M)) {
			currentLevel = WayTypeEnum.getHigher (currentLevel);
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.Space)) {
			showOnlyCurrentLevel ^= true;
			currentLevel = 0.111f;
			filterWays ();
		} else if (Input.GetKeyDown (KeyCode.LeftShift)) {
			debugIndex = ++debugIndex % debugIndexNodes.Count;
			WayReference[] wayReferences = FindObjectsOfType<WayReference> ();
			foreach (WayReference wayReference in wayReferences) {
				if (wayReference.OriginalColor != Color.magenta) {
					wayReference.gameObject.GetComponent<Renderer> ().material.color = wayReference.OriginalColor;
					wayReference.OriginalColor = Color.magenta;
				}
			}
		} else if (Input.GetKeyDown (KeyCode.N)) {
			GameObject car = null; 
//			GameObject car = GameObject.Find ("Camaro(ish)(Clone)");
			if (car != null) {
				Vehicle carObj = car.GetComponent<Vehicle> ();
				carObj.fadeOutAndDestroy ();
			} else {
				createNewCar ();
			}
		} else if (Input.GetKeyDown (KeyCode.F)) {
			followCar ^= true;
			if (!followCar) {
				Vehicle.detachCurrentCamera ();
				mainCamera.enabled = true;
			}
		} else if (Input.GetKeyDown (KeyCode.Q)) {
			PubSub.publish("points:inc", 13579);
		} else if (Input.GetKeyDown (KeyCode.W)) {
			PubSub.publish("points:dec", 24680);
		}


//		foreach(KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
//		{
//			if (Input.GetKeyDown(kcode))
//				Debug.Log("KeyCode down: " + kcode);
//		}

		// Left mouse button
		if (Input.GetMouseButton (0)) {
			// Drag logic
			bool firstFrame = Input.GetMouseButtonDown (0);
			Vector3 mousePosition = Input.mousePosition;

			if (!firstFrame) {
				Vector3 diffMove = mousePosition - prevMousePosition;
				CameraHandler.Move(diffMove);
			}
			prevMousePosition = mousePosition;

			// Click logic
			if (firstFrame) {
				Vector3 mouseWorldPoint = mainCamera.ScreenToWorldPoint(mousePosition);
				PubSub.publish ("Click", mouseWorldPoint);
			}
		}

		if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
			float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
//			Debug.Log (scrollAmount);
			CameraHandler.CustomZoom (scrollAmount);
		}

		// Touch interactions
		if (Input.touchSupported) {
			// Zoom
			if (Input.touchCount == 2) {
				Touch touchOne = Input.GetTouch (0);
				Touch touchTwo = Input.GetTouch (1);

				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
				Vector2 touchTwoPrevPos = touchTwo.position - touchTwo.deltaPosition;

				float prevTouchDeltaMag = (touchOnePrevPos - touchTwoPrevPos).magnitude;
				float touchDeltaMag = (touchOne.position - touchTwo.position).magnitude;

				float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

				CameraHandler.CustomZoom (-deltaMagnitudeDiff / 100f);
			}
		}

		// Draw debugIndex stuff
		Dictionary<long, List<WayReference>> debugWayIndex;
		if (debugIndex > 0) {
			switch (debugIndexNodes[debugIndex]) {
			case "endpoint": debugWayIndex = NodeIndex.endPointIndex; break;
			case "straightWay": debugWayIndex = NodeIndex.straightWayIndex; break;
			case "intersections": debugWayIndex = NodeIndex.intersectionWayIndex; break;
			case "all": debugWayIndex = NodeIndex.nodeWayIndex; break;
			default: debugWayIndex = null; break;
			}
		} else {
			debugWayIndex = null;
		}
		if (CurrentWayReference.Value != null && CurrentTarget.Value != null && CurrentTarget.Key != CurrentWayReference.Key && CurrentPath == null) {
			Pos source = CurrentWayReference.Key;
			Pos target = CurrentTarget.Key;
			CurrentPath = calculateCurrentPath(source, target);
		}

		if (CurrentPath != null) {
			// Clear colours
			WayReference[] wayReferences = FindObjectsOfType<WayReference> ();
			foreach (WayReference wayReference in wayReferences) {
				if (wayReference.OriginalColor != Color.magenta) {
					wayReference.gameObject.GetComponent<Renderer>().material.color = wayReference.OriginalColor;
					wayReference.OriginalColor = Color.magenta;
				}
			}

			// Highlight parts of way
			Pos prev = null;
			foreach (Pos node in CurrentPath) {
				if (prev != null) {
					WayReference wayReference = NodeIndex.getWayReference(node.Id, prev.Id);
					if (wayReference != null) {
						GameObject wayObject = wayReference.gameObject;
						if (wayReference.OriginalColor == Color.magenta) {
							wayReference.OriginalColor = wayObject.GetComponent<Renderer>().material.color;
						}
						wayObject.GetComponent<Renderer>().material.color = Color.blue;
					}
				}
				prev = node;
			}
		} else {
			if (debugWayIndex != null) {
				foreach (long key in debugWayIndex.Keys.ToList()) {
					foreach (WayReference wayReference in debugWayIndex[key]) {
						if (wayReference.node1.Id == key || wayReference.node2.Id == key) {
							GameObject wayObject = wayReference.gameObject;
							if (wayReference.OriginalColor == Color.magenta) {
								wayReference.OriginalColor = wayObject.GetComponent<Renderer>().material.color;
							}
							wayObject.GetComponent<Renderer>().material.color = Color.blue;
						}
					}
				}
			}
			
			if (CurrentWayReference.Value) {
				GameObject wayObject = CurrentWayReference.Value.gameObject;
				if (CurrentWayReference.Value.OriginalColor == Color.magenta) {
					CurrentWayReference.Value.OriginalColor = wayObject.GetComponent<Renderer>().material.color;
				}
				wayObject.GetComponent<Renderer>().material.color = Color.gray;
				
				if (NodeIndex.nodeWayIndex.ContainsKey(CurrentWayReference.Value.node1.Id)) {
					List<WayReference> node1Connections = NodeIndex.nodeWayIndex[CurrentWayReference.Value.node1.Id];
					List<WayReference> node2Connections = NodeIndex.nodeWayIndex[CurrentWayReference.Value.node2.Id];
					foreach (WayReference node1Connection in node1Connections) {
						if (node1Connection != CurrentWayReference.Value) {
							GameObject node1WayObject = node1Connection.gameObject;
							if (node1Connection.OriginalColor == Color.magenta) {
								node1Connection.OriginalColor = node1WayObject.GetComponent<Renderer>().material.color;
							}
							node1WayObject.GetComponent<Renderer>().material.color = Color.yellow;
						}
					}
					foreach (WayReference node2Connection in node2Connections) {
						if (node2Connection != CurrentWayReference.Value) {
							GameObject node2WayObject = node2Connection.gameObject;
							if (node2Connection.OriginalColor == Color.magenta) {
								node2Connection.OriginalColor = node2WayObject.GetComponent<Renderer>().material.color;
							}
							node2WayObject.GetComponent<Renderer>().material.color = Color.green;
						}
					}
				}
			}
		}
	}

	public void addInitAnimationRequest () {
		animationItemsQueue++;
	}

	public void removeInitAnimationRequest () {
		animationItemsQueue--;
		if (animationItemsQueue == 0) {
			StartCoroutine (fadeToMainCamera());
		}
	}

	private IEnumerator fadeToMainCamera () {
		// Wait half a second
		yield return new WaitForSeconds (0.5f);
		// Fade between cameras
		yield return StartCoroutine( ScreenWipe.use.CrossFadePro (introCamera, mainCamera, 1.0f) );
		// Now the game starts
		PubSub.publish ("mainCameraActivated");
	}

	// TODO - Temporary counter
//	int carNo = 0;

	public void createNewCar () {

		// TODO - Temporary counter
//		if (carNo > 1) {
//			return;
//		}

		Pos pos1 = getRandomEndPoint (null);
		Pos pos2 = getRandomEndPoint (pos1);

//		List<Pos> randomEndpointPair = getRandomPredefinedEndPointsPair(
//			new long[][] {
////				new long[] {20L, 340L}, // TODO - If heavy traffic, this might get stuck along the way
////				new long[] {340L, 20L},  // TODO - If heavy traffic, this might get stuck along the way
//				new long[] {344L, 340L},
////				new long[] {340L, 344L},
////				new long[] {325L, 340L},
////				new long[] {325L, 20L}
//			}
//		);
//		Pos pos1 = randomEndpointPair [0];
//		Pos pos2 = randomEndpointPair [1];

//		Pos pos1 = getSpecificEndPoint (15L);
//		Pos pos2 = getSpecificEndPoint (172L);
		// Pos -> Vector3
		Vector3 position = getCameraPosition(pos1) + new Vector3(0f, 0f, -0.15f);
		GameObject vehicleInstance = Instantiate (vehicle, position, Quaternion.identity) as GameObject;
		Vehicle vehicleObj = vehicleInstance.GetComponent<Vehicle> ();

		// TODO - Temporary counter
//		if (carNo++ == 0) {
//			vehicleObj.slow = true;
//		}

		vehicleObj.StartPos = pos1;
		vehicleObj.CurrentPosition = pos1;
		vehicleObj.EndPos = pos2;
//		vehicleObj.GetComponent<Renderer> ().material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

		if (followCar) {
			mainCamera.enabled = false;
			vehicleObj.setDebug ();
		} else {
			Vehicle.detachCurrentCamera();
			mainCamera.enabled = true;
		}
	}

	private Pos getRandomEndPoint (Pos notPos)
	{
		List<long> endPoints = NodeIndex.endPointIndex.Keys.ToList();
		Pos chosenEndPoint = null;

		do {
			chosenEndPoint = NodeIndex.nodes[endPoints[UnityEngine.Random.Range (0, endPoints.Count)]];
			// Validate endPoint as ok way to end at (certain size)
		} while (
			notPos == chosenEndPoint || 
	        NodeIndex.endPointIndex[chosenEndPoint.Id][0].way.WayWidthFactor < WayTypeEnum.MINIMUM_DRIVE_WAY ||
			(notPos != null && calculateCurrentPath(notPos, chosenEndPoint).Count == 0)
		);

		return chosenEndPoint;
	}

	private List<Pos> getRandomPredefinedEndPointsPair (long[][] listOfPairs)
	{
		List<Pos> pair = new List<Pos> ();
		int index = UnityEngine.Random.Range (0, listOfPairs.Count());
		pair.Add (getSpecificEndPoint(listOfPairs[index][0]));
		pair.Add (getSpecificEndPoint(listOfPairs[index][1]));
		return pair;
	}

	private Pos getSpecificEndPoint (long specificPosId)
	{
		Pos chosenEndPoint = null;

		Dictionary<long, List<WayReference>> wayRefsDict = NodeIndex.endPointIndex.Where (p => p.Value.Where (q => q.Id == specificPosId).ToList ().Count == 1).ToDictionary (p => p.Key, p => p.Value);
		List<WayReference> wayRefs = wayRefsDict.Values.ToList () [0];
		WayReference wayRef = wayRefs [0];
		if (NodeIndex.endPointIndex.ContainsKey (wayRef.node1.Id)) {
			chosenEndPoint = wayRef.node1;
		} else {
			chosenEndPoint = wayRef.node2;
		}

		return chosenEndPoint;
	}

	public static List<Pos> calculateCurrentPath (Pos source, Pos target) {
		List<Pos> calculatedPath = new List<Pos> ();

		Dictionary<long, NodeDistance> visitedPaths = new Dictionary<long, NodeDistance> ();

		bool impossible = false;
		Pos current = source;
		visitedPaths.Add (current.Id, new NodeDistance (0, source, true));
		while (current != target) {
			visitedPaths[current.Id].visited = true;
			float currentCost = visitedPaths[current.Id].cost;

			List<KeyValuePair<Pos, WayReference>> neighbours = current.getNeighbours();
			foreach (KeyValuePair<Pos, WayReference> neighbour in neighbours) {
				// Calculate cost to node
				Pos neighbourNode = neighbour.Key;
				WayReference wayReference = neighbour.Value;
				float cost = currentCost + wayReference.gameObject.transform.localScale.magnitude / wayReference.way.WayWidthFactor;
				if (!visitedPaths.ContainsKey(neighbourNode.Id)) {
					visitedPaths.Add (neighbourNode.Id, new NodeDistance(cost, current));
				} else if (!visitedPaths[neighbourNode.Id].visited) {
					if (cost < visitedPaths[neighbourNode.Id].cost) {
						visitedPaths[neighbourNode.Id].cost = cost;
						visitedPaths[neighbourNode.Id].source = current;
					}
				} else {
					continue;
				}
			}

			current = getLowestUnvisitedCostNode(visitedPaths);
			if (current == null) {
				impossible = true;
				break;
			}
		}

		if (!impossible) {
//			float smallestAllowedPath = 0.25f;
//			bool first = true;
			current = target;
//			Pos previous = current;
			while (current != null) {

//				if (!first && current != source && NodeIndex.getWayReference(previous.Id, current.Id).gameObject.transform.localScale.x < smallestAllowedPath) {
//					Debug.Log ("Vehicle will skip wayReference: " + NodeIndex.getWayReference(previous.Id, current.Id) + ", length: " + NodeIndex.getWayReference(previous.Id, current.Id).gameObject.transform.localScale.x);
//				} else {
					calculatedPath.Insert (0, current);
//				}

				if (current == source) {
					break;
				}
//				previous = current;
//				first = false;

				current = visitedPaths [current.Id].source;
			}
		}

		return calculatedPath;
	}

	private static Pos getLowestUnvisitedCostNode (Dictionary<long, NodeDistance> nodes) {
		float lowestCost = float.PositiveInfinity;
		long closestNodeId = -1;
		foreach (KeyValuePair<long, NodeDistance> nodeEntry in nodes) {
			if (!nodeEntry.Value.visited && nodeEntry.Value.cost < lowestCost) {
				lowestCost = nodeEntry.Value.cost;
				closestNodeId = nodeEntry.Key;
			}
		}

		if (closestNodeId != -1) {
			return NodeIndex.nodes [closestNodeId];
		} else {
			return null;
		}
	}

	private IEnumerator loadXML () {
		WWW www = new WWW (mapFileName);
		
		yield return www;

		XmlDocument xmlDoc = new XmlDocument();
		Debug.Log (www.url);
		xmlDoc.LoadXml(www.text);

		XmlNode boundsNode = xmlDoc.SelectSingleNode ("/osm/bounds");
		XmlAttributeCollection boundsAttributes = boundsNode.Attributes;
		decimal minlat = Convert.ToDecimal (boundsAttributes.GetNamedItem ("minlat").Value);
		decimal maxlat = Convert.ToDecimal (boundsAttributes.GetNamedItem ("maxlat").Value);
		decimal minlon = Convert.ToDecimal (boundsAttributes.GetNamedItem ("minlon").Value);
		decimal maxlon = Convert.ToDecimal (boundsAttributes.GetNamedItem ("maxlon").Value);
		mapBounds = new Rect ((float)minlon, (float)minlat, (float)(maxlon - minlon), (float)(maxlat - minlat));

		float latDiff = (float)(maxlat - minlat);
		float refLatDiff = 0.00191f;
		float x = 0.9f / (20f * refLatDiff);
		heightFactor = x * (refLatDiff * refLatDiff) / latDiff;

//		Camera mainCamera = Camera.main;
		// TODO - Take these out from the camera
		float cameraMinX = -cameraOrtographicSize;
		float cameraMinY = -cameraOrtographicSize;
		float cameraMaxX = cameraOrtographicSize;
		float cameraMaxY = cameraOrtographicSize;
		cameraBounds = new Rect (cameraMinX, cameraMinY, cameraMaxX - cameraMinX, cameraMaxY - cameraMinY);

		XmlNodeList nodeNodes = xmlDoc.SelectNodes("/osm/node");
		foreach (XmlNode xmlNode in nodeNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long id = Convert.ToInt64(attributes.GetNamedItem("id").Value);
			Pos node = new Pos(id, (float)Convert.ToDecimal(attributes.GetNamedItem("lon").Value), (float)Convert.ToDecimal(attributes.GetNamedItem("lat").Value));
			addTags(node, xmlNode);
			NodeIndex.nodes.Add (id, node);
		}
		Map.Nodes = NodeIndex.nodes.Values.ToList();

		XmlNodeList wayNodes = xmlDoc.SelectNodes("/osm/way");
		foreach (XmlNode xmlNode in wayNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long wayId = Convert.ToInt64 (attributes.GetNamedItem ("id").Value);
			Way way = new Way (wayId);
			addTags(way, xmlNode);
			addNodes(way, xmlNode);

			Map.Ways.Add(way);
			Map.WayIndex.Add (wayId, way);
		}

		handleRelations (xmlDoc);

//		Pos testPos1 = NodeIndex.getPosById (266706407L);
//		Pos testPos2 = NodeIndex.getPosById (29524373L);
//		List<Pos> path = calculateCurrentPath (testPos1, testPos2);
		NodeIndex.calculateIndexes ();
//		Debug.Log (NodeIndex.endPointIndex.Count);

		// TODO - Real one
		IntersectionOverlap.Create ();

		TrafficLightIndex.AutoInitTrafficLights ();
		//TrafficLightIndex.AutosetTrafficLightProperties ();

		// Place out natural objects (trees, ...)
		foreach (Pos node in Map.Nodes) {
			string naturalValue = node.getTagValue ("natural");
			if (naturalValue != null) {
				int randomSeed = Math3d.GetDecimals(node.Lon) * Math3d.GetDecimals(node.Lat);
				System.Random treeAngle = new System.Random(randomSeed);
				float treeRotation = (float)(treeAngle.NextDouble ()) * 360f;
				Instantiate (treeObject, getCameraPosition(node), Quaternion.Euler(new Vector3(0, 0, treeRotation)));
			}
		}
		         

		// Read config
		WWW wwwConfig = new WWW (configFileName);
		
		yield return wwwConfig;

		XmlDocument xmlDocConfig = new XmlDocument ();
		xmlDocConfig.LoadXml (wwwConfig.text);

		XmlNodeList objectNodes = xmlDocConfig.SelectNodes ("//object");
		foreach (XmlNode objectNode in objectNodes) {
			string type = objectNode.Attributes.GetNamedItem ("type").Value;
			switch (type) {
				case "TrafficLight":
					TrafficLightIndex.ApplyConfig (objectNode);
					break;
				case "Roof": 
				case "Street": 
				case "Outdoors":
				default: 
					initRoofStreetOrOutdoors (type, objectNode); 
					break;
			}
		}
		// TODO move this to after all materials have finished loading
		List<GameObject> allBuldingRoofs = Misc.NameStartsWith ("BuildingRoof (");
		foreach (KeyValuePair<long, Dictionary<string, string>> objectEntry in objectProperties) {
			GameObject buildingRoofObj = GameObject.Find ("BuildingRoof (" + objectEntry.Key + ")");
			if (buildingRoofObj != null) {
//				Debug.Log("BuildingRoof (" + objectEntry.Key + ")");
				BuildingRoof buildingRoof = buildingRoofObj.GetComponent<BuildingRoof>();
				buildingRoof.setProperties(objectEntry.Value);
				allBuldingRoofs.Remove (buildingRoofObj);
			}
		}

		if (allBuldingRoofs.Count > 0) {
			Dictionary<string, string> standardRoof = new Dictionary<string, string> ();
			standardRoof.Add ("material", "2");
			standardRoof.Add ("wall", "1000");
			foreach (GameObject buildingRoofObj in allBuldingRoofs) {
				BuildingRoof buildingRoof = buildingRoofObj.GetComponent<BuildingRoof>();
				string buildingRoofName = buildingRoofObj.name;
				string buildingRoofId = buildingRoofName.Substring(buildingRoofName.IndexOf('(') + 1);
				buildingRoofId = buildingRoofId.Substring(0, buildingRoofId.IndexOf(')'));

				// Set height based on id
				System.Random builingHeight = new System.Random(int.Parse(buildingRoofId));
				int buildingHeight = builingHeight.Next (10, 40);
				standardRoof.Add ("height", "" + buildingHeight);
				standardRoof.Add ("id", buildingRoofId);
				initRoofStreetOrOutdoors ("Roof", standardRoof); 

				buildingRoof.setProperties(standardRoof);

				standardRoof.Remove	("id");
				standardRoof.Remove ("height");
			}
		}
	}

	private void initRoofStreetOrOutdoors (string type, Dictionary<string, string> properties) {
		long id = long.Parse(properties["id"]);
		foreach (KeyValuePair<string, string> property in properties) {
			switch (property.Key) {
				case "material": StartCoroutine (MaterialManager.LoadMaterial(property.Value, type)); break;
				case "wall": StartCoroutine (MaterialManager.LoadMaterial(property.Value, "Wall")); break;
				case "height": break;
				default: break;
			}
		}
		objectProperties.Add(id, properties);
	}

	private void initRoofStreetOrOutdoors (string type, XmlNode objectNode) {
		Dictionary<string, string> properties = new Dictionary<string, string>();

		string id = objectNode.Attributes.GetNamedItem ("id").Value;
		properties.Add ("id", id);
		foreach (XmlNode propertyNode in objectNode.ChildNodes) {
			properties.Add(propertyNode.Name, propertyNode.InnerText);
		}
		initRoofStreetOrOutdoors (type, properties);
	}
	
	private void addTags (NodeWithTags node, XmlNode xmlNode)
	{
		XmlNodeList tagNodes = xmlNode.SelectNodes ("tag");
		foreach (XmlNode tagNode in tagNodes) {
			XmlAttributeCollection attributes = tagNode.Attributes;
			node.addTag(new Tag(attributes.GetNamedItem("k").Value, attributes.GetNamedItem("v").Value));
		}
	}

	private void addNodes (Way way, XmlNode xmlNode)
	{
		// Only instantiate ways, not landuse
		if (way.WayWidthFactor != 0) {
			XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
			Pos prev = null;
			foreach (XmlAttribute refAttribute in nodeRefs) {
				Pos pos = NodeIndex.nodes [Convert.ToInt64 (refAttribute.Value)];
				if (prev != null) {
					if (!way.Building) {
						WayReference wayReference = createPartOfWay (prev, pos, way);
						NodeIndex.addWayReferenceToNode (prev.Id, wayReference);
						NodeIndex.addWayReferenceToNode (pos.Id, wayReference);
					}
				}
				prev = pos;
			}
		}
		if (way.Building) {
			GameObject building = Instantiate (buildingObject) as GameObject;
			building.transform.position = new Vector3 (0f, 0f, -0.098f);
			BuildingRoof roof = building.GetComponent<BuildingRoof> ();
			roof.createBuildingWithXMLNode (xmlNode);
		} else if (way.LandUse) {
			GameObject landuse = Instantiate (landuseObject) as GameObject;
			landuse.transform.position = new Vector3 (0f, 0f, -0.098f);
			LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
			surface.createLanduseWithXMLNode (xmlNode, way);
		} else { 
			if (way.getTagValue ("area") == "yes") {
				GameObject landuse = Instantiate (landuseObject) as GameObject;
				landuse.transform.position = new Vector3 (0f, 0f, -0.099f);
				LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
				surface.createLanduseWithXMLNode (xmlNode, way, way.getWayType ());
			} else if (way.WayWidthFactor == WayTypeEnum.PLATFORM) {
				GameObject landuse = Instantiate (landuseObject) as GameObject;
				landuse.transform.position = new Vector3 (0f, 0f, -0.099f);
				LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
				surface.createLanduseAreaWithXMLNode (xmlNode, way, way.getWayType (), WayTypeEnum.EXPANDED_PLATFORM);
			}
		}
	}

	void handleRelations (XmlDocument xmlDoc) {
		XmlNodeList relationNodes = xmlDoc.SelectNodes("/osm/relation");
		foreach (XmlNode xmlNode in relationNodes) {

			string xmlNodeId = xmlNode.Attributes ["id"].Value;
			XmlNode xmlNodeBuildingTag = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='building' and @v='yes']");
			if (xmlNodeBuildingTag != null) {
				XmlAttribute xmlNodeWayOuterAttribute = (XmlAttribute) xmlNode.SelectSingleNode ("/osm/relation[@id='" + xmlNodeId + "']/member[@role='outer']/@ref");
				string outerWallWayId = xmlNodeWayOuterAttribute.Value;
				XmlNode wayNode = xmlDoc.SelectSingleNode ("/osm/way[@id='" + outerWallWayId + "']");
				XmlNode wayNodeIsBuilding = xmlDoc.SelectSingleNode ("/osm/way[@id='" + outerWallWayId + "']/tag[@k='building' and @v='yes']");

				if (wayNodeIsBuilding == null) {
					GameObject building = Instantiate (buildingObject) as GameObject;
					building.transform.position = new Vector3 (0f, 0f, -0.098f);
					BuildingRoof roof = building.GetComponent<BuildingRoof> ();
					roof.createBuildingWithXMLNode (wayNode);
				}
			}
		}
		// TODO Subtract inner walls from the outer mesh.
	}

	private WayReference createPartOfWay (Pos previousPos, Pos currentPos, Way wayObject)
	{
		Vector3 position1 = getCameraPosition (previousPos);
		Vector3 position2 = getCameraPosition (currentPos);

		Vector3 wayVector = position2 - position1;
		Vector3 position = getMidPoint (position1, position2);

		GameObject way;
		Vector3 originalScale;
		Quaternion rotation = Quaternion.FromToRotation (oneVector, wayVector);

		Vector3 eulerRotation = rotation.eulerAngles;
		if (eulerRotation.z == 0f) {
			if (eulerRotation.y != 0f) {
				rotation = Quaternion.Euler(0f, 0f, eulerRotation.y);
			} else {
				rotation = Quaternion.Euler(0f, 0f, eulerRotation.x);
			}
		}

		if (wayObject.CarWay) {
			way = Instantiate (partOfWay, position, rotation) as GameObject;
			originalScale = partOfWay.transform.localScale;
			way.name = "CarWay (" + previousPos.Id + ", " + currentPos.Id + ")";
		} else {
			way = Instantiate (partOfNonCarWay, position, rotation) as GameObject;
			originalScale = partOfNonCarWay.transform.localScale;
			way.name = "NonCarWay (" + previousPos.Id + ", " + currentPos.Id + ")";
		}
		WayReference wayReference = way.GetComponent<WayReference> ();
		wayReference.Id = ++WayReference.WayId;
		wayReference.way = wayObject;
		wayReference.node1 = previousPos;
		wayReference.node2 = currentPos;
		wayObject.addWayReference (wayReference);

		// Target value = wayObject.WayWidthFactor

		// Roundabouts special logic
		bool isRoundabout = wayObject.getTagValue("junction") == "roundabout";

		float xStretchFactor = Vector3.Magnitude (wayVector) * Settings.wayLengthFactor;
		float yStretchFactor = wayObject.WayWidthFactor * Settings.currentMapWidthFactor;
		way.transform.localScale = new Vector3 (xStretchFactor * originalScale.x, yStretchFactor * originalScale.y, originalScale.z);

		// Mark up small ways - TODO - Need to handle different scales / zoom
		float smallestAllowedPath = 0.25f;
		if (way.transform.localScale.x < smallestAllowedPath) {
			wayReference.SmallWay = true;
		}

		// Create gameObject with graphics for middle of way -****-
		// TODO - Name it, apply material...
		bool drawFullWay = wayReference.way.WayWidthFactor == WayTypeEnum.PLATFORM;
		GameObject middleOfWay = createMiddleOfWay (way, drawFullWay);

		float colliderWidthPct = Mathf.Min (yStretchFactor / (xStretchFactor * 1.5f), 0.5f);
		List<BoxCollider> colliders = wayReference.GetComponents<BoxCollider> ().ToList ();
		BoxCollider leftCollider = colliders [colliders.Count - 2];
		BoxCollider rightCollider = colliders [colliders.Count - 1];

		leftCollider.size = new Vector3 (colliderWidthPct, 1f, leftCollider.size.z);
		leftCollider.center = new Vector3 (-0.5f + colliderWidthPct / 2f, 0f, 0f);
		rightCollider.size = new Vector3 (colliderWidthPct, 1f, leftCollider.size.z);
		rightCollider.center = new Vector3 (0.5f - colliderWidthPct / 2f, 0f, 0f);

		HandleWayTags (previousPos, currentPos, way, rotation);

		return wayReference;
	}

	void HandleWayTags (Pos previousPos, Pos currentPos, GameObject way, Quaternion rotation) {

		// Check if this way has a traffic light, and add it
		if (previousPos.getTagValue ("crossing") == "traffic_signals") {
			HandleTrafficSignalForNode (previousPos, currentPos, way, rotation, true);
		}
		if (currentPos.getTagValue ("crossing") == "traffic_signals") {
			HandleTrafficSignalForNode (previousPos, currentPos, way, rotation, false);
		}
	}
	
	void HandleTrafficSignalForNode (Pos previousPos, Pos currentPos, GameObject way, Quaternion rotation, bool isNode1)
	{
		Vector3 position1 = getCameraPosition (previousPos);
		Vector3 position2 = getCameraPosition (currentPos);
		WayReference wayReference = way.GetComponent<WayReference> ();
		float fieldsInRelevantDirection = wayReference.getNumberOfFieldsInDirection (previousPos);
		float fieldsTotal = wayReference.getNumberOfFields ();
		float colliderPercentageY = fieldsInRelevantDirection / fieldsTotal;
		float percentagePosYFromMiddle = (isNode1 ? -1f : 1f) * (fieldsInRelevantDirection / fieldsTotal - 0.5f);
		Vector3 adjustPos = new Vector3 (way.transform.localScale.y / 2f, way.transform.localScale.y * percentagePosYFromMiddle, 0);
		Vector3 rotatedAdjustPos = rotation * adjustPos;
		Vector3 lightPosition;
		Quaternion lightRotation;
		if (isNode1) {
			lightPosition = new Vector3(position1.x + rotatedAdjustPos.x, position1.y + rotatedAdjustPos.y, -0.15f);
			lightRotation = rotation * Quaternion.Euler(new Vector3(0, 0, 180f)) * trafficLight.transform.rotation;
		} else {
			lightPosition = new Vector3 (position2.x - rotatedAdjustPos.x, position2.y - rotatedAdjustPos.y, -0.15f);
			lightRotation = rotation * trafficLight.transform.rotation;
		}
		GameObject light = Instantiate (trafficLight, lightPosition, lightRotation) as GameObject;
		TrafficLightLogic trafficLightInstance = light.GetComponent<TrafficLightLogic> ();
		if (isNode1) {
			trafficLightInstance.setProperties (previousPos, rotation.eulerAngles.z, currentPos);
		} else {
			trafficLightInstance.setProperties (currentPos, rotation.eulerAngles.z, previousPos);
		}
		trafficLightInstance.setColliders (wayReference, colliderPercentageY, isNode1);
		TrafficLightIndex.AddTrafficLight (trafficLightInstance);
	}
	
	private GameObject createMiddleOfWay (GameObject way, bool drawFullWay) {
		WayReference wayReference = way.GetComponent<WayReference> ();

		Vector3 wayPosition = way.transform.position;
		Vector3 wayScale = way.transform.localScale;
		Vector3 fromPos = new Vector3 (wayPosition.x - wayScale.x / 2f, wayPosition.y - wayScale.y / 2f, 0);
		Vector3 toPos = new Vector3 (wayPosition.x + wayScale.x / 2f, wayPosition.y + wayScale.y / 2f, 0);

		if (!drawFullWay) {
			fromPos += new Vector3 (wayScale.y / 2f, 0f, 0f);
			toPos -= new Vector3 (wayScale.y / 2f, 0f, 0f);
		}

		Quaternion rotation = way.transform.rotation;
		GameObject middleOfWay = MapSurface.createPlaneMeshForPoints (fromPos, toPos);
		middleOfWay.name = "Plane Mesh for " + way.name;
		if (wayReference.way.CarWay) {
			middleOfWay.transform.position = middleOfWay.transform.position - new Vector3 (0, 0, 0.1f);
		} else {
			middleOfWay.transform.position = middleOfWay.transform.position - new Vector3 (0, 0, 0.099f);
		}
		// TODO - Config for material
		// Small ways are not drawn with material or meshes
		if (!wayReference.SmallWay || !wayReference.way.CarWay) {
			AutomaticMaterialObject middleOfWayMaterialObject = middleOfWay.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
			if (wayReference.way.CarWay) {
				middleOfWayMaterialObject.requestMaterial ("2002-Street", null); // TODO - Default material
				// Draw lines on way if car way
				WayLine wayLineObject = middleOfWay.AddComponent<WayLine> () as WayLine;
				wayLineObject.create (wayReference);
			} else {
				middleOfWayMaterialObject.requestMaterial ("2003-Street", null); // TODO - Default material
				// Non Car Ways never have lines and only have one field in each direction 
				wayReference.fieldsFromPos1ToPos2 = 1;
				wayReference.fieldsFromPos2ToPos1 = 1;
			}
		}

		middleOfWay.transform.rotation = rotation;

		return middleOfWay;
	}

	private Vector3 getMidPoint (Vector3 position1, Vector3 position2)
	{
		return ((position2 - position1) / 2) + position1;
	}

	public static Vector3 getCameraPosition (Pos pos)
	{
		float posX = pos.Lon;
		float posY = pos.Lat;

		float cameraPosX = ((posX - mapBounds.x) / mapBounds.width) * cameraBounds.width + cameraBounds.x;
		float cameraPosY = ((posY - mapBounds.y) / mapBounds.height) * cameraBounds.height + cameraBounds.y;

		return new Vector3 (cameraPosX, cameraPosY, 0);
	}
	
	public void OnGUI () {
		int y = -20;
		GUI.Label(new Rect(0, y+=20, 100, 20), debugIndexNodes[debugIndex]);

		if (CurrentWayReference.Value != null) {
			GUI.Label (new Rect(0, y+=20, 500, 20), "WayReference id: " + CurrentWayReference.Value.Id + ", cost: " + (CurrentWayReference.Value.gameObject.transform.localScale.magnitude / CurrentWayReference.Value.way.WayWidthFactor));
			GUI.Label (new Rect(0, y+=20, 500, 20), "WayReference node1: " + CurrentWayReference.Value.node1.Lon + ", " + CurrentWayReference.Value.node1.Lat);
			GUI.Label (new Rect(0, y+=20, 500, 20), "WayReference node2: " + CurrentWayReference.Value.node2.Lon + ", " + CurrentWayReference.Value.node2.Lat);
			if (NodeIndex.nodeWayIndex.ContainsKey(CurrentWayReference.Value.node1.Id)) {
				GUI.Label (new Rect(0, y+=20, 500, 20), "Node 1 Connections: " + NodeIndex.nodeWayIndex[CurrentWayReference.Value.node1.Id].Count + ": " + getIdStrings(NodeIndex.nodeWayIndex[CurrentWayReference.Value.node1.Id]));
				GUI.Label (new Rect(0, y+=20, 500, 20), "Node 2 Connections: " + NodeIndex.nodeWayIndex[CurrentWayReference.Value.node2.Id].Count + ": " + getIdStrings(NodeIndex.nodeWayIndex[CurrentWayReference.Value.node2.Id]));
			}
			Way way = CurrentWayReference.Value.way;
			GUI.Label (new Rect(0, y+=20, 500, 20), "Way info: " + (way.CarWay ? "Car way" : (way.Building ? "Building" : "Smaller way")) + " - " + way.WayWidthFactor);
			GUI.Label (new Rect(0, y+=20, 500, 200), getTagNames(way));
		}
	}

	private string getTagNames(Way way) {
		string wayTags = "";
		foreach (Tag tag in way.getTags()) {
			wayTags += (wayTags.Length > 0 ? "\n" : "") + tag.Key + "=" + tag.Value;
		}
		return wayTags;
	}

	private string getIdStrings(List<WayReference> wayReferences) {
		string wayIds = "";

		foreach (WayReference wayReference in wayReferences) {
			wayIds += (wayIds.Length > 0 ? ", " : "") + wayReference.Id;
		}

		return wayIds;
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

	public void onMessage (string message, object data) {
		if (message == "Vehicle:emitGas") {
			Vehicle vehicle = (Vehicle)data;
			Vector3 emitPosition = vehicle.getEmitPosition () + new Vector3 (0f, 0f, mainCamera.transform.position.z + 1f);
			GameObject emission = Instantiate (vehicleEmission, emitPosition, vehicle.gameObject.transform.rotation) as GameObject;
//			DebugFn.arrow(vehicle.transform.position, emitPosition);
			emission.GetComponent<Emission> ().Amount = vehicle.getEmissionAmount ();
			ParticleSystem particleSystem = emission.GetComponent<ParticleSystem> ();
			particleSystem.Simulate (0.10f, true);
			particleSystem.Play (true);

			StartCoroutine (destroyEmission (particleSystem));
		} else if (message == "mainCameraActivated") {
			CameraHandler.InitialZoom ();
			pointsCamera.enabled = true;
		}
	}

	public IEnumerator destroyEmission (ParticleSystem emission) {
		yield return new WaitForSeconds (emission.duration);
		cameraEmission += emission.gameObject.GetComponent<Emission> ().Amount;
		Destroy (emission.gameObject);
	}

	public static bool isRunning () {
		return Game.running;
	}
}
