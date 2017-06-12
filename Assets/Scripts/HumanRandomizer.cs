using UnityEngine;
using System.Collections;

public class HumanRandomizer : ObjectRandomizer {
	public const float START_INTERVAL = 4f;
	public const float RANDOM_VARIATION = 1f;
	public const float MIN_INTERVAL = 1f;
	public const float INTERVAL_DECREASE_RATE = 0f;

	public static HumanRandomizer instance = null;

	public HumanRandomizer (bool enabled, float interval, float randomVariation, float minInterval, float intervalDecreaseRate, float delay, int randomSeed) : base(enabled, interval, randomVariation, minInterval, intervalDecreaseRate, delay, randomSeed) {
	}

	protected override void newObject () {
		Game.instance.giveBirth ();
	}

	public static void Create(bool enabled = true, float interval = START_INTERVAL, float randomVariation = RANDOM_VARIATION, float minInterval = MIN_INTERVAL, float intervalDecreaseRate = INTERVAL_DECREASE_RATE, float delay = DEFAULT_DELAY, int randomSeed = -1) {
		if (instance != null) {
			HumanRandomizer.Destroy ();
		}

		instance = new HumanRandomizer (enabled, interval, randomVariation, minInterval, intervalDecreaseRate, delay, randomSeed);
	}

	public static void Create(Randomizer randomizer, Level level) {
		HumanRandomizer.Create (randomizer.enabled, randomizer.interval, randomizer.variation, randomizer.minInterval, randomizer.intervalDecreaseRate, randomizer.delay, level.randomSeed);
	}

	public static void Destroy() {
		if (instance != null) {
			instance.stop ();
			instance = null;
		}
	}
}