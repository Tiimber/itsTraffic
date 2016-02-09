using System.Collections;
using UnityEngine;

public class VehicleRandomizer {
	private float interval;
	private System.Random random;

	private const float RANDOM_VARIATION = 1f;
	private const float MIN_INTERVAL = 1f;
	private const float INTERVAL_DECREASE_RATE = 0.02f;

	public VehicleRandomizer (float interval = 5.5f) {
		this.interval = interval;
		random = new System.Random ((int)Game.randomSeed);
		Singleton<Game>.Instance.StartCoroutine(createNewVehicles ());
	}

	private IEnumerator createNewVehicles () {
		while (Game.isRunning ()) {
			float nextTime = interval + (float)random.NextDouble() * VehicleRandomizer.RANDOM_VARIATION; 
			yield return new WaitForSeconds(nextTime);
			if (interval > MIN_INTERVAL) {
				interval -= interval * VehicleRandomizer.INTERVAL_DECREASE_RATE;
			} 
			newVehicle ();
		}
	}

	private void newVehicle () {
		if (Game.isRunning ()) {
//			Game.instance.createNewCar ();
//			Debug.Log ("NEW CAR!");
		}
	}
}
