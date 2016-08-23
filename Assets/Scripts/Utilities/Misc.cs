using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public class Misc {

	public static System.Random random = new System.Random();

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

	public static bool isAngleAccepted (float angle1, float angle2, float acceptableAngleDiff, float fullAmountDegrees = 360f) {
		float angleDiff = Mathf.Abs (angle1 - angle2);
		return angleDiff <= acceptableAngleDiff || angleDiff >= fullAmountDegrees - acceptableAngleDiff;
	}

	public static float kmhToMps (float speedChangeKmh)
	{
		return speedChangeKmh * 1000f / 3600f;
	}

	public static T DeepClone<T>(T original)
	{
		// Construct a temporary memory stream
		MemoryStream stream = new MemoryStream();

		// Construct a serialization formatter that does all the hard work
		BinaryFormatter formatter = new BinaryFormatter();

		// This line is explained in the "Streaming Contexts" section
		formatter.Context = new StreamingContext(StreamingContextStates.Clone);

		// Serialize the object graph into the memory stream
		formatter.Serialize(stream, original);

		// Seek back to the start of the memory stream before deserializing
		stream.Position = 0;

		// Deserialize the graph into a new set of objects
		// and return the root of the graph (deep copy) to the caller
		return (T)(formatter.Deserialize(stream));
	}

	public static List<Vector3> posToVector3(List<Pos> positions) {
		List<Vector3> vectors = new List<Vector3> ();

		foreach (Pos pos in positions) {
			vectors.Add (Game.getCameraPosition (pos));
		}

		return vectors;
	}

	public static string pickRandom (List<string> strings) {
		return strings [Misc.randomRange (0, strings.Count-1)];
	}

	public static Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] pix = new Color[width * height];
		for(int i = 0; i < pix.Length; ++i)
		{
			pix[i] = col;
		}
		Texture2D result = new Texture2D(width, height);
		result.SetPixels(pix);
		result.Apply();
		return result;
	}

	public static object getMoney (float money) {
		// TODO - Currency for current country - Taken from map info on country
		return "$" + Mathf.Round(money * 100f) / 100f;
	}

	public static object getDistance (float distance) {
		// TODO - Maybe in US weird mesurements if in USA
		return Mathf.FloorToInt(distance) + "m";
	}

	public static bool isInside (Vector2 pos, Rect rect) {
		return rect.Contains (pos);
	}

	public static long daysToTicks (int days) {
		return (long) days * 24 * 60 * 60 * 1000 * 1000 * 10;
	}

	public static float getDistance (Vector3 from, Vector3 to) {
		return (from - to).magnitude; 
	}

	//Breadth-first search
	public static Transform FindDeepChild(Transform aParent, string aName) {
		var result = aParent.Find(aName);
		if (result != null) {
			return result;
		}
		foreach(Transform child in aParent) {
			result = Misc.FindDeepChild(child, aName);
			if (result != null)
				return result;
		}
		return null;
	}

	// Convert string with comma separated longs to list of longs
	public static List<long> parseLongs (string passengerIdsStr, char separator = ',') {
		List<long> ids = new List<long> ();
		if (passengerIdsStr != null) {
			string[] idStrings = passengerIdsStr.Split (separator);
			foreach (string id in idStrings) {
				ids.Add (Convert.ToInt64 (id));
			}
		}
		return ids;
	}

	public static List<List<long>> parseLongMultiList (String intStrings, char listSeparator, char itemSeparator) {
		List<List<long>> multiList = new List<List<long>> ();
		string[] lists = intStrings.Split (listSeparator);
		foreach (string list in lists) {
			multiList.Add (Misc.parseLongs(list, itemSeparator));
		}

		return multiList;
	}

	public static string xmlString(XmlNode attributeNode, string defaultValue = null) {
		if (attributeNode != null) {
			return attributeNode.Value;
		}
		return defaultValue;
	}

	public static bool xmlBool(XmlNode attributeNode, bool defaultValue = false) {
		string strVal = Misc.xmlString (attributeNode);
		return strVal == "true" ? true : defaultValue;
	}

	public static int xmlInt(XmlNode attributeNode, int defaultValue = 0) {
		string strVal = Misc.xmlString (attributeNode);
		if (strVal != null) {
			return Convert.ToInt32 (strVal);
		}
		return defaultValue;
	}

	public static float xmlFloat(XmlNode attributeNode, float defaultValue = 0f) {
		string strVal = Misc.xmlString (attributeNode);
		if (strVal != null) {
			return Convert.ToSingle (strVal);
		}
		return defaultValue;
	}

	public static long xmlLong(XmlNode attributeNode, long defaultValue = 0L) {
		string strVal = Misc.xmlString (attributeNode);
		if (strVal != null) {
			return Convert.ToInt64 (strVal);
		}
		return defaultValue;
	}
		
	public static void setRandomSeed (int randomSeed) {
		Misc.random = new System.Random (randomSeed);
	}

	public static float randomRange(float min, float max) {
		double value = Misc.random.NextDouble ();
		return min + (max - min) * (float)value;
	}

	public static int randomRange(int min, int max) {
		return Misc.random.Next (min, max);
	}

	public static object randomTime () {
		return Misc.randomRange (0, 23) + ":" + Misc.randomRange (0, 59);
	}

	public static DateTime parseDate (string dob) {
		string[] dateParts = dob.Split ('-');
		return new DateTime (Convert.ToInt32 (dateParts [0]), Convert.ToInt32 (dateParts [1]), dateParts.Length > 2 ? Convert.ToInt32 (dateParts [2]) : 1);
	}

	public static Color parseColor (string skinColor) {
		string[] colorParts = skinColor.Split (',');
		float r = Convert.ToInt32 (colorParts [0]) / 255f;
		float g = Convert.ToInt32 (colorParts [1]) / 255f;
		float b = Convert.ToInt32 (colorParts [2]) / 255f;

		return new Color (r, g, b);
	}

	public static Vector3 parseVector (string startVector) {
		string[] xy = startVector.Split (',');
		return new Vector3 (Convert.ToSingle(xy[0]), Convert.ToSingle(xy[1]), 0);
	}

	public static Tuple2<float, float> getOffsetPctFromCenter (Vector3 zoomPoint) {
		float centerX = Screen.width / 2f;
		float centerY = Screen.height / 2f;

		float offsetX = zoomPoint.x - centerX;
		float offsetY = zoomPoint.y - centerY;

		return new Tuple2<float, float> (offsetX / centerX, offsetY / centerY);
	}
}
