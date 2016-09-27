using UnityEngine;

public class PlayerPrefsData {
    private static string levelKeyPrefix = "level_";

    private static string levelPointsSuffix = "_points";
    private static string levelStarsSuffix = "_stars";

    public static int GetLevelPoints(string id) {
        return PlayerPrefs.GetInt(levelKeyPrefix + id + levelPointsSuffix, 0);
    }

    public static void SetLevelPoints(string id, int points) {
        PlayerPrefs.SetInt(levelKeyPrefix + id + levelPointsSuffix, points);
    }

    public static int GetLevelStars(string id) {
        return PlayerPrefs.GetInt(levelKeyPrefix + id + levelStarsSuffix, 0);
    }

    public static void SetLevelStars(string id, int stars) {
        PlayerPrefs.SetInt(levelKeyPrefix + id + levelStarsSuffix, stars);
    }

    public static void Save() {
        PlayerPrefs.Save();
    }

    public static void DeleteAll() {
        PlayerPrefs.DeleteAll();
    }
}
