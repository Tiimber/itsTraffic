using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenericHumanSounds : MonoBehaviour {

	public List<AudioClip> ambientHumanSounds;

	public static GenericHumanSounds instance;

	private static int humansPerChannel = 20;
	private static int numberOfActiveChannels = 0;
	private static List<AudioSource> ambientSoundSources;

	void Start () {
		GenericHumanSounds.instance = this;

		GenericHumanSounds.ambientSoundSources = new List<AudioSource> (ambientHumanSounds.Count);
		for (int i = 0; i < ambientHumanSounds.Count; i++) {
			AudioSource ambientSoundSource = Game.instance.gameObject.AddComponent<AudioSource> ();
			ambientSoundSource.playOnAwake = false;
			ambientSoundSource.spatialBlend = 1f;
			ambientSoundSource.clip = GenericHumanSounds.instance.ambientHumanSounds[i];
			ambientSoundSource.loop = true;
			ambientSoundSource.time = Random.Range (0f, GenericHumanSounds.instance.ambientHumanSounds[i].length);
			GenericHumanSounds.ambientSoundSources.Add (ambientSoundSource);
		}
	}

	public static void HumanCountChange() {
		int amount = HumanLogic.numberOfHumans;
		int calculatedNumberOfChannels = Mathf.CeilToInt((float) amount / humansPerChannel);
		int wantedNumberOfChannels = Mathf.Clamp (calculatedNumberOfChannels, 1, GenericHumanSounds.instance.ambientHumanSounds.Count);
		if (wantedNumberOfChannels < numberOfActiveChannels) {
			for (int i = wantedNumberOfChannels; i < numberOfActiveChannels; i++) {
				GenericHumanSounds.stopAmbientSound (i);
			}
		} else if (wantedNumberOfChannels > numberOfActiveChannels) {
			for (int i = numberOfActiveChannels; i < wantedNumberOfChannels; i++) {
				GenericHumanSounds.startAmbientSound (i);
			}
		}
		numberOfActiveChannels = wantedNumberOfChannels;
	}

	private static void stopAmbientSound(int n) {
		GenericHumanSounds.ambientSoundSources [n].Pause ();
	}

	private static void startAmbientSound(int n) {
		GenericHumanSounds.ambientSoundSources [n].Play ();
	}

}
