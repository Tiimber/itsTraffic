using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialIcon : MonoBehaviour {
    private string icon;
    private float iconSpacing = 4f;
    private string flashIcon;
    private float flashIconTime = 0f;
    private float flashIconOpacity;
    private Coroutine flashIconCoroutine;

    public void setFlashIcon(string flashIcon){
        this.flashIcon = flashIcon;
        this.flashIconOpacity = 0f;

        this.enabled = true;
        if (flashIconCoroutine != null) {
            StopCoroutine(flashIconCoroutine);
        }
        flashIconCoroutine = StartCoroutine(fadeFlashIcon(2f));
    }

    public void setIcon(string icon) {
        this.icon = icon;
        this.enabled = true;
    }

    private IEnumerator fadeFlashIcon(float displayTime) {
        float fadeTime = 0.7f;
        float currentTime = 0f;

        while (currentTime < fadeTime) {
            yield return null;
            currentTime = Mathf.Min(currentTime + Time.unscaledDeltaTime, fadeTime);
            this.flashIconOpacity = Mathf.Lerp(0f, 1f, currentTime / fadeTime);
        }
        if (displayTime > 0) {
            yield return new WaitForSecondsRealtime(displayTime);
            currentTime = 0f;
            while (currentTime < fadeTime) {
                yield return null;
                currentTime = Mathf.Min(currentTime + Time.unscaledDeltaTime, fadeTime);
                this.flashIconOpacity = Mathf.Lerp(1f, 0f, currentTime / fadeTime);
            }
            this.flashIcon = null;

            if (this.icon == null) {
                this.enabled = false;
            }
        }
        flashIconCoroutine = null;
    }

    private float calculateOffsetY() {
        ObjectPixelSize objectPixelSize = gameObject.GetComponent<ObjectPixelSize>();
        float fieldOfView = CameraHandler.currentRenderCamera.fieldOfView;
        float widthToHeightDiff = objectPixelSize.widthAt1FOV - objectPixelSize.heightAt1FOV;
        var rotationRadians = Misc.ToRadians(gameObject.transform.rotation.eulerAngles.z);
        return iconSpacing + (objectPixelSize.heightAt1FOV + widthToHeightDiff*Mathf.Abs(Mathf.Sin(rotationRadians))) * Mathf.Pow(fieldOfView, -0.907399f);
    }

    void OnGUI() {
        if (gameObject != null) {
            if (icon != null) {
                // TODO Move loading of icon outside OnGUI - load icon one time.
                Texture2D iconTexture = Resources.Load<Texture2D>("Graphics/" + icon);
                if (Game.instance == null || CameraHandler.currentRenderCamera == null) {
                    Debug.Log("Something is null");
                }
                Vector3 screenPos = Game.instance.objectToScreenPos(gameObject, CameraHandler.currentRenderCamera);
                float width = iconTexture.width;
                float height = iconTexture.height;
                float offsetY = calculateOffsetY();

                GUI.DrawTexture(new Rect(screenPos.x - width / 2f, Screen.height - screenPos.y - (height + offsetY), width, height), iconTexture);
            }
            if (flashIcon != null) {
                Texture2D iconTexture = Resources.Load<Texture2D>("Graphics/weathericons/" + flashIcon);
                Vector3 screenPos = Game.instance.objectToScreenPos(gameObject, CameraHandler.currentRenderCamera);
                float width = iconTexture.width;
                float height = iconTexture.height;


                Color color = GUI.color;
                color.a = this.flashIconOpacity;
                GUI.color = color;
                GUI.DrawTexture(new Rect(screenPos.x - width / 2f, Screen.height - screenPos.y - height / 2f, width, height), iconTexture);
                color.a = 1f;
                GUI.color = color;
            }
        }
    }
}
