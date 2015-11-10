using System.Collections;
using UnityEngine;

public class CameraHandler {
	private const float MIN_ZOOM_LEVEL = 5f;
	private const float MAX_ZOOM_LEVEL = 0.5f; // TODO - Adjust

	private static float CalculatedOptimalZoom = 3.5f; // TODO - We want this to be automatic and depending on map and/or device type

	private static Camera main;

	public static void setMainCamera (Camera camera) {
		main = camera;
	}

	public static void InitialZoom () {
		float fromZoom = MIN_ZOOM_LEVEL;
		float toZoom = CalculatedOptimalZoom;
		Singleton<Game>.Instance.StartCoroutine (ZoomFromTo(fromZoom, toZoom, 1f));
	}

	private static IEnumerator ZoomFromTo (float start, float end, float time) {
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			main.orthographicSize = Mathf.SmoothStep(start, end, Mathf.SmoothStep(0f, 1f, t));
			yield return t;
		}
	}

	public static void CustomZoom (float amount) {
		Singleton<Game>.Instance.StartCoroutine (ZoomWithAmount(-amount/5f, 0.25f));
	}

	private static IEnumerator ZoomWithAmount (float amount, float time) {
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			float targetZoom = main.orthographicSize + Mathf.SmoothStep(0f, amount, t);
			if (amount > 0f) {
				targetZoom = Mathf.Min (targetZoom, MIN_ZOOM_LEVEL);
			} else {
				targetZoom = Mathf.Max (targetZoom, MAX_ZOOM_LEVEL);
			}
			main.orthographicSize = targetZoom;
			yield return t;
		}
	}
}
