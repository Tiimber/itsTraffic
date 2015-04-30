using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pos : NodeWithTags {
	public float Lon { set; get; }
	public float Lat { set; get; }

	public Pos (long id, float lon, float lat) : base(id) {
		this.Lon = lon;
		this.Lat = lat;
	}

	public List<System.Collections.Generic.KeyValuePair<Pos, WayReference>> getNeighbours () {
		List<KeyValuePair<Pos, WayReference>> neighbours = new List<KeyValuePair<Pos, WayReference>> ();
		if (NodeIndex.nodeWayIndex.ContainsKey (Id)) {
			List<WayReference> wayReferences = NodeIndex.nodeWayIndex [Id];
			foreach (WayReference wayReference in wayReferences) {
				neighbours.Add (new KeyValuePair<Pos, WayReference> (wayReference.node1 == this ? wayReference.node2 : wayReference.node1, wayReference));
			}
		}
		return neighbours;
	}
}
