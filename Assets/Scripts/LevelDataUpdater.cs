using UnityEngine;

public class LevelDataUpdater : MonoBehaviour {

    public void updateLevelStars() {
		LevelInfo[] levelInfos = GetComponentsInChildren<LevelInfo>();
        foreach (LevelInfo levelInfo in levelInfos) {
            levelInfo.fetchData();
        }
    }
}
