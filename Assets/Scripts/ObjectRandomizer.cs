using UnityEngine;
using System.Collections;

public class ObjectRandomizer {
	private static System.Random random;

	private float interval;
	private float randomVariation;
	private float minInterval;
	private float intervalDecreaseRate;

	private bool isRunning = true;

	public ObjectRandomizer (float interval, float randomVariation, float minInterval, float intervalDecreaseRate) {
		if (random == null) {
			random = new System.Random ((int)Game.randomSeed);
		}
		this.interval = interval;
		this.randomVariation = randomVariation;
		this.minInterval = minInterval;
		this.intervalDecreaseRate = intervalDecreaseRate;
		Singleton<Game>.Instance.StartCoroutine(createNewObjects ());
	}

	private IEnumerator createNewObjects () {
		while (Game.isRunning () && isRunning) {
			float nextTime = interval + (float)random.NextDouble() * randomVariation; 
			yield return new WaitForSeconds(nextTime);
			if (interval > minInterval) {
				interval -= interval * intervalDecreaseRate;
			}
			if (Game.isRunning () && isRunning) {
				newObject ();
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
