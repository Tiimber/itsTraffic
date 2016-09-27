using UnityEngine;
using System.Collections;

public class PlaySoundEffectOnce : MonoBehaviour {

    public AudioClip audioClip;

	// Use this for initialization
	void Start () {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.loop = false;
        audioSource.minDistance = 0f;
        audioSource.maxDistance = 100f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Linear(0f, 1f, 1f, 1f));
        audioSource.volume = Game.instance.soundEffectsVolume;
        audioSource.Play();

        StartCoroutine(destroyWhenDone(audioClip.length + 0.5f));
	}

	public IEnumerator destroyWhenDone(float time) {
        yield return new WaitForSecondsRealtime(time);
        Destroy(this.gameObject);
	}

}
