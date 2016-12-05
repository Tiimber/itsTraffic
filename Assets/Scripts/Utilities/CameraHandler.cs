using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler {
	private static float MIN_ZOOM_LEVEL = 8f;
	private static float INTRO_ZOOM_LEVEL = 5f;
	private static float MAX_ZOOM_LEVEL = 0.5f; // TODO - Adjust

	private static float CalculatedOptimalZoom = 3.5f; // TODO - We want this to be automatic and depending on map and/or device type

	private static Camera main;
    private static Vector3 mainRestorePosition;
    private static float mainRestoreSize;

	private static Camera perspectiveCamera;
    private static Vector3 perspectiveRestorePosition;
    private static float perspectiveRestoreFieldOfView;

	public static void SetIntroZoom (float zoom) {
		CameraHandler.INTRO_ZOOM_LEVEL = zoom;
	}

	public static void SetMainCamera (Camera camera) {
		main = camera;
	}

    public static void SetZoomLevels(float min = 8f, float max = 0.5f) {
        MIN_ZOOM_LEVEL = min;
        MAX_ZOOM_LEVEL = max;
    }

    public static void SetPerspectiveCamera (Camera camera) {
        perspectiveCamera = camera;
    }

    public static void SetRestoreState () {
		mainRestorePosition = main.transform.position;
        mainRestoreSize = main.orthographicSize;

        perspectiveRestorePosition = perspectiveCamera.transform.position;
        perspectiveRestoreFieldOfView = perspectiveCamera.fieldOfView;
    }

    public static void Restore () {
		main.transform.position = mainRestorePosition;
        main.orthographicSize = mainRestoreSize;

        perspectiveCamera.transform.position = perspectiveRestorePosition;
        perspectiveCamera.fieldOfView = perspectiveRestoreFieldOfView;
    }

	public static void InitialZoom () {
		float fromZoom = INTRO_ZOOM_LEVEL;
		float toZoom = CalculatedOptimalZoom;
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomFromTo(fromZoom, toZoom, 1f));
	}

	private static IEnumerator ZoomFromTo (float start, float end, float time) {
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			main.orthographicSize = Mathf.SmoothStep(start, end, Mathf.SmoothStep(0f, 1f, t));
			yield return t;
		}
	}

	public static void CustomZoom (float amount, Vector3 zoomPoint) {
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomWithAmount(-amount/5f, 0.25f, zoomPoint));
	}

	private static IEnumerator ZoomWithAmount (float amount, float time, Vector3 zoomPoint) {

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
			float zoomDelta = main.orthographicSize - targetZoom;
			main.orthographicSize = targetZoom;

			// Try to zoom in towards a specific point
			Tuple2<float, float> offsetPctFromCenter = Misc.getOffsetPctFromCenter (zoomPoint);
			float xZoomRatio = Mathf.Max((float) Screen.width / (float) Screen.height, 1f);
			float yZoomRatio = Mathf.Max((float) Screen.height / (float) Screen.width, 1f);
			Vector3 zoomOffsetMove = new Vector3 ((zoomDelta * xZoomRatio) * offsetPctFromCenter.First, (zoomDelta * yZoomRatio) * offsetPctFromCenter.Second, 0f);
			Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(zoomOffsetMove, 0f, false));

			yield return t;
		}
	}

	public static void Move(Vector3 move) {
        if (main.gameObject.activeSelf) {
			float cameraSize = main.orthographicSize;
			float screenHeight = Screen.height;
			float screenDisplayFactor = cameraSize * 2f / screenHeight;

			Vector3 adjustedMove = move * -1f;

			Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(adjustedMove * screenDisplayFactor, 0.3f));
        }
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
				t += Time.unscaledDeltaTime / time;
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

	public static void MoveTo(GameObject gameObject) {
		Vector3 cameraPosition = main.transform.position;
        Vector3 objectPosition = gameObject.transform.position;
        Vector3 moveCameraToObjectVector = objectPosition - cameraPosition;
        moveCameraToObjectVector.z = 0f;
        Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(moveCameraToObjectVector, 0.3f));
	}
}
