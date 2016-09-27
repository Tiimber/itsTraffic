using UnityEngine;
using System.Collections;

public class PopupWindowStyles : MonoBehaviour {

	protected GUIStyle borderStyle;
	protected GUIStyle windowStyle;
	protected GUIStyle titleStyle;
	protected GUIStyle subtitleStyle;
	protected GUIStyle subtitleStyleRight;
	protected GUIStyle textStyle;
	protected GUIStyle textStyleRight;

    protected Texture2D starFilled;
    protected Texture2D starOutlined;
    protected Texture2D highscoreStamp;

    void Awake() {
        starFilled = Resources.Load<Texture2D>("Graphics/filled_star");
        starOutlined = Resources.Load<Texture2D>("Graphics/outlined_star");
        highscoreStamp = Resources.Load<Texture2D>("Graphics/highscore_stamp");
    }

    public void OnGUI() {
		if (windowStyle == null) {
			windowStyle = new GUIStyle (GUI.skin.box);
			windowStyle.normal.background = Misc.MakeTex (2, 2, new Color (0.3f, 0.3f, 0.3f, 0.8f));
		}
		if (borderStyle == null) {
			borderStyle = new GUIStyle (GUI.skin.box);
			borderStyle.normal.background = Misc.MakeTex (2, 2, new Color (0.1f, 0.1f, 0.1f, 0.9f));
		}
		if (titleStyle == null) {
			titleStyle = new GUIStyle ();
			titleStyle.fontStyle = FontStyle.Bold;
			titleStyle.fontSize = 24;
			titleStyle.normal.textColor = Color.white;
		}
		if (subtitleStyle == null) {
			subtitleStyle = new GUIStyle ();
			subtitleStyle.fontStyle = FontStyle.Bold;
			subtitleStyle.fontSize = 18;
			subtitleStyle.normal.textColor = Color.white;
		}
		if (textStyle == null) {
			textStyle = new GUIStyle ();
			textStyle.fontSize = 16;
			textStyle.normal.textColor = Color.white;
		}
		if (textStyleRight == null) {
			textStyleRight = new GUIStyle ();
			textStyleRight.fontSize = 16;
			textStyleRight.normal.textColor = Color.white;
            textStyleRight.alignment = TextAnchor.MiddleRight;
        }
		if (subtitleStyleRight == null) {
			subtitleStyleRight = new GUIStyle ();
			subtitleStyleRight.fontStyle = FontStyle.Bold;
			subtitleStyleRight.fontSize = 18;
			subtitleStyleRight.normal.textColor = Color.white;
			subtitleStyleRight.alignment = TextAnchor.MiddleRight;
		}
	}

}
