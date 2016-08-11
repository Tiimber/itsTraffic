using System.Collections;
using UnityEngine;

public class CameraHandler {
	private static float MIN_ZOOM_LEVEL = 8f;
	private static float INTRO_ZOOM_LEVEL = 5f;
	private const float MAX_ZOOM_LEVEL = 0.5f; // TODO - Adjust

	private static float CalculatedOptimalZoom = 3.5f; // TODO - We want this to be automatic and depending on map and/or device type

	private static Camera main;

	public static void SetIntroZoom (float zoom) {
		CameraHandler.INTRO_ZOOM_LEVEL = zoom;
	}

	public static void SetMainCamera (Camera camera) {
		main = camera;
	}

	public static void InitialZoom () {
		float fromZoom = INTRO_ZOOM_LEVEL;
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
			// TODO - Clamp?
			if (amount > 0f) {
				targetZoom = Mathf.Min (targetZoom, MIN_ZOOM_LEVEL);
			} else {
				targetZoom = Mathf.Max (targetZoom, MAX_ZOOM_LEVEL);
			}
			main.orthographicSize = targetZoom;
			Singleton<Game>.Instance.StartCoroutine (MoveWithVector(Vector3.zero, 0f, false));
			yield return t;
		}
	}

	public static void Move(Vector3 move) {
		float cameraSize = main.orthographicSize;
		float screenHeight = Screen.height;
		float screenDisplayFactor = cameraSize * 2f / screenHeight;

		Vector3 adjustedMove = move * -1f;

		Singleton<Game>.Instance.StartCoroutine (MoveWithVector(adjustedMove * screenDisplayFactor, 0.3f));
	}

	private static IEnumerator MoveWithVector (Vector3 moveVector, float time, bool doAnimate = true) {
		float t = 0f;
		Vector3 velocity = Vector3.zero;
		Vector3 lastPosition = Vector3.zero;

		Vector3 targetPosition = main.transform.position + moveVector;
		float cameraSize = main.orthographicSize;
		float maxYOffset = MIN_ZOOM_LEVEL - cameraSize;

		float aspectRatio = Screen.width / Screen.height;
		float maxXOffset = MIN_ZOOM_LEVEL - cameraSize / (aspectRatio * 2);

		moveVector.x = Mathf.Clamp (targetPosition.x, -maxXOffset, maxXOffset) - main.transform.position.x;
		moveVector.y = Mathf.Clamp (targetPosition.y, -maxYOffset, maxYOffset) - main.transform.position.y;

		if (doAnimate && time > 0f) {
			while (t <= 1f) {
				t += Time.deltaTime / time;
				Vector3 newPosition = Vector3.SmoothDamp (lastPosition, moveVector, ref velocity, time, Mathf.Infinity, t);
				main.transform.position += newPosition - lastPosition;
				lastPosition = newPosition;
				yield return t;
			}
		} else {
			// TODO - This is also for low end devices
			main.transform.position += moveVector;
			yield return time;
		}
	}
}
