using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IntersectionOverlap {

	private static bool off = false;


	private const string CARWAY_MATERIAL_ID = "2002-Asphalt";
	private const string SMALL_WAY_MATERIAL_ID = "2003-Asphalt-small";

	public static void Create () {
		if (off) {
			return;
		}
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

		Dictionary<long, List<WayReference>> endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && !p.Value[0].way.EndPointImpossible).ToDictionary (p => p.Key, p => p.Value);
		Dictionary<long, List<WayReference>> straightWayOrIntersectionIndex = nodeWayIndex.Where (p => p.Value.Count >= 2).ToDictionary (p => p.Key, p => p.Value);

		foreach (KeyValuePair<long, List<WayReference>> endPoints in endPointIndex) {
			WayReference endPoint = endPoints.Value[0];
			WayObjectEndPoint.create (endPoints.Key, endPoint, CARWAY_MATERIAL_ID);
		}

		foreach (KeyValuePair<long, List<WayReference>> straightWayEntry in straightWayOrIntersectionIndex) {
			WayObjectStraight.create (straightWayEntry.Key, straightWayEntry.Value, CARWAY_MATERIAL_ID);
		}

		CreateSmallerWays ();
	}

	static List<float> WITHOUT_INTERSECTION = new List<float>() {
		WayTypeEnum.PLATFORM
	}; 

	public static void CreateSmallerWays () {

		foreach (float wayType in WayTypeEnum.WayTypes) {
			if (wayType > 0f && wayType < 1f) {
				Dictionary<long, List<WayReference>> nodeWayIndex = NodeIndex.nodeWayIndex.
					ToDictionary(
						entry => entry.Key, 
						entry => new List<WayReference>(entry.Value.Where(
							p => !((WayReference)p).way.CarWay && p.way.WayWidthFactor == wayType
						))
					).Where(
						p => p.Value.Count > 0
					).ToDictionary(
						entry => entry.Key, 
						entry => entry.Value
					);

				Dictionary<long, List<WayReference>> endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && !p.Value[0].way.EndPointImpossible).ToDictionary (p => p.Key, p => p.Value);
				Dictionary<long, List<WayReference>> straightWayOrIntersectionIndex = nodeWayIndex.Where (p => p.Value.Count >= 2).ToDictionary (p => p.Key, p => p.Value);

				foreach (KeyValuePair<long, List<WayReference>> endPoints in endPointIndex) {
					WayReference endPoint = endPoints.Value[0];
					WayObjectEndPoint.create (endPoints.Key, endPoint, SMALL_WAY_MATERIAL_ID);
				}

				foreach (KeyValuePair<long, List<WayReference>> straightWayEntry in straightWayOrIntersectionIndex) {
					WayObjectStraight.create (straightWayEntry.Key, straightWayEntry.Value, SMALL_WAY_MATERIAL_ID);
				}
			}
		}
	}
}
