using System.Collections.Generic;
using UnityEngine;

public class AchievementUpdater : MonoBehaviour {

    private static float ACHIEVEMENT_HEIGHT_WITH_MARGIN = 44f;
    public GameObject achievementTemplate;
    private List<GameObject> shownAchievements = new List<GameObject>();

    public void refresh() {
        clearOldAchievements();

		List<Tuple3<string, string, int>> fulfilledAchievements = Achievements.GetFulfilledAchievements ();
		List<Tuple3<string, string, int>> unfulfilledAchievements = Achievements.GetNonSecretUnfulfilledAchievements ();
		List<Tuple3<string, string, int>> secretAchievements = Achievements.GetSecretUnfulfilledAchievements ();

        float row = 0;
        addAchievements(fulfilledAchievements, "fullfilled", ref row);
        addAchievements(unfulfilledAchievements, "unfulfilled", ref row);
        addAchievements(secretAchievements, "secret", ref row);
    }

    private void addAchievements(System.Collections.Generic.List<Tuple3<string, string, int>> achievements, string type, ref float row) {
        Transform contentTransform = transform.Find("Viewport/Content");
        foreach (Tuple3<string, string, int> achievement in achievements) {
            GameObject achievementObj = Instantiate (achievementTemplate, contentTransform, false) as GameObject;
            achievementObj.transform.localPosition += new Vector3 (0f, row * -ACHIEVEMENT_HEIGHT_WITH_MARGIN, 0f);
            achievementObj.name = "Achievement #" + row;
            AchievementInfo achievementInfo = achievementObj.GetComponent<AchievementInfo> ();
            achievementInfo.setMetaData (achievement, type);
            if (row == 0) {
                achievementInfo.hideLine();
            }
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
