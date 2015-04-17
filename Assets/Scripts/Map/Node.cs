using UnityEngine;
using System.Collections;

public class Pos : NodeWithTags {
	public decimal Lon { set; get; }
	public decimal Lat { set; get; }

	public Pos (long id, decimal lon, decimal lat) : base(id) {
		this.Lon = lon;
		this.Lat = lat;
	}
}
