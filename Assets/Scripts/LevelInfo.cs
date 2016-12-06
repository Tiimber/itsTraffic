﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelInfo : MonoBehaviour {

    public string id;

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


    public void fetchData(bool newRecord = false) {
        int stars = PlayerPrefsData.GetLevelStars(id);
        MissionStarAmount missionStarAmount = GetComponent<MissionStarAmount>();
        missionStarAmount.setStarAmount(stars, newRecord);
    }

    private IEnumerator loadImage(Level level) {

        WWW www = CacheWWW.Get(level.iconUrl);
        yield return www;
        Texture2D materialTexture = new Texture2D (256, 256);
        www.LoadImageIntoTexture (materialTexture);

        Sprite imageSprite = Sprite.Create(materialTexture, new Rect(0, 0, 256, 256), Vector3.zero);

        Image image = GetComponent<Image> ();
        image.sprite = imageSprite;

        // Flag icon
        string countryCode = level.countryCode;
        GameObject flag = Misc.FindDeepChild (transform, "flag").gameObject;
        RawImage flagImage = flag.GetComponent<RawImage>();
        flagImage.texture = Misc.getCountryFlag(countryCode);
        flagImage.color = Color.white;
    }
}
