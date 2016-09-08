using UnityEngine;
using System.Collections;

public class LevelInfo : MonoBehaviour {

    public string id;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void fetchData() {
        string levelStarsKey = "level_" + id + "_stars";
        if (PlayerPrefs.HasKey(levelStarsKey)) {
			int stars = PlayerPrefs.GetInt(levelStarsKey);
            MissionStarAmount missionStarAmount = GetComponent<MissionStarAmount>();
	        // TODO - Initial fetch should call "setInitialStarAmount"
            missionStarAmount.setStarAmount(stars);
        }
    }
}
