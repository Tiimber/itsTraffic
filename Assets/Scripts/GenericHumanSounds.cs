﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenericHumanSounds : MonoBehaviour, IPubSub {

	private static float volume = 0.8f;

	public List<AudioClip> ambientHumanSounds;

	public static GenericHumanSounds instance;

	private static int humansPerChannel = 20;
	public static int numberOfActiveChannels = 0;
	private static List<AudioSource> ambientSoundSources;

	void Start () {
		GenericHumanSounds.instance = this;
		PubSub.subscribe ("Volume:ambient", this);

		GenericHumanSounds.ambientSoundSources = new List<AudioSource> (ambientHumanSounds.Count);
		for (int i = 0; i < ambientHumanSounds.Count; i++) {
			AudioSource ambientSoundSource = Game.instance.gameObject.AddComponent<AudioSource> ();
			ambientSoundSource.playOnAwake = false;
			ambientSoundSource.spatialBlend = 1f;
			ambientSoundSource.clip = GenericHumanSounds.instance.ambientHumanSounds[i];
			ambientSoundSource.loop = true;
			ambientSoundSource.volume = volume;
			ambientSoundSource.time = Misc.randomRange (0f, GenericHumanSounds.instance.ambientHumanSounds[i].length);
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

	public static void stopAmbientSound(int n) {
		GenericHumanSounds.ambientSoundSources [n].Pause ();
	}

	private static void startAmbientSound(int n) {
		GenericHumanSounds.ambientSoundSources [n].Play ();
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "Volume:ambient") {
			float volume = (float) data;
			foreach (AudioSource audioSource in ambientSoundSources) {
                if (audioSource != null) {
					audioSource.volume = volume;
                }
			}
		}

		return PROPAGATION.DEFAULT;
	}
}
