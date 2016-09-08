using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class BriefPopup : PopupWindowStyles, IPubSub {

	private bool show = false;
	private Vector2 scrollPosition = Vector2.zero;
	private Rect windowRect;
	private Level level;

	private const float FOOTER_HEIGHT = 40f; // TODO - Set this to a good value

	void Start () {
		PubSub.subscribe ("brief:display", this);
	}

	void Update () {
		if (show) {
			// TODO - Touch?
			if (Input.anyKey) {
				hideBrief ();
				Game.instance.freezeGame (false);
			}
		}
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "brief:display") {
			Level level = (Level) data;
			showBrief (level);
			return PROPAGATION.STOP_IMMEDIATELY;
		}
		return PROPAGATION.DEFAULT;
	}

	public void showBrief(Level level) {
		this.level = level;
		scrollPosition = Vector2.zero;
		show = true;
	}

	public void hideBrief() {
		show = false;
	}

	public bool isShown() {
		return show;
	}

	public Rect getWindowRect() {
		return windowRect;
	}

	public new void OnGUI() {
		base.OnGUI ();

		if (show) {
			float popupWidth = Screen.width / 2f;
			float popupHeight = Screen.height / 2f;
//			float contentHeight = Screen.height * 2;//information.Count * 25; // TODO - calculate precise information height
			windowRect = new Rect (Screen.width / 2f - popupWidth / 2f, Screen.height / 2f - popupHeight / 2f, popupWidth, popupHeight);
			Rect viewRect = new Rect (0, 0, popupWidth, popupHeight);

			GUI.Box (windowRect, "", windowStyle);

			using (var popupScrollScope = new GUI.ScrollViewScope (windowRect, Vector2.zero, viewRect)) {
				float y = 0;
				printTitle (level.name, ref y, popupWidth, titleStyle);

				float briefHeight = popupHeight - (y + FOOTER_HEIGHT); 
				Rect briefRect = new Rect (0, y, popupWidth, briefHeight);
				Rect briefViewRect = new Rect (0, 0, popupWidth, briefHeight);
				using (var scrollScope = new GUI.ScrollViewScope (briefRect, scrollPosition, briefViewRect)) {
					// TODO - Fix scroll
					scrollPosition = scrollScope.scrollPosition;

					GUI.Label (new Rect (5f, 0, popupWidth - 5f, 1200f), level.brief.Replace("\\n", Environment.NewLine)); // TODO - Calculate lines
				}

				// Time of day
				GUI.Label (new Rect(5f, popupHeight - (FOOTER_HEIGHT - 5f), popupWidth / 3f, FOOTER_HEIGHT - 10f), "Time: " + level.timeOfDay, subtitleStyle);
				// Previous stars
				// TODO - When stored result - draw stars
				// Random seed
				GUI.Label (new Rect(popupWidth * 2f / 3f - 5f, popupHeight - (FOOTER_HEIGHT - 5f), popupWidth / 3f, FOOTER_HEIGHT - 10f), level.randomSeedStr, subtitleStyleRight);
			}
		}
	}

	private void printTitle (string title, ref float y, float windowWidth, GUIStyle titleStyle) {
		GUI.Label (new Rect (5f, 5f + y, -5f + windowWidth, titleStyle.fontSize + 6f), title, titleStyle);
		y += titleStyle.fontSize + 6f;
	}
}