using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Way : NodeWithTags {
	private List<Pos> nodes = new List<Pos> ();
	public float WayWidthFactor { set; get; } 
	public bool CarWay { set; get; }
	public WayReference WayReference { set; get; }
	
	public Way (long id) : base(id) {
		WayWidthFactor = 0.1F;
	}

	public List<Pos> getPoses () {
		return nodes;
	}

	public void addPos (Pos node) {
		nodes.Add (node);
	}

//	override public void processTags () {}

	override public void addTag (Tag tag) {
		base.addTag(tag);
		switch (tag.Key) {
		case "highway":
			CarWay = false;
			switch (tag.Value) {
				case "primary": WayWidthFactor = WayTypeEnum.HIGHWAY_PRIMARY; CarWay = true; break;			
				case "primary_link": WayWidthFactor = WayTypeEnum.HIGHWAY_PRIMARY_LINK; CarWay = true; break;			
				case "secondary": WayWidthFactor = WayTypeEnum.HIGHWAY_SECONDARY; CarWay = true; break;
				case "secondary_link": WayWidthFactor = WayTypeEnum.HIGHWAY_SECONDARY_LINK; CarWay = true; break;
				case "tertiary": WayWidthFactor = WayTypeEnum.HIGHWAY_TERTIARY; CarWay = true; break;
				case "tertiary_link": WayWidthFactor = WayTypeEnum.HIGHWAY_TERTIARY_LINK; CarWay = true; break;
				case "motorway": WayWidthFactor = WayTypeEnum.MOTORWAY; CarWay = true; break;
				case "motorway_link": WayWidthFactor = WayTypeEnum.MOTORWAY_LINK; CarWay = true; break;
				case "residential": WayWidthFactor = WayTypeEnum.RESIDENTIAL; CarWay = true; break;
				case "unclassified": WayWidthFactor = WayTypeEnum.UNCLASSIFIED; CarWay = true; break;
				case "platform": WayWidthFactor = WayTypeEnum.PLATFORM; break;
				case "footway": WayWidthFactor = WayTypeEnum.FOOTWAY; break;
				case "pedestrian": WayWidthFactor = WayTypeEnum.PEDESTRIAN; break;
				case "living_street": WayWidthFactor = WayTypeEnum.LIVING_STREET; break;
				case "path": WayWidthFactor = WayTypeEnum.PATH; break;
				case "service": WayWidthFactor = WayTypeEnum.SERVICE; break;
				case "cycleway": WayWidthFactor = WayTypeEnum.CYCLEWAY; break;
				case "steps": WayWidthFactor = WayTypeEnum.STEPS; break;
				// 
				default: Debug.Log("Highway type unknown: " + tag.Value); break;
			}
			break;
		case "landuse":
			WayWidthFactor = 0;
			switch (tag.Value) {
//				case "farmland": WayWidthFactor = 0.25f; break;
				// retail
				// residential
				// commercial
				// industrial
				// 
//				default: Debug.Log("Landuse type unknown: " + tag.Value); break;
			}
			break;
		}
	}
}
