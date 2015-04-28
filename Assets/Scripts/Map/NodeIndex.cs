
using System.Collections.Generic;
using System.Linq;

public class NodeIndex
{
	public static Dictionary<long, List<WayReference>> nodeWayIndex = new Dictionary<long, List<WayReference>>();

	public static Dictionary<long, List<WayReference>> endPointIndex;
	public static Dictionary<long, List<WayReference>> straightWayIndex;
	public static Dictionary<long, List<WayReference>> intersectionWayIndex;


	public static void addWayReferenceToNode (long id, WayReference partOfWay) {
		if (!nodeWayIndex.ContainsKey (id)) {
			nodeWayIndex.Add(id, new List<WayReference>());
		}
		nodeWayIndex [id].Add (partOfWay);
	}
	public static void calculateIndexes () {
		endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1).ToDictionary (p => p.Key, p => p.Value);
		straightWayIndex = nodeWayIndex.Where (p => p.Value.Count == 2).ToDictionary (p => p.Key, p => p.Value);
		intersectionWayIndex = nodeWayIndex.Where (p => p.Value.Count > 2).ToDictionary (p => p.Key, p => p.Value);
	}
}
