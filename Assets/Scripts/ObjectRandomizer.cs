using UnityEngine;
using System.Collections;

public class ObjectRandomizer {
	public const float DEFAULT_DELAY = 0f;

	private static System.Random random;

	private float delay = 0f;
	private float interval;
	private float randomVariation;
	private float minInterval;
	private float intervalDecreaseRate;

	private bool isRunning = true;

	public ObjectRandomizer (float interval, float randomVariation, float minInterval, float intervalDecreaseRate, float delay, int randomSeed) {
		if (random == null || randomSeed != -1) {
			random = new System.Random (randomSeed != -1 ? randomSeed : (int)Game.randomSeed);
		}
		if (delay > 0f) {
			this.delay = delay;
		}
		this.interval = interval;
		this.randomVariation = randomVariation;
		this.minInterval = minInterval;
		this.intervalDecreaseRate = intervalDecreaseRate;
		Singleton<Game>.Instance.StartCoroutine(createNewObjects ());
	}

	private IEnumerator createNewObjects () {
		while (Game.isRunning () && isRunning) {
			if (delay > 0f) {
				yield return new WaitForSeconds (delay);
				delay = 0f;
			} else {
				float nextTime = interval + (float)random.NextDouble () * randomVariation; 
				yield return new WaitForSeconds (nextTime);
				if (interval > minInterval) {
					interval -= interval * intervalDecreaseRate;
				}
				if (Game.isRunning () && isRunning) {
					if (!Game.isPaused ()) {
						newObject ();
					}
				}
			}
		}
	}

	protected virtual void newObject () {
		Debug.Log ("Create object");
	}

	protected void stop() {
		isRunning = false;
	}
}
