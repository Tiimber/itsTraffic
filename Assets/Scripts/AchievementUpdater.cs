using System.Collections.Generic;
using UnityEngine;

public class AchievementUpdater : MonoBehaviour {

    private static float ACHIEVEMENT_HEIGHT_WITH_MARGIN = 42f;
    public GameObject achievementTemplate;
    private List<GameObject> shownAchievements = new List<GameObject>();

    public void refresh() {
        clearOldAchievements();

		List<KeyValuePair<string, int>> fulfilledAchievements = Achievements.GetFulfilledAchievements ();
		List<KeyValuePair<string, int>> unfulfilledAchievements = Achievements.GetNonSecretUnfulfilledAchievements ();
		List<KeyValuePair<string, int>> secretAchievements = Achievements.GetSecretUnfulfilledAchievements ();

        float row = 0;
        addAchievements(fulfilledAchievements, "fullfilled", ref row);
        addAchievements(unfulfilledAchievements, "unfulfilled", ref row);
        addAchievements(secretAchievements, "secret", ref row);
    }

    private void addAchievements(System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>> achievements, string type, ref float row) {
        Transform contentTransform = transform.FindChild("Viewport/Content");
        foreach (KeyValuePair<string, int> achievement in achievements) {
            GameObject achievementObj = Instantiate (achievementTemplate, contentTransform, false) as GameObject;
            achievementObj.transform.localPosition += new Vector3 (0f, row * -ACHIEVEMENT_HEIGHT_WITH_MARGIN, 0f);
            achievementObj.name = "Achievement #" + row;
            AchievementInfo achievementInfo = achievementObj.GetComponent<AchievementInfo> ();
            achievementInfo.setMetaData (achievement.Key, achievement.Value, type);
            shownAchievements.Add (achievementObj);
            row = row + 1;
        }
//        contentTransform.
        RectTransform contentRectTransform = contentTransform.GetComponent<RectTransform> ();
        contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, row * ACHIEVEMENT_HEIGHT_WITH_MARGIN);
    }

    public void clearOldAchievements() {
		foreach (GameObject achievement in shownAchievements) {
			Destroy(achievement);
		}
    }
}
