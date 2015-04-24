
using System.Collections.Generic;
using System.Linq;

public class NodeIndex
{
	public static Dictionary<long, List<Way>> nodeWayIndex = new Dictionary<long, List<Way>>();

	public static Dictionary<long, List<Way>> endPointIndex;
	public static Dictionary<long, List<Way>> straightWayIndex;
	public static Dictionary<long, List<Way>> intersectionWayIndex;


	public static void addWayToNode (long id, Way way) {
		if (!nodeWayIndex.ContainsKey (id)) {
			nodeWayIndex.Add(id, new List<Way>());
		}
		nodeWayIndex [id].Add (way);
	}
	public static void calculateIndexes () {
		endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && p.Value[0].CarWay).ToDictionary (p => p.Key, p => p.Value);
		straightWayIndex = nodeWayIndex.Where (p => p.Value.Count == 2).ToDictionary (p => p.Key, p => p.Value);
		intersectionWayIndex = nodeWayIndex.Where (p => p.Value.Count > 2).ToDictionary (p => p.Key, p => p.Value);
	}
}
