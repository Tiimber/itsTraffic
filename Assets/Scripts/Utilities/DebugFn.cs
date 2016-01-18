using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DebugFn : MonoBehaviour
{
	private static bool enabled = true;

	private static Vector3 offsetZ = new Vector3 (0, 0, -0.1f);

	public static void print(Vector3 vector) {
		if (enabled) {
			Debug.Log ("Vector3 (" + vector.x + ", " + vector.y + ", " + vector.z + ")");
		}
	}

	public static void print(List<Vector3> vectors) {
		if (enabled) {
			Debug.Log ("[");
			foreach (Vector3 vector in vectors) {
				print (vector);
			}
			Debug.Log ("]");
		}
	}

	public static void print(Vector3[] vectors) {
		if (enabled) {
			print (vectors.ToList ());
		}
	}

	public static void print(Vector2 vector) {
		if (enabled) {
			Debug.Log ("Vector2 (" + vector.x + ", " + vector.y + ")");
		}
	}
	
	public static void print(List<Vector2> vectors) {
		if (enabled) {
			Debug.Log ("[");
			foreach (Vector2 vector in vectors) {
				print (vector);
			}
			Debug.Log ("]");
		}
	}

	private static float X_LENGTH = 0.02f;

	public static void arrow(Vector3 start, Vector3 eulerAngles, Vector3 lengthVector) {
		Quaternion quaternion = Quaternion.Euler (eulerAngles);
		Vector3 move = quaternion * lengthVector;
		Vector3 end = start + move;
	
		arrowFromTo (start, end);
	}

	private static void arrowFromTo (Vector3 start, Vector3 end) {
		if (enabled) {
			Debug.DrawLine (start + offsetZ + new Vector3 (-X_LENGTH, -X_LENGTH, 0f), start + offsetZ + new Vector3 (X_LENGTH, X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			Debug.DrawLine (start + offsetZ + new Vector3 (-X_LENGTH, X_LENGTH, 0f), start + offsetZ + new Vector3 (X_LENGTH, -X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			
			Debug.DrawLine (start + offsetZ, end + offsetZ, Color.yellow, float.MaxValue, false);
		}
	}

	public static void arrow(Vector3 start, Vector3 end) {
		arrowFromTo (start, end);
	}

	public static void arrow(Vector2 start, Vector2 end) {
		arrowFromTo (new Vector3(start.x, start.y), new Vector3(end.x, end.y));
	}

	public static void square(Vector3 pos) {
		if (enabled) {
			Debug.DrawLine (pos + offsetZ + new Vector3 (-X_LENGTH, -X_LENGTH, 0f), pos + offsetZ + new Vector3 (X_LENGTH, -X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			Debug.DrawLine (pos + offsetZ + new Vector3 (X_LENGTH, -X_LENGTH, 0f), pos + offsetZ + new Vector3 (X_LENGTH, X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			Debug.DrawLine (pos + offsetZ + new Vector3 (X_LENGTH, X_LENGTH, 0f), pos + offsetZ + new Vector3 (-X_LENGTH, X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			Debug.DrawLine (pos + offsetZ + new Vector3 (-X_LENGTH, X_LENGTH, 0f), pos + offsetZ + new Vector3 (-X_LENGTH, -X_LENGTH, 0f), Color.yellow, float.MaxValue, false);

			Debug.DrawLine (pos + offsetZ + new Vector3 (-X_LENGTH, -X_LENGTH, 0f), pos + offsetZ + new Vector3 (X_LENGTH, X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
			Debug.DrawLine (pos + offsetZ + new Vector3 (-X_LENGTH, X_LENGTH, 0f), pos + offsetZ + new Vector3 (X_LENGTH, -X_LENGTH, 0f), Color.yellow, float.MaxValue, false);
		}
	}

	public static void DrawBounds(Bounds bounds) {
		if (enabled) {
			Debug.DrawLine (bounds.center - bounds.size / 2f, bounds.center + bounds.size / 2f, Color.green, float.MaxValue, false);
			Debug.DrawLine (bounds.center - new Vector3 (bounds.size.x, -bounds.size.y, bounds.size.z) / 2f, bounds.center + new Vector3 (bounds.size.x, -bounds.size.y, bounds.size.z) / 2f, Color.green, float.MaxValue, false);
		}
	}

	public static void DrawOutline (List<Vector2> points) {
		Vector2 prev = points.Last ();
		foreach (Vector2 curr in points) {
			arrow(prev, curr);
			prev = curr;
		}
	}
}
