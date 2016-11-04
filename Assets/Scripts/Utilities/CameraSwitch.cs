using System.Collections;
using UnityEngine;

public class CameraSwitch : MonoBehaviour {

    public IEnumerator animate (Camera from, Camera to, float time, bool copyFromCamera = true) {
        from.enabled = false;
        from.gameObject.SetActive(false);
        to.enabled = false;
        to.gameObject.SetActive(false);

        // Cameras info
        Vector3 fromWorldPosition = Misc.getWorldPos(from.transform);
        float fromSize = from.orthographicSize;
        float toSize = to.orthographicSize;

        // Copy "from" camera
        GameObject cameraCopy = GameObject.Instantiate (copyFromCamera ? from.gameObject : to.gameObject);
        cameraCopy.transform.parent = null;
        cameraCopy.transform.position = fromWorldPosition;
        Camera cameraCopyCamera = cameraCopy.GetComponent<Camera>();
        cameraCopyCamera.orthographicSize = fromSize;
        AudioListener copyAudioListener = cameraCopy.GetComponentInChildren<AudioListener>();
        if (copyAudioListener != null) {
            Destroy(copyAudioListener);
        }

        // Move AudioListener from "from" camera into temporary camera
        AudioListenerHolder fromAudioListenerHolder = Misc.FindAudioListenerHolder(from.gameObject);
        AudioListener audioListener = fromAudioListenerHolder.GetComponentInChildren<AudioListener>();
        audioListener.transform.parent = cameraCopy.transform;

        AudioListenerHolder toAudioListenerHolder = Misc.FindAudioListenerHolder(to.gameObject);

        Vector3 audioListenerMoveVector = toAudioListenerHolder.relativePos - fromAudioListenerHolder.relativePos;

        cameraCopyCamera.enabled = true;
        cameraCopy.SetActive(true);

        yield return doAnimate(cameraCopy.GetComponent<Camera>(), to.transform, toSize, audioListener, audioListenerMoveVector, time);

//        Debug.Log("Audio listener before pos: " + Misc.getWorldPos(audioListener.transform));
        audioListener.transform.parent = toAudioListenerHolder.transform;
        audioListener.transform.localPosition = toAudioListenerHolder.relativePos;
//        Debug.Log("To camera pos: " + Misc.getWorldPos(to.transform));
//        Debug.Log("Audio listener after pos: " + Misc.getWorldPos(audioListener.transform));

        to.enabled = true;
        to.gameObject.SetActive(true);
        cameraCopyCamera.enabled = false;
        cameraCopy.SetActive(false);
        Destroy(cameraCopy);
    }

    private IEnumerator doAnimate(Camera cameraToMove, Transform targetTransform, float targetOrtographicSize, AudioListener audioListener, Vector3 audioListenerMoveVector, float transitionDuration) {
		float t = 0.0f;
        Transform cameraTransform = cameraToMove.transform;
		Vector3 startingPos = cameraTransform.position;
        float startingOrtographicSize = cameraToMove.orthographicSize;
        Vector3 audioListenerStartPosition = audioListener.transform.localPosition;
        Vector3 audioListenerEndPosition = audioListenerStartPosition + audioListenerMoveVector;
		while (t < 1.0f) {
			t += Time.deltaTime / transitionDuration;

            cameraToMove.orthographicSize = Mathf.Lerp(startingOrtographicSize, targetOrtographicSize, t);
			cameraTransform.position = Vector3.Lerp (startingPos, Misc.getWorldPos(targetTransform), t);
            audioListener.transform.localPosition = Vector3.Lerp(audioListenerStartPosition, audioListenerEndPosition, t);
			yield return null;
		}
	}
}