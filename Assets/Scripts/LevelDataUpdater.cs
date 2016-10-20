using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class LevelDataUpdater : MonoBehaviour {

    private static Vector3 MISSION_BLOCK_SIZE = new Vector3(123f, -123f, 0f);
    private List<GameObject> shownLevels = new List<GameObject>();

    private static List<float> Distances = new List<float>();

    // TODO - both should be http:// later
    private const string BUNDLED_LEVELS_URL = "file:///Users/robbin/ItsTraffic/Assets/StreamingAssets/bundled-missions.xml";

    public enum LEVEL_TYPES {
        BUNDLED,
        CUSTOM
    }

    public LEVEL_TYPES levelType;
    public GameObject missionTemplate;

    private Levels levels;
    private string filter = "";

    public void setFilter(string filter) {
        this.filter = filter;
    }


    public void updateLevelStars() {
		LevelInfo[] levelInfos = GetComponentsInChildren<LevelInfo>();
        foreach (LevelInfo levelInfo in levelInfos) {
            levelInfo.fetchData();
        }
    }

    private void loadLevelList() {
        string customLevelsUrl = Game.instance.endpointBaseUrl + Game.instance.customLevelsRelativeUrl;
        if (filter != "") {
            customLevelsUrl += "?filter=" + Uri.EscapeUriString(filter);
        }
        string levelListUrl = levelType == LEVEL_TYPES.BUNDLED ? BUNDLED_LEVELS_URL : customLevelsUrl;
        StartCoroutine (loadLevels (levelListUrl));
    }

    private IEnumerator loadLevels (string levelListUrl) {
        // TODO - Prevent DDOS attack, cache locally for a while before making request subsequential times
        // TODO - Loading spinner
        WWW www = new WWW (levelListUrl);

        yield return www;

//        Debug.Log(levelListUrl + " - " + www.text);
        XmlDocument xmlDoc = new XmlDocument ();
        xmlDoc.LoadXml (www.text);

        levels = new Levels(xmlDoc);

        // Add distances to each result in list
        Distances.Clear();
        foreach (Level level in levels.levels) {
            Distances.Add(Misc.getDistanceBetweenEarthCoordinates(Game.instance.lon, Game.instance.lat, level.lon, level.lat));
            Distances.Sort();
        }

        updateLevelGameObjects();
    }

    public static float getNextDistance(float currentDistance) {
        bool returnNext = false;
        foreach (float distance in Distances) {
            if (returnNext) {
                return distance;
            }
            if (distance > currentDistance) {
                return distance;
            }else if (distance == currentDistance) {
                returnNext = true;
            }
        }
        return Distances [Distances.Count - 1];
    }

    public static float getPreviousDistance(float currentDistance) {
        for (int i = Distances.Count - 1; i > 0; i--) {
            float distance = Distances [i];
            if (distance < currentDistance) {
                return Distances [i];
            } else if (distance == currentDistance) {
                return Distances [i - 1];
            }
        }
        return Distances [0];
    }

    public void updateLevelGameObjects() {
        // TODO - Calculate
        int MAX_ROWS = 50;
        int MAX_COLUMNS = 3;

        int row = 0;
        int column = 0;


        clearOldLevels();

        foreach (Level level in levels.levels) {

            float distance = Game.instance.searchDistance;
            if (Misc.getDistanceBetweenEarthCoordinates(Game.instance.lon, Game.instance.lat, level.lon, level.lat) > distance) {
                continue;
            }


            GameObject mission = Instantiate (missionTemplate, transform, false) as GameObject;
            if (levelType == LEVEL_TYPES.CUSTOM) {
                mission.transform.localPosition += new Vector3(0f, -missionTemplate.transform.position.y - 34f, 0f); // -34f for search input
            }
            mission.transform.localPosition += new Vector3(column * MISSION_BLOCK_SIZE.x, row * MISSION_BLOCK_SIZE.y, 0f);
            mission.name = levelType + "-" + level.id;
            LevelInfo missionLevelInfo = mission.GetComponent<LevelInfo> ();
            missionLevelInfo.setMetaData(level);
            shownLevels.Add(mission);

            column++;
            if (column >= MAX_COLUMNS) {
                column = 0;
                row++;

                if (row >= MAX_ROWS) {
                    // TODO - Logic for next page or similar
                    break;
                }
            }
        }
    }

    public void refresh() {
        loadLevelList();
    }

    private void clearOldLevels() {
        foreach (GameObject level in shownLevels) {
            Destroy(level);
        }
    }

}
