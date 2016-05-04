using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

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
}
