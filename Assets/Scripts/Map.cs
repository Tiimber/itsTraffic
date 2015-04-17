using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Map {
	public static List<Pos> Nodes { set; get; }
	public static List<Way> Ways { set; get; }

	static Map () {
		Ways = new List<Way> ();
	}
}
