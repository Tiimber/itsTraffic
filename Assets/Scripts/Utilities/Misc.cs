using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class Misc {

	public static System.Random random = new System.Random();
    public static bool haveScannedInputs = false;
    public static bool hasMouse = true;
    public static List<Joystick> joysticks = new List<Joystick>();

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

    // Get child to a GameObject with tag
    public static GameObject GetGameObjectWithMenuValue(Transform parent) {
        GameObject gameObjectWithTag = null;

        // Get all MenuValue GameObjects
        MenuValue[] menuValueObjects = Resources.FindObjectsOfTypeAll<MenuValue>();

        // Check which one has the sent in parent
        foreach (MenuValue menuValueObject in menuValueObjects) {
            GameObject gameObject = menuValueObject.gameObject;
            if (Misc.HasParent(gameObject, parent)) {
                gameObjectWithTag = gameObject;
                break;
            }
        }


        return gameObjectWithTag;
    }

    public static bool HasParent(GameObject gameObject, Transform parent) {
        Transform transform = gameObject.transform;
        while (transform != null) {
            if (transform == parent) {
                return true;
            }
            transform = transform.parent;
        }

		return false;
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

	public static List<List<long>> parseLongMultiList (string intStrings, char listSeparator, char itemSeparator) {
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
		return strVal == "true" ? true : (strVal == null ? defaultValue : false);
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

    public static float randomPlusMinus(float medium, float plusMinus) {
        return randomRange(medium - plusMinus, medium + plusMinus);
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

    public static DateTime parseDateTime (string date, string time) {
        DateTime dateTime = DateTime.Now;
        if (date != null) {
            string[] dateParts = date.Split ('-');
            dateTime = dateTime.AddYears(Convert.ToInt32 (dateParts [0]) - dateTime.Year);
            dateTime = dateTime.AddMonths(Convert.ToInt32 (dateParts [1]) - dateTime.Month);
            dateTime = dateTime.AddDays(Convert.ToInt32 (dateParts [2]) - dateTime.Day);
        }
        if (time != null) {
            string[] timeParts = time.Split (':');
            dateTime = dateTime.AddHours(Convert.ToInt32 (timeParts [0]) - dateTime.Hour);
            dateTime = dateTime.AddMinutes(Convert.ToInt32 (timeParts [1]) - dateTime.Minute);
            dateTime = dateTime.AddSeconds(timeParts.Length > 2 ? Convert.ToInt32 (timeParts [2]) - dateTime.Second : -dateTime.Second);
            dateTime = dateTime.AddMilliseconds(-dateTime.Millisecond);
        }
		return dateTime;
    }

	public static Color parseColor (string color) {
		string[] colorParts = color.Split (',');
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

	public static AudioListenerHolder FindAudioListenerHolder(GameObject gameObject) {
        AudioListenerHolder audioListenerHolder = gameObject.GetComponent<AudioListenerHolder> ();
        if (audioListenerHolder != null) {
            return audioListenerHolder;
        }
        for (int child = 0; child < gameObject.transform.childCount; child++) {
            audioListenerHolder = Misc.FindAudioListenerHolder(gameObject.transform.GetChild(child).gameObject);
            if (audioListenerHolder != null) {
                return audioListenerHolder;
            }
        }
        return null;
	}

	public static IEnumerator _WaitForRealSeconds(float aTime) {
		while (aTime > 0f) {
			aTime -= Mathf.Clamp (Time.unscaledDeltaTime, 0, 0.2f);
            yield return null;
		}
	}

	public static Coroutine WaitForRealSeconds(float aTime) {
		Game gameSingleton = Singleton<Game>.Instance;
        return gameSingleton.StartCoroutine (_WaitForRealSeconds (aTime));
	}

    public static Vector3 getWorldPos(Transform transform) {
        Vector3 worldPosition = transform.localPosition;
        while (transform.parent != null) {
            transform = transform.parent;
            worldPosition += transform.localPosition;
        }
        return worldPosition;
    }

    public static Quaternion getWorldRotation(Transform transform) {
        return transform.rotation;
//        Quaternion worldRotation = transform.localRotation;
//        while (transform.parent != null) {
//            transform = transform.parent;
//            worldRotation += transform.localRotation;
//        }
//        return worldRotation;
    }

    public static string maxDecimals(float value, int decimals = 2) {
        if (value != 0f) {
            return value.ToString("#." + getDecimalSpots(decimals));
        } else {
            return value.ToString("0." + getDecimalSpots(decimals));
        }
    }

    private static string getDecimalSpots(int decimals) {
        string decimalChars = "";
        for (int i = 0; i < decimals; i++) {
            decimalChars += "#";
        }
        return decimalChars;
    }

    public class Size {
        public int width;
        public int height;
    }

	public static Size getImageSize(int width, int height, int targetWidth, int targetHeight) {
        Size size = new Size ();
		float ratioX = (float) width / targetWidth;
		float ratioY = (float) height/ targetHeight;
        if (ratioX > 1f || ratioY > 1f) {
			float scaleFactor = Mathf.Max(ratioX, ratioY);
            size.width = Mathf.RoundToInt(targetWidth / scaleFactor);
            size.height = Mathf.RoundToInt(targetHeight / scaleFactor);
        } else {
            size.width = width;
            size.height = height;
        }
		return size;
	}

	public static AudioListener getAudioListener() {
        AudioListener[] audioListener = Resources.FindObjectsOfTypeAll<AudioListener>();
        if (audioListener != null && audioListener.Length > 0) {
            return audioListener[0];
        }
        return null;
	}

	public static MeshFilter[] FilterCarWays(MeshFilter[] allWayFilters) {
        List<MeshFilter> filtered = new List<MeshFilter> ();
        foreach (MeshFilter wayFilter in allWayFilters) {
            if (!(wayFilter.name.StartsWith("CarWay (") || wayFilter.name.StartsWith("NonCarWay ("))) {
                filtered.Add(wayFilter);
            }
        }
		return filtered.ToArray();
	}

    public static float ToRadians(float degrees) {
        return (Mathf.PI / 180f) * degrees;
    }

    public static float ToDegrees(float radians) {
        return 180f * radians / Mathf.PI;
    }

    public static float getDistanceBetweenEarthCoordinates (float lon1, float lat1, float lon2, float lat2) {
        float R = 6371e3f; // metres
		float φ1 = ToRadians (lat1);
		float φ2 = ToRadians (lat2);
		float Δφ = ToRadians (lat2 - lat1);
		float Δλ = ToRadians (lon2 - lon1);

		float a = Mathf.Sin (Δφ / 2f) * Mathf.Sin (Δφ / 2f) +
			Mathf.Cos (φ1) * Mathf.Cos (φ2) *
			Mathf.Sin (Δλ / 2f) * Mathf.Sin (Δλ / 2f);
        float c = 2f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1f-a));

        return R * c;
    }

	public static List<int> splitInts(string str) {
		return str.Split (',').Select<string, int>(int.Parse).ToList<int>();
	}

	public static Texture getCountryFlag(string countryCode) {
        return Resources.Load("Graphics/flags/" + countryCode) as Texture;
	}

    // Readable in format "1s", "5m 30s" (always number + suffix grouped, parts separated with spaces)
	public static long getTsForReadable(string readable) {
        long ms = 0;
        string[] parts = readable.Split(' ');

		foreach (string part in parts) {
            if (part.EndsWith("ms")) {
                ms += long.Parse(part.Substring(0, part.Length - 2));
            } else if (part.EndsWith("s")) {
                ms += long.Parse(part.Substring(0, part.Length - 1)) * 1000;
            } else if (part.EndsWith("m")) {
                ms += long.Parse(part.Substring(0, part.Length - 1)) * 1000 * 60;
            } else if (part.EndsWith("h")) {
                ms += long.Parse(part.Substring(0, part.Length - 1)) * 1000 * 60 * 60;
            } else if (part.EndsWith("d")) {
                ms += long.Parse(part.Substring(0, part.Length - 1)) * 1000 * 60 * 60 * 24;
            }
        }

        return ms;
	}

//	https://github.com/fiorix/freegeoip/releases (need https://golang.org?)
    public static IEnumerator getGeoLocation() {
		WWW www = CacheWWW.Get(Game.endpointBaseUrl + Game.getLocationRelativeUrl);
        yield return www;

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(www.text);

        Game.instance.lat = Convert.ToSingle (xmlDoc.SelectSingleNode ("/geoData/lat").InnerText);
        Game.instance.lon = Convert.ToSingle (xmlDoc.SelectSingleNode ("/geoData/lon").InnerText);
        Game.instance.countryCode = Convert.ToString (xmlDoc.SelectSingleNode ("/geoData/countryCode").InnerText);
        Game.instance.country = Convert.ToString (xmlDoc.SelectSingleNode ("/geoData/country").InnerText);
    }

	public static void refreshInputMethods() {
		Misc.hasMouse = Input.mousePresent;

        joysticks.Clear();
        int index = 0;

		string[] joystickNames = Input.GetJoystickNames();
		Array.ForEach<string>(joystickNames, name => joysticks.Add(new Joystick(index++, name)));

        haveScannedInputs = true;
	}

    public static Text GetMenuValueTextForKey(string menuValueKey) {
        MenuValue[] menuValueObjects = Resources.FindObjectsOfTypeAll<MenuValue>();
        foreach (MenuValue menuValueObject in menuValueObjects) {
            if (menuValueObject.key == menuValueKey) {
                return menuValueObject.GetComponent<Text>();
            }
        }
        return null;
    }

	public static List<string> getInputMethodNames() {
        List<string> inputNames = new List<string>();
        if (!hasMouse && joysticks.Count == 0) {
            inputNames.Add(Joystick.GetUnavailableName());
        } else {
			if (Misc.hasMouse) {
				inputNames.Add(Joystick.GetMouseAndKeyboardName());
			}
            if (Misc.joysticks.Count > 0) {
				inputNames.Add(Misc.joysticks[0].getName());
			}
        }
        return inputNames;
    }

	public static float getMeshArea(Mesh mesh) {
		return VolumeOfMesh(mesh);
	}

	private static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
		float v321 = p3.x * p2.y;
		float v231 = p2.x * p3.y;
		float v312 = p3.x * p1.y;
		float v132 = p1.x * p3.y;
		float v213 = p2.x * p1.y;
		float v123 = p1.x * p2.y;
		return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
	}

	private static float VolumeOfMesh(Mesh mesh) {
		float volume = 0;
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		for (int i = 0; i < mesh.triangles.Length; i += 3)
		{
			Vector3 p1 = vertices [triangles [i + 0]];
			Vector3 p2 = vertices [triangles [i + 1]];
			Vector3 p3 = vertices [triangles [i + 2]];
			volume += SignedVolumeOfTriangle (p1, p2, p3);
		}
		return Mathf.Abs (volume);
	}

	public static bool CompareVectorLists(List<Vector3> vec1, List<Vector3> vec2) {
        if (vec1 == vec2) {
            return true;
        }

        if (vec1.Count != vec2.Count) {
        	return false;
        }

        for (int i = 0; i < vec1.Count; i++) {
            if (!vec1[i].Equals(vec2[i])) {
                return false;
            }
        }

		return true;
	}

    // Below code ported from http://stackoverflow.com/a/8764866
    public static Dictionary<string, float> getSunPosition(DateTime dateTime, float lon, float lat) {
        int year = dateTime.Year;
        int dayOfYear = dateTime.DayOfYear;
        float hour = (float)dateTime.Hour;
        int min = dateTime.Minute;
        int sec = dateTime.Second;

        float twopi = 2f * Mathf.PI;
        float deg2rad = Mathf.PI / 180f;

        // Get Julian date - 2400000
        hour = hour + min / 60 + sec / 3600; // hour plus fraction
        int delta = year - 1949;
        int leap = (int)Math.Truncate (delta / 4f); // former leapyears
        float jd = 32916.5f + delta * 365 + leap + dayOfYear + hour / 24f;

        // The input to the Atronomer's almanach is the difference between
        // the Julian date and JD 2451545.0 (noon, 1 January 2000)
        float time = jd - 51545f;

        // Ecliptic coordinates

        // Mean longitude
        float mnlon = 280.46f + 0.9856474f * time;
        mnlon = mnlon % 360f;
        if (mnlon < 0) {
            mnlon += 360f;
        }

        // Mean anomaly
        float mnanom = 357.528f + 0.9856003f * time;
        mnanom = mnanom % 360f;
        if (mnanom < 0) {
            mnanom += 360f;
        }
        mnanom = mnanom * deg2rad;

        // Ecliptic longitude and obliquity of ecliptic
        float eclon = mnlon + 1.915f * Mathf.Sign (mnanom) + 0.02f * Mathf.Sin (2f * mnanom);
        eclon = eclon % 360f;
        if (eclon < 0) {
            eclon += 360f;
        }
        float oblqec = 23.439f - 0.0000004f * time;
        eclon = eclon * deg2rad;
        oblqec = oblqec * deg2rad;

        // Celestial coordinates
        // Right ascension and declination
        float num = Mathf.Cos (oblqec) * Mathf.Sin (eclon);
        float den = Mathf.Cos (eclon);
        float ra = Mathf.Atan (num / den);
        if (den < 0) {
            ra += Mathf.PI;
        } else if (den >= 0 && num < 0) {
            ra += twopi;
        }
        float dec = Mathf.Asin (Mathf.Sin (oblqec) * Mathf.Sin (eclon));

        // Local coordinates
        // Greenwich mean sidereal time
        float gmst = 6.697375f + 0.0657098242f * time + hour;
        gmst = gmst % 24f;
        if (gmst < 0) {
            gmst += 24f;
        }

        // Local mean sidereal time
        float lmst = gmst + lon / 15f;
        lmst = lmst % 24f;
        if (lmst < 0) {
            lmst += 24f;
        }
        lmst = lmst * 15f * deg2rad;

        // Hour angle
        float ha = lmst - ra;
        if (ha < -Mathf.PI) {
            ha += twopi;
        } else if (ha > Mathf.PI) {
            ha -= twopi;
        }

        // Latitude to radians
        lat = lat * deg2rad;

        // Azimuth and elevation
        float el = Mathf.Asin (Mathf.Sin (dec) * Mathf.Sin (lat) + Mathf.Cos (dec) * Mathf.Cos (lat) * Mathf.Cos (ha));
        float az = Mathf.Asin (-Mathf.Cos (dec) * Mathf.Sin (ha) / Mathf.Cos (el));

        // For logic and names, see Spencer, J.W. 1989. Solar Energy. 42(4):353
        bool cosAzPos = 0 <= Mathf.Sin (dec) - Mathf.Sin (el) * Mathf.Sin (lat);
        bool sinAzNeg = Mathf.Sin (az) < 0;
        if (cosAzPos && sinAzNeg) {
            az += twopi;
        } else if (!cosAzPos) {
            az = Mathf.PI - az;
        }

        el = el / deg2rad;
        az = az / deg2rad;
        lat = lat / deg2rad;

        return new Dictionary<string, float> () {
            {"elevation", el},
            {"azimuth", az}
        };
    }

    public static Quaternion getSunRotation (float azimuth) {
        return Quaternion.Euler(getSunRotationX(azimuth), getSunRotationY(azimuth), 0f);
    }

    private static float getSunRotationX(float azimuth) {
        float x = azimuth;
        float a = 44.76907f;
        float b = 0.2250797f;
        float c = -0.01406229f;
        float d = 0.0000746504f;
        float e = -1.036811f * Mathf.Pow (10f, -7f);
        return a + b * x + c * Mathf.Pow (x, 2f) + d * Mathf.Pow (x, 3f) + e * Mathf.Pow (x, 4f);
    }

    private static float getSunRotationY(float azimuth) {
        float x = azimuth;
        float a = 1.974222f;
        float b = -1.320782f;
        float c = 0.01091512f;
        float d = -0.00002021318f;
        return a + b * x + c * Mathf.Pow (x, 2f) + d * Mathf.Pow (x, 3f);
    }

    public static float getSunIntensity(float elevation) {
        float x = elevation;
        float a = -0.1f;
        float b = 0.06783333f;
        float c = -0.002375f;
        float d = 0.00007083333f;
        float e = -0.00000125f;
        float f = 8.333333f * Mathf.Pow (10f, -9f);
        return Mathf.Clamp(a + b * x + c * Mathf.Pow(x, 2f) + d * Mathf.Pow(x, 3f) + e * Mathf.Pow(x, 4f) + f * Mathf.Pow(x, 5f), 0f, 1f);
    }

	public static Vector2 getScreenPos(Vector3 cameraPos) {
		Vector3 screenPoint = Game.instance.mainCamera.WorldToScreenPoint (cameraPos);
        // Revert so that top is 0px
        screenPoint.y = Screen.height - screenPoint.y;
        return screenPoint;
	}

    public class UrlBuilder {
        string url;
        Dictionary<string, string> query = new Dictionary<string, string>();

        public UrlBuilder() {
            this.url = "";
        }

        public UrlBuilder(string url) {
            this.url = url;
        }

        public UrlBuilder addUrl(string url) {
            this.url += url;
            return this;
        }

        public UrlBuilder addQuery(string key, string value) {
            this.query.Add(Uri.EscapeUriString(key), Uri.EscapeUriString(value));
            return this;
        }

        public UrlBuilder addQuery(string key, int value) {
            return addQuery(key, "" + value);
        }

        public UrlBuilder addQuery(string key, float value) {
            return addQuery(key, "" + value);
        }

        public string build() {
            string result = url;
            if (query.Count > 0) {
                result += "?";
                bool first = true;
                foreach (KeyValuePair<string, string> queryPart in query) {
                    if (first) {
                        first = false;
                    } else {
                        result += "&";
                    }
                    result += queryPart.Key + "=" + queryPart.Value;
                }
            }
            return result;
        }
    }

	public static void DestroyChildren(Transform parent) {
		for (int i = parent.childCount - 1; i >= 0; --i) {
			GameObject.Destroy(parent.GetChild(i).gameObject);
		}
        parent.DetachChildren();
	}
}
