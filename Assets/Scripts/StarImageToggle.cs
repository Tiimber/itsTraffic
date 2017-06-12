using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StarImageToggle : MonoBehaviour {

	private bool active = false;

    // For animation of new stars
    private static float animationDuration = 0.5f;
    private static Vector3 animationStartingScale = new Vector3(2f, 2f, 1f);
    private static Vector3 animationTargetScale = new Vector3(1f, 1f, 1f);
    private static Quaternion animationStartRotation = Quaternion.Euler(0f, 0f, 180f);
    private static Quaternion animationTargetRotation = Quaternion.identity;
    private static Color animationStartColor = Color.white;

    public void setActiveState(bool active = true, bool newRecord = false) {
        bool doAnimate = newRecord && active != this.active;
		this.active = active;
		updateImage ();

        if (doAnimate) {
            Singleton<StarImageToggle>.Instance.StartCoroutine(animateActiveStar ());
        }
	}

	private void updateImage() {
		GameObject activeStar = transform.Find ("active").gameObject;
		GameObject inactiveStar = transform.Find ("inactive").gameObject;

		activeStar.SetActive (active);
		inactiveStar.SetActive (!active);

	}

	private IEnumerator animateActiveStar() {
        GameObject activeStar = transform.Find ("active").gameObject;
		Graphic graphic = activeStar.GetComponent<Graphic> ();

        Color animationColor = new Color(animationStartColor.r, animationStartColor.g, animationStartColor.b, 0f);
		graphic.color = animationColor;

        yield return waitForVisibility();

        float t = 0.0f;
        while (t < 1.0f) {
            t += Time.unscaledDeltaTime / animationDuration;

            activeStar.transform.localScale = Vector3.Lerp (animationStartingScale, animationTargetScale, t);
	        activeStar.transform.rotation = Quaternion.Lerp(animationStartRotation, animationTargetRotation, t);
            animationColor.a = Mathf.Lerp(0f, 1f, t);
            graphic.color = animationColor;
            yield return null;
        }
	}

	public IEnumerator waitForVisibility() {
        yield return null;
        while (!Game.instance.menuSystem.activeSelf) {
            yield return new WaitForSecondsRealtime(0.1f);
        }
	}

}
