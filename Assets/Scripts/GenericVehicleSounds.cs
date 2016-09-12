using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenericVehicleSounds : MonoBehaviour, IPubSub {

	private static float volume = 0.8f;

	public AudioClip majorCrashSound;
	public AudioClip minorCrashSound;

	public AudioClip ambientTrafficSound;

	public static GenericVehicleSounds instance;

	private static int maxAmbientChannels = 4;
	private static int vehiclesPerChannel = 20;
	private static int numberOfActiveChannels = 0;
	private static List<AudioSource> ambientSoundSources;

	void Start () {
		GenericVehicleSounds.instance = this;
		PubSub.subscribe ("Volume:ambient", this);

		GenericVehicleSounds.ambientSoundSources = new List<AudioSource> (maxAmbientChannels);
		for (int i = 0; i < maxAmbientChannels; i++) {
			AudioSource ambientSoundSource = Game.instance.gameObject.AddComponent<AudioSource> ();
			ambientSoundSource.playOnAwake = false;
			ambientSoundSource.spatialBlend = 1f;
			ambientSoundSource.clip = GenericVehicleSounds.instance.ambientTrafficSound;
			ambientSoundSource.loop = true;
			ambientSoundSource.volume = volume;
			ambientSoundSource.time = Misc.randomRange (0f, GenericVehicleSounds.instance.ambientTrafficSound.length);
			GenericVehicleSounds.ambientSoundSources.Add (ambientSoundSource);
		}
	}

	public static void VehicleCountChange() {
		int amount = Vehicle.numberOfCars;
		int calculatedNumberOfChannels = Mathf.CeilToInt((float) amount / vehiclesPerChannel);
		int wantedNumberOfChannels = Mathf.Clamp (calculatedNumberOfChannels, 1, maxAmbientChannels);
		if (wantedNumberOfChannels < numberOfActiveChannels) {
			for (int i = wantedNumberOfChannels; i < numberOfActiveChannels; i++) {
				GenericVehicleSounds.stopAmbientSound (i);
			}
		} else if (wantedNumberOfChannels > numberOfActiveChannels) {
			for (int i = numberOfActiveChannels; i < wantedNumberOfChannels; i++) {
				GenericVehicleSounds.startAmbientSound (i);
			}
		}
		numberOfActiveChannels = wantedNumberOfChannels;
	}

	public static void stopAmbientSound(int n) {
		GenericVehicleSounds.ambientSoundSources [n].Pause ();
	}

	private static void startAmbientSound(int n) {
		GenericVehicleSounds.ambientSoundSources [n].Play ();
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "Volume:ambient") {
			float volume = (float) data;
			foreach (AudioSource audioSource in ambientSoundSources) {
				audioSource.volume = volume;
			}
		}

		return PROPAGATION.DEFAULT;
	}
}
