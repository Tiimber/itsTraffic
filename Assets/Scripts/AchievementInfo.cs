using UnityEngine;
using UnityEngine.UI;

public class AchievementInfo : MonoBehaviour {

    private static Color fulfilledColor = new Color(0.28f, 0.875f, 0.16f);
//    private static Color unfulfilledColor = new Color(0.87f, 0.61f, 0.16f);
    private static Color unfulfilledColor = new Color(0.77f, 0.77f, 0.77f);
    private static Color secretColor = new Color(0.77f, 0.77f, 0.77f);

    public void setMetaData (string label, int points, string type) {
        // Update achievement label
		Transform labelTransform = Misc.FindDeepChild (transform, "Label");
        Text labelText = labelTransform.GetComponent<Text> ();
        labelText.text = label;

        // Update achievement points
        Transform pointsTransform = Misc.FindDeepChild (transform, "Points");
        Text pointsText = pointsTransform.GetComponentInChildren<Text> ();
        pointsText.text = "" + points;

        switch (type) {
            case "unfulfilled":
                labelText.color = unfulfilledColor;
                pointsText.color = unfulfilledColor;
            	break;
            case "secret":
                labelText.color = secretColor;
                pointsText.color = secretColor;
            	break;
            case "fulfilled":
            default:
                labelText.color = fulfilledColor;
                pointsText.color = fulfilledColor;
	            break;
        }
    }
}
