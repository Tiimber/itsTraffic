using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleSounds : MonoBehaviour, IPubSub {

	private float volume = 0.8f;

	public AudioClip shortHonkSound;
	public AudioClip twoShortHonkSound;
	public AudioClip longHonkSound;
	public AudioClip extraLongHonkSound;
	public AudioClip sirenSound;

	public enum HonkVariant {
		SHORT,
		TWO_SHORT,
		LONG,
		EXTRA_LONG
	}

	private Dictionary<HonkVariant, AudioClip> honks = new Dictionary<HonkVariant, AudioClip> ();

	private AudioSource soundSource;
	private AudioSource soundSourceSiren;
	private float frustrationLevel = 0f;
	private static float FRUSTRATION_INCREASE_ON_HONK = 1f; 

	// Use this for initialization
	void Start () {
		// Init the honks dictionary
		initHonkMapping ();

		volume = Game.instance.soundEffectsVolume;

		PubSub.subscribe ("Volume:effects", this);

		// Init the audioSource
		soundSource = gameObject.AddComponent<AudioSource> ();
		soundSource.playOnAwake = false;
		soundSource.clip = honks [HonkVariant.SHORT];
		soundSource.spatialBlend = 1f;
		soundSource.volume = volume;
//		honkSoundSource.Play ();

        if (sirenSound != null) {
			// Init the audioSource for siren
			soundSourceSiren = gameObject.AddComponent<AudioSource> ();
			soundSourceSiren.playOnAwake = false;
			soundSourceSiren.clip = sirenSound;
			soundSourceSiren.spatialBlend = 1f;
			soundSourceSiren.loop = true;
			soundSourceSiren.volume = volume;
        }
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

	public void decreaseFrustrationLevelOnManualHonk() {
		frustrationLevel -= FRUSTRATION_INCREASE_ON_HONK;
	}

	public void honk (bool startHonk = true) {
		if (startHonk) {
			if (!soundSource.isPlaying) {
				frustrationLevel += FRUSTRATION_INCREASE_ON_HONK;
				pickHonkSound ();
				soundSource.Play ();
			}
		} else {
			// 10 sec cooldown for one honk
			frustrationLevel = Mathf.Max(0f, frustrationLevel - 0.1f * Time.deltaTime);
			soundSource.Stop ();
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

		soundSource.clip = activeHonkSound;
	}

    public void playSiren(bool fadeIn = true, float time = 2f) {
        if (!soundSourceSiren.isPlaying) {
            if (fadeIn) {
				StartCoroutine(fadeSirenSound(0.1f, 1f, time));
			}
			soundSourceSiren.Play();
        }
    }

    public void stopSiren(bool fadeOut = true, float time = 2f) {
        if (soundSourceSiren != null && soundSourceSiren.isPlaying) {
            if (fadeOut) {
                StartCoroutine(fadeOutSirenSound(time));
            } else {
                soundSourceSiren.Stop();
                soundSourceSiren.time = 0f;
            }
        }
    }

    private IEnumerator fadeOutSirenSound(float time = 2f) {
        yield return fadeSirenSound(1f, 0f, time);
        soundSourceSiren.Stop();
        soundSourceSiren.time = 0f;
    }

    private IEnumerator fadeSirenSound(float from, float to, float time) {
        float fullDiff = to - from;
        soundSourceSiren.volume = from;

        while (soundSourceSiren.volume < to) {
            yield return new WaitForSecondsRealtime(1f/60f);
            soundSourceSiren.volume += fullDiff / time * Time.deltaTime;
        }
    }

	public void playMajorCrashSound() {
		playCrashSound (GenericVehicleSounds.instance.majorCrashSound);
	}

	public void playMinorCrashSound() {
		playCrashSound (GenericVehicleSounds.instance.minorCrashSound);
	}

	private void playCrashSound(AudioClip crashSound) {
		if (soundSource.isPlaying) {
			soundSource.Stop ();
		}
		soundSource.clip = crashSound;
		soundSource.Play ();
		// Prevent honking just after crash
		frustrationLevel = 0f;
	}
		
	public PROPAGATION onMessage (string message, object data) {
		if (message == "Volume:effects") {
			if (!GetComponent<Vehicle> ().destroying) {
				float volume = (float)data;
				soundSource.volume = volume;
			}
		}

		return PROPAGATION.DEFAULT;
	}

	void OnDestroy() {
		PubSub.unsubscribe ("Volume:effects", this);
	}
}
