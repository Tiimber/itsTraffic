using UnityEngine;

public class LevelInfo : MonoBehaviour {

    public string id;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void fetchData() {
        int stars = PlayerPrefsData.GetLevelStars(id);
        MissionStarAmount missionStarAmount = GetComponent<MissionStarAmount>();
        // TODO - Initial fetch should call "setInitialStarAmount"
        missionStarAmount.setStarAmount(stars);
    }
}
