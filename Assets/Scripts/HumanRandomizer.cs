using UnityEngine;
using System.Collections;

public class HumanRandomizer : ObjectRandomizer {
	private const float START_INTERVAL = 4f;
	private const float RANDOM_VARIATION = 1f;
	private const float MIN_INTERVAL = 1f;
	private const float INTERVAL_DECREASE_RATE = 0f;

	public static HumanRandomizer instance = null;

	public HumanRandomizer (float interval, float randomVariation, float minInterval, float intervalDecreaseRate) : base(interval, randomVariation, minInterval, intervalDecreaseRate) {
	}

	protected override void newObject () {
		Game.instance.giveBirth ();
	}

	public static void Create(float interval = START_INTERVAL, float randomVariation = RANDOM_VARIATION, float minInterval = MIN_INTERVAL, float intervalDecreaseRate = INTERVAL_DECREASE_RATE) {
		if (instance != null) {
			HumanRandomizer.Destroy ();
		}

		instance = new HumanRandomizer (interval, randomVariation, minInterval, intervalDecreaseRate);
	}

	public static void Destroy() {
		if (instance != null) {
			instance.stop ();
			instance = null;
		}
	}
}