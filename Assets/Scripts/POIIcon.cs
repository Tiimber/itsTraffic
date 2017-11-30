using System.Collections.Generic;
using UnityEngine;

public class POIIcon : MonoBehaviour, IPubSub {

    private static Dictionary<string, GameObject> groups = new Dictionary<string, GameObject> ();

    private static int BUILDINGROOF_LAYER_MASK = -1;

    private const float THRESHOLD_CAMERA_ZOOM_ICON_SWAP = 2.5f;
    private const float DISTANCE_FROM_ROOFTOPS = 0.01f;

    private static Dictionary<string, Material> groupMaterials = new Dictionary<string, Material>();
    private const string SHOP = "Shop";
    private const string FOOD = "Food";
    private const string HOTEL = "Hotel";
    private const string ATM = "ATM";
    private static List<string> groupKeys = new List<string> () {
        SHOP,
        FOOD,
        HOTEL,
        ATM
    };

    public static void createPotentialPOI(Pos node) {
        if (groupMaterials.Count == 0) {
            loadGroupMaterials();
        }

        if (isInterestingPOI (node)) {
            string group = getGroup (node);
            if (group != null) {
                if (!groups.ContainsKey (group)) {
                    groups.Add (group, new GameObject (group));
                }

                GameObject poi = Instantiate(Game.instance.poiObject, Game.getCameraPosition(node) + Game.instance.poiObject.transform.position, Game.instance.poiObject.transform.rotation, groups [group].transform) as GameObject;
                poi.name = (node.hasTag ("name") ? node.getTagValue ("name") : "") + "(POI:" + node.Id + ")";
                POIIcon poiIcon = poi.GetComponent<POIIcon> ();
                poiIcon.node = node;
                poiIcon.group = group;
            }
        }
    }

    private static bool isInterestingPOI(Pos node) {
        return isShop (node) || isRestaurant (node) || isHotel (node) || isATM (node);
    }

    private static bool isShop(Pos node) {
        return node.hasTag ("shop");
    }

    private static bool isRestaurant(Pos node) {
        return node.hasTagWithValues ("amenity", "fast_food", "pub", "restaurant", "cafe");
    }

    private static bool isHotel(Pos node) {
        return node.hasTagWithValues ("tourism", "hotel");
    }

    private static bool isATM(Pos node) {
        return node.hasTagWithValues ("amenity", "atm");
    }

    public static string getGroup(Pos node) {
        if (isShop (node)) {
            return SHOP;
        } else if (isRestaurant (node)) {
            return FOOD;
        } else if (isHotel (node)) {
            return HOTEL;
        } else if (isATM (node)) {

            return ATM;
        }
        return null;
    }

    private List<InformationHuman> peopleGoingHere = new List<InformationHuman>();
    private Pos node;
    private string group;
    private bool isFalling;
    private float targetZ = 0f;
    private float fallSpeed = 0f;
    private float delay = 1f;
    private bool showingSmall = true;
    private GameObject bigIcon;
    private GameObject smallIcon;

    void Start() {
        if (BUILDINGROOF_LAYER_MASK == -1) {
            BUILDINGROOF_LAYER_MASK = LayerMask.GetMask(new string[]{"BuildingRoof"});
        }

        RaycastHit hit;
        bool buildingWasHit = Physics.Raycast(transform.position, Vector3.forward, out hit, 40f, BUILDINGROOF_LAYER_MASK);

        // Subscribe to know who are going here
        PubSub.subscribe("TargetPOI(" + node.Id + "):Add", this);
        PubSub.subscribe("TargetPOI(" + node.Id + "):Remove", this);
        PubSub.subscribe("gameIsReady", this);

        // Add InformationPOI, since we now have inited this icon
        this.gameObject.AddComponent<InformationPOI>();

        // Get small and big icon gameobjects
        bigIcon = Misc.FindDeepChild(transform, "icon-big").gameObject;
        smallIcon = Misc.FindDeepChild(transform, "icon-small").gameObject;

        // Set big icon material
        MeshRenderer bigIconMeshRenderer = bigIcon.GetComponent<MeshRenderer> ();
        bigIconMeshRenderer.material = groupMaterials[group];

        isFalling = true;
        Game.instance.addInitAnimationRequest();

        if (buildingWasHit) {
            BuildingRoof buildingRoof = hit.transform.gameObject.GetComponent<BuildingRoof> ();
            // Land on the building
            targetZ = -buildingRoof.getTargetHeight() - DISTANCE_FROM_ROOFTOPS;
        } else {
            // Land on the ground
            targetZ = -0.1f - DISTANCE_FROM_ROOFTOPS;
        }

        // Set random velocity
        fallSpeed = Misc.randomPlusMinus ((transform.position.z - targetZ) / 3f, 1f);
    }

    public string getName() {
        return node.getTagValue("name") != null ? node.getTagValue("name") : group;
    }

    public string getGroup() {
        return group;
    }

    public string getAddress() {
        string address = null;
        string street = node.getTagValue ("addr:street");
        string houseNumber = node.getTagValue ("addr:housenumber");
        if (street != null) {
            address = street;
            if (houseNumber != null) {
                address += " " + houseNumber;
            }
        }
        return address;
    }

    public List<InformationHuman> getPeopleGoingHere() {
        return peopleGoingHere;
    }

    void Update() {
        if (delay > 0) {
            delay -= Time.unscaledDeltaTime;
        } else if (isFalling) {
            float newZ = transform.position.z + fallSpeed * Time.unscaledDeltaTime;
            if (newZ > targetZ) {
                transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
                isFalling = false;
                Game.instance.removeInitAnimationRequest();
            } else {
                transform.position -= new Vector3(0f, 0f, fallSpeed * Time.unscaledDeltaTime);
                fallSpeed += fallSpeed * (0.1f * Time.unscaledDeltaTime);
            }
        } else {
//            Debug.Log("1: " + Game.instance);
//            Debug.Log("2: " + Game.instance.perspectiveCamera);
//            Debug.Log("3: " + Game.instance.perspectiveCamera.gameObject);
//            Debug.Log("4: " + Game.instance.perspectiveCamera.gameObject.activeSelf);
            bool isMainCameraActive = CameraHandler.IsMapReadyForInteraction;
//            bool isMainCameraActive = Game.instance.perspectiveCamera.gameObject.activeSelf;
//            bool isMainOrIntroCameraActive = Game.instance.orthographicCamera.gameObject.activeSelf || Game.instance.perspectiveCamera.gameObject.activeSelf;
            if (isMainCameraActive) {
                if (showingSmall) {
                    // Show big icons if main camera is not active or main camera is zoomed in enough
                    if (Game.instance.orthographicCamera.orthographicSize < THRESHOLD_CAMERA_ZOOM_ICON_SWAP) {
                        // Swap to big
                        showingSmall = false;
                        bigIcon.GetComponent<FadeObjectInOut>().FadeIn();
                        smallIcon.GetComponent<FadeObjectInOut>().FadeOut();
                    }
                } else {
                    // Show small icons if main camera is active and not zoomed in enough
                    if (Game.instance.orthographicCamera.orthographicSize >= THRESHOLD_CAMERA_ZOOM_ICON_SWAP) {
                        // Swap to small
                        showingSmall = true;
                        bigIcon.GetComponent<FadeObjectInOut>().FadeOut();
                        smallIcon.GetComponent<FadeObjectInOut>().FadeIn();
                    }
                }
            }
        }

    }

    public static void loadGroupMaterials() {
        Material[] poiMaterials = Resources.LoadAll<Material> ("POI/");
        foreach (Material poiMaterial in poiMaterials) {
            foreach (string groupKey in groupKeys) {
                if (poiMaterial.name.EndsWith(groupKey)) {
                    groupMaterials.Add(groupKey, poiMaterial);
                    break;
                }
            }
        }
    }

    public PROPAGATION onMessage(string message, object data) {
        if (message == "TargetPOI(" + node.Id + "):Add") {
            InformationHuman personGoingHere = (InformationHuman) data;
            peopleGoingHere.Add(personGoingHere);
            return PROPAGATION.STOP_IMMEDIATELY;
        } else if (message == "TargetPOI(" + node.Id + "):Remove") {
            InformationHuman personGoingHere = (InformationHuman) data;
            peopleGoingHere.Remove(personGoingHere);
            return PROPAGATION.STOP_IMMEDIATELY;
        } else if (message == "gameIsReady") {
            adjustPOIIconAfterAnimationsDone();
        }
        return PROPAGATION.DEFAULT;
    }

    private void adjustPOIIconAfterAnimationsDone() {
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ - (BuildingRoof.bodyPositionWhileRising.z - BuildingRoof.bodyPositionAfterRising.z));
    }

    void OnDestroy() {
        PubSub.unsubscribeAllForSubscriber(this);
    }

    public static void clear(){
        groups.Clear();
        groupMaterials.Clear();
    }
}