using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System;

public class DataCollector : MonoBehaviour {

	private bool output = true;

	private float lastDataDiff = 0f;
	private float dataDiffThreshold = 1f;
	private bool diffCollected = false;

	private static Dictionary<string, InnerData> Data = new Dictionary<string, InnerData>();
	private static Dictionary<string, InnerData> CopyData;
	private static Dictionary<string, float> DiffData = new Dictionary<string, float>();

	private void calculateDataDiff () {
		if (CopyData != null) {
			foreach (string key in CopyData.Keys) {
				DiffData [key] = Data [key].value - CopyData [key].value;
			}
			diffCollected = true;
		}
		CopyData = Misc.DeepClone(Data);
	}

	void OnGUI() {
		if (output) {
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

	public static void InitLabel (string label) {
		Data.Add (label, new InnerData());
	}

	public static void Add (string label, InnerData amount) {
		if (amount.value != 0f) {
			Add (label, amount.value);
		}
	}

	public static void Add (string label, float amount) {
		if (!Data.ContainsKey (label)) {
			Data.Add (label, new InnerData(amount));
		} else {
			((InnerData)Data [label]).add (amount);
		}
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