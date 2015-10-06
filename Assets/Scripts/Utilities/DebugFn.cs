using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DebugFn : MonoBehaviour
{
	public static void print(Vector3 vector) {
		Debug.Log ("Vector3 (" + vector.x + ", " + vector.y + ", " + vector.z + ")");
	}

	public static void print(List<Vector3> vectors) {
		Debug.Log ("[");
		foreach (Vector3 vector in vectors) {
			print (vector);
		}
		Debug.Log ("]");
	}

	public static void print(Vector3[] vectors) {
		print (vectors.ToList());
	}
}

