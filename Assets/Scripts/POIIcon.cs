using System.Collections.Generic;
using UnityEngine;

public class POIIcon : MonoBehaviour {

    private static Dictionary<string, GameObject> groups = new Dictionary<string, GameObject> ();

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
                poi.name = node.hasTag ("name") ? node.getTagValue ("name") : "" + node.Id;
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

    private Pos node;
    private string group;
    private bool isFalling;
    private float targetY = 0f;
    private float fallSpeed = 0f;
    private bool showingSmall = true;
    private GameObject bigIcon;
    private GameObject smallIcon;

    void Start() {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, 30f)) {
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

            BuildingRoof buildingRoof = hit.transform.gameObject.GetComponent<BuildingRoof> ();
            if (buildingRoof != null) {
                // Land on the building
                targetY = -buildingRoof.getTargetHeight() - DISTANCE_FROM_ROOFTOPS;
            } else {
                // Land on the ground
                targetY = -0.1f - DISTANCE_FROM_ROOFTOPS;
            }

            // Set random velocity
            fallSpeed = Misc.randomPlusMinus ((transform.position.z - targetY) / 3f, 1f);

            // TODO - Instantiate with information from this.node and put appropriate graphics (depending on zoom)
        } else {
            GameObject.Destroy(this);
        }
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

    void Update() {
        if (isFalling) {
            float newZ = transform.position.z + fallSpeed * Time.unscaledDeltaTime;
            if (newZ > targetY) {
                transform.position = new Vector3(transform.position.x, transform.position.y, targetY);
                isFalling = false;
                Game.instance.removeInitAnimationRequest();
            } else {
                transform.position -= new Vector3(0f, 0f, fallSpeed * Time.unscaledDeltaTime);
                fallSpeed += fallSpeed * (0.1f * Time.unscaledDeltaTime);
            }
        } else {
            if (showingSmall) {
                if (Game.instance.mainCamera.orthographicSize < THRESHOLD_CAMERA_ZOOM_ICON_SWAP) {
                    // Swap to big
                    showingSmall = false;
                    bigIcon.GetComponent<FadeObjectInOut>().FadeIn();
                    smallIcon.GetComponent<FadeObjectInOut>().FadeOut();
                }
            } else {
                if (Game.instance.mainCamera.orthographicSize >= THRESHOLD_CAMERA_ZOOM_ICON_SWAP) {
                    // Swap to small
                    showingSmall = true;
                    bigIcon.GetComponent<FadeObjectInOut>().FadeOut();
                    smallIcon.GetComponent<FadeObjectInOut>().FadeIn();
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
}