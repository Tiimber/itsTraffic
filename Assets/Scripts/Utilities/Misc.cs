using UnityEngine;
using System.Collections;
using System;

public class Misc{
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
}
