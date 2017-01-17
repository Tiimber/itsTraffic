using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public class CountryChooser : MonoBehaviour, IPubSub {

    public Camera mapCamera;
    private const float MAX_CAMERA_SIZE = 60f;
    private const float MIN_CAMERA_SIZE = 25f;
    private static Rect mapBounds = new Rect (-180f, -90f, 180f, 90f);
    private static Rect cameraBounds = new Rect (-MAX_CAMERA_SIZE, -MAX_CAMERA_SIZE, MAX_CAMERA_SIZE, MAX_CAMERA_SIZE);

    private Country hoveredCountry;
    public Material countryMaterial;
    public GameObject countryNameContainer;
    public GameObject cityPoint;
    public bool loadPreGeneratedCountries = false;

    private Vector3 previousMousePosition;
    private float clickReleaseTimer = 0f;
    private Vector3 mouseDownPosition;
    private Vector3 prevMouseDownPosition;
    private const float CLICK_RELEASE_TIME = 0.2f;
    private const float THRESHOLD_MAX_MOVE_TO_BE_CONSIDERED_CLICK = 30f;
    private const float FOCUSED_COUNTRY_ZOOM_FACTOR = 10f;
    private bool countryFocused = false;
    private float zoomSize = 1f;
    private List<Cities.CityObj> currentCities;

    private static Texture2D focusedCountryCloseButton;


    void Awake() {
        focusedCountryCloseButton = Resources.Load<Texture2D>("Graphics/close_button");
    }

    // Use this for initialization
    void Start() {
        PubSub.subscribe("Country:select", this);

        // For camera handling
        CameraHandler.IsMapReadyForInteraction = true;
        CameraHandler.SetMainCamera(mapCamera);
        CameraHandler.SetZoomLevels(MAX_CAMERA_SIZE, MIN_CAMERA_SIZE);

        float widthHeightRatio = Misc.GetWidthRatio();

        // Width to height ratio
        if (widthHeightRatio > 1f) {
            float xSpan = MAX_CAMERA_SIZE * 2;
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

        if (!countryFocused) {
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

            // Click/drag
            if (!Input.GetMouseButton (0) && clickReleaseTimer > 0f) {
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
        } else {
            // // TODO - Temporary code
            // if (Misc.randomRange(0f, 1000f) < 10f) {
            //     int randomLevel = Mathf.CeilToInt(Misc.randomRange(0.01f, 10f));
            //     setVisibleCitiesLevel(randomLevel);
            // }
        }

        // Zoom -- allow both when focused on country and not (will just have different size constraints)
        if (Input.GetAxis ("Mouse ScrollWheel") != 0) {
            float scrollAmount = Input.GetAxis ("Mouse ScrollWheel");
            if (countryFocused) {
                Singleton<SingletonInstance>.Instance.StartCoroutine(zoomOnFocusedCountry(scrollAmount, mousePosition));
            } else {
                CameraHandler.CustomZoom (scrollAmount, mousePosition);
            }
        }
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
        }


        previousMousePosition = mousePosition;
    }

    void OnGUI() {
        if (countryFocused) {
            Misc.Size imageSize = Misc.getImageSize(focusedCountryCloseButton.width, focusedCountryCloseButton.height, 64, 64);
            float imageWidth = imageSize.width;
            float imageHeight = imageSize.height;
            if (GUI.Button(new Rect(20, 20, imageWidth, imageHeight), focusedCountryCloseButton, GUIStyle.none)) {
                StartCoroutine(unfocusCountry());
            }
        }
    }

    public IEnumerator getCountryData() {
        // Get country metadata
        WWW www = CacheWWW.Get (Game.endpointBaseUrl + Game.countryMetaDataRelativeUrl);
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
            WWW countryWWW = CacheWWW.Get (Game.endpointBaseUrl + Game.countryMetaDataRelativeUrl + Game.countryCodeDataQuerystringPrefix + code);
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
            // Debug.Log("Clicked: " + countryData["code"] + ", " + countryData["name"]);
            StartCoroutine(focusCountry(countryData["code"]));
        }
        return PROPAGATION.DEFAULT;
    }

    private IEnumerator unfocusCountry() {
        countryFocused = false;
        string code = hoveredCountry.code;
        hoveredCountry = null;

        setVisibleCitiesLevel(0);
        GameObject.Destroy(GameObject.Find("Cities-" + code));

        CameraHandler.SetZoomLevels(MAX_CAMERA_SIZE, MIN_CAMERA_SIZE);
        CameraHandler.SetCenterPoint(Vector3.zero);
        yield return CameraHandler.ResetZoom();

        // Fade in all countries
        fadeAllCountries(false, code);
    }

    private IEnumerator focusCountry(string code) {
        countryFocused = true;
        hoveredCountry.onUnfocused();
        float endTime = Time.time + Country.focusTimeMax;
        
        // Start loading cities
        WWW www = CacheWWW.Get (Game.endpointBaseUrl + Game.citiesMetaDataRelativeUrl + Game.countryCodeDataQuerystringPrefix + code);

        // Reset zoom
        yield return CameraHandler.ResetZoom ();

        // Fade out other countries
        fadeAllCountries(true, code);

        // "Smart zoom" selected country
        Rect countryRect = hoveredCountry.rect;
        float countrySize = Mathf.Max(countryRect.width / Misc.GetWidthRatio(), countryRect.height / Misc.GetHeightRatio());
        Vector3 countryCenter = hoveredCountry.countryCenter;
        zoomSize = countrySize / 2f + MAX_CAMERA_SIZE / 120f;
        CameraHandler.ZoomToSizeAndMoveToPointThenSetNewMinMaxZoomAndCenter(zoomSize, countryCenter, FOCUSED_COUNTRY_ZOOM_FACTOR);

        // Make sure cities data is loaded before parsing cities data
        yield return www;
        XmlDocument xmlDoc = new XmlDocument ();
        xmlDoc.LoadXml (www.text);

        // Parse cities and place them out
        Cities cities = new Cities(xmlDoc);
        createCities(cities, code, zoomSize);
        setVisibleCitiesLevel(1);

        // If any time is left before country have finished it's unfocus - wait a bit
        float timeLeft = endTime - Time.time;
        if (timeLeft > 0) {
            yield return new WaitForSeconds(timeLeft);
        }
    }

    private void fadeAllCountries(bool fadeOut, string exceptionCode) {
        GameObject[] allCountries = GameObject.FindGameObjectsWithTag("Country");
        foreach (GameObject countryGO in allCountries) {
            Country country = countryGO.GetComponent<Country>();
            if (country.code != exceptionCode) {
                if (fadeOut) {
                    country.fadeOut(CameraHandler.GetBackgroundColor());
                } else {
                    country.fadeIn(CameraHandler.GetBackgroundColor());
                }
            }
        }
    }

    private void createCities (Cities cities, string code, float zoomSize) {
        currentCities = new List<Cities.CityObj>();
        GameObject citiesParent = new GameObject("Cities-" + code);
        citiesParent.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        foreach (Cities.City city in cities.cities) {
            // Instantiate GameObject
            GameObject cityObj = Instantiate(cityPoint, citiesParent.transform) as GameObject;
            cityObj.name = city.name;

            // Set city meta data
            Cities.CityObj cityData = cityObj.AddComponent<Cities.CityObj>();
            cityData.city = city;
            cityData.setOriginalOrtho(zoomSize);
            currentCities.Add(cityData);

            // Position city
            Pos pos = new Pos (-1L, city.lon, city.lat);
            cityObj.transform.position = Game.getCameraPosition (pos, mapBounds, cameraBounds);
        }
    }

    private void setVisibleCitiesLevel (int level) {
        foreach (Cities.CityObj city in currentCities) {
            city.setVisibleLevel(level);
        }
    }

    private IEnumerator zoomOnFocusedCountry (float scrollAmount, Vector3 mousePosition) {
        yield return CameraHandler.CustomZoomIEnumerator (scrollAmount, mousePosition);
        // Figure out which "level" of cities to show
        float targetZoomLevel = Mathf.Clamp(CameraHandler.GetOrthograpicSize() - scrollAmount, zoomSize / FOCUSED_COUNTRY_ZOOM_FACTOR, zoomSize);
        int zoomLevel = Mathf.Max(1, Mathf.RoundToInt(10f - ((targetZoomLevel - (zoomSize / FOCUSED_COUNTRY_ZOOM_FACTOR)) / (zoomSize - (zoomSize / FOCUSED_COUNTRY_ZOOM_FACTOR)) * 10f)));
        setVisibleCitiesLevel(zoomLevel);
    }
}
