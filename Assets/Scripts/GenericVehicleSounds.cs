using UnityEngine;
using System.Collections;

public class GenericVehicleSounds : MonoBehaviour {

	public AudioClip majorCrashSound;
	public AudioClip minorCrashSound;

	public static GenericVehicleSounds instance;

	void Start () {
		GenericVehicleSounds.instance = this;
	}
}
