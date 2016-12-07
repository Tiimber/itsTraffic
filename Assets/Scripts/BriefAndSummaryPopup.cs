using UnityEngine;
using System;

public class BriefAndSummaryPopup : PopupWindowStyles, IPubSub {

	private bool show = false;
	private Vector2 scrollPosition = Vector2.zero;
	private Rect windowRect;
	private Level level;
	private Summary summary;

	private const float FOOTER_HEIGHT = 40f; // TODO - Set this to a good value

	void Start () {
		PubSub.subscribe ("brief:display", this);
		PubSub.subscribe ("summary:display", this);
	}

	void Update () {
		if (show) {
			// TODO - Touch?
            // Level brief closes on any input
            // Summary only closes on button press
			if (Input.anyKey && level != null) {
				hide ();
				Game.instance.freezeGame (false);
                PubSub.publish("clock:start");
			}
		}
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "brief:display") {
			Level level = (Level) data;
			showBrief (level);
			return PROPAGATION.STOP_IMMEDIATELY;
		} else if (message == "summary:display") {
			Summary summary = (Summary) data;
            showSummary (summary);
            return PROPAGATION.STOP_IMMEDIATELY;
        }
		return PROPAGATION.DEFAULT;
	}

	public void showBrief(Level level) {
		this.level = level;
        this.summary = null;
		scrollPosition = Vector2.zero;
		show = true;
	}

	public void showSummary(Summary summary) {
		this.summary = summary;
        this.level = null;
		scrollPosition = Vector2.zero;
		show = true;
	}

	public void hide() {
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
			windowRect = new Rect (Screen.width / 2f - popupWidth / 2f, Screen.height / 2f - popupHeight / 2f, popupWidth, popupHeight);
			Rect viewRect = new Rect (0, 0, popupWidth, popupHeight);

			GUI.Box (windowRect, "", windowStyle);

			using (GUI.ScrollViewScope popupScrollScope = new GUI.ScrollViewScope (windowRect, Vector2.zero, viewRect, SCROLLBAR_NOT_VISIBLE, SCROLLBAR_NOT_VISIBLE)) {
                if (level != null) {
	                // Brief
                    OnGUIBrief(popupWidth, popupHeight);
                } else if (summary != null) {
                    // Summary
                    OnGUISummary(popupWidth, popupHeight);
                }
			}
		}
	}

    private void OnGUISummary(float popupWidth, float popupHeight) {
        float y = 0;

        printTitle (summary.name, ref y, popupWidth, titleStyle);

        float summaryHeight = popupHeight - (y + FOOTER_HEIGHT);
        Rect summaryRect = new Rect (0, y, popupWidth, summaryHeight);
        float summaryContentHeight = getSummaryContentHeight(popupWidth);
        Rect summaryViewRect = new Rect (0, 0, popupWidth, summaryContentHeight);
        using (GUI.ScrollViewScope scrollScope = new GUI.ScrollViewScope (summaryRect, scrollPosition, summaryViewRect, SCROLLBAR_NOT_VISIBLE, VERTICAL_SCROLLBAR_VISIBLE)) {
            scrollPosition = scrollScope.scrollPosition;

            drawSummaryInnerContents(popupWidth);
        }

        // Footer - Main Menu, Retry(, Next Mission)
        float buttonWidth = 150f;
        float buttonHeight = FOOTER_HEIGHT - 10f;
        float middleX = popupWidth / 2f;
        if (GUI.Button(new Rect(middleX - middleX / 2f - buttonWidth / 2f, popupHeight - FOOTER_HEIGHT / 2f - buttonHeight / 2f, buttonWidth, buttonHeight), "Main Menu")) {
            Game.instance.exitLevel();
            hide();
        }
        if (GUI.Button(new Rect(middleX - buttonWidth / 2f, popupHeight - FOOTER_HEIGHT / 2f - buttonHeight / 2f, buttonWidth, buttonHeight), "Retry")) {
            Game.instance.restartLevel();
            hide();
        }
        if (GUI.Button(new Rect(middleX + middleX / 2f - buttonWidth / 2f, popupHeight - FOOTER_HEIGHT / 2f - buttonHeight / 2f, buttonWidth, buttonHeight), "Next Mission")) {
            // TODO - Play next mission
            hide();
        }
    }

    private float getSummaryContentHeight(float popupWidth) {
        return drawSummaryInnerContents(popupWidth, true);
    }

    private float drawSummaryInnerContents(float popupWidth, bool onlyCalculation = false) {
        float pointY = 0;

        printTitle("Points:", ref pointY, popupWidth, subtitleStyle, onlyCalculation);

        int points = summary.pointsBefore;

        // Present included points
        foreach (PointCalculator.Point point in summary.alreadyIncluded) {
            printPointData(point, ref pointY, popupWidth, onlyCalculation);
        }

        // Present each "not yet included" points
        foreach (PointCalculator.Point point in summary.notYetIncluded) {
            printPointData(point, ref pointY, popupWidth, onlyCalculation);
            points += point.calculatedValue;
        }

        // Present total points
        PointCalculator.Point totalPoints = new PointCalculator.Point(PointCalculator.Point.TYPE_TOTAL_POINTS, "Total", points);
        if (!onlyCalculation) {
            EditorGUIx.DrawLine (new Vector2 (5f, 4f + pointY), new Vector2 (popupWidth - 5f, 4f + pointY), 2f);
        }
        pointY += 7f;
        printPointData(totalPoints, ref pointY, popupWidth, onlyCalculation);

        if (!summary.failedMission) {
            drawStars(summary.numberOfStars, ref pointY, popupWidth, onlyCalculation: onlyCalculation);
        }

        if (summary.newHighscore) {
            drawHighscoreStamp(ref pointY, popupWidth, onlyCalculation);
            if (!onlyCalculation) {
                if (!summary.havePlayedHighscoreSound) {
                    summary.havePlayedHighscoreSound = true;
                    GenericSoundEffects.playHighscoreSerenade();
                }
            }
        }

        if (summary.failedMission) {
            drawFailedStamp(ref pointY, popupWidth, onlyCalculation);
            if (!onlyCalculation) {
                if (!summary.havePlayedFailedSound) {
                    summary.havePlayedFailedSound = true;
                    GenericSoundEffects.playFailSound();
                }
            }
        }

        return pointY;
    }

    private void OnGUIBrief(float popupWidth, float popupHeight) {
        float y = 0;

        printTitle (level.name, ref y, popupWidth, titleStyle);

        float briefHeight = popupHeight - (y + FOOTER_HEIGHT);
        Rect briefRect = new Rect (0, y, popupWidth, briefHeight);
        float briefTextHeight = getBriefTextHeight();
        Rect briefViewRect = new Rect (0, 0, popupWidth, briefTextHeight);
        using (GUI.ScrollViewScope scrollScope = new GUI.ScrollViewScope (briefRect, scrollPosition, briefViewRect, SCROLLBAR_NOT_VISIBLE, VERTICAL_SCROLLBAR_VISIBLE)) {
            scrollPosition = scrollScope.scrollPosition;
            GUI.Label (new Rect (5f, 0, popupWidth - 5f, briefTextHeight), level.brief.Replace("\\n", Environment.NewLine), textStyle);
        }

        // Time of day
        GUI.Label (new Rect(5f, popupHeight - (FOOTER_HEIGHT - 5f), popupWidth / 3f, FOOTER_HEIGHT - 10f), "Time: " + level.timeOfDay, subtitleStyle);

        // Previous stars
        int stars = PlayerPrefsData.GetLevelStars(level.id);
        float starsY = popupHeight - starFilled.height;
        drawStars(stars, ref starsY, popupWidth, -5f);

        // Random seed
        GUI.Label (new Rect(popupWidth * 2f / 3f - 5f, popupHeight - (FOOTER_HEIGHT - 5f), popupWidth / 3f, FOOTER_HEIGHT - 10f), level.randomSeedStr, subtitleStyleRight);
    }

    private float getBriefTextHeight() {
        string[] briefSplit = level.brief.Split (new String[]{"\\n"}, StringSplitOptions.None);
        int numberOfLines = briefSplit.Length;
        float requiredHeight = numberOfLines * textStyle.lineHeight;
//        Debug.Log("Lines: " + numberOfLines);
//        Debug.Log("Fontsize: " + textStyle.fontSize);
//        Debug.Log("Fixedheight: " + textStyle.fixedHeight);
//        Debug.Log("Height needed: " + requiredHeight);
        return requiredHeight;
    }

	private void printTitle (string title, ref float y, float windowWidth, GUIStyle titleStyle, bool onlyCalculation = false) {
        if (!onlyCalculation) {
            GUI.Label (new Rect (5f, 5f + y, -5f + windowWidth, titleStyle.fontSize + 6f), title, titleStyle);
        }
		y += titleStyle.fontSize + 6f;
	}

    private void printPointData(PointCalculator.Point point, ref float y, float windowWidth, bool onlyCalculation) {

        if (!onlyCalculation) {
            float columnWidth = (windowWidth - 2f * 5f) / 5f;

            long objectiveId = point.id;
            if (objectiveId == -1L) {
                bool hasThreshold = point.threshold != -1L;

                // Point label
                GUI.Label (new Rect (5f, y, 2f * columnWidth, textStyle.fontSize + 6f), point.label, textStyle);

                // Amount
                bool hasOwnAmount = point.hasOwnAmountCalculation ();
                float ownAmount = point.getOwnAmount ();
                if (hasOwnAmount) {
                    if (ownAmount != -1f) {
                        GUI.Label (new Rect (5f + 2f * columnWidth, y, columnWidth, textStyleRight.fontSize + 6f), Misc.maxDecimals (ownAmount) + "x ", textStyleRight);
                    }
                } else if (hasThreshold) {
                    GUI.Label (new Rect (5f + 2f * columnWidth, y, columnWidth, textStyleRight.fontSize + 6f), Mathf.FloorToInt (point.amount / point.threshold) + "x ", textStyleRight);
                } else {
                    GUI.Label (new Rect (5f + 2f * columnWidth, y, columnWidth, textStyleRight.fontSize + 6f), Misc.maxDecimals (point.amount) + "x ", textStyleRight);
                }

                // Point per amount
                if (ownAmount != -1f) {
                    GUI.Label (new Rect (5f + 2f * (2 * columnWidth), y, columnWidth, textStyleRight.fontSize + 6f), "" + point.value, textStyle);
                }

                // Total points
                GUI.Label (new Rect (windowWidth - 5f - columnWidth, y, columnWidth, textStyleRight.fontSize + 6f), "" + point.calculatedValue, textStyleRight);
            } else {
                // Objective label
                string objectiveLabel = point.label;
                if (objectiveLabel == null) {
                    objectiveLabel = summary.objectives.get (objectiveId).label;
                }
                GUI.Label (new Rect (5f, y, 4f * columnWidth, textStyle.fontSize + 6f), objectiveLabel, textStyle);

                // Total points
                GUI.Label (new Rect (windowWidth - 5f - columnWidth, y, columnWidth, textStyleRight.fontSize + 6f), "" + point.calculatedValue, textStyleRight);
            }
        }

        y+= textStyle.fontSize + 6f;
    }

    private void drawHighscoreStamp(ref float y, float windowWidth, bool onlyCalculation) {
        float center = windowWidth / 2f;
        Misc.Size imageSize = Misc.getImageSize(highscoreStamp.width, highscoreStamp.height, 617, 230);
        float imageWidth = imageSize.width;
        float imageHeight = imageSize.height;
        if (!onlyCalculation) {
            GUI.Label(new Rect(center - imageWidth / 2f, 20f + y, imageWidth, imageHeight), highscoreStamp);
        }
        y += 20f + imageHeight + 5f;
    }

    private void drawFailedStamp(ref float y, float windowWidth, bool onlyCalculation) {
        float center = windowWidth / 2f;
        Misc.Size imageSize = Misc.getImageSize(failedStamp.width, failedStamp.height, 426, 230);
        float imageWidth = imageSize.width;
        float imageHeight = imageSize.height;
        if (!onlyCalculation) {
            GUI.Label(new Rect(center - imageWidth / 2f, 20f + y, imageWidth, imageHeight), failedStamp);
        }
        y += 20f + imageHeight + 5f;
    }

    private void drawStars(int number, ref float y, float windowWidth, float marginY = 20f, bool onlyCalculation = false) {
        int numberInclusiveEmpty = 3;

        float margin = 10f;
        float starWidth = starFilled.width;
        float starHeight = starFilled.height;

        if (!onlyCalculation) {
            float center = windowWidth / 2f;
            float fullDrawWidth = numberInclusiveEmpty * starWidth + (numberInclusiveEmpty - 1) * margin;
            float firstStarLeft = center - fullDrawWidth / 2f;
            for (int i = 0; i < numberInclusiveEmpty; i++) {
                float left = firstStarLeft + i * (starWidth + margin);
                GUI.Label(new Rect(left, marginY + y, starWidth, starHeight), i < number ? starFilled : starOutlined);
            }

            if (number > 3) {
                float left = firstStarLeft + fullDrawWidth + margin / 2f;
                GUI.Label(new Rect(left, marginY + y, starWidth / 2f, starHeight / 2f), starFilled);

                if (number > 4) {
                    GUI.Label(new Rect(left, marginY + y + starHeight / 2f, starWidth / 2f, starHeight / 2f), starFilled);
                }
            }
        }

        y += marginY + starHeight + 5f;
    }
}