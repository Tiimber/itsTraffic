using UnityEngine;
using System.Collections;

public class Pos : NodeWithTags {
	public float Lon { set; get; }
	public float Lat { set; get; }

	public Pos (long id, float lon, float lat) : base(id) {
		this.Lon = lon;
		this.Lat = lat;
	}
}
