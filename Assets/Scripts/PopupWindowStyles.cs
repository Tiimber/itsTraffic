using UnityEngine;
using System.Collections;

public class PopupWindowStyles : MonoBehaviour {

	protected GUIStyle borderStyle;
	protected GUIStyle windowStyle;
	protected GUIStyle titleStyle;
	protected GUIStyle subtitleStyle;
	protected GUIStyle subtitleStyleRight;

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
		if (subtitleStyleRight == null) {
			subtitleStyleRight = new GUIStyle ();
			subtitleStyleRight.fontStyle = FontStyle.Bold;
			subtitleStyleRight.fontSize = 18;
			subtitleStyleRight.normal.textColor = Color.white;
			subtitleStyleRight.alignment = TextAnchor.MiddleRight;
		}
	}

}
