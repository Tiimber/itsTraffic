using System.Collections;
using System.Collections.Generic;
using com.spacepuppy;
using UnityEditor;
using UnityEngine;

public class Country : MonoBehaviour {

    // Shared material
    public static Material Material;

    private static Vector3 defaultFocusScale = new Vector3 (1.5f, 1.5f, 1f);
    private static float targetScaleArea = 20;
    private static float defaultScalePerAxis = 1.5f;
    private Vector3 customFocusScale = new Vector3 (1.5f, 1.5f, 1f);
    private static Vector3 normalScale = new Vector3 (1f, 1f, 1f);
    private static Color innerColor = new Color (0.13f, 0.47f, 0.76f);
    private static int startFontSize = 16;
    private static int zoomedFontSize = 30;

    public bool focused = false;
    public float focusTime = focusTimeMin;
    public bool doUpdate;
    public const float focusTimeMax = 0.5f;
    private const float focusTimeMin = 0f;

    public string countryName;
    public string code;
    public Material material;
    public Material innerMaterial;
    public Color countryColor;
    private Color privateInnerColor = Color.clear;
    public Vector3 center;
    public Vector3 countryCenter;
    public Rect rect;
    private GameObject landareasParent;
    private GameObject countryNameParent;
    private TextMesh countryNameTextMesh;

    // Used for temporary storage to "merge" inner countries
    private List<List<Vector3>> innerCoords = new List<List<Vector3>> ();
    private List<List<Vector3>> outerCoords = new List<List<Vector3>> ();
    public bool mergedWithOther;
    public string ownName;
    public string otherName;

    // Use this for initialization
    void Start() {
        if (!mergedWithOther) {
            name = countryName;
        }
        if (Material != null) {
            material = Instantiate (Material);
            innerMaterial = Instantiate (Material);
        }

        System.Random randomForCountry = new System.Random (countryName.GetHashCode ());
        countryColor = new Color ((float) randomForCountry.NextDouble (), (float) randomForCountry.NextDouble (), (float) randomForCountry.NextDouble ());

        if (GetComponentsInChildren<MeshArea> ().Length > 0) {
            if (mergedWithOther) {
                // Hide the "originals", and "steal" the color from the inner country
                GameObject originalCountry = GameObject.Find (ownName);
                GameObject otherCountry = GameObject.Find (otherName);
                originalCountry.SetActive(false);
                otherCountry.SetActive(false);
                privateInnerColor = otherCountry.GetComponent<Country>().countryColor;
            }
            setupDone ();
        }
    }

    public void setupDone() {
        setCountryGameObjects ();

        ColorHSV countryColorHSV = new ColorHSV (countryColor);
        countryColorHSV.v = 1.0f;
        material.color = ColorHSV.ToColor (countryColorHSV);
        innerMaterial.color = (privateInnerColor != Color.clear ? privateInnerColor : innerColor);

        MeshArea[] meshAreas = GetComponentsInChildren<MeshArea> ();
        float biggestValue = -1f;
        foreach (MeshArea meshArea in meshAreas) {
            MeshRenderer meshRenderer = meshArea.gameObject.GetComponent<MeshRenderer> ();
            // Only change material to country color if "outer", otherwise it's considered a sea
            if (meshRenderer.name.Contains ("Outer")) {
                meshRenderer.material = material;
            } else {
                meshRenderer.material = innerMaterial;
            }

            // Determine center of mesh
            if (meshArea.area > biggestValue) {
                biggestValue = meshArea.area;
                MeshFilter meshFilter = meshArea.gameObject.GetComponent<MeshFilter> ();
                center = new Vector3 (meshFilter.mesh.bounds.center.x, meshFilter.mesh.bounds.center.y, 0f);

                countryCenter = meshArea.gameObject.GetComponent<MapSurface>().calculatedCenter;
                // TODO - Extend rect if many areas are "big"
                rect = meshArea.gameObject.GetComponent<MapSurface>().rect; 
            }
        }

        // How much do we want to scale?
        if (biggestValue * Mathf.Pow (defaultScalePerAxis, 2) < targetScaleArea) {
            float scaleFactor = Mathf.Sqrt (targetScaleArea / biggestValue);
            customFocusScale = new Vector3 (scaleFactor, scaleFactor, 1f);
        }

        // Fix all positioning, so we can scale correctly
        foreach (MeshArea meshArea in meshAreas) {
            Mesh mesh = meshArea.gameObject.GetComponent<MeshFilter> ().mesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) {
                vertices [i] -= center;
            }
            mesh.vertices = vertices;

            LanduseSurface landuseSurface = meshArea.gameObject.AddComponent<LanduseSurface> ();
            landuseSurface.createMeshCollider (false);

            mesh.RecalculateBounds ();
            mesh.RecalculateNormals ();
        }
        transform.position += center;
    }

    void Update() {
        doUpdate = false;
        if (focused && focusTime < focusTimeMax) {
            focusTime = Mathf.Min (focusTimeMax, focusTime + Time.unscaledDeltaTime);
            doUpdate = true;
        } else if (!focused && focusTime > focusTimeMin) {
            focusTime = Mathf.Max (focusTimeMin, focusTime - Time.unscaledDeltaTime);
            doUpdate = true;
        }

        if (doUpdate) {
            // Scale
            Vector3 scale = Vector3.Slerp (normalScale, customFocusScale, focusTime / focusTimeMax);
            scale.z = 1f;
            landareasParent.transform.localScale = scale;

            // Text color and size
            Color textColor = countryNameTextMesh.color;
            textColor.a = Mathf.Clamp (focusTime / focusTimeMax, 0f, 1f);
            countryNameTextMesh.color = textColor;
            countryNameTextMesh.fontSize = Mathf.RoundToInt (startFontSize + ((zoomedFontSize - startFontSize) * focusTime / focusTimeMax));

            if (focusTime == 0f) {
                transform.position = new Vector3 (transform.position.x, transform.position.y, 0f);
                countryNameParent.SetActive (false);
            }

            MeshArea[] meshAreas = GetComponentsInChildren<MeshArea> ();
            foreach (MeshArea meshArea in meshAreas) {
                meshArea.gameObject.GetComponent<MeshFilter> ().mesh.RecalculateBounds ();
            }
        }
    }

    public void onFocused() {
        focused = true;
        transform.position = new Vector3 (transform.position.x, transform.position.y, -1f);
        countryNameParent.SetActive (true);
    }

    public void onUnfocused() {
        focused = false;
    }

    public void saveMeshes(bool createNew = true) {
        string saveName = mergedWithOther ? ownName + "_merged" : this.name;
        Object prefab = PrefabUtility.CreateEmptyPrefab ("Assets/MapObjects/Resources/FullMap/" + saveName + ".prefab");
        PrefabUtility.ReplacePrefab (gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);

        if (createNew) {
            MeshArea[] meshAreas = GetComponentsInChildren<MeshArea> ();
            int i = 0;
            foreach (MeshArea meshArea in meshAreas) {
                Mesh mesh = meshArea.gameObject.GetComponent<MeshFilter> ().mesh;
                if (mergedWithOther) {
                    mesh = UnityEngine.Object.Instantiate(mesh);
                }
                AssetDatabase.CreateAsset (mesh, "Assets/MapObjects/Resources/FullMap/" + saveName + "_" + meshArea.name + ".asset");
            }

            AssetDatabase.SaveAssets ();
        }
    }

    private void setCountryGameObjects() {
        Transform countryNameTransform = Misc.FindDeepChild (transform, "CountryName");
        countryNameTextMesh = countryNameTransform.gameObject.GetComponentInChildren<TextMesh> ();
        countryNameParent = countryNameTextMesh.transform.parent.gameObject;

        landareasParent = Misc.FindDeepChild (transform, "Land").gameObject;
    }

    public void addCoords(List<Vector3> coords, bool outer = true) {
        if (outer) {
            outerCoords.Add (coords);
        } else {
            innerCoords.Add (coords);
        }
    }

    public List<Vector3> getInner() {
        return innerCoords[0];
    }

    public bool hasInner() {
        return innerCoords.Count > 0;
    }

    public bool hasInnerAsOuter(List<Vector3> otherCountryInnerCoords) {
        bool foundMatch = false;
        if (outerCoords.Count == 1) {
            foundMatch = Misc.CompareVectorLists(otherCountryInnerCoords, outerCoords[0]);

            if (!foundMatch) {
                otherCountryInnerCoords.Reverse(1, otherCountryInnerCoords.Count - 1);
                foundMatch = Misc.CompareVectorLists(otherCountryInnerCoords, outerCoords[0]);
                otherCountryInnerCoords.Reverse(1, otherCountryInnerCoords.Count - 1);
            }
        }

        return foundMatch;
    }

    public MeshRenderer getOuterMeshRenderer() {
        MeshArea meshArea = GetComponentInChildren<MeshArea> ();
        return meshArea.gameObject.GetComponent<MeshRenderer> ();
    }

    public MeshRenderer getInnerMeshRenderer() {
        return Misc.FindDeepChild(transform, "Inner0").gameObject.GetComponent<MeshRenderer>();
    }

    public void updateInnerWithOuter(string innerName, MeshRenderer innerMesh) {
        this.mergedWithOther = true;
        this.ownName = this.name;
        this.otherName = innerName;
        this.name += " + " + innerName;
        countryNameTextMesh.text += "\n(incl. " + innerName + ")";
        getInnerMeshRenderer ().material = innerMesh.material;
        saveMeshes();
    }

    public void fadeOut(Color targetColor) {
        StartCoroutine(fadeToColor(targetColor));
    }

    public void fadeIn(Color fromColor) {
        StartCoroutine(fadeToOriginalColor(fromColor));
    }

    private IEnumerator fadeToColor(Color targetColor) {
        float time = 0.25f;

        ColorHSV countryColorHSV = new ColorHSV (countryColor);
        countryColorHSV.v = 1.0f;
        Color originalOuterColor = ColorHSV.ToColor (countryColorHSV);
        Color originalInnerColor = (privateInnerColor != Color.clear ? privateInnerColor : innerColor);

        List<MeshRenderer> outers = new List<MeshRenderer>();
        List<MeshRenderer> inners = new List<MeshRenderer>();
        foreach (MeshArea meshArea in GetComponentsInChildren<MeshArea> ()) {
            MeshRenderer meshRenderer = meshArea.gameObject.GetComponent<MeshRenderer> ();
            if (meshRenderer.name.Contains ("Outer")) {
                outers.Add(meshRenderer);
            } else {
                inners.Add(meshRenderer);
            }
        }

        float t = 0;
        while (t < time) {
            yield return null;
            t += Time.unscaledDeltaTime;
            Color curOuterCol = Color.Lerp(originalOuterColor, targetColor, t / time);
            Color curInnerCol = Color.Lerp(originalInnerColor, targetColor, t / time);
            foreach (MeshRenderer meshRenderer in outers) {
                material.color = curOuterCol;
                meshRenderer.material = material;
            }
            foreach (MeshRenderer meshRenderer in inners) {
                innerMaterial.color = curInnerCol;
                meshRenderer.material = material;
            }
        }

        yield return null;
    }

    private IEnumerator fadeToOriginalColor(Color fromColor) {
        float time = 0.25f;

        ColorHSV countryColorHSV = new ColorHSV (countryColor);
        countryColorHSV.v = 1.0f;
        Color originalOuterColor = ColorHSV.ToColor (countryColorHSV);
        Color originalInnerColor = (privateInnerColor != Color.clear ? privateInnerColor : innerColor);

        List<MeshRenderer> outers = new List<MeshRenderer>();
        List<MeshRenderer> inners = new List<MeshRenderer>();
        foreach (MeshArea meshArea in GetComponentsInChildren<MeshArea> ()) {
            MeshRenderer meshRenderer = meshArea.gameObject.GetComponent<MeshRenderer> ();
            if (meshRenderer.name.Contains ("Outer")) {
                outers.Add(meshRenderer);
            } else {
                inners.Add(meshRenderer);
            }
        }

        float t = 0;
        while (t < time) {
            yield return null;
            t += Time.unscaledDeltaTime;
            Color curOuterCol = Color.Lerp(fromColor, originalOuterColor, t / time);
            Color curInnerCol = Color.Lerp(fromColor, originalInnerColor, t / time);
            foreach (MeshRenderer meshRenderer in outers) {
                material.color = curOuterCol;
                meshRenderer.material = material;
            }
            foreach (MeshRenderer meshRenderer in inners) {
                innerMaterial.color = curInnerCol;
                meshRenderer.material = material;
            }
        }

        yield return null;
    }
}
