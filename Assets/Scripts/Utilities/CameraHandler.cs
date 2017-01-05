using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler {
	private static float MIN_ZOOM_LEVEL_ORTHOGRAPHIC = 8f;
	private static float INTRO_ZOOM_LEVEL_ORTHOGRAPHIC = 5f; // = 19f
	private static float MAX_ZOOM_LEVEL_ORTHOGRAPHIC = 0.5f; // TODO - Adjust
	private static Vector3 CENTER_POINT = Vector3.zero;

	private static float CALCULATED_OPTIMAL_ZOOM_ORTHOGRAPHIC = 3.5f; // TODO - We want this to be automatic and depending on map and/or device type // 13.4f

	private static Camera main;
    private static Vector3 MAIN_RESTORE_POSITION;
    private static float MAIN_RESTORE_SIZE_ORTHOGRAPHIC;

	private static Camera perspectiveCamera;
    private static Vector3 RESTORE_POSITION_PERSPECTIVE;
    private static float RESTORE_FIELD_OF_VIEW_PERSPECTIVE;

	public static bool IsMapReadyForInteraction = false;

	public static void SetIntroZoom (float zoom) {
		CameraHandler.INTRO_ZOOM_LEVEL_ORTHOGRAPHIC = zoom;
	}

	public static void SetMainCamera (Camera camera) {
		main = camera;
	}

    public static void SetZoomLevels(float min = 8f, float max = 0.5f) {
        MIN_ZOOM_LEVEL_ORTHOGRAPHIC = min;
        MAX_ZOOM_LEVEL_ORTHOGRAPHIC = max;
    }

	public static void SetCenterPoint(Vector3 center) {
		CENTER_POINT = center;
	}

    public static void SetPerspectiveCamera (Camera camera) {
        perspectiveCamera = camera;
    }

    public static void SetRestoreState () {
		MAIN_RESTORE_POSITION = main.transform.position;
        MAIN_RESTORE_SIZE_ORTHOGRAPHIC = main.orthographicSize;

        RESTORE_POSITION_PERSPECTIVE = perspectiveCamera.transform.position;
        RESTORE_FIELD_OF_VIEW_PERSPECTIVE = perspectiveCamera.fieldOfView;
    }

    public static void Restore () {
		main.transform.position = MAIN_RESTORE_POSITION;
        main.orthographicSize = MAIN_RESTORE_SIZE_ORTHOGRAPHIC;

        perspectiveCamera.transform.position = RESTORE_POSITION_PERSPECTIVE;
        perspectiveCamera.fieldOfView = RESTORE_FIELD_OF_VIEW_PERSPECTIVE;
    }

	public static void InitialZoom () {
		float fromZoom = INTRO_ZOOM_LEVEL_ORTHOGRAPHIC;
		float toZoom = CALCULATED_OPTIMAL_ZOOM_ORTHOGRAPHIC;
		Singleton<SingletonInstance>.Instance.StartCoroutine (ZoomFromTo(fromZoom, toZoom, 1f));
	}

	public static IEnumerator ResetZoom() {
		float centerX = Screen.width / 2f;
		float centerY = Screen.height / 2f;
		Vector3 centerPos = new Vector3(centerX, centerY, 0f);
		yield return ZoomWithAmount(MIN_ZOOM_LEVEL_ORTHOGRAPHIC, 0.25f, centerPos);
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
		bool hasPerspective = perspectiveCamera != null;
		Vector3 cameraPos = main.transform.position;
		Vector3 targetPos = new Vector3(point.x, point.y, cameraPos.z);
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			float animTime = Mathf.SmoothStep(0f, 1f, t);
			float orthographicSize = Mathf.SmoothStep(start, end, animTime);
			Vector3 cameraPosition = Vector3.Lerp(cameraPos, targetPos, animTime);

			main.orthographicSize = orthographicSize;
			main.transform.position = cameraPosition;

			if (hasPerspective) {
				perspectiveCamera.fieldOfView = GetPerspectiveForOrthographicSize(orthographicSize);
				perspectiveCamera.transform.position = cameraPosition;
			}

			yield return null;
		}
	}

	private static IEnumerator ZoomFromTo (float start, float end, float time) {
		bool hasPerspective = perspectiveCamera != null;
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			float orthographicSize = Mathf.SmoothStep(start, end, Mathf.SmoothStep(0f, 1f, t));

			main.orthographicSize = orthographicSize;
			if (hasPerspective) {
				perspectiveCamera.fieldOfView = GetPerspectiveForOrthographicSize(orthographicSize);
			}

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
		bool hasPerspective = perspectiveCamera != null;
		float t = 0f;
		while (t <= 1f) {
			t += Time.deltaTime / time;
			float targetZoom = main.orthographicSize + Mathf.SmoothStep(0f, amount, t);
			// TODO - Clamp?
			if (amount > 0f) {
				targetZoom = Mathf.Min (targetZoom, MIN_ZOOM_LEVEL_ORTHOGRAPHIC);
			} else {
				targetZoom = Mathf.Max (targetZoom, MAX_ZOOM_LEVEL_ORTHOGRAPHIC);
			}
			float zoomDelta = main.orthographicSize - targetZoom;
			main.orthographicSize = targetZoom;

			if (hasPerspective) {
				perspectiveCamera.fieldOfView = GetPerspectiveForOrthographicSize(targetZoom);
			}

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
        if (CameraHandler.IsMapReadyForInteraction && move != Vector3.zero) {
			float cameraSize = main.orthographicSize;
			float screenHeight = Screen.height;
			float screenDisplayFactor = cameraSize * 2f / screenHeight;

			Vector3 adjustedMove = move * -1f;
			currentMoveTo = Singleton<SingletonInstance>.Instance.StartCoroutine (MoveWithVector(adjustedMove * screenDisplayFactor, 0.3f));
        }
	}

	private static IEnumerator MoveWithVector (Vector3 moveVector, float time, bool doAnimate = true) {
		bool hasPerspective = perspectiveCamera != null;

		float t = 0f;
		Vector3 velocity = Vector3.zero;
		Vector3 lastPosition = Vector3.zero;

		Vector3 startPosition = main.transform.position;
		Vector3 targetPosition = startPosition + moveVector;
		float cameraSize = main.orthographicSize;

		float maxYOffset = (MIN_ZOOM_LEVEL_ORTHOGRAPHIC - cameraSize) * Misc.GetHeightRatio();
		float maxXOffset = (MIN_ZOOM_LEVEL_ORTHOGRAPHIC - cameraSize) * Misc.GetWidthRatio();

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
				if (hasPerspective) {
					perspectiveCamera.transform.position += newPosition - lastPosition;
				}
				lastPosition = newPosition;
				// Vector3 newPosition = Vector3.Slerp (startPosition, clampedTargetPosition, t);
				// main.transform.position = newPosition;
				// if (hasPerspective) {
				// 	   perspectiveCamera.transform.position = newPosition;
				// }
				yield return null;
			}
		} else {
			// TODO - This is also for low end devices
			main.transform.position = clampedTargetPosition;
			if (hasPerspective) {
				perspectiveCamera.transform.position = clampedTargetPosition;
			}
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

	public static float GetPerspectiveForOrthographicSize (float orthographicSize) {
		const float m = 5.6f/1.5f;
		const float c = 1f/3f;
		return m * orthographicSize + c;
	}
}
