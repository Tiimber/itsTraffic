using System.Collections;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class LevelDataUpdater : MonoBehaviour {

    private static Vector3 MISSION_BLOCK_SIZE = new Vector3(123f, -123f, 0f);
    private static float MISSION_BLOCK_MARGIN_Y = 23f;
    private static float BUTTONS_HEIGHT = 46f;
    private static float BUTTONS_MARGIN = 10f;
    private static string SORT_NEAREST_YOU = "Nearest you";
    private static string DEFAULT_SORT = SORT_NEAREST_YOU;


    // TODO - both should be http:// later
    private const string BUNDLED_LEVELS_URL = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/bundled-missions.xml";

    public enum LEVEL_TYPES {
        BUNDLED,
        CUSTOM
    }

    public LEVEL_TYPES levelType;
    public GameObject missionTemplate;
    public GameObject previousButton;
    public GameObject nextButton;

    private Levels levels;
    private string filter = "";
    private int page = 0;
    private string sortValue = DEFAULT_SORT;

    public void setFilter(string filter) {
        this.filter = filter;
    }


    public void updateLevelStars(bool newRecord = false) {
		LevelInfo[] levelInfos = GetComponentsInChildren<LevelInfo>();
        foreach (LevelInfo levelInfo in levelInfos) {
            levelInfo.fetchData(newRecord);
        }
    }

    private void loadLevelList(int page = 0) {
        string levelListUrl = BUNDLED_LEVELS_URL;
        if (levelType == LEVEL_TYPES.CUSTOM) {
            Misc.UrlBuilder url = new Misc.UrlBuilder(Game.endpointBaseUrl + Game.customLevelsRelativeUrl);
            if (filter != "") {
                url.addQuery("filter", filter);
            }
            if (page != 0) {
                url.addQuery("page", page);
            }
            url.addQuery("sort", sortValue);
            if (sortValue == SORT_NEAREST_YOU) {
                url.addQuery("lon", Game.instance.lon);
                url.addQuery("lat", Game.instance.lat);
            }

            levelListUrl = url.build();
        }

//        Debug.Log(levelListUrl);
        StartCoroutine (loadLevels (levelListUrl));
    }

    private IEnumerator loadLevels (string levelListUrl) {

        string loadingSpinnerId = levelType == LEVEL_TYPES.CUSTOM ? "custom-levels" : "bundled-levels";
        LoadingSpinner.StartSpinner(loadingSpinnerId);

        WWW www = CacheWWW.Get(levelListUrl);

        yield return www;

        XmlDocument xmlDoc = new XmlDocument ();
        xmlDoc.LoadXml (www.text);

        levels = new Levels(xmlDoc);

        LoadingSpinner.StopSpinner(loadingSpinnerId);

        updateLevelGameObjects();
    }

    public void sort() {
        string chosenSortValue = DEFAULT_SORT;
        Transform sortDropdown = Misc.FindDeepChild (transform, "Sort Dropdown");
        if (sortDropdown != null) {
            Dropdown sort = sortDropdown.GetComponent<Dropdown> ();
            chosenSortValue = sort.options [sort.value].text;
        }

        sortValue = chosenSortValue;
        loadLevelList();
    }

    public void updateLevelGameObjects() {
        // TODO - Calculate
        int MAX_ROWS = 50;
        int MAX_COLUMNS = 3;

        int row = 0;
        int column = 0;


        clearOldLevels();

        // Get parent for levels (scrollable list)
        Transform levelListViewport = Misc.FindDeepChild(transform, "Levels list viewport content");
        foreach (Level level in levels.levels) {

            GameObject mission = Instantiate (missionTemplate, levelListViewport, false) as GameObject;
            mission.transform.localPosition += new Vector3(column * MISSION_BLOCK_SIZE.x, row * MISSION_BLOCK_SIZE.y, 0f);
            mission.name = levelType + "-" + level.id;
            LevelInfo missionLevelInfo = mission.GetComponent<LevelInfo> ();
            missionLevelInfo.setMetaData(level);

            column++;
            if (column >= MAX_COLUMNS) {
                column = 0;
                row++;

                if (row >= MAX_ROWS) {
                    break;
                }
            }
        }

        int numberOfUsedRows = ((row + 1) + (column == 0 ? -1 : 0));

        bool hasButtons = false;
        if (levels.hasPrevious || levels.hasNext) {
            // Add previous/next buttons
            hasButtons = true;
            if (levels.hasPrevious) {
                GameObject btnPrevious = Instantiate(previousButton, levelListViewport, false) as GameObject;
                // This calculation is very strange
                btnPrevious.transform.localPosition += new Vector3(0f, numberOfUsedRows * MISSION_BLOCK_SIZE.y - MISSION_BLOCK_MARGIN_Y + BUTTONS_HEIGHT - BUTTONS_MARGIN, 0f);
                Button prevBtn = btnPrevious.GetComponent<Button> ();
                btnPrevious.GetComponent<Button> ().onClick.AddListener (() => {
                    changePage (-1);
                });
            }
            if (levels.hasNext) {
                GameObject btnNext = Instantiate(nextButton, levelListViewport, false) as GameObject;
                // This calculation is very strange
                btnNext.transform.localPosition += new Vector3(0f, numberOfUsedRows * MISSION_BLOCK_SIZE.y - MISSION_BLOCK_MARGIN_Y + BUTTONS_HEIGHT - BUTTONS_MARGIN, 0f);
                btnNext.GetComponent<Button> ().onClick.AddListener (() => {
                    changePage (1);
                });
            }
        }

        RectTransform contentRectTransform = levelListViewport.GetComponent<RectTransform> ();
        contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, numberOfUsedRows * Mathf.Abs(MISSION_BLOCK_SIZE.y) - MISSION_BLOCK_MARGIN_Y + (hasButtons ? BUTTONS_HEIGHT : 0));
    }

    public void changePage(int diff) {
        page += diff;
        if (page < 0) {
            page = 0;
        }
        loadLevelList(page);
    }

    public void refresh() {
        loadLevelList(page);
    }

    private void clearOldLevels() {
        // Remove old levels (and prev/next-button)
        Transform levelListViewport = Misc.FindDeepChild(transform, "Levels list viewport content");
        Misc.DestroyChildren(levelListViewport);
    }

    public float getDistanceTo(Level level) {
        return Misc.getDistanceBetweenEarthCoordinates(Game.instance.lon, Game.instance.lat, level.lon, level.lat);
    }

}
