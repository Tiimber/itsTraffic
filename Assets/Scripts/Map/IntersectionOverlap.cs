using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IntersectionOverlap {

	public static void Create () {
		Dictionary<long, List<WayReference>> nodeWayIndex = NodeIndex.nodeWayIndex.
			ToDictionary(
				entry => entry.Key, 
				entry => new List<WayReference>(entry.Value.Where(
					p => ((WayReference)p).way.CarWay
				))
			).Where(
				p => p.Value.Count > 0
			).ToDictionary(
				entry => entry.Key, 
				entry => entry.Value
			);

		// TODO Remove keys where the values have 0 length lists
		Dictionary<long, List<WayReference>> endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && !p.Value[0].way.EndPointImpossible).ToDictionary (p => p.Key, p => p.Value);
		Dictionary<long, List<WayReference>> straightWayIndex = nodeWayIndex.Where (p => p.Value.Count == 2).ToDictionary (p => p.Key, p => p.Value);
		Dictionary<long, List<WayReference>> intersectionWayIndex = nodeWayIndex.Where (p => p.Value.Count > 2).ToDictionary (p => p.Key, p => p.Value);

		foreach (KeyValuePair<long, List<WayReference>> endPoints in endPointIndex) {
			WayReference endPoint = endPoints.Value[0];
			WayObjectEndPoint.create (endPoints.Key, endPoint);
		}

		foreach (KeyValuePair<long, List<WayReference>> straightWayEntry in straightWayIndex) {
			WayObjectStraight.create (straightWayEntry.Key, straightWayEntry.Value);
		}

		foreach (KeyValuePair<long, List<WayReference>> intersectionEntry in intersectionWayIndex) {
			WayObjectStraight.create (intersectionEntry.Key, intersectionEntry.Value);
//			WayObjectIntersection.create (intersectionEntry.Key, intersectionEntry.Value);
		}

//		Debug.Log (nodeWayIndex);
	}
}
