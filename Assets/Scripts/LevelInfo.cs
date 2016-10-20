using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelInfo : MonoBehaviour {

    public string id;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void setMetaData (Level level) {
        id = level.id;

        // Update mission name
        Text text = GetComponentInChildren<Text> ();
        text.text = "\n\n\n\n" + level.name; // TODO - Shouldn't need newlines

        // Update image
        StartCoroutine(loadImage(level));

        // Set click action
        GetComponent<Button>().onClick.AddListener(() => { Game.instance.startMission(level.fileUrl); });


        fetchData();
    }


    public void fetchData() {
        int stars = PlayerPrefsData.GetLevelStars(id);
        MissionStarAmount missionStarAmount = GetComponent<MissionStarAmount>();
        // TODO - Initial fetch should call "setInitialStarAmount"
        missionStarAmount.setStarAmount(stars);
    }

    private IEnumerator loadImage(Level level) {

        WWW www = new WWW (level.iconUrl);
        yield return www;
        Texture2D materialTexture = new Texture2D (256, 256);
        www.LoadImageIntoTexture (materialTexture);

        // TODO - Cache downloaded icons (at least for bundled levels)

        Sprite imageSprite = Sprite.Create(materialTexture, new Rect(0, 0, 256, 256), Vector3.zero);

        Image image = GetComponent<Image> ();
        image.sprite = imageSprite;

    }
}
