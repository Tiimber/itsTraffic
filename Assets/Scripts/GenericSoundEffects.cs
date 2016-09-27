using System.Collections.Generic;
using UnityEngine;

public class GenericSoundEffects : MonoBehaviour {

    public AudioClip presentEachPointPart; // TODO - Use
    public AudioClip showBriefSound; // TODO - Use
    public AudioClip highscoreSerenade;

    private static GenericSoundEffects instance;

	// Use this for initialization
	void Start () {
		instance = this;
	}

    void playAudio(string name) {
        switch (name) {
            case "highscoreSerenade":
            	playAudioClip(highscoreSerenade, name);
            	break;
            // TODO ---
        }
    }

    void playAudioClip(AudioClip clip, string name) {
		AudioListener audioListener = Misc.getAudioListener();
        if (audioListener != null) {
            Vector3 audioListenerPosition = Misc.getWorldPos(audioListener.transform);
			GameObject soundEffect = new GameObject("Sound effect: " + name);
            PlaySoundEffectOnce playSoundEffectOnce = soundEffect.AddComponent<PlaySoundEffectOnce>();
			playSoundEffectOnce.audioClip = clip;
            soundEffect.transform.position = audioListenerPosition;
        }
    }

    public static void playHighscoreSerenade() {
        instance.playAudio("highscoreSerenade");
    }

}
