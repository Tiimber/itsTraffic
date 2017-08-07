using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO - All relation that we are interested in needs to be fetched and merged with the .osm-file when building levels
// http://api.openstreetmap.com/api/0.6/relation/2577883/full

public class Game : MonoBehaviour, IPubSub {

	public GameObject planeGameObject;
	public GameObject menuSystem;
	public Camera orthographicCamera;
	public Camera perspectiveCamera;
	public Camera pointsCamera;

    public Transform waysParent;
    public Transform buildingsParent;

	public static Game instance;
	private int animationItemsQueue = 0;
	private float cameraEmission = 0f;
    public float graphicsQuality = 0.8f;
	public const float WAYS_Z_POSITION = -0.11f;
    private const float OPTIMAL_LAT_SPAN = 0.00191f;

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

    public static string endpointBaseUrl = "http://localhost:4002/";
    public static string customLevelsRelativeUrl = "custom-levels";
    public static string getLocationRelativeUrl = "get-location";
    public static string countryMetaDataRelativeUrl = "countries";
    public static string citiesMetaDataRelativeUrl = "cities";
    public static string countryCodeDataQuerystringPrefix = "?country=";

	private const float CLICK_RELEASE_TIME = 0.2f; 
	private const float THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK = 30f;

	public GameObject partOfWay;
	public GameObject partOfNonCarWay;
	public List<VehiclesDistribution> vehicles;
	public GameObject landuseObject;
	public GameObject trafficLight;
	public GameObject treeObject;
	public GameObject vehicleEmission;
	public GameObject vehicleVapour; 
	public GameObject vehicleHalo;
	public GameObject wayCrossing;
	public GameObject human;
    public GameObject poiObject;
    public GameObject sun;

	// These are not really rects, just four positions minX, minY, maxX, maxY
	private static Rect cameraBounds;
	private static Rect mapBounds;
	private static float latitudeToLongitudeRatio = 1f;

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
	private float leftClickReleaseTimer = 0f;
	private float rightClickReleaseTimer = 0f;
    private bool rightClickDown = false;
	private Vector3 rightMouseDownPosition;
	private Vector3 rightMousePosition;
	private Vector3 mouseDownPosition;
	private Vector3 prevMousePosition;

	private int debugIndex = 0;
	private List<string> debugIndexNodes = new List<string> () {
		"none", "endpoint", "straightWay", "intersections", "all"
	};

    public float lon;
    public float lat;
    public string countryCode;
    public string country;

    void Awake () {
        Game.instance = this;

//        Physics.gravity = new Vector3(0f, 0f, 9.81f);

        Misc.refreshInputMethods();

        StartCoroutine(getUserLocation());
    }

	// Use this for initialization
	void Start () {
		showMenu ();
		paused = false;
		initDataCollection ();
		calculateVehicleFrequency ();

        Achievements.testAll(true);

		StartCoroutine (MaterialManager.Init ());

		CameraHandler.SetIntroZoom (cameraOrtographicSize);
		CameraHandler.SetMainCamera (orthographicCamera);
        CameraHandler.SetPerspectiveCamera (perspectiveCamera);
        CameraHandler.SetRestoreState ();
		PubSub.subscribe ("gameIsReady", this);

//		Time.timeScale = 0.1f;
		// Subscribe to when emission is let out from vehicles
		PubSub.subscribe ("Vehicle:emitGas", this);
		// Subscribe to when vapour is let out from vehicles
		PubSub.subscribe ("Vehicle:emitVapour", this);
		// Subscribe to when vehicles crashes and needs attention
		PubSub.subscribe("Vehicle:createDangerHalo", this);
		// Subscribe to when crashed vehicles are removed, to remove the danger halo
		PubSub.subscribe("Vehicle:removeDangerHalo", this);

        // Subscribe to clock updating minutes (for progression of sun)
        PubSub.subscribe("clock:minuteProgressed", this);
	}

    public void restartLevel() {
        string levelFileUrl = loadedLevel.fileUrl;
		exitLevel();
        startMission(levelFileUrl);
    }

    public void exitLevel() {
        showMenu();

        running = false;
        paused = false;
        frozen = false;

        // Destroy "MapObjects" (vehicles, humans...)
        GameObject[] mapObjects = GameObject.FindGameObjectsWithTag("MapObject");
        foreach (GameObject mapObject in mapObjects) {
            Destroy(mapObject);
        }
        // Destroy "POI" objects
        GameObject[] poiObjects = GameObject.FindGameObjectsWithTag("POI");
        foreach (GameObject poiObject in poiObjects) {
            Destroy(poiObject);
        }

        objectProperties.Clear();

        CameraHandler.Restore();
        Map.Clear();
        NodeIndex.Clear();
        DataCollector.Clear();
        initDataCollection();
        PubSub.publish("points:clear");

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
		perspectiveCamera.enabled = true;
		StartCoroutine (loadXML (mapFileName, configFileName));
	}

	public void startMission(string levelSetupFileUrl = null) {
        // TODO - Next line to be removed later
        levelSetupFileUrl = levelSetupFileUrl == null ? levelSetupFileName : levelSetupFileUrl;
		perspectiveCamera.enabled = true;
		StartCoroutine (loadLevelSetup (levelSetupFileUrl));
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

        // TODO - Temporary - testing sun
		if (Input.GetKeyDown (KeyCode.Plus) || Input.GetKeyDown (KeyCode.P)) {
			changeSunTime(15);
		} else if (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.M)) {
            changeSunTime(-15);
		}

		// Explosion!
		if (Input.GetKeyDown (KeyCode.Alpha0)) {
            makeExplosion(1);
		} else if (Input.GetKeyDown (KeyCode.Alpha1)) {
            makeExplosion(2);
		} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
            makeExplosion(3);
		} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
            makeExplosion(8);
		} else if (Input.GetKeyDown (KeyCode.Alpha4)) {
            makeExplosion(13);
		} else if (Input.GetKeyDown (KeyCode.Alpha5)) {
            makeExplosion(24);
		}

        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            CameraHandler.ZoomToSizeAndMoveToPoint(0.7524731f, new Vector3(4.771079f,-1.98f, -30f));
            GameObject gameObjectBus = GameObject.Find ("Vehicle (id:103)");
            gameObjectBus.transform.rotation = Quaternion.Euler(0f,0f,-111f);
            GameObject gameObjectCar = GameObject.Find ("Vehicle (id:102)");
            gameObjectCar.transform.rotation = Quaternion.Euler(0f,0f,-207f);
            StartCoroutine(scriptedZoom());
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
//				orthographicCamera.enabled = true;
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
        // TODO Only for debug. Might be removed.
        else if (Input.GetKeyDown (KeyCode.Y)) {
            Application.targetFrameRate = Application.targetFrameRate == 120 ? 20 : 120;
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
				cancelFollow();
			} else {
				mouseDownPosition = mousePosition;
			}
			prevMousePosition = mousePosition;

			// Click logic
			if (firstFrame) {
				leftClickReleaseTimer = CLICK_RELEASE_TIME;
			} else {
				leftClickReleaseTimer -= Time.deltaTime;
			}
		} else if (leftClickReleaseTimer > 0f) {
			// Button not pressed, and was pressed < 0.2s, accept as click if not moved too much
			if (Misc.getDistance (mouseDownPosition, prevMousePosition) < THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK) {
                // TODO - Click when zoomed into vehicle - should show information window again
				// Vector3 mouseWorldPoint = screenToWorldPos(mouseDownPosition);
				// Debug.Log(mouseDownPosition + " => " + screenToWorldPos(mouseDownPosition) + " vs. " + orthographicCamera.ScreenToWorldPoint(mouseDownPosition));
				// PubSub.publish ("Click", mouseWorldPoint);
				PubSub.publish ("Click", mouseDownPosition);
				leftClickReleaseTimer = 0f;
			}
		}

        // Right click (hold)
        if (Input.GetMouseButton (1)) {
			// Click logic
            bool firstFrame = Input.GetMouseButtonDown (1);
            if (firstFrame) {
                rightClickReleaseTimer = CLICK_RELEASE_TIME;
                rightMouseDownPosition = Input.mousePosition;
            } else {
                rightClickReleaseTimer -= Time.deltaTime;
            }
		} else if (rightClickReleaseTimer > 0f) {
            rightMousePosition = rightMouseDownPosition;
            // TODO - Right click "down" should not toggle if nothing is selected with the click (not sending RMove on upcoming frames)
            rightClickDown = !rightClickDown;
            PubSub.publish ("RClick", rightMouseDownPosition);
            rightClickReleaseTimer = 0f;
        }

        if (rightClickDown) {
            if (rightMousePosition != Input.mousePosition) {
                rightMousePosition = Input.mousePosition;
				PubSub.publish("RMove", rightMousePosition);
            }
        }

		// TODO - This is for debug - choosing endpoints

		if (Game.debugMode) {
			if (Input.GetMouseButtonDown (1)) {
				Vector3 mousePosition = Input.mousePosition;
				Vector3 mouseWorldPoint = screenToWorldPosInBasePlane (mousePosition);
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
/*
		if (Game.humanDebugMode) {
			if (Input.GetMouseButtonDown (1)) {
				Vector3 mousePosition = Input.mousePosition;
				Vector3 mouseWorldPoint = screenToWorldPosInBasePlane (mousePosition);
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
*/

		if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
			float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
//			Debug.Log (scrollAmount);
			if (Game.running && scrollOk (Input.mousePosition)) {
				if (following()) {
					CameraHandler.CustomZoom (scrollAmount);
				} else {
					CameraHandler.CustomZoom (scrollAmount, Input.mousePosition);
				}
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

	private bool following () {
		InformationWindow informationWindow = GetComponent<InformationWindow> ();
		return informationWindow.isShown () || informationWindow.isFollowing ();
	}

	private void cancelFollow () {
		GetComponent<InformationWindow> ().stopFollow ();
	}

	public void addInitAnimationRequest () {
		animationItemsQueue++;
	}

	public void removeInitAnimationRequest () {
		animationItemsQueue--;
		if (animationItemsQueue == 0) {
			PubSub.publish ("gameIsReady");
			// StartCoroutine (fadeToMainCamera());
		}
	}

	// private IEnumerator fadeToMainCamera () {
	// 	// Wait half a second
	// 	yield return new WaitForSeconds (0.5f);
	// 	// Fade between cameras
	// 	yield return StartCoroutine( ScreenWipe.use.CrossFadePro (perspectiveCamera, orthographicCamera, 1.0f) );
	// 	// Now the game starts
	// 	PubSub.publish ("gameIsReady");
	// }

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
			humanLogic.setStartAndEndInfo (startInfo, endInfo, data, NodeIndex.getPosById(endPos));
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
			position = Misc.parseVector(data.startVector) + new Vector3 (0f, 0f, Vehicle.START_POSITION_Z);
		} else {
			position = getCameraPosition (pos1) + new Vector3 (0f, 0f, Vehicle.START_POSITION_Z);
		}
		GameObject vehicleInstance = Instantiate (getVehicleToInstantiate (data), position, Quaternion.identity) as GameObject;
		Vehicle vehicleObj = vehicleInstance.GetComponent<Vehicle> ();

		vehicleObj.StartPos = pos1;
		vehicleObj.CurrentPosition = pos1;
		vehicleObj.EndPos = pos2;

//		if (followCar) {
//			orthographicCamera.enabled = false;
//			vehicleObj.setDebug ();
//		} else {
//			Vehicle.detachCurrentCamera();
//			orthographicCamera.enabled = true;
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

    public static List<Pos> calculateCurrentPaths (Pos source, Pos target, Pos previousPoint, List<Pos> wayPoints, bool isVehicle, bool isBackingOk = false) {
        if (wayPoints == null || wayPoints.Count == 0) {
            return calculateCurrentPath(source, target, isVehicle, previousPoint);
        }

        List<Pos> wayPointsIncludingTarget = new List<Pos>(wayPoints);
        wayPointsIncludingTarget.Add(target);

        List<Pos> calculatedPath = calculateCurrentPath(source, wayPointsIncludingTarget[0], isVehicle, isBackingOk ? null : previousPoint);
        if (calculatedPath.Count > 1) {
            // Loop through all wayPoints (+target) to get remaining paths
            for (int i = 1; i < wayPointsIncludingTarget.Count; i++) {
				// Remove last item in calculated path, since it will be included in this iteration (as "source")
                calculatedPath.RemoveAt(calculatedPath.Count - 1);

				List<Pos> wayPointPath = calculateCurrentPath(wayPointsIncludingTarget[i - 1], wayPointsIncludingTarget[i], isVehicle, isVehicle ? calculatedPath[calculatedPath.Count - 1] : null);
                if (wayPointPath.Count < 2) {
                    // Part of path is impossible, fallback to calculating simple "source to target" path
                    return calculateCurrentPath(source, target, isVehicle, previousPoint);
                } else {
                    calculatedPath.AddRange(wayPointPath);
                }
            }
        }

        return calculatedPath;
    }

    public static List<Pos> calculateCurrentPaths (Pos source, Pos target, Pos previousPoint, Pos middle, bool isVehicle, bool isBackingOk = false) {
        List<Pos> calculatedPath = new List<Pos>();

		List<Pos> firstHalfPath = calculateCurrentPath(source, middle, isVehicle, isBackingOk ? null : previousPoint);
        if (firstHalfPath.Count > 1) {
            // Remove middle point (will be added in second half)
            firstHalfPath.RemoveAt(firstHalfPath.Count - 1);

			List<Pos> secondHalfPath = calculateCurrentPath(middle, target, isVehicle, isBackingOk ? null : firstHalfPath[firstHalfPath.Count - 1]);

            if (secondHalfPath.Count > 1) {
                calculatedPath.AddRange(firstHalfPath);
				calculatedPath.AddRange(secondHalfPath);
            }
        }

        return calculatedPath;
    }

	public static List<Pos> calculateCurrentPath (Pos source, Pos target, bool isVehicle = true, Pos previousPoint = null) {
		List<Pos> calculatedPath = new List<Pos> ();

		Dictionary<long, NodeDistance> visitedPaths = new Dictionary<long, NodeDistance> ();

		bool impossible = false;
		Pos current = source;
		visitedPaths.Add (current.Id, new NodeDistance (0, source, true));
		while (current != target) {
			visitedPaths[current.Id].visited = true;
			float currentCost = visitedPaths[current.Id].cost;

			List<KeyValuePair<Pos, WayReference>> neighbours = current.getNeighbours(previousPoint);
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
            previousPoint = current;
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
		perspectiveCamera.enabled = true;
		perspectiveCamera.gameObject.SetActive (true);
		showMenu (false);

        WWW www = CacheWWW.Get(levelFileName);

		yield return www;

		XmlDocument xmlDoc = new XmlDocument();
//		Debug.Log (www.url);
		xmlDoc.LoadXml(www.text);

		loadedLevel = new Level (xmlDoc);
        calculateVehicleFrequency ();
        preCalculateLevelSunProperties ();
        setCurrentSunProperties ();
        StartCoroutine (loadXML (loadedLevel.mapUrl, loadedLevel.configUrl));
	}

	private IEnumerator loadXML (string mapFileName, string configFileName) {
        freezeGame(false);
		perspectiveCamera.enabled = true;
		perspectiveCamera.gameObject.SetActive (true);
		showMenu (false);

        WWW www = CacheWWW.Get(mapFileName);

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
//		mapBounds = new Rect ((float)minlon, (float)minlat, (float)(maxlon - minlon), (float)(maxlat - minlat));

		float latDiff = (float)(maxlat - minlat);
		float lonDiff = (float)(maxlon - minlon);

        float outsideMapBoundsLat = latDiff - OPTIMAL_LAT_SPAN;
        float outsidePercent = outsideMapBoundsLat / latDiff;
        float outsideMapBoundsLon = lonDiff * outsidePercent;
        float lonDisplaySpan = lonDiff - outsideMapBoundsLon;

        // Map bounds are the center half of the full map constraints
//        mapBounds = new Rect ((float)minlon + lonDiff / 4f, (float)minlat + latDiff / 4f, lonDiff / 2f, latDiff / 2f);
        mapBounds = new Rect ((float)minlon + outsideMapBoundsLon / 2f, (float)minlat + outsideMapBoundsLat / 2f, lonDisplaySpan, OPTIMAL_LAT_SPAN);

		float refLatDiff = 0.00191f;
		float x = 0.9f / (20f * refLatDiff);
		heightFactor = x * (refLatDiff * refLatDiff) / latDiff;

		// Width to height ratio
		float widthHeightRatio = (float) Screen.width / (float) Screen.height;

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
		latitudeToLongitudeRatio = getLatitudeScale((float)minlat + (float)(maxlat - minlat) / 2f);
		Debug.Log(latitudeToLongitudeRatio);

		XmlNodeList nodeNodes = xmlDoc.SelectNodes("/osm/node");
		foreach (XmlNode xmlNode in nodeNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long id = Convert.ToInt64(attributes.GetNamedItem("id").Value);
			Pos node = new Pos(id, (float)Convert.ToDecimal(attributes.GetNamedItem("lon").Value), (float)Convert.ToDecimal(attributes.GetNamedItem("lat").Value));
			addTags(node, xmlNode);
			if (!NodeIndex.nodes.ContainsKey(id)) {
				NodeIndex.nodes.Add (id, node);
			}
		}
		Map.Nodes = NodeIndex.nodes.Values.ToList();

		handleRelations (xmlDoc);

		createOutsideArea ();

		XmlNodeList wayNodes = xmlDoc.SelectNodes("/osm/way");
		foreach (XmlNode xmlNode in wayNodes) {
			XmlAttributeCollection attributes = xmlNode.Attributes;
			long wayId = Misc.xmlLong(attributes.GetNamedItem("id"));

			Way way = new Way (wayId);
			addTags (way, xmlNode);
			addNodes (way, xmlNode);

			if (!NodeIndex.buildingWayIds.Contains (wayId)) {
				if (!Map.WayIndex.ContainsKey(wayId)) {
					Map.Ways.Add (way);
					Map.WayIndex.Add (wayId, way);
				}
			} else {
				createWayArea(xmlNode, way);

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
		WWW wwwConfig = CacheWWW.Get(configFileName);
		
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
				case "Driveway":
				case "Walkway": 
				case "Outdoors":
				default: 
					initRoofStreetOrOutdoors (type, objectNode); 
					break;
			}
		}
		// TODO move this to after all materials have finished loading
		List<GameObject> allBuildings = Misc.NameStartsWith ("Building (");
		List<GameObject> arenaRoofs = Misc.NameStartsWith ("Stadium (");
		allBuildings.AddRange(arenaRoofs);
        // TODO - This doesn't seem to apply materials correct (not loaded) on level start
		foreach (KeyValuePair<long, Dictionary<string, string>> objectEntry in objectProperties) {
			GameObject buildingRoofObj = GameObject.Find ("Building (" + objectEntry.Key + ")");
			if (buildingRoofObj == null) {
				buildingRoofObj = GameObject.Find ("Stadium (" + objectEntry.Key + ")");
			}
			if (buildingRoofObj != null) {
//				Debug.Log("BuildingRoof (" + objectEntry.Key + ")");
				BuildingRoof buildingRoof = buildingRoofObj.GetComponent<BuildingRoof>();
				buildingRoof.setProperties(objectEntry.Value);
				allBuildings.Remove (buildingRoofObj);
			}
		}

        foreach (Pos node in NodeIndex.nodes.Values) {
            POIIcon.createPotentialPOI(node);
        }

        if (allBuildings.Count > 0) {
			Dictionary<string, string> standardRoof = new Dictionary<string, string> ();
			standardRoof.Add ("material", "2");
			standardRoof.Add ("wall", "1000");
			foreach (GameObject buildingRoofObj in allBuildings) {
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

		// Capture soccerfields
		List<GameObject> soccerFields = Misc.FindShallowStartsWith("Landuse - soccerfield");
		foreach (GameObject soccerfield in soccerFields) {
			soccerfield.AddComponent<SoccerField>();
		}
	}

	private float getLatitudeScale(float latitude) {
		// latitude = lat;
		// Convert latitude to radians
		float latRad = Misc.ToRadians(latitude);

		float m1 = 111132.92f;		// latitude calculation term 1
		float m2 = -559.82f;		// latitude calculation term 2
		float m3 = 1.175f;			// latitude calculation term 3
		float m4 = -0.0023f;		// latitude calculation term 4
		float p1 = 111412.84f;		// longitude calculation term 1
		float p2 = -93.5f;			// longitude calculation term 2
 		float p3 = 0.118f;			// longitude calculation term 3

		// Calculate the length of a degree of latitude and longitude in meters
		float latlen = m1 + (m2 * Mathf.Cos(2 * latitude)) + (m3 * Mathf.Cos(4 * latitude)) + (m4 * Mathf.Cos(6 * latitude));
		float longlen = (p1 * Mathf.Cos(latitude)) + (p2 * Mathf.Cos(3 * latitude)) + (p3 * Mathf.Cos(5 * latitude));
		
		return latlen / longlen;
	}

	private void createOutsideArea () {
		GameObject landuse = Instantiate (landuseObject) as GameObject;
		landuse.transform.position = new Vector3 (0f, 0f, -0.098f);
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
					if (!way.Building && way.getTagValue("highway") != null) {
						WayReference wayReference = createPartOfWay (prev, pos, way);
						NodeIndex.addWayReferenceToNode (prev.Id, wayReference);
						NodeIndex.addWayReferenceToNode (pos.Id, wayReference);
					}
				}
				prev = pos;
			}
		}

        if (way.Building) {
            GameObject building = new GameObject();
            building.transform.position = new Vector3 (0f, 0f, -0.098f);
            building.transform.parent = buildingsParent;
            BuildingRoof roof = building.AddComponent<BuildingRoof> ();
			roof.createBuildingWithXMLNode (xmlNode);
		} else if (way.LandUse) {
			GameObject landuse = Instantiate (landuseObject) as GameObject;
			landuse.transform.position = new Vector3 (0f, 0f, -0.098f);
			LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
			surface.createLanduseWithXMLNode (xmlNode, way);
		} else {
			createWayArea(xmlNode, way);
		}
	}

	private void createWayArea(XmlNode xmlNode, Way way) {
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
		} else if (way.getWayType() != "unknown") {
			GameObject landuse = Instantiate (landuseObject) as GameObject;
			landuse.transform.position = new Vector3 (0f, 0f, -0.098f);
			LanduseSurface surface = landuse.GetComponent<LanduseSurface> ();
			surface.createLanduseWithXMLNode (xmlNode, way, way.getWayType ());
		}
	}

	void handleRelations (XmlDocument xmlDoc) {
        // Bus lines
        HashSet<string> busLines = new HashSet<string>();
        XmlNodeList relationRoutes = xmlDoc.SelectNodes("/osm/relation[./tag[@k='route' and @v='bus']]");
        foreach (XmlNode relationRoute in relationRoutes) {
            string xmlNodeId = relationRoute.Attributes ["id"].Value;
            XmlAttribute tagAttribute = (XmlAttribute) relationRoute.SelectSingleNode ("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='ref']/@v");
            string tagValue = tagAttribute.Value;
            if (tagValue != null) {
                busLines.Add(" - " + tagValue);
            }
        }
        ModelGeneratorVehicles.setBusLines(busLines.ToList());

		XmlNodeList relationNodes = xmlDoc.SelectNodes("/osm/relation");
        // TODO - continue; whenever a relation has been handles - no need to check multiple "area types" for the same xmlNode
		foreach (XmlNode xmlNode in relationNodes) {

			string xmlNodeId = xmlNode.Attributes ["id"].Value;
			XmlNode xmlNodeBuildingTag = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='building']");
			if (xmlNodeBuildingTag != null) {
				XmlAttribute xmlNodeWayOuterAttribute = (XmlAttribute) xmlNode.SelectSingleNode ("/osm/relation[@id='" + xmlNodeId + "']/member[@role='outer']/@ref");
				string outerWallWayId = xmlNodeWayOuterAttribute.Value;
				XmlNode wayNode = xmlDoc.SelectSingleNode ("/osm/way[@id='" + outerWallWayId + "']");
				XmlNode wayNodeIsBuilding = xmlDoc.SelectSingleNode ("/osm/way[@id='" + outerWallWayId + "']/tag[@k='building']");

				if (wayNodeIsBuilding == null) {
					// Add the wayIds of buildings to a list
					// TODO - Seems to not draw the buildings outline/inline, but still have them as nodeWays in the debug squares
					XmlNodeList xmlNodeWayBuildings = xmlNode.SelectNodes ("/osm/relation[@id='" + xmlNodeId + "']/member/@ref");
					foreach (XmlNode wallIdNode in xmlNodeWayBuildings) {
						NodeIndex.buildingWayIds.Add (Convert.ToInt64(wallIdNode.Value));
					}


					GameObject building = new GameObject();
					building.transform.position = new Vector3 (0f, 0f, -0.098f);
                    building.transform.parent = buildingsParent;
					bool isStadium = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='building' and @v='stadium']") != null;
					if (!isStadium) {
	                    BuildingRoof roof = building.AddComponent<BuildingRoof> ();
						roof.createBuildingWithXMLNode (wayNode);
					} else {
						// Stadium is a bit special - we need to slice it in half and take away the inner (to place the field)
						createStadium(xmlDoc, xmlNode, building, wayNode);
					}
				}
			}

			// TODO - Merge river and water?
			XmlNode xmlNodeRiverbankTag = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='waterway' and @v='riverbank']");
			if (xmlNodeRiverbankTag != null) {
				List<Vector3> riverBankNodes = new List<Vector3> ();
				XmlNodeList xmlRiverNodeWaysOuter = xmlNode.SelectNodes ("/osm/relation[@id='" + xmlNodeId + "']/member[@role='outer']");
				foreach (XmlNode xmlRiverNodeWayOuter in xmlRiverNodeWaysOuter) {
					XmlAttributeCollection wayAttributes = xmlRiverNodeWayOuter.Attributes;
					if (Misc.xmlString(wayAttributes.GetNamedItem("type")) == "way") {
						long wayId = Misc.xmlLong(wayAttributes.GetNamedItem("ref"));
						XmlNodeList riverbankNodesForWay = xmlDoc.SelectNodes ("/osm/way[@id='" + wayId + "']/nd");
						// Not all ways in a "riverbank" exists, if not, we will have to fill some gaps
						if (riverbankNodesForWay != null) {
							foreach(XmlNode riverBankNode in riverbankNodesForWay) {
								long nodeId = Misc.xmlLong(riverBankNode.Attributes.GetNamedItem("ref"));
								Vector3 nodeVector = Game.getCameraPosition(NodeIndex.nodes[nodeId]);
								if (NodeIndex.nodes.ContainsKey(nodeId) && !riverBankNodes.Contains(nodeVector)) {
									riverBankNodes.Add(nodeVector);
								}
							}
						}
					}
				}

				GameObject river = Instantiate (landuseObject) as GameObject;
				LanduseSurface landuseSurface = river.GetComponent<LanduseSurface> ();
				landuseSurface.createLanduseAreaWithVectors(riverBankNodes, "river");
				river.transform.position = new Vector3 (0f, 0f, -0.098f);
			}

			XmlNode xmlNodeWaterbankTag = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='natural' and @v='water']");
			if (xmlNodeWaterbankTag != null) {
				List<Vector3> waterBankNodes = new List<Vector3> ();
				XmlNodeList xmlWaterNodeWaysOuter = xmlNode.SelectNodes ("/osm/relation[@id='" + xmlNodeId + "']/member[@role='outer']");
				foreach (XmlNode xmlWaterNodeWayOuter in xmlWaterNodeWaysOuter) {
					XmlAttributeCollection wayAttributes = xmlWaterNodeWayOuter.Attributes;
					if (Misc.xmlString(wayAttributes.GetNamedItem("type")) == "way") {
						long wayId = Misc.xmlLong(wayAttributes.GetNamedItem("ref"));
						XmlNodeList waterNodesForWay = xmlDoc.SelectNodes ("/osm/way[@id='" + wayId + "']/nd");
						// Not all ways in "water" exists, if not, we will have to fill some gaps
						if (waterNodesForWay != null) {
							foreach(XmlNode waterNode in waterNodesForWay) {
								long nodeId = Misc.xmlLong(waterNode.Attributes.GetNamedItem("ref"));
								Vector3 nodeVector = Game.getCameraPosition(NodeIndex.nodes[nodeId]);
								if (NodeIndex.nodes.ContainsKey(nodeId) && !waterBankNodes.Contains(nodeVector)) {
									waterBankNodes.Add(nodeVector);
								}
							}
						}
					}
				}

				GameObject water = Instantiate (landuseObject) as GameObject;
				LanduseSurface landuseSurface = water.GetComponent<LanduseSurface> ();
				landuseSurface.createLanduseAreaWithVectors(waterBankNodes, "water");
				water.transform.position = new Vector3 (0f, 0f, -0.098f);
			}

            // Pedestrian areas
            XmlNode xmlNodePedestrianArea = xmlNode.SelectSingleNode("/osm/relation[@id='" + xmlNodeId + "']/tag[@k='highway' and @v='pedestrian']");
            if (xmlNodePedestrianArea != null) {
                List<Vector3> pedestrianAreaNodes = new List<Vector3> ();
                XmlNodeList xmlPedestrianAreaOuter = xmlNode.SelectNodes ("/osm/relation[@id='" + xmlNodeId + "']/member[@role='outer']");
                foreach (XmlNode xmlPedestrianAreaWayOuter in xmlPedestrianAreaOuter) {
                    XmlAttributeCollection wayAttributes = xmlPedestrianAreaWayOuter.Attributes;
                    if (Misc.xmlString(wayAttributes.GetNamedItem("type")) == "way") {
                        long wayId = Misc.xmlLong(wayAttributes.GetNamedItem("ref"));
                        XmlNodeList pedestrianNodesForWay = xmlDoc.SelectNodes ("/osm/way[@id='" + wayId + "']/nd");
						// Not all ways in "water" exists, if not, we will have to fill some gaps
                        if (pedestrianNodesForWay != null) {
                            foreach(XmlNode pedestrianNode in pedestrianNodesForWay) {
                                long nodeId = Misc.xmlLong(pedestrianNode.Attributes.GetNamedItem("ref"));
                                Vector3 nodeVector = Game.getCameraPosition(NodeIndex.nodes[nodeId]);
                                if (NodeIndex.nodes.ContainsKey(nodeId) && !pedestrianAreaNodes.Contains(nodeVector)) { // TODO - Is this (+above) if-clause needed/correct? It takes the value above, that is checked here
                                    pedestrianAreaNodes.Add(nodeVector);
                                }
                            }
                        }
                    }
                }

                GameObject pedestrianArea = Instantiate (landuseObject) as GameObject;
                LanduseSurface landuseSurface = pedestrianArea.GetComponent<LanduseSurface> ();
                landuseSurface.createLanduseAreaWithVectors(pedestrianAreaNodes, "pedestrian");
                pedestrianArea.transform.position = new Vector3 (0f, 0f, -0.098f);

				GameObject pedestrianGameObject = Misc.FindDeepChild(pedestrianArea.transform, "Plane Mesh For Points").gameObject;
                pedestrianGameObject.layer = LayerMask.NameToLayer("Planes");
                pedestrianGameObject.GetComponent<MapSurface>().createMeshCollider(false);
            }

            // TODO - Parking? More types
		}
		// TODO Subtract inner walls from the outer mesh.
	}

	private void createStadium(XmlDocument xmlDoc, XmlNode relationNode, GameObject parent, XmlNode backupWayNode) {
		bool canCreateCorrect = false;
		
		try {
			// Get outer and inner way references
			XmlNode outerWayMemberNode = relationNode.SelectSingleNode("member[@role='outer']");
			XmlNode innerWayMemberNode = relationNode.SelectSingleNode("member[@role='inner']");
			if (outerWayMemberNode != null && innerWayMemberNode != null) {
				// Get actual outer and inner ways
				XmlNode outerWayNode = xmlDoc.SelectSingleNode ("/osm/way[@id='" + outerWayMemberNode.Attributes.GetNamedItem("ref").Value + "']");
				XmlNode innerWayNode = xmlDoc.SelectSingleNode ("/osm/way[@id='" + innerWayMemberNode.Attributes.GetNamedItem("ref").Value + "']");
				// Get List of nodes ids for each
				XmlNodeList outerNodeReferences = outerWayNode.SelectNodes ("nd");
				XmlNodeList innerNodeReferences = innerWayNode.SelectNodes ("nd");
				
				// Get actual outer nodes vectors
				List<Vector3> outerNodes = new List<Vector3>();
				foreach (XmlNode outerNodeReference in outerNodeReferences) {
					long nodeId = Misc.xmlLong(outerNodeReference.Attributes.GetNamedItem("ref"));
					Pos node = NodeIndex.nodes[nodeId];
					Vector3 nodeVector = Game.getCameraPosition(node);
					outerNodes.Add(nodeVector);
				}

				// Get actual inner nodes vectors
				List<Vector3> innerNodes = new List<Vector3>();
				foreach (XmlNode innerNodeReference in innerNodeReferences) {
					long nodeId = Misc.xmlLong(innerNodeReference.Attributes.GetNamedItem("ref"));
					Pos node = NodeIndex.nodes[nodeId];
					Vector3 nodeVector = Game.getCameraPosition(node);
					innerNodes.Add(nodeVector);
				}

				BuildingRoof roof = parent.AddComponent<BuildingRoof> ();
				parent.name = "Stadium (" + relationNode.Attributes.GetNamedItem("id").Value + ")";
				canCreateCorrect = roof.createSplitMeshes(outerNodes, innerNodes);
			}
		} finally {
			if (!canCreateCorrect) {
				// Backup - if not working, create arena as "solid block" instead
				BuildingRoof roof = parent.AddComponent<BuildingRoof> ();
				roof.createBuildingWithXMLNode (backupWayNode);
			}
		}
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
            way.transform.parent = waysParent;
			way.name = "CarWay (" + previousPos.Id + ", " + currentPos.Id + ")";
		} else {
			way = Instantiate (partOfNonCarWay, position, rotation) as GameObject;
			originalScale = partOfNonCarWay.transform.localScale;
            way.transform.parent = waysParent;
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
		float yStretchFactor = wayObject.WayWidthFactor * Settings.wayWidthFactor;
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
		leftCollider.center = new Vector3 (-0.5f + colliderWidthPct / 2f, 0f, leftCollider.center.z);
		rightCollider.size = new Vector3 (colliderWidthPct, 1f, rightCollider.size.z);
		rightCollider.center = new Vector3 (0.5f - colliderWidthPct / 2f, 0f, rightCollider.center.z);

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
		// if (wayReference.way.CarWay) {
		// 	middleOfWay.transform.position = middleOfWay.transform.position + new Vector3 (0, 0, WAYS_Z_POSITION);
		// } else {
			middleOfWay.transform.position = middleOfWay.transform.position + new Vector3 (0, 0, WAYS_Z_POSITION);
		// }
        middleOfWay.transform.parent = waysParent;

		// Add rigidbody and mesh collider, so that they will fall onto the underlying plane
		Misc.AddGravityToWay(middleOfWay);
        Misc.AddWayObjectComponent(middleOfWay);

		// TODO - Config for material
		// Small ways are not drawn with material or meshes
		if (!wayReference.SmallWay || !wayReference.way.CarWay) {
			AutomaticMaterialObject middleOfWayMaterialObject = middleOfWay.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
			if (wayReference.way.CarWay) {
				middleOfWayMaterialObject.requestMaterial ("2002-Driveway", null); // TODO - Default material
				// Draw lines on way if car way
				WayLine wayLineObject = middleOfWay.AddComponent<WayLine> () as WayLine;
				wayLineObject.create (wayReference);
			} else {
				middleOfWayMaterialObject.requestMaterial ("4003-Walkway", null); // TODO - Default material
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
        return getCameraPosition(pos, Game.mapBounds, Game.cameraBounds);
	}

	public static Vector3 getCameraPosition (Pos pos, Rect mapBounds, Rect cameraBounds)
	{
		float posX = pos.Lon;
		float posY = pos.Lat;

		// float cameraPosX = ((posX - mapBounds.x) / mapBounds.width) * cameraBounds.width / latitudeToLongitudeRatio + cameraBounds.x;
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
			Vector3 emitPosition = vehicle.getEmitPosition () + new Vector3 (0f, 0f, -0.1f);
			GameObject emission = Instantiate (vehicleEmission, emitPosition, vehicle.gameObject.transform.rotation) as GameObject;
//			DebugFn.arrow(vehicle.transform.position, emitPosition);
			emission.GetComponent<Emission> ().Amount = vehicle.getEmissionAmount ();
			ParticleSystem particleSystem = emission.GetComponent<ParticleSystem> ();
			particleSystem.Simulate (0.10f, true);
			particleSystem.Play (true);

			StartCoroutine (destroyEmission (particleSystem));
		} else if (message == "Vehicle:emitVapour") {
			Vehicle vehicle = (Vehicle)data;
			Vector3 emitPosition = vehicle.getEmitPosition () + new Vector3 (0f, 0f, -0.1f);
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
				Vector3 haloPosition = new Vector3 (vehicle.transform.position.x, vehicle.transform.position.y, -0.1f);
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
		} else if (message == "gameIsNotReady") {
			CameraHandler.IsMapReadyForInteraction = false;
		} else if (message == "gameIsReady") {
			CameraHandler.IsMapReadyForInteraction = true;
			Game.running = true;

			List<GameObject> ways = Misc.FindGameObjectsWithLayer(LayerMask.NameToLayer("Ways"));
			Misc.SetGravityState(ways);
            Misc.SetAverageZPosition(ways);
            Misc.ReleaseGravityConstraintsOnWay(ways);
            Misc.SetWeightOnWays(ways);

			if (loadedLevel != null) {
                VehicleRandomizer.Create (loadedLevel.vehicleRandomizer, loadedLevel);
				HumanRandomizer.Create (loadedLevel.humanRandomizer, loadedLevel);
				HumanLogic.HumanRNG = new System.Random (loadedLevel.randomSeed);
				Misc.setRandomSeed (loadedLevel.randomSeed);
				CustomObjectCreator.initWithSetup (loadedLevel.setup);
                // TODO - May not be needed, we set these when parsing level data
				PubSub.publish ("clock:setTime", loadedLevel.timeOfDay);
				PubSub.publish ("clock:setDisplaySeconds", loadedLevel.timeDisplaySeconds);
				PubSub.publish ("clock:setSpeed", loadedLevel.timeProgressionFactor);
				PubSub.publish ("clock:stop");

				bool showBrief = true; // TODO - Setting
				if (showBrief) {
                    freezeGame (true);
					PubSub.publish ("brief:display", loadedLevel);
                    // Combine meshes in ways and buildings
					CombineMeshes combineWayMeshes = waysParent.GetComponent<CombineMeshes> ();
                    combineWayMeshes.combineMeshes();
				}
			} else {
				VehicleRandomizer.Create ();
				HumanRandomizer.Create ();
				HumanLogic.HumanRNG = new System.Random ((int)Game.randomSeed);
				Misc.setRandomSeed ((int)Game.randomSeed);
				PubSub.publish ("clock:setTime", Misc.randomTime());
                PubSub.publish ("clock:setDisplaySeconds", true);
                PubSub.publish ("clock:setSpeed", 1);
                PubSub.publish ("clock:stop");
            }

			CameraHandler.InitialZoom ();
//			pointsCamera.enabled = true;
			pointsCamera.gameObject.SetActive (true);
			GenericVehicleSounds.VehicleCountChange ();
			GenericHumanSounds.HumanCountChange ();
		} else if (message == "clock:minuteProgressed") {
            Clock clock = (Clock)data;
            changeSunTime(clock.hour, clock.minutes);
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
        if (loadedLevel != null && loadedLevel.vehiclesDistribution != null && loadedLevel.vehiclesDistribution.Count > 0) {
            loadedLevel.vehiclesDistribution.ForEach(vehicle => sumVehicleFrequency += vehicle.frequency);
        } else {
            vehicles.ForEach(vehicle => sumVehicleFrequency += vehicle.frequency);
        }
	}

	private GameObject getVehicleToInstantiate (Setup.VehicleSetup data = null) {
        // First try and find the correct car for the data setup
        if (data != null && data.brand != null) {
			VehiclesDistribution foundVehicle = vehicles.Find (vehicle => vehicle.brand == data.brand);
            if (foundVehicle != null) {
                return foundVehicle.vehicle.gameObject;
            }
        }

        // If no data setup, or no matching brand, select randomly with the setup vehicle distribution
		float randomPosition = Misc.randomRange (0f, sumVehicleFrequency);

        bool haveLevelDistributionSetup = loadedLevel != null && loadedLevel.vehiclesDistribution != null && loadedLevel.vehiclesDistribution.Count > 0;
        List<VehiclesDistribution> vehicleDistributionSetup = haveLevelDistributionSetup ? loadedLevel.vehiclesDistribution : vehicles;

		foreach (VehiclesDistribution vehicle in vehicleDistributionSetup) {
			randomPosition -= vehicle.frequency;
			if (randomPosition <= 0f) {
				return vehicle.vehicle.gameObject;
			}
		}
		return null;
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
            menuCustomVisible (false);
            menuAchievementsVisible (false);
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
		subMenu.SetActive (show);
		if (show) {
			menuOptionsVisible (false);
            menuCustomVisible (false);
            menuAchievementsVisible (false);
            subMenu.GetComponent<LevelDataUpdater>().refresh();
		}
	}

	public void toggleCustomSubmenu() {
		GameObject customSubMenu = Misc.FindDeepChild(menuSystem.transform, "CustomSubmenu").gameObject;
		menuCustomVisible(!customSubMenu.activeSelf);
	}

	private void menuCustomVisible(bool show) {
        GameObject customSubMenu = Misc.FindDeepChild(menuSystem.transform, "CustomSubmenu").gameObject;
        customSubMenu.SetActive (show);
		if (show) {
			menuOptionsVisible (false);
            menuStartVisible (false);
            menuAchievementsVisible (false);
            customSubMenu.GetComponent<LevelDataUpdater>().refresh();
		}
	}

    public void toggleAchievementsSubmenu() {
        GameObject achievementsSubMenu = Misc.FindDeepChild(menuSystem.transform, "AchievementsSubmenu").gameObject;
        menuAchievementsVisible(!achievementsSubMenu.activeSelf);
    }

    private void menuAchievementsVisible(bool show) {
        GameObject achievementsSubMenu = Misc.FindDeepChild(menuSystem.transform, "AchievementsSubmenu").gameObject;
        achievementsSubMenu.SetActive (show);
        if (show) {
            menuOptionsVisible (false);
            menuStartVisible (false);
            menuCustomVisible (false);
			achievementsSubMenu.GetComponent<AchievementUpdater> ().refresh();
        }
    }

    // TODO - Temporary for filter distance
    public void customSearch(bool distanceFilterOnly = false) {
        GameObject customSubMenu = Misc.FindDeepChild(menuSystem.transform, "CustomSubmenu").gameObject;
        GameObject searchField = Misc.FindDeepChild(customSubMenu.transform, "Search field").gameObject;
        customSubMenu.GetComponent<LevelDataUpdater>().setFilter(searchField.GetComponent<InputField>().text);
        if (distanceFilterOnly) {
            customSubMenu.GetComponent<LevelDataUpdater>().updateLevelGameObjects();
        } else {
            customSubMenu.GetComponent<LevelDataUpdater>().refresh();
        }
    }

    private void savePlayerPrefs(MenuValue menuValue, object value) {
        if (Menu.haveInitialized) {
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

            PlayerPrefsData.Save ();
        }
    }

    public void graphicsQualityChanged() {
        GameObject slider = getCurrentSelectedGameObject("Graphics Quality");
        if (slider != null) {
            float value = slider.GetComponent<Slider>().value;

            MenuValue menuValue = slider.GetComponent<MenuValue>();
            savePlayerPrefs(menuValue, value);

            graphicsQuality = value;

            PubSub.publish ("Graphics:quality", value);
        }
    }

	public void musicVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject("Music");
        if (slider != null) {
			float value = slider.GetComponent<Slider>().value;

			MenuValue menuValue = slider.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, value);

            PubSub.publish ("Volume:music", value); // TODO
        }
	}

	public void ambientSoundVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject("Ambient Sounds");
		if (slider != null) {
			float value = slider.GetComponent<Slider> ().value;

			MenuValue menuValue = slider.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, value);

			PubSub.publish ("Volume:ambient", value);
		}
	}

	public void soundEffectsVolumeChanged() {
		GameObject slider = getCurrentSelectedGameObject("Sound Effects");
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
        Text languageText = Misc.GetMenuValueTextForKey("Options:language");
        if (languageText != null) {
            string previousValue = languageText.text;
			string nextValue = getNextLanguage(previousValue);

			MenuValue menuValue = languageText.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, nextValue);

			languageText.text = nextValue;

//            PubSub.publish ("Language:set", nextValue);
        }
    }

    public void changeLocation() {
        Text locationText = Misc.GetMenuValueTextForKey("Options:location");
        if (locationText != null) {
            string previousValue = locationText.text;
			string nextValue = getNextLanguage(previousValue);

			MenuValue menuValue = locationText.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, nextValue);

			locationText.text = nextValue;

//            PubSub.publish ("Location:set", nextValue);
        }
    }

    private string getNextLanguage(string prevLanguage) {
        return languages[(languages.IndexOf(prevLanguage) + 1) % languages.Count];
    }

    public void toggleController() {
		Text controllerText = Misc.GetMenuValueTextForKey("Options:controller");
		if (controllerText != null) {
			string previousValue = controllerText.text;
			string nextValue = getNextController(previousValue);

			MenuValue menuValue = controllerText.GetComponent<MenuValue>();
			savePlayerPrefs(menuValue, nextValue);

			controllerText.text = nextValue;

//            PubSub.publish ("Language:set", nextValue);
		}
	}

    private string getNextController(string prevController) {
		List<string> controllerNames = Misc.getInputMethodNames();
        if (controllerNames.Contains(prevController)) {
            return controllerNames[(controllerNames.IndexOf(prevController) + 1) % controllerNames.Count];
        } else {
            return controllerNames[0];
        }
    }

    int clearClickCount = 0;
    public void clearAllData() {
        clearClickCount++;
        GameObject parent = GameObject.Find("Reset All");
        int childCount = parent.transform.childCount;
        if (clearClickCount >= childCount - 1) {
            PlayerPrefsData.DeleteAll();
        }
        if (clearClickCount < childCount) {
            parent.transform.GetChild(Math.Min(2, clearClickCount-1)).gameObject.SetActive(false);
            parent.transform.GetChild(Math.Min(2, clearClickCount)).gameObject.SetActive(true);
        }
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
		PubSub.publish("gameIsNotReady");

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

        int points = pointsBefore;

        foreach (PointCalculator.Point point in notYetIncludedPoints) {
            points += point.calculatedValue;
        }

        int numberOfStars = pointCalculator.getNumberOfStars(points);

        DataCollector.saveNumberOfStarsStat(numberOfStars);

        // Save stats - if not already exists with higher value
        bool newHighscore = saveScore(points, numberOfStars);

		Summary summary = new Summary (loadedLevel.name, type, pointsBefore, objectives, alreadyIncludedPoints, notYetIncludedPoints, numberOfStars, newHighscore);
        PubSub.publish("summary:display", summary);

        Achievements.testAll();
    }

	private bool saveScore(int points, int numberOfStars) {
        bool shouldSaveNewScore = true;

        int prevPoints = PlayerPrefsData.GetLevelPoints(loadedLevel.id);
        int prevStars = PlayerPrefsData.GetLevelStars(loadedLevel.id);
        if (prevPoints >= points && prevStars >= numberOfStars) {
            shouldSaveNewScore = false;
        }

        if (numberOfStars > prevStars) {
            int diffStars = numberOfStars - prevStars;
            DataCollector.saveNumberOfTotalStarsStat(diffStars);
        }

        if (shouldSaveNewScore) {
            PlayerPrefsData.SetLevelPoints(loadedLevel.id, points);
            PlayerPrefsData.SetLevelStars(loadedLevel.id, numberOfStars);
			PlayerPrefsData.Save ();

            // Update level info for shown levels in menu - bundled & custom
            GameObject subMenu = Misc.FindDeepChild(menuSystem.transform, "StartSubmenu").gameObject;
            if (subMenu != null) {
                subMenu.GetComponent<LevelDataUpdater>().updateLevelStars();
            }
            GameObject customSubMenu = Misc.FindDeepChild(menuSystem.transform, "CustomSubmenu").gameObject;
            if (customSubMenu != null) {
                customSubMenu.GetComponent<LevelDataUpdater>().updateLevelStars(true);
            }
        }

        return shouldSaveNewScore;
	}

	public GameObject getCurrentSelectedGameObject(String fallbackGameObjectName) {
        if (EventSystem.current != null) {
			// Try to resolve gameObject
			Transform optionsSubmenuTransform = Misc.FindDeepChild (menuSystem.transform, "OptionsSubmenu");
			Transform changedGameObjectParent = Misc.FindDeepChild (optionsSubmenuTransform, fallbackGameObjectName);
			GameObject settingsGameObject = Misc.GetGameObjectWithMenuValue(changedGameObjectParent);
			return settingsGameObject;
        }
        return null;
	}

	private IEnumerator getUserLocation() {

        if (!Input.location.isEnabledByUser) {
            // Get location by IP
            yield return Misc.getGeoLocation();
        } else {
	        // Location services enabled
            Input.location.Start(10f, 1f);

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1f);
                maxWait--;
            }
            if (Input.location.status == LocationServiceStatus.Failed) {
                Debug.Log("ERROR: Location could not be fetched, will try to fetch by IP instead");
                yield return Misc.getGeoLocation();
            } else {
                Debug.Log(Input.location.lastData.longitude + " - " + Input.location.lastData.latitude);
                lon = Input.location.lastData.longitude;
                lat = Input.location.lastData.latitude;

                Input.location.Stop();
            }
        }

        Debug.Log("Your geo location: " + lon + " , " + lat);
        Debug.Log("You - Lund: " + Misc.getDistanceBetweenEarthCoordinates(lon, lat, 13.1910f, 55.7047f));
        Debug.Log("You - Jönköping: " + Misc.getDistanceBetweenEarthCoordinates(lon, lat, 14.1618f, 57.7826f));
    }

    private Dictionary<string, Dictionary<string, float>> sunPositionDataForLoadedLevel;
    private void preCalculateLevelSunProperties() {
        // Will pre-calculate the rest of the current day + 24hrs of azimuth and elevation for the sun, and smoothen them out where needed
        DateTime levelDate = new DateTime(loadedLevel.dateTime.Ticks);
        List<float> azimuthData = new List<float>();
        List<float> elevationData = new List<float>();

        int startDay = levelDate.Day;
        while (levelDate.Day < startDay + 2) {
            Dictionary<string, float> sunPositionAtTime = Misc.getSunPosition (levelDate, loadedLevel.lon, loadedLevel.lat);
            azimuthData.Add(sunPositionAtTime["azimuth"]);
            elevationData.Add(sunPositionAtTime["elevation"]);
            levelDate = levelDate.AddMinutes(1);
        }

        // Redistribute the data smoother
        redistributeValues (elevationData);
        redistributeValues (azimuthData);

        // Put them back in with their timestamps as keys, always put first value in there, whenever elevation is <= 0, duplicates are not needed
        sunPositionDataForLoadedLevel = new Dictionary<string, Dictionary<string, float>>();
        levelDate = new DateTime(loadedLevel.dateTime.Ticks);
        addSunPosition(levelDate, sunPositionDataForLoadedLevel, elevationData[0], azimuthData[0]);
        bool isBelowZero = elevationData[0] <= 0f;

        for (int i = 1; i < elevationData.Count; i++) {
            levelDate = levelDate.AddMinutes(1);

            float currentElevation = elevationData[i];
            float currentAzimuth = azimuthData[i];
			bool isCurrentBelowZero = currentElevation <= 0f;
            if (!(isBelowZero && isCurrentBelowZero) || i == elevationData.Count - 1) {
                addSunPosition(levelDate, sunPositionDataForLoadedLevel, currentElevation, currentAzimuth);
            }
        }
    }

    private void redistributeValues(List<float> data) {
        float previousValue = data [0];
        int previousValueFirstIndex = 0;
        for (int i = 1; i < data.Count; i++) {
            float currentValue = data [i];
            if (currentValue != previousValue) {
				List<float> distributedRange = getDistributedRange(i - previousValueFirstIndex + 1, previousValue, currentValue);
                int j = 0;
                foreach (float distributedValue in distributedRange) {
                    data [previousValueFirstIndex+j] = distributedValue;
                    j++;
                }
                previousValue = currentValue;
                previousValueFirstIndex = i;
            }
        }
    }

    private List<float> getDistributedRange (int amount, float startValue, float endValue) {
        List<float> distributedValues = new List<float>();
        float stepValue = (endValue - startValue) / (amount - 1);
        for (float i = startValue; i < endValue + stepValue / 2f; i += stepValue) {
            distributedValues.Add(i);
        }
        return distributedValues;
    }

    public void addSunPosition(DateTime dateTime, Dictionary<string, Dictionary<string, float>> sunPositions, float elevationValue, float azimuthValue) {
        sunPositionDataForLoadedLevel.Add(dateTime.ToString("dd HH:mm"), new Dictionary<string, float>() {
            {"elevation", elevationValue},
            {"azimuth", azimuthValue}
        });
    }

	private void setCurrentSunProperties() {
//        Dictionary<string, float> sunPosition = Misc.getSunPosition (loadedLevel.dateTime, loadedLevel.lon, loadedLevel.lat);
        string currentTimeKey = loadedLevel.dateTime.ToString("dd HH:mm");
        if (sunPositionDataForLoadedLevel.ContainsKey(currentTimeKey)) {
            Dictionary<string, float> sunPosition = sunPositionDataForLoadedLevel[currentTimeKey];
			sun.transform.rotation = Misc.getSunRotation (sunPosition ["azimuth"]);
			sun.GetComponentInChildren<Light>().intensity = 0.7f * Misc.getSunIntensity (sunPosition ["elevation"]);
        }

//        Debug.Log("Sun elevation: " + sunPosition["elevation"]);
//        Debug.Log("Sun azimuth: " + sunPosition["azimuth"]);

/*
		DateTime dt = new DateTime(2016, 12, 02, 0, 0, 0, 0);
		int safety = 1000;
		while (dt.Day == 2 && safety-- > 0) {
			Debug.Log(dt.ToString("yyyy-MM-dd HH:mm"));
			Dictionary<string, float> sunPositionAtTime = Misc.getSunPosition (dt, loadedLevel.lon, loadedLevel.lat);
			Debug.Log("Sun elevation: " + sunPositionAtTime["elevation"]);
			Debug.Log("Sun azimuth: " + sunPositionAtTime["azimuth"]);
			dt = dt.AddMinutes(15);
		}
*/
    }

    private void changeSunTime (int hour, int minute) {
        DateTime dt = loadedLevel.dateTime;
        dt = dt.AddMinutes(minute - dt.Minute);
        dt = dt.AddHours(hour - dt.Hour);
        loadedLevel.dateTime = dt;

        setCurrentSunProperties ();
    }

    private void changeSunTime (int minutesToAdd) {
        DateTime dt = loadedLevel.dateTime;
        dt = dt.AddMinutes(minutesToAdd);
        loadedLevel.dateTime = dt;

        Dictionary<string, float> sunPosition = Misc.getSunPosition (loadedLevel.dateTime, loadedLevel.lon, loadedLevel.lat);
        Debug.Log(dt.ToString("HH:mm") + " - " + sunPosition["elevation"] + " = " + Misc.getSunIntensity (sunPosition ["elevation"]) + ", rotation:" + Misc.getSunRotation (sunPosition ["azimuth"]).eulerAngles.z);

        setCurrentSunProperties ();
    }

	private bool planeInitialized = false;
	private Plane plane;
	public Vector3 screenToWorldPosInBasePlane(Vector3 mousePosition) {
		if (!planeInitialized) {
			plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeGameObject.transform.position.z));
			planeInitialized = true;
		}
		Ray ray = perspectiveCamera.ScreenPointToRay(mousePosition);
		float distance;
	    plane.Raycast(ray, out distance);
		return ray.GetPoint(distance);
	}

	public Vector3 screenToWorldPosInPlane(Vector3 mousePosition, Plane plane) {
		Ray ray = perspectiveCamera.ScreenPointToRay(mousePosition);
		float distance;
	    plane.Raycast(ray, out distance);
		return ray.GetPoint(distance);
	}

    public Vector3 objectToScreenPos(GameObject positionObj, Camera camera = null) {
        if (camera == null) {
        	return perspectiveCamera.WorldToScreenPoint(positionObj.transform.position);
        } else {
			return camera.WorldToScreenPoint(positionObj.transform.position);
		}
    }

	private void makeExplosion(int explosionFactor) {
        stopAll();
        turnOnAllGravity();
        GameObject explosionSphere = GameObject.Find("ExplosionSphere");
        Collider[] colliders = Physics.OverlapSphere(explosionSphere.transform.position, 40f);
        foreach (Collider hit in colliders) {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddExplosionForce(explosionFactor * 100f, explosionSphere.transform.position, 40f);
            }
        }
	}

    private void turnOnAllGravity() {
        InterfaceHelper.FindObjects<IExplodable>().ToList<IExplodable>().ForEach(i => i.turnOnExplodable());
    }

    // Game over (probably with explosion or something)
    private void stopAll() {
        CustomObjectCreator.Destroy();
        VehicleRandomizer.Destroy();
        HumanRandomizer.Destroy();
        paused = true;
    }

	private IEnumerator scriptedZoom() {
        yield return new WaitForSeconds(24f);
        Vector3 startPoint = new Vector3(4.771079f, -1.98f, -30f);
        yield return CameraHandler.ZoomWithAmount(0.002f,4f);
        yield return new WaitForSeconds(4f);
        yield return CameraHandler.ZoomWithAmount(0.004f,8f);
	}
}
