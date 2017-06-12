using System.Collections;
using UnityEngine;

public class VehicleRandomizer : ObjectRandomizer {
	public const float START_INTERVAL = 5.5f;
	public const float RANDOM_VARIATION = 1f;
	public const float MIN_INTERVAL = 1f;
	public const float INTERVAL_DECREASE_RATE = 0.02f;

	private static VehicleRandomizer instance = null; 

	public VehicleRandomizer (bool enabled, float interval, float randomVariation, float minInterval, float intervalDecreaseRate, float delay, int randomSeed) : base(enabled, interval, randomVariation, minInterval, intervalDecreaseRate, delay, randomSeed) {
	}
		
	protected override void newObject () {
		Game.instance.createNewCar ();
	}

	public static void Create(bool enabled = true, float interval = START_INTERVAL, float randomVariation = RANDOM_VARIATION, float minInterval = MIN_INTERVAL, float intervalDecreaseRate = INTERVAL_DECREASE_RATE, float delay = DEFAULT_DELAY, int randomSeed = -1) {
		if (instance != null) {
			VehicleRandomizer.Destroy ();
		}

		instance = new VehicleRandomizer (enabled, interval, randomVariation, minInterval, intervalDecreaseRate, delay, randomSeed);
	}

	public static void Create(Randomizer randomizer, Level level) {
		VehicleRandomizer.Create (randomizer.enabled, randomizer.interval, randomizer.variation, randomizer.minInterval, randomizer.intervalDecreaseRate, randomizer.delay, level.randomSeed);
	}

	public static void Destroy() {
		if (instance != null) {
			instance.stop ();
			instance = null;
		}
	}
}
