using System.Collections;
using UnityEngine;

public class GameTimer {
	
	private static float time;

	public static void resetTime () {
		time = Time.time;
	}
		

	public static float elapsedTime () {
		return Time.time - time;
	}
}

