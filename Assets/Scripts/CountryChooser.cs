using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class CountryChooser : MonoBehaviour, IPubSub {

    public Camera mapCamera;
    private static float cameraSize = 60f;
    private static Rect mapBounds = new Rect (-180f, -90f, 180f, 90f);
    private static Rect cameraBounds = new Rect (-cameraSize, -cameraSize, cameraSize, cameraSize);

    private Country hoveredCountry;
    public Material countryMaterial;
    public GameObject countryNameContainer;
    public bool loadPreGeneratedCountries = false;

    private Vector3 previousMousePosition;
    private float clickReleaseTimer = 0f;
    private Vector3 mouseDownPosition;
    private Vector3 prevMouseDownPosition;
    private const float CLICK_RELEASE_TIME = 0.2f;
    private const float THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK = 30f;

    // Use this for initialization
    void Start() {
        PubSub.subscribe("Country:select", this);

        // For camera handling
        CameraHandler.SetMainCamera(mapCamera);
        CameraHandler.SetZoomLevels(60f, 25f);

        float widthHeightRatio = (float) Screen.width / (float) Screen.height;

        // Width to height ratio
        if (widthHeightRatio > 1f) {
            float xSpan = cameraSize * 2;
            float addedXSpan = xSpan * widthHeightRatio - xSpan;
            cameraBounds.x -= addedXSpan / 2f;
            cameraBounds.width += addedXSpan / 2f;
        }

        Country.Material = countryMaterial;
        if (loadPreGeneratedCountries) {
            GameObject[] countryObjects = Resources.LoadAll<GameObject> ("FullMap/");
            List<Mesh> countryMeshes = Resources.LoadAll<Mesh> ("FullMap/").ToList();
            foreach (GameObject countryObject in countryObjects) {
                GameObject country = Instantiate (countryObject, transform) as GameObject;
                Country countryObj = country.GetComponent<Country>();
                country.name = countryObj.mergedWithOther ? countryObj.ownName + " + " + countryObj.otherName : countryObject.name;
                string name = country.name;
                // Get all meshes and apply them
                string meshPrefix = (countryObj.mergedWithOther ? countryObj.ownName + "_merged" : name) + "_";
//                Debug.Log("EXAMPLE: " + countryMeshes[0].name);
                List<Mesh> meshesForCountry = countryMeshes.FindAll (mesh => mesh.name.StartsWith (meshPrefix));
//                Debug.Log(name + ": " + meshesForCountry.Count);
                foreach (Mesh meshForCountry in meshesForCountry) {
                    // Get suffix ("Outer0", "Inner1"...)
                    string meshSuffix = meshForCountry.name.Substring(meshPrefix.Length);
                    // Get MeshFilter which should get this mesh
                    if (meshSuffix.StartsWith("Outer") || meshSuffix.StartsWith("Inner")) {
//                        Debug.Log(name + "(" + meshSuffix + "): " + Misc.FindDeepChild (country.transform, meshSuffix));
                        MeshFilter countryMesh = Misc.FindDeepChild (country.transform, meshSuffix).GetComponent<MeshFilter>();
                        countryMesh.mesh = Instantiate(meshForCountry);
                    }
                }
            }
        } else {
            StartCoroutine (getCountryData ());
        }
    }

    // Update is called once per frame
    void Update() {
        // Get mouse position
        Vector3 mousePosition = Input.mousePosition;
        // Only check hover logic if input position changed at all
        if (previousMousePosition != mousePosition) {
            // Check if it hovers on a country
            Ray ray = mapCamera.ScreenPointToRay (Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast (ray, out hit)) {
                Transform countryHovered = hit.transform;
                if (countryHovered.name.StartsWith("Inner")) {
                    countryHovered = null;
                } else {
                    while (countryHovered != null && countryHovered.tag != "Country") {
                        countryHovered = countryHovered.parent;
                    }
                }
                if (countryHovered != null) {
                    Country hovered = countryHovered.GetComponent<Country> ();
                    if (hovered != hoveredCountry) {
                        if (hoveredCountry != null) {
                            hoveredCountry.onUnfocused();
                        }
                        hoveredCountry = hovered;
                        hoveredCountry.onFocused();
                    }
                }
            } else {
                if (hoveredCountry != null) {
                    hoveredCountry.onUnfocused();
                }
                hoveredCountry = null;
            }

            previousMousePosition = mousePosition;
        }

        // Zoom
        if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
            float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
            CameraHandler.CustomZoom (scrollAmount, Input.mousePosition);
        }
        // Click/drag
        if (Input.GetMouseButton (0)) {
            // Drag logic
            bool firstFrame = Input.GetMouseButtonDown (0);

            if (!firstFrame) {
                Vector3 diffMove = mousePosition - prevMouseDownPosition;
                CameraHandler.Move (diffMove);
            } else {
                mouseDownPosition = mousePosition;
            }
            prevMouseDownPosition = mousePosition;

            // Click logic
            if (firstFrame) {
                clickReleaseTimer = CLICK_RELEASE_TIME;
            } else {
                clickReleaseTimer -= Time.deltaTime;
            }
        } else if (clickReleaseTimer > 0f) {
            // Button not pressed, and was pressed < 0.2s, accept as click if not moved too much
            if (Misc.getDistance (mouseDownPosition, prevMouseDownPosition) < THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK) {
                Vector3 mouseWorldPoint = mapCamera.ScreenToWorldPoint (mouseDownPosition);
                clickReleaseTimer = 0f;

                // Handle click
                if (hoveredCountry != null) {
                    if (hoveredCountry.mergedWithOther) {
                        Debug.Log(hoveredCountry.ownName + " or " + hoveredCountry.otherName);
                        // TODO - Select which one of these countries? (Only two cases...)
//                        PubSub.publish("Country:select", countryData);
                    } else {
                        Dictionary<string, string> countryData = new Dictionary<string, string>(){
                            {"code", hoveredCountry.code},
                            {"name", hoveredCountry.name}
                        };
                        PubSub.publish("Country:select", countryData);
                    }
                }
            }
        }
    }

    public IEnumerator getCountryData() {
        // Get country metadata
        WWW www = CacheWWW.Get (Game.endpointBaseUrl + Game.countryMetaDataRelativeUrl, Misc.getTsForReadable ("30d"));
        yield return www;

        XmlDocument xmlDoc = new XmlDocument ();
        xmlDoc.LoadXml (www.text);

        XmlNodeList countries = xmlDoc.SelectNodes ("/countries/country");
        int numLeft = countries.Count;
        foreach (XmlNode country in countries) {
            // TODO - Remove debug log - and to break after a few
            Debug.Log (numLeft--);
//            if (numLeft < 227) {
//                break;
//            }

            XmlAttributeCollection countryAttributes = country.Attributes;

            string code = Misc.xmlString (countryAttributes.GetNamedItem ("code"));
            string name = Misc.xmlString (countryAttributes.GetNamedItem ("name"));
            // TODO
//            string code = "ZA";
//            string name = "South Africa";

            // Country parent
            GameObject countryParent = new GameObject (name);
            Country countryObj = countryParent.AddComponent<Country> ();
            countryObj.countryName = name;
            countryObj.code = code;
            countryObj.tag = "Country";
            countryParent.transform.parent = transform;

            // Landarea parent
            GameObject landareaParent = new GameObject ("Land");
            landareaParent.transform.parent = countryParent.transform;

            // Get country full outline data
            WWW countryWWW = CacheWWW.Get (Game.endpointBaseUrl + Game.countryMetaDataRelativeUrl + Game.countryCodeDataQuerystringPrefix + code, Misc.getTsForReadable ("30d"));
            yield return countryWWW;

            XmlDocument countryDataDoc = new XmlDocument ();
            countryDataDoc.LoadXml (countryWWW.text);

            XmlNodeList polygons = countryDataDoc.SelectNodes ("/country/polygons/polygon");
            int outerIndex = 0;
            int innerIndex = 0;
            foreach (XmlNode polygon in polygons) {
                // Outer coordinates
                List<Vector3> outerCoordinates = new List<Vector3>();
                yield return getCoordinates(polygon, "outer", outerCoordinates);
                if (outerCoordinates.Count > 0) {
                    GameObject outerPart = MapSurface.createPlaneMeshForPoints (outerCoordinates);
                    outerPart.name = "Outer" + outerIndex++;
                    outerPart.transform.parent = landareaParent.transform;
                    countryObj.addCoords(outerCoordinates);
                }

                // Inner coordinates
                List<Vector3> innerCoordinates = new List<Vector3>();
                yield return getCoordinates(polygon, "inner", innerCoordinates);
                if (innerCoordinates.Count > 0) {
                    GameObject innerPart = MapSurface.createPlaneMeshForPoints (innerCoordinates);
                    innerPart.name = "Inner" + innerIndex++;
                    innerPart.transform.parent = landareaParent.transform;
                    innerPart.transform.localPosition = new Vector3(innerPart.transform.localPosition.x, innerPart.transform.localPosition.y, innerPart.transform.localPosition.z - 0.1f);
                    countryObj.addCoords(innerCoordinates, false);
                }
            }

            // Country name
            GameObject countryNameContainerInstance = Instantiate (countryNameContainer, countryObj.transform) as GameObject;
            TextMesh countryNameTextMesh = countryNameContainerInstance.GetComponentInChildren<TextMesh> ();
            countryNameTextMesh.text = name;
            countryObj.setupDone ();
            countryObj.saveMeshes();

//            break; // TODO
        }

        analyzeAndMergeInners();


        Debug.Log ("Done!");
    }

    private void analyzeAndMergeInners() {
        // Cycle through all Country-objects with "Inner"
        List<Country> countriesWithInner = GetComponentsInChildren<Country> ().ToList<Country> ().FindAll (country => country.hasInner ());
        List<Country> countriesWithoutInner = GetComponentsInChildren<Country> ().ToList<Country> ().FindAll (country => !countriesWithInner.Contains(country));
        foreach(Country countryWithInner in countriesWithInner) {
            // Find the real country for a country that specified containing "Inner"
            List<Vector3> inner = countryWithInner.getInner();
            Country outerForInnerMatch = countriesWithoutInner.Find (countryWithoutInner => countryWithoutInner.hasInnerAsOuter (inner));
            if (outerForInnerMatch != null) {
//                Debug.Log("Outer: " + countryWithInner.name + " includes: " + outerForInnerMatch.name);
                // Hide inner
//                outerForInnerMatch.mergedWithOther = true;
                outerForInnerMatch.gameObject.SetActive(false);
//                outerForInnerMatch.saveMeshes(false);

                countryWithInner.updateInnerWithOuter(outerForInnerMatch.name, outerForInnerMatch.getOuterMeshRenderer ());
            }
        }
    }

    public IEnumerator getCoordinates(XmlNode polygon, string coordsGroup, List<Vector3> coordinates) {
        XmlNodeList outerCoords = polygon.SelectNodes (coordsGroup + "/coords");
        yield return parseLonLatToWorldPositions (outerCoords, coordinates);
        if (coordinates.Count > 1 && coordinates [0] == coordinates [coordinates.Count - 1]) {
            coordinates.RemoveAt (coordinates.Count - 1);
        }
    }

    public static IEnumerator parseLonLatToWorldPositions(XmlNodeList coordsInstances, List<Vector3> coordinates) {
        foreach (XmlNode coordsInstance in coordsInstances) {
            XmlNodeList coords = coordsInstance.SelectNodes ("coord");
            foreach (XmlNode coord in coords) {
                string coordStr = coord.InnerText;
                yield return null;
                string[] coordSplit = coordStr.Split (',');
                float lon = Convert.ToSingle (coordSplit [0]);
                float lat = Convert.ToSingle (coordSplit [1]);
                Pos pos = new Pos (-1L, lon, lat);
                coordinates.Add (Game.getCameraPosition (pos, mapBounds, cameraBounds));
            }
        }
    }

    public PROPAGATION onMessage(string message, object data) {
        if (message == "Country:select") {
            Dictionary<string, string> countryData = (Dictionary<string, string>)data;
            Debug.Log("Clicked: " + countryData["code"] + ", " + countryData["name"]);
        }
        return PROPAGATION.DEFAULT;
    }

}
