using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO - Next time - When restarting level:
// - Game clock ticks before it's set

public class Game : MonoBehaviour, IPubSub {

	public Camera menuCamera;
	public GameObject menuSystem;
	public Camera mainCamera;
	public Camera introCamera;
	public Camera pointsCamera;

	public static Game instance;
	private int animationItemsQueue = 0;
	private float cameraEmission = 0f;

	public static long randomSeed = Misc.currentTimeMillis ();
	private static bool running = false;
	private static bool paused = false;
	private static bool frozen = false;

	// TODO - Either remove or use to help with building levels
	private static bool debugMode = false;
	private static bool humanDebugMode = false;
	private static List<Pos> debugDrawBetween = new List<Pos>();
	private static List<Pos> humanDebugDrawBetween = new List<Pos>();

	public static KeyValuePair<Pos, WayReference> CurrentWayReference { set; get; }
	public static KeyValuePair<Pos, WayReference> CurrentTarget { set; get; }
	public static List<Pos> CurrentPath { set; get; }

//	private string mapFileName = "http://samlingar.com/itsTraffic/testmap01.osm";
//	private string mapFileName = "file:///home/anders/Programmering/itsTraffic/Assets/StreamingAssets/testmap08.osm";
//	private string mapFileName = "file:///home/anders/Programmering/itsTraffic/Assets/StreamingAssets/testmap01.osm";
//	private string mapFileName = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/testmap09.osm";
	private string mapFileName = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/djakne-kvarteret.osm";
//	private string configFileName = "http://samlingar.com/itsTraffic/testmap03-config.xml";
	private string configFileName = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/testmap08-config.xml";

	private string levelSetupFileName = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/level-robbin.xml";

	private const float CLICK_RELEASE_TIME = 0.2f; 
	private const float THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK = 30f;

	public GameObject partOfWay;
	public GameObject partOfNonCarWay;
	public List<VehiclesDistribution> vehicles;
	public GameObject buildingObject;
	public GameObject landuseObject;
	public GameObject trafficLight;
	public GameObject treeObject;
	public GameObject vehicleEmission;
	public GameObject vehicleVapour; 
	public GameObject vehicleHalo;
	public GameObject wayCrossing;
	public GameObject human;

	// These are not really rects, just four positions minX, minY, maxX, maxY
	private static Rect cameraBounds;
	private static Rect mapBounds;

	// TODO - When switched to ortographic camera, set this in those objects
	public static float cameraOrtographicSize = 5f;
	public static float heightFactor;

	public float soundEffectsVolume = 0.8f;

	private Vector3 oneVector = Vector3.right;
	
	private float currentLevel = WayTypeEnum.WayTypes.First<float>();
	private bool showOnlyCurrentLevel = false;
//	private bool followCar = false;
	private float sumVehicleFrequency;

	private Dictionary<long, Dictionary<string, string>> objectProperties = new Dictionary<long, Dictionary<string, string>>();
	private Dictionary<int, Light> dangerHalos = new Dictionary<int, Light> ();

	public Level loadedLevel = null;
	private float clickReleaseTimer = 0f;
	private Vector3 mouseDownPosition;
	private Vector3 prevMousePosition;

	private int debugIndex = 0;
	private List<string> debugIndexNodes = new List<string> () {
		"none", "endpoint", "straightWay", "intersections", "all"
	};

	// Use this for initialization
	void Start () {
		showMenu ();
		paused = false;
		initDataCollection ();
		calculateVehicleFrequency ();

		Game.instance = this;

		StartCoroutine (MaterialManager.Init ());

		CameraHandler.SetIntroZoom (cameraOrtographicSize);
		CameraHandler.SetMainCamera (mainCamera);
        CameraHandler.SetPerspectiveCamera (introCamera);
        CameraHandler.SetRestoreState ();
		PubSub.subscribe ("mainCameraActivated", this);

//		Time.timeScale = 0.1f;
		// Subscribe to when emission is let out from vehicles
		PubSub.subscribe ("Vehicle:emitGas", this);
		// Subscribe to when vapour is let out from vehicles
		PubSub.subscribe ("Vehicle:emitVapour", this);
		// Subscribe to when vehicles crashes and needs attention
		PubSub.subscribe("Vehicle:createDangerHalo", this);
		// Subscribe to when crashed vehicles are removed, to remove the danger halo
		PubSub.subscribe("Vehicle:removeDangerHalo", this);
	}

    private void exitLevel() {
        showMenu();

        running = false;
        paused = false;
        frozen = false;

        GameObject[] mapObjects = GameObject.FindGameObjectsWithTag("MapObject");
        foreach (GameObject mapObject in mapObjects) {
            Destroy(mapObject);
        }

        objectProperties.Clear();

        CameraHandler.Restore();
        Map.Clear();
        NodeIndex.Clear();
        DataCollector.Clear();
        initDataCollection();

        loadedLevel = null;
    }

	public void startGame() {
		if (paused) {
			showMenu (false);
		} else {
			toggleStartSubmenu ();
		}
	}

	public void startEndlessMode() {
		introCamera.enabled = true;
		StartCoroutine (loadXML (mapFileName, configFileName));
	}

	public void startMission() {
		introCamera.enabled = true;
		StartCoroutine (loadLevelSetup (levelSetupFileName));
	}
		
	private void showMenu(bool show = true) {
		menuSystem.SetActive (show);
		Game.paused = show;
		pointsCamera.gameObject.SetActive (!show);
	}

	void initDataCollection ()
	{
        DataCollector.InitLabel ("Elapsed Time");
        DataCollector.InitLabel ("Total # of vehicles");
		DataCollector.InitLabel ("Total # of people");
		DataCollector.InitLabel ("Vehicles reached goal");
		DataCollector.InitLabel ("Manual traffic light switches");
	}
	
	// Update is called once per frame
	void Update () {
		if (Game.isRunning () && Game.isMovementEnabled()) {
			DataCollector.Add ("Elapsed Time", Time.deltaTime);
		}
//		if (Input.GetKeyDown (KeyCode.Plus) || Input.GetKeyDown (KeyCode.P)) {
//			currentLevel = WayTypeEnum.getLower (currentLevel);
//			filterWays ();
//		} else if (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.M)) {
//			currentLevel = WayTypeEnum.getHigher (currentLevel);
//			filterWays ();
//		} else if (Input.GetKeyDown (KeyCode.Space)) {
//			showOnlyCurrentLevel ^= true;
//			currentLevel = 0.111f;
//			filterWays ();

//		} else if (Input.GetKeyDown (KeyCode.LeftShift)) {
//			debugIndex = ++debugIndex % debugIndexNodes.Count;
//			WayReference[] wayReferences = FindObjectsOfType<WayReference> ();
//			foreach (WayReference wayReference in wayReferences) {
//				if (wayReference.OriginalColor != Color.magenta) {
//					wayReference.gameObject.GetComponent<Renderer> ().material.color = wayReference.OriginalColor;
//					wayReference.OriginalColor = Color.magenta;
//				}
//			}
//		} else 
		if (Input.GetKeyDown (KeyCode.N)) {
			GameObject car = null; 
//			GameObject car = GameObject.Find ("Camaro(ish)(Clone)");
			if (car != null) {
				Vehicle carObj = car.GetComponent<Vehicle> ();
				carObj.fadeOutAndDestroy ();
			} else {
				createNewCar ();
//				giveBirth();
			}
		} else if (Input.GetKeyDown (KeyCode.P)) {
			// Pause (Freeze)
			// TODO - Just for testing, should at least not be a button...
			toggleFreezeGame ();
		} else if (Input.GetKeyDown(KeyCode.D)) {
			// TODO - Temporary
			toggleDebugMode ();
		} else if (Input.GetKeyDown(KeyCode.S)) {
			// TODO - Temporary
			toggleHumanDebugMode ();
//		} else if (Input.GetKeyDown (KeyCode.F)) {
//			followCar ^= true;
//			if (!followCar) {
//				Vehicle.detachCurrentCamera ();
//				mainCamera.enabled = true;
//			}
		} else if (Input.GetKeyDown (KeyCode.Q)) {
			PubSub.publish ("points:inc", 13579);
		} else if (Input.GetKeyDown (KeyCode.W)) {
			PubSub.publish ("points:dec", 24680);
		} else if (Input.GetKeyDown (KeyCode.T)) {
			bool isShiftKeyDown = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
			if (isShiftKeyDown) {
				HumanLogic.TURBO = 1f;
			} else {
				HumanLogic.TURBO++;
			}
		} else if (Input.GetKeyDown (KeyCode.Escape)) {
			if (!paused) {
				showMenu ();
			}
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
				CameraHandler.Move (diffMove);
			} else {
				mouseDownPosition = mousePosition;
			}
			prevMousePosition = mousePosition;

			// Click logic
			if (firstFrame) {
				clickReleaseTimer = CLICK_RELEASE_TIME;
			} else {
				clickReleaseTimer -= Time.deltaTime;
			}
		} else if (clickReleaseTimer > 0f) {
			// Button not pressed, and was pressed < 0.2s, accept as click if not moved too much
			if (Misc.getDistance (mouseDownPosition, prevMousePosition) < THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK) {
				Vector3 mouseWorldPoint = mainCamera.ScreenToWorldPoint (mouseDownPosition);
				PubSub.publish ("Click", mouseWorldPoint);
				clickReleaseTimer = 0f;
			}
		}

		// TODO - This is for debug - choosing endpoints
		if (Game.debugMode) {
			if (Input.GetMouseButtonDown (1)) {
				Vector3 mousePosition = Input.mousePosition;
				Vector3 mouseWorldPoint = mainCamera.ScreenToWorldPoint (mousePosition);
				Pos pos = NodeIndex.getPosClosestTo (mouseWorldPoint);
				debugDrawBetween.Add (pos);
				if (debugDrawBetween.Count > 2) {
					debugDrawBetween.RemoveAt (0);
				}
				if (debugDrawBetween.Count == 2) {
					Debug.Log ("Position Ids: " + debugDrawBetween[0].Id + " - " + debugDrawBetween[1].Id);
				}
			}

			// Draw lines!
			if (debugDrawBetween.Count > 1) {
				List<Pos> path = Game.calculateCurrentPath(debugDrawBetween[0], debugDrawBetween[1], true);
				DebugFn.temporaryOverride (new Color (0.3f, 0.3f, 1f), 0.05f);
				DebugFn.DebugPath (path);
				DebugFn.temporaryOverride (Color.black);
			}
		}

		if (Game.humanDebugMode) {
			if (Input.GetMouseButtonDown (1)) {
				Vector3 mousePosition = Input.mousePosition;
				Vector3 mouseWorldPoint = mainCamera.ScreenToWorldPoint (mousePosition);
				Pos pos = NodeIndex.getPosClosestTo (mouseWorldPoint, false);
				humanDebugDrawBetween.Add (pos);
				if (humanDebugDrawBetween.Count > 2) {
					humanDebugDrawBetween.RemoveAt (0);
				}
				if (humanDebugDrawBetween.Count == 2) {
					Debug.Log ("Position Ids: " + humanDebugDrawBetween[0].Id + " - " + humanDebugDrawBetween[1].Id);
				}
			}

			// Draw lines!
			if (humanDebugDrawBetween.Count > 1) {
				List<Pos> path = Game.calculateCurrentPath(humanDebugDrawBetween[0], humanDebugDrawBetween[1], true);
				DebugFn.temporaryOverride (new Color (0.3f, 0.3f, 1f), 0.05f);
				DebugFn.DebugPath (path);
				DebugFn.temporaryOverride (Color.black);
			}
		}

		if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
			float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
//			Debug.Log (scrollAmount);
			if (Game.running && scrollOk (Input.mousePosition)) {
				CameraHandler.CustomZoom (scrollAmount, Input.mousePosition);
			}
		}

		// Touch interactions
		if (Input.touchSupported) {
			// Zoom
			if (Input.touchCount == 2) {
				Touch touchOne = Input.GetTouch (0);
				Touch touchTwo = Input.GetTouch (1);

				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
				Vector2 touchTwoPrevPos = touchTwo.position - touchTwo.deltaPosition;
				Vector2 centerPos = touchOne.position + (touchTwo.position - touchOne.position) / 2;

				float prevTouchDeltaMag = (touchOnePrevPos - touchTwoPrevPos).magnitude;
				float touchDeltaMag = (touchOne.position - touchTwo.position).magnitude;

				float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

				// TODO - Scroll OK here (like above)
				if (Game.running) {
					CameraHandler.CustomZoom (-deltaMagnitudeDiff / 100f, centerPos);
				}
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

	private bool scrollOk (Vector3 pos) {
		InformationWindow informationWindow = GetComponent<InformationWindow> ();
		return !informationWindow.isShown () || !Misc.isInside (pos, informationWindow.getWindowRect ());
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

	public void giveBirth(Setup.PersonSetup data) {
		long startPos;
		if (data.startPos != 0) {
			startPos = data.startPos;
		} else {
			startPos = getRandomHumanPos ();
		}
		long endPos;
		if (data.endPos != 0) {
			endPos = data.endPos;
		} else {
			endPos = getRandomHumanPos (startPos);
		}

		makePerson (startPos, endPos, data);
	}

	public void giveBirth() {
		long startPos = getRandomHumanPos ();
		long endPos = getRandomHumanPos (startPos);

//		Debug.Log (startPos + " - " + endPos);

//		bool forceOneDirectionOnly = false;
//		bool forceEveryOther = false;

//		long one = 3426695523L;
//		long other = 3414497903L;

//		long one = 3402678107L;
//		long other = 2715587963L;

//		// Specific points, debug
//		if (forceOneDirectionOnly) {
//			startPos = one;
//			endPos = other;
//		} else {
//			startPos = forceEveryOther ? (HumanLogic.humanInstanceCount % 2 == 0 ? one : other) : (UnityEngine.Random.value < 0.5f ? one : other);
//			endPos = forceEveryOther ? (HumanLogic.humanInstanceCount % 2 == 0 ? other : one) : (startPos == one ? other : one);
//		}

		makePerson (startPos, endPos);
	}

	private void makePerson (long startPos, long endPos, Setup.PersonSetup data = null) {
		// Find closest walk path - start and end pos
		if (NodeIndex.walkNodes.Count > 0) {
			Tuple3<Pos, WayReference, Vector3> startInfo = NodeIndex.humanSpawnPointsInfo[startPos];
			Tuple3<Pos, WayReference, Vector3> endInfo = NodeIndex.humanSpawnPointsInfo[endPos];

//			Vector3 position;
//			if (data != null && data.startVector != null) {
//				position = Misc.parseVector(data.startVector) + new Vector3 (0f, 0f, startInfo.Third.z);
//			} else {
//				position = startInfo.Third;
//			}
				
			// Place out human
			GameObject humanInstance = Instantiate (human, startInfo.Third, Quaternion.identity) as GameObject;
			HumanLogic humanLogic = humanInstance.GetComponent<HumanLogic> ();
			if (data != null) {
				humanLogic.setPersonality (data);
				if (data.id > 0) {
					humanLogic.name = "Human (id:" + data.id + ")";
				}
			}
			humanLogic.setStartAndEndInfo (startInfo, endInfo);
		}
	}

	public void createNewCar (Setup.VehicleSetup data) {
		Pos pos1 = NodeIndex.getPosById (data.startPos);
		Pos pos2 = NodeIndex.getPosById (data.endPos);

		makeCar (pos1, pos2, data);
	}

	public void createNewCar () {

		Pos pos1 = getRandomEndPoint ();
		Pos pos2 = getRandomEndPoint (pos1);

//		Pos pos1 = getSpecificEndPoint (46L);
//		Pos pos2 = getSpecificEndPoint (34L);

		makeCar (pos1, pos2);
	}

	private void makeCar(Pos pos1, Pos pos2, Setup.VehicleSetup data = null) {
		// Pos -> Vector3
		Vector3 position;
		if (data != null && data.startVector != null) {
			position = Misc.parseVector(data.startVector) + new Vector3 (0f, 0f, -0.15f);
		} else {
			position = getCameraPosition (pos1) + new Vector3 (0f, 0f, -0.15f);
		}
		GameObject vehicleInstance = Instantiate (getVehicleToInstantiate(data), position, Quaternion.identity) as GameObject;
		Vehicle vehicleObj = vehicleInstance.GetComponent<Vehicle> ();

		vehicleObj.StartPos = pos1;
		vehicleObj.CurrentPosition = pos1;
		vehicleObj.EndPos = pos2;

//		if (followCar) {
//			mainCamera.enabled = false;
//			vehicleObj.setDebug ();
//		} else {
//			Vehicle.detachCurrentCamera();
//			mainCamera.enabled = true;
//		}

		if (data != null) {
			vehicleObj.setCharacteristics (data);
			if (data.id > 0) {
				vehicleObj.name = "Vehicle (id:" + data.id + ")";
			}
		}
	}

	private long getRandomHumanPos (long notPos = long.MinValue) {
		List<long> humanPoints = NodeIndex.nodesOfInterest.Keys.ToList();
		long chosenHumanPoint = long.MinValue;

		do {
			chosenHumanPoint = humanPoints[Misc.randomRange (0, humanPoints.Count)];
		} while (notPos == chosenHumanPoint);

		return chosenHumanPoint;
	}

	private Pos getRandomEndPoint (Pos notPos = null)
	{
		List<long> endPoints = NodeIndex.endPointDriveWayIndex.Keys.ToList();
		Pos chosenEndPoint = null;

		do {
			chosenEndPoint = NodeIndex.nodes[endPoints[Misc.randomRange (0, endPoints.Count)]];
			// Validate endPoint as ok way to end at (certain size)
		} while (
			notPos == chosenEndPoint || 
			(notPos != null && calculateCurrentPath(notPos, chosenEndPoint).Count == 0)
		);

		return chosenEndPoint;
	}

	private List<Pos> getRandomPredefinedEndPointsPair (long[][] listOfPairs)
	{
		List<Pos> pair = new List<Pos> ();
		int index = Misc.randomRange (0, listOfPairs.Count());
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

	public static List<Pos> calculateCurrentPath (Pos source, Pos target, bool isVehicle = true) {
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
				float cost = currentCost + wayReference.getTravelCost(isVehicle);
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

	private IEnumerator loadLevelSetup (string levelFileName) {
		introCamera.enabled = true;
		introCamera.gameObject.SetActive (true);
		showMenu (false);

		WWW www = new WWW (levelFileName);

		yield return www;

		XmlDocument xmlDoc = new XmlDocument();
//		Debug.Log (www.url);
		xmlDoc.LoadXml(www.text);

		loadedLevel = new Level (xmlDoc);
		StartCoroutine (loadXML (loadedLevel.mapUrl, loadedLevel.configUrl));
	}

	private IEnumerator loadXML (string mapFileName, string configFileName) {
        freezeGame(false);
		introCamera.enabled = true;
		introCamera.gameObject.SetActive (true);
		showMenu (false);

		WWW www = new WWW (mapFileName);
		
		yield return www;

		XmlDocument xmlDoc = new XmlDocument();
//		Debug.Log (www.url);
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

		// Width to height ratio
		float widthHeightRatio = (float) Screen.width / (float) Screen.height;

//		Camera mainCamera = Camera.main;
		// TODO - Take these out from the camera
		float cameraMinX = -cameraOrtographicSize;
		float cameraMinY = -cameraOrtographicSize;
		float cameraMaxX = cameraOrtographicSize;
		float cameraMaxY = cameraOrtographicSize;

		if (widthHeightRatio > 1f) {
			float xSpan = (cameraMaxX - cameraMinX);
			float addedXSpan = xSpan * widthHeightRatio - xSpan;
			cameraMinX -= addedXSpan / 2f;
			cameraMaxX += addedXSpan / 2f;
		}

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

		handleRelations (xmlDoc);

		createOutsideArea ();

		XmlNodeList wayNodes = xmlDoc.SelectNodes("/osm/way");
		foreach (XmlNode xmlNode in wayNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			string wayIdStr = attributes.GetNamedItem ("id").Value;
			long wayId = Convert.ToInt64 (wayIdStr);
			if (!NodeIndex.buildingWayIds.Contains (wayId)) {
				Way way = new Way (wayId);
				addTags (way, xmlNode);
				addNodes (way, xmlNode);

				Map.Ways.Add (way);
				Map.WayIndex.Add (wayId, way);
			} else {
				XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
				foreach (XmlAttribute refAttribute in nodeRefs) {
					long nodeId = Convert.ToInt64 (refAttribute.Value);
					NodeIndex.nodeIdsForBuildingWays.Add (nodeId);
				}
			}
		}
			
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
        // TODO - This doesn't seem to apply materials correct (not loaded) on level start
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

	private void createOutsideArea () {
		GameObject landuse = Instantiate (landuseObject) as GameObject;
		landuse.transform.position = new Vector3 (0f, 0f, -0.097f);
		LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
		surface.createBackgroundLanduse ();
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
					// Add the wayIds of buildings to a list
					// TODO - Seems to not draw the buildings outline/inline, but still have them as nodeWays in the debug squares
					XmlNodeList xmlNodeWayBuildings = xmlNode.SelectNodes ("/osm/relation[@id='" + xmlNodeId + "']/member/@ref");
					foreach (XmlNode wallIdNode in xmlNodeWayBuildings) {
						NodeIndex.buildingWayIds.Add (Convert.ToInt64(wallIdNode.Value));
					}

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

	public static Pos createTmpPos (Vector3 third) {
		float lon = (third.x - cameraBounds.x) / cameraBounds.width * mapBounds.width + mapBounds.x;
		float lat = (third.y - cameraBounds.y) / cameraBounds.height * mapBounds.height + mapBounds.y;

		return new Pos (-1L, lon, lat);
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
			bool shouldShow = false;
			if (showOnlyCurrentLevel && currentWayWidthFactor == currentLevel) {
				shouldShow = true;
//				wayReference.GetComponent<Renderer>().enabled = true;
			} else if (!showOnlyCurrentLevel && currentWayWidthFactor >= currentLevel) {
				shouldShow = true;
//				wayReference.GetComponent<Renderer>().enabled = true;
			} else {
//				wayReference.GetComponent<Renderer>().enabled = false;
			}
			setRendererStateForGameObject (GameObject.Find ("Plane Mesh for " + wayReference.name), shouldShow);
		}
	}

	private void setRendererStateForGameObject (GameObject gameObject, bool shouldRender) {
		gameObject.GetComponent<Renderer> ().enabled = shouldRender;
		gameObject.GetComponentsInChildren<Renderer> ().ToList ().ForEach (p => p.enabled = shouldRender);
	}

	public PROPAGATION onMessage (string message, object data) {
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
		} else if (message == "Vehicle:emitVapour") {
			Vehicle vehicle = (Vehicle)data;
			Vector3 emitPosition = vehicle.getEmitPosition () + new Vector3 (0f, 0f, mainCamera.transform.position.z + 1f);
			GameObject emission = Instantiate (vehicleVapour, emitPosition, vehicle.gameObject.transform.rotation) as GameObject;
			ParticleSystem particleSystem = emission.GetComponent<ParticleSystem> ();
			Renderer particleRenderer = particleSystem.GetComponent<Renderer> ();
			Material particleMaterial = particleRenderer.material;
			particleMaterial.color = vehicle.getVapourColor ();

			particleSystem.Simulate (0.90f, true);
			particleSystem.Play (true);

			StartCoroutine (destroyEmission (particleSystem, false));
		} else if (message == "Vehicle:createDangerHalo") {
			Vehicle vehicle = (Vehicle)data;
			if (!dangerHalos.ContainsKey (vehicle.vehicleId)) {
				Vector3 haloPosition = new Vector3 (vehicle.transform.position.x, vehicle.transform.position.y, -20f);
				GameObject dangerHalo = Instantiate (vehicleHalo, haloPosition, Quaternion.identity) as GameObject;
				dangerHalos.Add (vehicle.vehicleId, dangerHalo.GetComponent<Light> ());
				if (dangerHalos.Count == 1) {
					StartCoroutine ("pulsateDangerHalos");
				}
			}
		} else if (message == "Vehicle:removeDangerHalo") {
			Vehicle vehicle = (Vehicle)data;
			Light dangerHaloLight = dangerHalos [vehicle.vehicleId];
			dangerHalos.Remove (vehicle.vehicleId);
			Destroy (dangerHaloLight.gameObject);
			if (dangerHalos.Count == 0) {
				StopCoroutine ("pulsateDangerHalos");
			}
		} else if (message == "mainCameraActivated") {
			Game.running = true;

			if (loadedLevel != null) {
                VehicleRandomizer.Create (loadedLevel.vehicleRandomizer, loadedLevel);
				HumanRandomizer.Create (loadedLevel.humanRandomizer, loadedLevel);
				HumanLogic.HumanRNG = new System.Random (loadedLevel.randomSeed);
				Misc.setRandomSeed (loadedLevel.randomSeed);
				CustomObjectCreator.initWithSetup (loadedLevel.setup);
				PubSub.publish ("clock:setTime", loadedLevel.timeOfDay);

				bool showBrief = true; // TODO - Setting
				if (showBrief) {
					freezeGame (true);
					PubSub.publish ("brief:display", loadedLevel);
				}
			} else {
				VehicleRandomizer.Create ();
				HumanRandomizer.Create ();
				HumanLogic.HumanRNG = new System.Random ((int)Game.randomSeed);
				Misc.setRandomSeed ((int)Game.randomSeed);
				PubSub.publish ("clock:setTime", Misc.randomTime());
			}

			CameraHandler.InitialZoom ();
//			pointsCamera.enabled = true;
			pointsCamera.gameObject.SetActive (true);
			GenericVehicleSounds.VehicleCountChange ();
			GenericHumanSounds.HumanCountChange ();
		}
		return PROPAGATION.DEFAULT;
	}

	public IEnumerator pulsateDangerHalos() {
		float time;
		float amplitude;
		while (dangerHalos.Count > 0) {
			time = Time.time / 1.3f * 2 * Mathf.PI;
			amplitude = Mathf.Cos (time) + 2.5f;
			foreach (Light halo in dangerHalos.Values) {
				if (halo != null) {
					halo.intensity = amplitude;
				}
			}
			yield return null;
		}
	}

	public IEnumerator destroyEmission (ParticleSystem emission, bool addToCameraEmission = true) {
		yield return new WaitForSeconds (emission.duration);
		if (addToCameraEmission && emission != null && emission.gameObject != null) {
			Emission emissionObject = emission.gameObject.GetComponent<Emission> ();
			if (emissionObject != null) {
	            cameraEmission += emissionObject.Amount;
            }
			Destroy (emission.gameObject);
		}
	}

	public static bool isRunning () {
		return Game.running;
	}

	public static bool isPaused () {
		return Game.paused;
	}

	private void calculateVehicleFrequency ()
	{
		sumVehicleFrequency = 0f;
		vehicles.ForEach(vehicle => sumVehicleFrequency += vehicle.frequency);
	}

	private GameObject getVehicleToInstantiate (Setup.VehicleSetup data = null) {
		// Only camaros for now
		return vehicles [0].vehicle.gameObject;
		// TODO - fetching correct car model from data (if not null)
		// TODO - When doing performance fixes, take this back for more car types
		//		float randomPosition = Misc.randomRange (0f, sumVehicleFrequency);
//		foreach (VehiclesDistribution vehicle in vehicles) {
//			randomPosition -= vehicle.frequency;
//			if (randomPosition <= 0f) {
//				return vehicle.vehicle.gameObject;
//			}
//		}
//		return null;
	}

	public void quitApp() {
		Application.Quit ();
	}

	public void toggleOptions() {
		GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "OptionsSubmenu").gameObject;
		menuOptionsVisible (!subMenu.activeSelf);
	}

	private void menuOptionsVisible(bool show) {
		if (show) {
			menuStartVisible (false);
		}
		GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "OptionsSubmenu").gameObject;
		subMenu.SetActive (show);
	}

	public void toggleStartSubmenu() {
		GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "StartSubmenu").gameObject;
		menuStartVisible(!subMenu.activeSelf);
	}

	private void menuStartVisible(bool show) {
        GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "StartSubmenu").gameObject;
		if (show) {
			menuOptionsVisible (false);
            subMenu.GetComponent<LevelDataUpdater>().updateLevelStars();
		}
		subMenu.SetActive (show);
	}

    private void savePlayerPrefs(MenuValue menuValue, object value) {
        // Key to store value with
        string key = menuValue.key;

        // Get stored keys
		string storedKeysStr = PlayerPrefs.GetString ("Menu:storedKeys");
        if (storedKeysStr != null && storedKeysStr != "") {
	        string[] storedKeys = storedKeysStr.Split(',');
            if (!storedKeys.Contains(key)) {
                storedKeysStr += "," + key;
            }
        } else {
            storedKeysStr = key;
        }

        PlayerPrefs.SetString("Menu:storedKeys", storedKeysStr);

        // Save actual value
        Type valueType = value.GetType();
        if (valueType == typeof(float)) {
            PlayerPrefs.SetFloat(key, (float) value);
        } else if (valueType == typeof(int)) {
            PlayerPrefs.SetInt(key, (int) value);
        } else if (valueType == typeof(string)) {
            PlayerPrefs.SetString(key, (string) value);
        }
    }

	public void musicVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject();
        if (slider != null) {
			float value = slider.GetComponent<Slider>().value;

			MenuValue menuValue = slider.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, value);

			// TODO
        }

	}

	public void ambientSoundVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject();
		if (slider != null) {
			float value = slider.GetComponent<Slider> ().value;

			MenuValue menuValue = slider.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, value);

			PubSub.publish ("Volume:ambient", value);
		}
	}

	public void soundEffectsVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject();
		if (slider != null) {
			float value = slider.GetComponent<Slider> ().value;

			MenuValue menuValue = slider.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, value);

			soundEffectsVolume = value;
			PubSub.publish ("Volume:effects", value);
		}
	}

    // TODO - Put this in correct place (top of this file) or even make this dynamically loadable from languages DB/File
    private List<string> languages = new List<string>() {
        "English",
        "Svenska",
        "Deutsch",
        "François",
        "Español"
    };
    public void toggleLanguage() {
        Text languageText = GetText("Options:language");
        if (languageText != null) {
            string previousValue = languageText.text;
			string nextValue = getNextLanguage(previousValue);

			MenuValue menuValue = languageText.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, nextValue);

			languageText.text = nextValue;

//            PubSub.publish ("Language:set", nextValue);
        }
    }

    private string getNextLanguage(string prevLanguage) {
        return languages[(languages.IndexOf(prevLanguage) + 1) % languages.Count];
    }

    int clearClickCount = 0;
    public void clearAllData() {
        clearClickCount++;
        GameObject parent = GameObject.Find("Reset All");
        int childCount = parent.transform.childCount;
        if (clearClickCount >= childCount - 1) {
            PlayerPrefs.DeleteAll();
        }
        if (clearClickCount < childCount) {
            parent.transform.GetChild(Math.Min(2, clearClickCount-1)).gameObject.SetActive(false);
            parent.transform.GetChild(Math.Min(2, clearClickCount)).gameObject.SetActive(true);
        }
    }

    private Text GetText (string menuValueKey) {
        MenuValue[] menuValueObjects = Resources.FindObjectsOfTypeAll<MenuValue>();
        foreach (MenuValue menuValueObject in menuValueObjects) {
            if (menuValueObject.key == menuValueKey) {
                return menuValueObject.GetComponent<Text>();
            }
        }
        return null;
    }

	public void toggleFreezeGame () {
        freezeGame(!Game.frozen);
	}

    public void freezeGame (bool freeze) {
        Game.frozen = freeze;
        Time.timeScale = freeze ? 0f : 1f;
    }

	public static bool isMovementEnabled() {
		return Game.instance != null && !Game.frozen;
	}

	void toggleDebugMode () {
		Game.debugMode = !Game.debugMode;
		Game.humanDebugMode = false;
		Debug.Log ("Toggled vehicle debug path: " + Game.debugMode);
	}

	void toggleHumanDebugMode () {
		Game.humanDebugMode = !Game.humanDebugMode;
		Game.debugMode = false;
		Debug.Log ("Toggled human debug path: " + Game.humanDebugMode);
	}

    public void gameEnd(string type, Objectives objectives) {

        DataCollector.saveStats();
        DataCollector.saveWinLoseStat(type);

        // We should have all we need in Objectives, PointCalculator and DataCollector, to determine points
        PointCalculator pointCalculator = loadedLevel.pointCalculator;

        // Hide potential information window
        PubSub.publish("InformationWindow:hide");

        // Destroy the randomizers
        VehicleRandomizer.Destroy();
        HumanRandomizer.Destroy();
        CustomObjectCreator.Destroy();

        // Reset number of animationItemsInQueue
        animationItemsQueue = 0;

        // Reset emission levels and danger halos
        cameraEmission = 0f;
        dangerHalos.Clear();
        StopCoroutine ("pulsateDangerHalos");

        // Reset humans and vehicles stats, and ambient sounds for them
        Vehicle.Reset ();
        HumanLogic.Reset ();
        GenericVehicleSounds.VehicleCountChange ();
        GenericVehicleSounds.stopAmbientSound(0);
        GenericVehicleSounds.numberOfActiveChannels = 0;
        GenericHumanSounds.HumanCountChange ();
        GenericHumanSounds.stopAmbientSound(0);
        GenericHumanSounds.numberOfActiveChannels = 0;

        freezeGame(true);
        int pointsBefore = GameObject.FindGameObjectWithTag("Points").GetComponent<Points>().points;

        List<PointCalculator.Point> alreadyIncludedPoints = pointCalculator.getPoints(true);
        List<PointCalculator.Point> notYetIncludedPoints = pointCalculator.getPoints(false, objectives);

        // TODO - Present result of play with "alreadyIncluded" stated

        int points = pointsBefore;

        foreach (PointCalculator.Point point in notYetIncludedPoints) {
            // TODO - Present each part of the points
            points += point.calculatedValue;
        }

        int numberOfStars = pointCalculator.getNumberOfStars(points);
        Debug.Log(points + ": " + numberOfStars);

        // Save stats - if not already exists with higher value
        bool newHighscore = saveScore(points, numberOfStars);

        // TODO - Present new highscore and play serenade

        // TODO - When summary popup closes with "exit"/"close" or similar, call this
        exitLevel();
    }

	private bool saveScore(int points, int numberOfStars) {
        bool shouldSaveNewScore = true;

		string levelKeyPrefix = "level_" + loadedLevel.id;
        if (PlayerPrefs.HasKey(levelKeyPrefix + "_points")) {
            int prevPoints = PlayerPrefs.GetInt (levelKeyPrefix + "_points");
            int prevStars = PlayerPrefs.GetInt (levelKeyPrefix + "_stars");
			if (prevPoints >= points && prevStars >= numberOfStars) {
                shouldSaveNewScore = false;
            }
        }

        if (shouldSaveNewScore) {
            PlayerPrefs.SetInt (levelKeyPrefix + "_points", points);
			PlayerPrefs.SetInt (levelKeyPrefix + "_stars", numberOfStars);
			PlayerPrefs.Save ();

            // Update level info for shown levels in menu
            GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "StartSubmenu").gameObject;
            subMenu.GetComponent<LevelDataUpdater>().updateLevelStars();
        }

        return shouldSaveNewScore;
	}

	public GameObject getCurrentSelectedGameObject() {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null) {
            return eventSystem.currentSelectedGameObject;
        }
		return null;
	}
}
