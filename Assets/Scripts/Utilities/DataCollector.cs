using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System;

public class DataCollector : MonoBehaviour {

    private const string ACCUMULATED_DATA_PREFIX = "AccumulatedData:";
	private bool output = false;

	private static float lastDataDiff = 0f;
	private static float dataDiffThreshold = 1f;
	private static bool diffCollected = false;

	private static bool touched = false;
	public static Dictionary<string, InnerData> Data = new Dictionary<string, InnerData>();
	private static Dictionary<string, InnerData> CopyData;
	private static Dictionary<string, float> DiffData = new Dictionary<string, float>();

	private static Objectives reportObjectivesData = null;
	private static PointCalculator pointCalculator = null;

	public static void registerObjectiveReporter(Objectives objectives) {
		DataCollector.reportObjectivesData = objectives;
	}

    public static void registerPointCalculator(PointCalculator pointCalculator) {
        DataCollector.pointCalculator = pointCalculator;
    }

    private void calculateDataDiff () {
		if (CopyData != null) {
			foreach (string key in CopyData.Keys) {
				DiffData [key] = Data [key].value - CopyData [key].value;
			}
			diffCollected = true;
		}
		CopyData = Misc.DeepClone(Data);
	}

    public static float GetValue(string key) {
        if (Data.ContainsKey(key)) {
            return Data[key].value;
        }
        return 0f;
    }

	void OnGUI() {
		if (touched) {
            if (reportObjectivesData != null) {
				reportObjectivesData.reportChange ();
            }
            if (pointCalculator != null) {
                pointCalculator.reportElapsedTime (Data["Elapsed Time"].value);
            }
			touched = false;
		}

		if (Game.isMovementEnabled() && output) {
			if (Time.time > lastDataDiff + dataDiffThreshold) {
				calculateDataDiff ();
				lastDataDiff = Time.time;
			}
			int w = Screen.width, h = Screen.height;
			int labelHeight = h * 2 / 100;

			GUIStyle style = new GUIStyle ();
			style.alignment = TextAnchor.UpperLeft;
			style.fontSize = labelHeight;
			style.normal.textColor = new Color (0.9f, 0.9f, 0.9f, 1.0f);

			int lines = Data.Count;
			int i = 0;
			foreach (string label in Data.Keys) {
				Rect rect = new Rect (0, h - labelHeight - (lines - i) * labelHeight, w, labelHeight);
				string text = label + ": " + Data [label];
				if (diffCollected && DiffData.ContainsKey (label)) {
					text += " (" + Mathf.RoundToInt(DiffData [label]) + ")";
				}
				GUI.Label (rect, text, style);
				i++;
			}
		}
	}

    public static void Clear () {
        lastDataDiff = 0f;
        diffCollected = false;

        touched = false;
        Data.Clear();
        if (CopyData != null) {
            CopyData.Clear();
        }
        DiffData.Clear();

        reportObjectivesData = null;
        pointCalculator = null;
    }

	public static void InitLabel (string label) {
		Data.Add (label, new InnerData());
		touched = true;
	}

	public static void Add (string label, InnerData amount) {
		if (amount.value != 0f) {
			Add (label, amount.value);
			touched = true;
		}
	}

	public static void Add (string label, float amount) {
		if (!Data.ContainsKey (label)) {
			Data.Add (label, new InnerData(amount));
		} else {
			((InnerData)Data [label]).add (amount);
		}
		touched = true;
	}

    public static void saveStats() {
        // Save these numbers to the total stats (used for eg. achievements...)
        foreach (KeyValuePair<string, InnerData> dataEntry in Data) {
            string key = dataEntry.Key;
            string storedEntryKey = ACCUMULATED_DATA_PREFIX + key;

            float value = dataEntry.Value.value;
			if (PlayerPrefs.HasKey(storedEntryKey)) {
                value += PlayerPrefs.GetFloat(storedEntryKey);
            }
            PlayerPrefs.SetFloat(storedEntryKey, value);
        }
    }

    public static void saveWinLoseStat(string type) {
        string key = ACCUMULATED_DATA_PREFIX + "WinLose:" + type;
        int value = 1;
        if (PlayerPrefs.HasKey(key)) {
            value += PlayerPrefs.GetInt(key);
        }
        PlayerPrefs.SetInt(key, value);
    }

    public static void saveNumberOfStarsStat(int numberOfStars) {
        string key = ACCUMULATED_DATA_PREFIX + "Stars:" + numberOfStars;
        int value = 1;
        if (PlayerPrefs.HasKey(key)) {
            value += PlayerPrefs.GetInt(key);
        }
        PlayerPrefs.SetInt(key, value);
    }

    public static void saveNumberOfTotalStarsStat(int additionalStars) {
        string key = ACCUMULATED_DATA_PREFIX + "TotalStars";
        int value = additionalStars;
        if (PlayerPrefs.HasKey(key)) {
            value += PlayerPrefs.GetInt(key);
        }
        PlayerPrefs.SetInt(key, value);
    }

    [Serializable]
	public class InnerData {
		public float value;

		public InnerData (float value = 0f) {
			this.value = value;
		}

		public void add (float amount) {
			this.value += amount;
		}

		public void reset () {
			this.value = 0f;
		}

		public override string ToString () {
			return Mathf.RoundToInt(value).ToString ();
		}
	}
}