using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Way : NodeWithTags {
	public float WayWidthFactor { set; get; } 
	public bool EndPointImpossible { set; get; }
	public bool CarWay { set; get; }
	public bool Building { set; get; }
	public List<WayReference> WayReferences = new List<WayReference> ();
	
	public Way (long id) : base(id) {
		WayWidthFactor = 0.1F;
		EndPointImpossible = false;
	}

	public void addWayReference (WayReference wayReference) {
		this.WayReferences.Add (wayReference);
	}

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
			case "platform": WayWidthFactor = WayTypeEnum.PLATFORM; EndPointImpossible = true; break;
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
			WayWidthFactor = 0.111f;
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
		case "building":
			Building = true;
			break;
		}
	}
}
