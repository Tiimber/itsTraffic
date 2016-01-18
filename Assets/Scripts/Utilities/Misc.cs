using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;

public class Misc {

	public static byte[] GetBytes(string str)
	{
		byte[] bytes = new byte[str.Length * sizeof(char)];
		System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
		return bytes;
	}
	
	public static string GetString(byte[] bytes)
	{
		char[] chars = new char[bytes.Length / sizeof(char)];
		System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
		return new string(chars);
	}

	private static DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public static long currentTimeMillis()
	{
		return (long) ((DateTime.UtcNow - Jan1st1970).TotalMilliseconds);
	}

	public static List<T> CloneBaseNodeList<T> (List<T> list) where T: MonoBehaviour {
		return new List<T> (list);
	}

	public static List<GameObject> NameStartsWith(string start) {
		List<GameObject> matches = new List<GameObject> ();
		GameObject[] gameObjects = GameObject.FindObjectsOfType (typeof(GameObject)) as GameObject[];
		foreach (GameObject gameObject in gameObjects){
			if (gameObject.name.StartsWith(start)) {
				matches.Add (gameObject);
			}
		}
		return matches;
	}

	public static Vector3 getWorldPos (XmlNode xmlNode) {
		Pos pos = NodeIndex.nodes [Convert.ToInt64 (xmlNode.Value)];
		return Game.getCameraPosition (pos);
	} 
}
