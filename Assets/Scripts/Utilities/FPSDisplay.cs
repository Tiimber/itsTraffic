using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
	float deltaTime = 0.0f;

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
	}

	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		int labelHeight = h * 2 / 100;
		Rect rect = new Rect(0, h - labelHeight, w, labelHeight);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = labelHeight;
		style.normal.textColor = new Color (1.0f, 1.0f, 1.0f, 1.0f);
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps), #cars {2}, #humans {3}", msec, fps, Vehicle.numberOfCars, HumanLogic.numberOfHumans);
		GUI.Label(rect, text, style);
	}
}