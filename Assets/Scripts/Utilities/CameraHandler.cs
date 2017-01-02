using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler {
	private static float MIN_ZOOM_LEVEL = 8f;
	private static float INTRO_ZOOM_LEVEL = 5f;
	private static float MAX_ZOOM_LEVEL = 0.5f; // TODO - Adjust
	private static Vector3 CENTER_POINT = Vector3.zero;

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

	public static void SetCenterPoint(Vector3 center) {
		CENTER_POINT = center;
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

	public static IEnumerator ResetZoom() {
		float centerX = Screen.width / 2f;
		float centerY = Screen.height / 2f;
		Vector3 centerPos = new Vector3(centerX, centerY, 0f);
		yield return ZoomWithAmount(MIN_ZOOM_LEVEL, 0.25f, centerPos);
	}

	public static void ZoomToSizeAndMoveToPointThenSetNewMinMaxZoomAndCenter(float size, Vector3 center, float zoomSizeFactor, float time = 0.3f) {
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomFromToAndMoveToPointThenSetNewMinMaxZoomAndCenter(size, center, zoomSizeFactor, time));
	}

	private static IEnumerator ZoomFromToAndMoveToPointThenSetNewMinMaxZoomAndCenter(float size, Vector3 center, float zoomSizeFactor, float time) {
		yield return ZoomFromToAndMoveToPoint(main.orthographicSize, size, center, time);
		CameraHandler.SetZoomLevels(size, size / zoomSizeFactor);
		CameraHandler.SetCenterPoint(center);
	}

	public static void ZoomToSizeAndMoveToPoint(float size, Vector3 center, float time = 0.3f) {
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomFromToAndMoveToPoint(main.orthographicSize, size, center, time));
	}

	private static IEnumerator ZoomFromToAndMoveToPoint(float start, float end, Vector3 point, float time) {
		Vector3 cameraPos = main.transform.position;
		Vector3 targetPos = new Vector3(point.x, point.y, cameraPos.z);
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			float animTime = Mathf.SmoothStep(0f, 1f, t);
			main.orthographicSize = Mathf.SmoothStep(start, end, animTime);
			main.transform.position = Vector3.Lerp(cameraPos, targetPos, animTime);
			yield return null;
		}
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

	public static IEnumerator CustomZoomIEnumerator (float amount, Vector3 zoomPoint) {
		yield return ZoomWithAmount(-amount/5f, 0.25f, zoomPoint);
	}

	public static void CustomZoom (float amount) {
		float centerX = Screen.width / 2f;
		float centerY = Screen.height / 2f;
		Vector3 centerPos = new Vector3(centerX, centerY, 0f);
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomWithAmount(-amount/5f, 0.25f, centerPos));
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
			Tuple2<float, float> offsetPctFromCenter = zoomPoint == null ? new Tuple2<float, float>(0f, 0f) : Misc.getOffsetPctFromCenter (zoomPoint);
			float xZoomRatio = Misc.GetWidthRatio();
			float yZoomRatio = Misc.GetHeightRatio();
			Vector3 zoomOffsetMove = new Vector3 ((zoomDelta * xZoomRatio) * offsetPctFromCenter.First, (zoomDelta * yZoomRatio) * offsetPctFromCenter.Second, 0f);
			Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(zoomOffsetMove, 0f, false));
			
			yield return t;
		}
	}

	public static Color GetBackgroundColor () {
		return main.backgroundColor;
	}

	public static float GetOrthograpicSize () {
		return main.orthographicSize;
	}

	public static void Move(Vector3 move) {
        if (main.gameObject.activeSelf && move != Vector3.zero) {
			float cameraSize = main.orthographicSize;
			float screenHeight = Screen.height;
			float screenDisplayFactor = cameraSize * 2f / screenHeight;

			Vector3 adjustedMove = move * -1f;
			currentMoveTo = Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(adjustedMove * screenDisplayFactor, 0.3f));
        }
	}

	private static IEnumerator MoveWithVector (Vector3 moveVector, float time, bool doAnimate = true) {
		float t = 0f;
		Vector3 velocity = Vector3.zero;
		Vector3 lastPosition = Vector3.zero;

		Vector3 startPosition = main.transform.position;
		Vector3 targetPosition = startPosition + moveVector;
		float cameraSize = main.orthographicSize;

		float maxYOffset = (MIN_ZOOM_LEVEL - cameraSize) * Misc.GetHeightRatio();
		float maxXOffset = (MIN_ZOOM_LEVEL - cameraSize) * Misc.GetWidthRatio();
		// float maxYOffset = (MIN_ZOOM_LEVEL - cameraSize) / Misc.GetHeightRatio();
		// float maxXOffset = (MIN_ZOOM_LEVEL - cameraSize) / Misc.GetWidthRatio();

		moveVector.x = Mathf.Clamp (targetPosition.x, CENTER_POINT.x - maxXOffset, CENTER_POINT.x + maxXOffset) - startPosition.x;
		moveVector.y = Mathf.Clamp (targetPosition.y, CENTER_POINT.y - maxYOffset, CENTER_POINT.y + maxYOffset) - startPosition.y;

		Vector3 clampedTargetPosition = startPosition + moveVector;

		// TODO - Do we need animations here at all?
		doAnimate = false;
		if (doAnimate && time > 0f) {
			while (t <= 1f) {
				t += Time.unscaledDeltaTime / time;
				Vector3 newPosition = Vector3.SmoothDamp (lastPosition, moveVector, ref velocity, time, Mathf.Infinity, t);
				main.transform.position += newPosition - lastPosition;
				lastPosition = newPosition;
				// Vector3 newPosition = Vector3.Slerp (startPosition, clampedTargetPosition, t);
				// main.transform.position = newPosition;
				yield return null;
			}
		} else {
			// TODO - This is also for low end devices
			main.transform.position = clampedTargetPosition;
			yield return time;
		}
	}

	private static Coroutine currentMoveTo = null;
	public static void MoveTo(GameObject gameObject, float time = 0.3f) {
        Vector3 objectPosition = gameObject.transform.position;
		MoveToPoint(objectPosition, time);
	}

	public static void MoveToPoint(Vector3 point, float time = 0.3f) {
		Vector3 cameraPosition = main.transform.position;
        Vector3 moveCameraToObjectVector = point - cameraPosition;
        moveCameraToObjectVector.z = 0f;
		if (currentMoveTo != null) {
			Singleton<SingletonInstance>.Instance.StopCoroutine(currentMoveTo);
		}
        currentMoveTo = Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(moveCameraToObjectVector, time));
	}
}
