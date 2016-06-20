using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class InformationWindow : MonoBehaviour, IPubSub {

	private bool show = false;
	private Vector2 scrollPosition = Vector2.zero;
	private InformationBase informationObject;
	private List<KeyValuePair<string, object>> information;
	private GUIStyle borderStyle;
	private GUIStyle windowStyle;
	private GUIStyle titleStyle;
	private GUIStyle subtitleStyle;
	private Rect windowRect;

	void Start () {
		PubSub.subscribe ("Click", this, 20);
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "Click") {
			Vector2 clickPos = (Vector3) data;
			InformationBase[] informationBaseObjects = FindObjectsOfType<InformationBase> ();
			foreach (InformationBase informationBaseObject in informationBaseObjects) {
				if (!informationBaseObject.passive) {
					CircleTouch informationObjectTouch = new CircleTouch (informationBaseObject.transform.position, 0.1f * 3f); // Click 0.1 (vehicle length) multiplied by three
					if (informationObjectTouch.isInside (clickPos)) {
						showInformation (informationBaseObject);
						return PROPAGATION.STOP_IMMEDIATELY;
					}
				}
			}
		}
		return PROPAGATION.DEFAULT;
	}

	public void showInformation(InformationBase info) {
		scrollPosition = Vector2.zero;
		informationObject = info;
		information = info.getInformation();
		show = true;
	}

	public void hideInformation() {
		informationObject.disposeInformation ();
		show = false;
	}

	public bool isShown() {
		return show;
	}

	public Rect getWindowRect() {
		return windowRect;
	}

	public void OnGUI() {
		if (windowStyle == null) {
			windowStyle = new GUIStyle(GUI.skin.box);
			windowStyle.normal.background = Misc.MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.8f));
		}
		if (borderStyle == null) {
			borderStyle = new GUIStyle(GUI.skin.box);
			borderStyle.normal.background = Misc.MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));
		}
		if (titleStyle == null) {
			titleStyle = new GUIStyle();
			titleStyle.fontStyle = FontStyle.Bold;
			titleStyle.fontSize = 24;
			titleStyle.normal.textColor = Color.white;
		}
		if (subtitleStyle == null) {
			subtitleStyle = new GUIStyle();
			subtitleStyle.fontStyle = FontStyle.Bold;
			subtitleStyle.fontSize = 18;
			subtitleStyle.normal.textColor = Color.white;
		}

		if (show) {
			float informationWindowWidth = Screen.width / 5f;
			float contentHeight = Screen.height * 2;//information.Count * 25; // TODO - calculate precise information height
			windowRect = new Rect (0, 0, informationWindowWidth, Screen.height);
			Rect viewRect = new Rect (0, 0, Screen.width / 5f, contentHeight);

			GUI.Box (windowRect, "", windowStyle);

			using (var scrollScope = new GUI.ScrollViewScope (windowRect, scrollPosition, viewRect)) {
				scrollPosition = scrollScope.scrollPosition;

				float y = 0;
				foreach (KeyValuePair<string, object> infoRow in information) {
					printKeyValuePair (infoRow, ref y, informationWindowWidth);
				}
//				GUI.Button (new Rect(20, 20, 100, 40), "Button");
			}; 
		}
	}

	private void printKeyValuePair (KeyValuePair<string, object> info, ref float y, float windowWidth) {
		float itemHeight = 25f;

		Type type = info.Value.GetType ();
		if (type == typeof(int)) {
			GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
			GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), string.Format ("{0}", (int)info.Value));
		} else if (type == typeof(DateTime)) {
			GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
			GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), String.Format ("{0:MMM yyyy}", (DateTime)info.Value));
		} else if (type == typeof(InformationHuman)) {
			EditorGUIx.DrawLine (new Vector2 (5f, 5f + y), new Vector2 (5f + windowWidth - 10f, 5f + y), 2f);

			y += 7f;

			printTitle (info.Key, ref y, windowWidth, subtitleStyle);

			List<KeyValuePair<string, object>> information = ((InformationHuman)info.Value).getInformation ();
			foreach (KeyValuePair<string, object> infoRow in information) {
				printKeyValuePair (infoRow, ref y, windowWidth);
			}

			return;
		} else if (type == typeof(List<InformationHuman>)) {
			List<InformationHuman> value = (List<InformationHuman>) info.Value;
			int count = value.Count;
			for (int i = 0; i < count; i++) {
				KeyValuePair<string, object> listEntry = new KeyValuePair<string, object>(info.Key + (count > 1 ? " " + (i+1) : ""), value[i]);
				printKeyValuePair (listEntry, ref y, windowWidth);
			}
		} else {
			// Strings (and the "rest")
			if (y == 0) {
				printTitle (info.Value.ToString(), ref y, windowWidth, titleStyle);
			} else {
				GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
				GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), "" + info.Value);
			}
		}

		y += itemHeight;
	}

	private void printTitle (string title, ref float y, float windowWidth, GUIStyle titleStyle) {
		GUI.Label (new Rect (5f, 5f + y, -5f + windowWidth, titleStyle.fontSize + 6f), title, titleStyle);
		y += titleStyle.fontSize + 6f;
	}
}