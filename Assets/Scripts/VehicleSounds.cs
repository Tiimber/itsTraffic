using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleSounds : MonoBehaviour {

	public AudioClip shortHonkSound;
	public AudioClip twoShortHonkSound;
	public AudioClip longHonkSound;
	public AudioClip extraLongHonkSound;

	public enum HonkVariant {
		SHORT,
		TWO_SHORT,
		LONG,
		EXTRA_LONG
	}

	private Dictionary<HonkVariant, AudioClip> honks = new Dictionary<HonkVariant, AudioClip> ();

	private AudioSource honkSoundSource;
	private float frustrationLevel = 0f;

	// Use this for initialization
	void Start () {
		// Init the honks dictionary
		initHonkMapping ();

		// Init the audioSource
		honkSoundSource = gameObject.AddComponent<AudioSource> ();
		honkSoundSource.playOnAwake = false;
		honkSoundSource.clip = honks [HonkVariant.SHORT];
		honkSoundSource.spatialBlend = 1f;
//		honkSoundSource.Play ();
	}

	private void initHonkMapping () {
		if (shortHonkSound != null) {
			honks.Add (HonkVariant.SHORT, shortHonkSound);
		}
		if (twoShortHonkSound != null) {
			honks.Add (HonkVariant.TWO_SHORT, twoShortHonkSound);
		}
		if (longHonkSound != null) {
			honks.Add (HonkVariant.LONG, longHonkSound);
		}
		if (extraLongHonkSound != null) {
			honks.Add (HonkVariant.EXTRA_LONG, extraLongHonkSound);
		}

		if (!honks.ContainsKey(HonkVariant.SHORT)) {
			if (honks.ContainsKey (HonkVariant.TWO_SHORT)) {
				honks.Add (HonkVariant.SHORT, honks [HonkVariant.TWO_SHORT]);
			} else if (honks.ContainsKey (HonkVariant.LONG)) {
				honks.Add (HonkVariant.SHORT, honks [HonkVariant.LONG]);
			} else if (honks.ContainsKey (HonkVariant.EXTRA_LONG)) {
				honks.Add (HonkVariant.SHORT, honks [HonkVariant.EXTRA_LONG]);
			}
		}
	
		if (!honks.ContainsKey(HonkVariant.TWO_SHORT)) {
			if (honks.ContainsKey (HonkVariant.SHORT)) {
				honks.Add (HonkVariant.TWO_SHORT, honks [HonkVariant.SHORT]);
			} else if (honks.ContainsKey (HonkVariant.LONG)) {
				honks.Add (HonkVariant.TWO_SHORT, honks [HonkVariant.LONG]);
			} else if (honks.ContainsKey (HonkVariant.EXTRA_LONG)) {
				honks.Add (HonkVariant.TWO_SHORT, honks [HonkVariant.EXTRA_LONG]);
			}
		}

		if (!honks.ContainsKey(HonkVariant.LONG)) {
			if (honks.ContainsKey (HonkVariant.EXTRA_LONG)) {
				honks.Add (HonkVariant.LONG, honks [HonkVariant.EXTRA_LONG]);
			} else if (honks.ContainsKey (HonkVariant.TWO_SHORT)) {
				honks.Add (HonkVariant.LONG, honks [HonkVariant.TWO_SHORT]);
			} else if (honks.ContainsKey (HonkVariant.SHORT)) {
				honks.Add (HonkVariant.LONG, honks [HonkVariant.SHORT]);
			}
		}

		if (!honks.ContainsKey(HonkVariant.EXTRA_LONG)) {
			if (honks.ContainsKey (HonkVariant.LONG)) {
				honks.Add (HonkVariant.EXTRA_LONG, honks [HonkVariant.LONG]);
			} else if (honks.ContainsKey (HonkVariant.TWO_SHORT)) {
				honks.Add (HonkVariant.EXTRA_LONG, honks [HonkVariant.TWO_SHORT]);
			} else if (honks.ContainsKey (HonkVariant.SHORT)) {
				honks.Add (HonkVariant.EXTRA_LONG, honks [HonkVariant.SHORT]);
			}
		}

		// TODO - If no honk sounds, fallback on generic car honks
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public float getFrustrationLevel () {
		return frustrationLevel;
	}

	public void honk (bool startHonk = true) {
		if (startHonk) {
			if (!honkSoundSource.isPlaying) {
				frustrationLevel += 1f;
				pickHonkSound ();
				honkSoundSource.Play ();
			}
		} else {
			// 10 sec cooldown for one honk
			frustrationLevel = Mathf.Max(0f, frustrationLevel - 0.1f * Time.deltaTime);
			honkSoundSource.Stop ();
		}
	}

	private void pickHonkSound () {
		AudioClip activeHonkSound;

		if (frustrationLevel < 3f) {
			// SHORT or TWO SHORT
			activeHonkSound = honks[Random.value < 0.7f ? HonkVariant.SHORT : HonkVariant.TWO_SHORT];
		} else if (frustrationLevel < 5f) {
			// LONG
			activeHonkSound = honks[HonkVariant.LONG];
		} else {
			// EXTRA_LONG
			activeHonkSound = honks[HonkVariant.EXTRA_LONG];
		}

		honkSoundSource.clip = activeHonkSound;
	}

}
