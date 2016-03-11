using UnityEngine;
using System.Collections;

public class VehicleSounds : MonoBehaviour {

	public AudioClip shortHonkSound;

	private AudioSource shortHonk;
	private int numberOfHonks = 0f;

	// Use this for initialization
	void Start () {
		shortHonk = gameObject.AddComponent<AudioSource> ();
		shortHonk.playOnAwake = false;
		shortHonk.clip = shortHonkSound;
		shortHonk.spatialBlend = 1f;
		shortHonk.Play ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void honk (bool startHonk = true) {
		if (startHonk) {
			if (!shortHonk.isPlaying) {
				// TODO - More irritated, play longer honk
				numberOfHonks += 1;
				shortHonk.Play ();
			}
		} else {
			shortHonk.Stop ();
		}
	}
}
