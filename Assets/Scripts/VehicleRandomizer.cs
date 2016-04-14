using System.Collections;
using UnityEngine;

public class VehicleRandomizer : ObjectRandomizer {
	private const float START_INTERVAL = 5.5f;
	private const float RANDOM_VARIATION = 1f;
	private const float MIN_INTERVAL = 1f;
	private const float INTERVAL_DECREASE_RATE = 0.02f;

	public VehicleRandomizer (float interval = START_INTERVAL, float randomVariation = RANDOM_VARIATION, float minInterval = MIN_INTERVAL, float intervalDecreaseRate = INTERVAL_DECREASE_RATE) : base(interval, randomVariation, minInterval, intervalDecreaseRate) {
	}
		
	protected override void newObject () {
		Game.instance.createNewCar ();
	}
}
