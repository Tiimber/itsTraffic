using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Way : NodeWithTags {
	private List<Pos> nodes = new List<Pos> ();
	public float WayWidthFactor { set; get; } 

	public Way (long id) : base(id) {
		WayWidthFactor = 0.1F;
	}

	public List<Pos> getPoses () {
		return nodes;
	}

	public void addPos (Pos node) {
		nodes.Add (node);
	}

	override public void addTag (Tag tag) {
		base.addTag(tag);
		switch (tag.Key) {
		case "highway":
			switch (tag.Value) {
				case "tertiary": WayWidthFactor = 1.5f; break;
				case "secondary": WayWidthFactor = 2f; break;
				case "primary": WayWidthFactor = 2.5f; break;			
				// footway
				// platform
				// pedestrian
				// living_street
				// path
				// service
				// cycleway
				// motorway
				// steps
				// 
				default: Debug.Log("Highway type unknown: " + tag.Value); break;
			}
			break;
		case "landuse":
			switch (tag.Value) {
				case "farmland": WayWidthFactor = 0.25f; break;
				// retail
				// residential
				// commercial
				// industrial
				// 
				default: Debug.Log("Landuse type unknown: " + tag.Value); break;
			}
			break;
		}
	}
}
