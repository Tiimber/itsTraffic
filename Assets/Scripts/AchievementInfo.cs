using UnityEngine;
using UnityEngine.UI;

public class AchievementInfo : MonoBehaviour {

    private static Color fulfilledColor = new Color(0.28f, 0.875f, 0.16f);
//    private static Color unfulfilledColor = new Color(0.87f, 0.61f, 0.16f);
    private static Color unfulfilledColor = new Color(0.77f, 0.77f, 0.77f);
    private static Color secretColor = new Color(0.77f, 0.77f, 0.77f);

    public void setMetaData (Tuple3<string, string, int> achievementData, string type) {
        // Update achievement label
		Transform labelTransform = Misc.FindDeepChild (transform, "Label");
        Text labelText = labelTransform.GetComponent<Text> ();
        labelText.text = achievementData.First;

        // Update achievement sublabel
		Transform subLabelTransform = Misc.FindDeepChild (transform, "SubLabel");
        Text subLabelText = subLabelTransform.GetComponent<Text> ();
        subLabelText.text = achievementData.Second;

        // Update achievement points
        Transform pointsTransform = Misc.FindDeepChild (transform, "Points");
        Text pointsText = pointsTransform.GetComponentInChildren<Text> ();
        pointsText.text = "" + achievementData.Third;

        switch (type) {
            case "unfulfilled":
                labelText.color = unfulfilledColor;
                subLabelText.color = unfulfilledColor;
                pointsText.color = unfulfilledColor;
            	break;
            case "secret":
                labelText.color = secretColor;
                subLabelText.color = secretColor;
                pointsText.color = secretColor;
            	break;
            case "fulfilled":
            default:
                labelText.color = fulfilledColor;
                subLabelText.color = fulfilledColor;
                pointsText.color = fulfilledColor;
	            break;
        }
    }
}
