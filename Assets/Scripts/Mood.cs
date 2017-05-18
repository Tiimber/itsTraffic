using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mood : MonoBehaviour {

    private float mood = 1f;
    private float angrySpeed = 0.1f;
    private float happySpeed = 0.05f;
    private float moodMin = 0f;
    private float moodMax = 1.5f;
    private string currentIcon;
    private Vector3 lastPosition;

    static List<KeyValuePair<float, string>> MOODS = new List<KeyValuePair<float, string>>() {
        new KeyValuePair<float, string>(0.15f, "WeatherIconSet-14"),
        new KeyValuePair<float, string>(0.35f, "WeatherIconSet-16"),
        new KeyValuePair<float, string>(0.75f, "WeatherIconSet-12"),
        new KeyValuePair<float, string>(float.MaxValue, "WeatherIconSet-01")
    };

    public void init(float mood = 1f, float angrySpeed = 0.1f, float happySpeed = 0.05f, float moodMin = 0f, float moodMax = 1.5f) {
        this.mood = mood;
        this.angrySpeed = angrySpeed;
        this.happySpeed = happySpeed;
        this.moodMin = moodMin;
        this.moodMax = moodMax;
        currentIcon = getMood();
        lastPosition = this.gameObject.transform.position;

        StartCoroutine(updateMood());
    }

    private string getMood() {
        return MOODS.First(p => p.Key >= mood).Value;
    }

    private IEnumerator updateMood() {
        yield return new WaitForSeconds(1f);
    	while (this.gameObject != null) {
            Vector3 currentPosition = this.gameObject.transform.position;
			mood = Mathf.Clamp(mood + (currentPosition != lastPosition ? happySpeed : -angrySpeed), moodMin, moodMax);
			string currentMood = getMood();
            if (gameObject.name.Contains("Bus")) {
                Debug.Log("Mood:" + mood + ", " + currentMood);
            }
            if (currentMood != currentIcon) {
            	currentIcon = currentMood;
                GetComponent<SpecialIcon>().setFlashIcon(currentIcon);
            }
            lastPosition = currentPosition;
            yield return new WaitForSeconds(1f);
        }
    }
}
