using System;
using System.Collections.Generic;
using UnityEngine;

public class InformationWindow : PopupWindowStyles, IPubSub {

	private bool show = false;
	private bool follow = false;
	private Vector2 scrollPosition = Vector2.zero;
	private InformationBase informationObject;
	private List<KeyValuePair<string, object>> information;
	private Rect windowRect;
    private Dictionary<Rect, InformationBase> clickAreas = new Dictionary<Rect, InformationBase>();

    void Start () {
		PubSub.subscribe ("Click", this, 20);
        PubSub.subscribe ("InformationWindow:hide", this);
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "Click") {
            // Check if a link was clicked inside of an already shown informationWindow
            Vector2 clickPosScreen = Misc.getScreenPos((Vector3) data);
            foreach (KeyValuePair<Rect, InformationBase> clickArea in clickAreas) {
                if (Misc.isInside(clickPosScreen, clickArea.Key)) {
                    scrollScreenToAndShowInformationFor(clickArea.Value);
                    return PROPAGATION.STOP_IMMEDIATELY;
                }
            }

            // Check for clicking to get information on actual informationBase-objects
            Vector2 clickPos = (Vector3) data;
			InformationBase[] informationBaseObjects = FindObjectsOfType<InformationBase> ();

			InformationBase clickedInformationBase = null;
			CircleTouch clickedCircleTouch = null;

			foreach (InformationBase informationBaseObject in informationBaseObjects) {
				if (!informationBaseObject.passive) {
                    float thresholdSurroundingArea = 0.1f * 3f; // Click 0.1 (vehicle length) multiplied by three
                    if (informationBaseObject.type == InformationBase.TYPE_POI) {
                        thresholdSurroundingArea = 0.07f; // Click area around POI is smaller
                    }
					CircleTouch informationObjectTouch = new CircleTouch (informationBaseObject.transform.position, thresholdSurroundingArea);
					if (informationObjectTouch.isInside (clickPos)) {

						if (informationObjectTouch.isCloser (clickPos, clickedCircleTouch)) {
							clickedCircleTouch = informationObjectTouch;
							clickedInformationBase = informationBaseObject;
						}
					}
				}
			}

			if (clickedInformationBase != null) {
				showInformation (clickedInformationBase);
				return PROPAGATION.STOP_IMMEDIATELY;
			}

		} else if (message == "InformationWindow:hide") {
            hideInformation();
        }
		return PROPAGATION.DEFAULT;
	}

	public void showInformation(InformationBase info) {
		scrollPosition = Vector2.zero;
		informationObject = info;
		information = info.getInformation();
		scrollToInformationBase(informationObject);
		follow = informationObject.GetType() == typeof(InformationHuman);
		show = true;
	}

	public void hideInformation() {
        switchCamera (true);
        if (informationObject != null) {
            informationObject.disposeInformation ();
        }
		show = false;
        unregisterClickAreas();
	}

	public bool isShown() {
		return show;
	}

	public bool isFollowing() {
		return follow;
	}

	public void stopFollow() {
		follow = false;
	}

	public Rect getWindowRect() {
		return windowRect;
	}

	public new void OnGUI() {
		base.OnGUI ();

		if (show) {
			float informationWindowWidth = Screen.width / 5f;
			float contentHeight = calculateContentHeight(informationWindowWidth); // TODO - calculate precise information height
			windowRect = new Rect (0, 0, informationWindowWidth, Screen.height);
			Rect viewRect = new Rect (0, 0, Screen.width / 5f + 10, contentHeight);

			GUI.Box (windowRect, "", windowStyle);

			using (GUI.ScrollViewScope scrollScope = new GUI.ScrollViewScope (windowRect, scrollPosition, viewRect, SCROLLBAR_NOT_VISIBLE, VERTICAL_SCROLLBAR_VISIBLE)) {
				scrollPosition = scrollScope.scrollPosition;

				float y = 0;
				foreach (KeyValuePair<string, object> infoRow in information) {
					printKeyValuePair (infoRow, ref y, informationWindowWidth);
				}
				if (GUI.Button (new Rect (informationWindowWidth - 35f, 5f, 20f, 20f), "X")) {
					hideInformation ();
				}
                // TODO - If we should keep this, make some text/image
                if (informationObject != null && informationObject.GetType() == typeof(InformationVehicle) && !informationObject.GetComponent<Vehicle>().destroying) {
                    if (GUI.Button (new Rect (informationWindowWidth - 35f, 35f, 20f, 20f), "")) {
						switchCamera ();
					}
                }

				if (follow && informationObject != null && informationObject.gameObject != null) {
					scrollToInformationBase (informationObject);
				}
			}
        }
	}

    private float calculateContentHeight(float informationWindowWidth) {
        float y = 0;
        foreach (KeyValuePair<string, object> infoRow in information) {
            // TODO - Don't really like this one, "onlyCalculation"
            printKeyValuePair (infoRow, ref y, informationWindowWidth, onlyCalculation: true);
        }
        return y;
    }

	private void printKeyValuePair (KeyValuePair<string, object> info, ref float y, float windowWidth, bool onlyCalculation = false) {
		if (info.Value == null) {
            return;
        }

		float itemHeight = 25f;

		Type type = info.Value.GetType ();
		if (type == typeof(int)) {
            if (!onlyCalculation) {
                GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
				GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), string.Format ("{0}", (int)info.Value));
            }
		} else if (type == typeof(DateTime)) {
            if (!onlyCalculation) {
				GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
				GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), string.Format ("{0:MMM yyyy}", (DateTime)info.Value));
			}
        } else if (type == typeof(InformationHuman)) {
            if (!onlyCalculation) {
				EditorGUIx.DrawLine (new Vector2 (5f, 5f + y), new Vector2 (5f + windowWidth - 10f, 5f + y), 2f);
			}
			y += 7f;

			printTitle (info.Key, ref y, windowWidth, subtitleStyle, onlyCalculation);

			List<KeyValuePair<string, object>> information = ((InformationHuman)info.Value).getInformation (onlyName: informationObject.GetType() != typeof(InformationVehicle));
			foreach (KeyValuePair<string, object> infoRow in information) {
				printKeyValuePair (infoRow, ref y, windowWidth, onlyCalculation);
			}

			return;
        } else if (type == typeof(InformationPOI)) {
			if (!onlyCalculation) {
				EditorGUIx.DrawLine (new Vector2 (5f, 5f + y), new Vector2 (5f + windowWidth - 10f, 5f + y), 2f);
			}
			y += 7f;

			printTitle (info.Key, ref y, windowWidth, subtitleStyle, onlyCalculation);

			List<KeyValuePair<string, object>> information = ((InformationPOI)info.Value).getInformation (onlyName: true);
			foreach (KeyValuePair<string, object> infoRow in information) {
				printKeyValuePair (infoRow, ref y, windowWidth, onlyCalculation);
			}

			return;
		} else if (type == typeof(InformationBase.InformationLink)) {
            if (!onlyCalculation) {
                GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
				Rect linkedArea = new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f);
				GUI.Label (linkedArea, "" + ((InformationBase.InformationLink)info.Value).name, linkStyle);
                registerClickArea(linkedArea, ((InformationBase.InformationLink)info.Value).informationBase);
            }
		} else if (type == typeof(List<InformationHuman>)) {
			List<InformationHuman> value = (List<InformationHuman>) info.Value;
			int count = value.Count;
			for (int i = 0; i < count; i++) {
				KeyValuePair<string, object> listEntry = new KeyValuePair<string, object>(info.Key + (count > 1 ? " " + (i+1) : ""), value[i]);
				printKeyValuePair (listEntry, ref y, windowWidth, onlyCalculation);
			}
		} else {
			// Strings (and the "rest")
			if (y == 0) {
				printTitle (info.Value.ToString(), ref y, windowWidth, titleStyle, onlyCalculation);
			} else {
                if (!onlyCalculation) {
					GUI.Label (new Rect (5f, 5f + y, windowWidth / 3f, 25f), info.Key + ":");
					GUI.Label (new Rect (5f + windowWidth / 3f + 5f, 5f + y, -5f + windowWidth / 3f * 2f - 10f, 25f), "" + info.Value);
				}
			}
		}

		y += itemHeight;
	}

	private void printTitle (string title, ref float y, float windowWidth, GUIStyle titleStyle, bool onlyCalculation) {
        if (!onlyCalculation) {
			GUI.Label (new Rect (5f, 5f + y, -5f + windowWidth, titleStyle.fontSize + 6f), title, titleStyle);
		}
		y += titleStyle.fontSize + 6f;
	}

    private void switchCamera(bool forceDetach = false) {
        if (informationObject != null) {
            Vehicle vehicle = informationObject.GetComponent<Vehicle>();
            if (vehicle != null && !vehicle.switchingCameraInProgress) {
                if (!vehicle.isOwningCamera && !forceDetach) {
					vehicle.grabCamera();
				} else {
					Vehicle.detachCurrentCamera();
				}
            }
        }
    }

	public void registerClickArea(Rect linkedArea, InformationBase informationBase) {
        if (!clickAreas.ContainsKey(linkedArea)) {
            clickAreas.Add(linkedArea, informationBase);
        }
	}

    public void unregisterClickAreas() {
        clickAreas.Clear();
    }

	private void scrollScreenToAndShowInformationFor(InformationBase informationBase) {
        hideInformation();
        showInformation(informationBase);
	}

	private void scrollToInformationBase(InformationBase informationBase) {
        CameraHandler.MoveTo(informationBase.gameObject, 0.2f);
	} 

}