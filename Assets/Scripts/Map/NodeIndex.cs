
using System.Collections.Generic;
using System.Linq;

public class NodeIndex
{
	public static Dictionary<long, Pos> nodes = new Dictionary<long, Pos> ();

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
		endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && !p.Value[0].way.EndPointImpossible).ToDictionary (p => p.Key, p => p.Value);
		straightWayIndex = nodeWayIndex.Where (p => p.Value.Count == 2).ToDictionary (p => p.Key, p => p.Value);
		intersectionWayIndex = nodeWayIndex.Where (p => p.Value.Count > 2).ToDictionary (p => p.Key, p => p.Value);
	}

	public static WayReference getWayReference (long id1, long id2) {
		foreach (WayReference wayReference in nodeWayIndex[id1]) {
			if (wayReference.node2.Id == id2 || wayReference.node1.Id == id2) {
				return wayReference;
			}
		}

		foreach (WayReference wayReference in nodeWayIndex[id2]) {
			if (wayReference.node2.Id == id1 || wayReference.node1.Id == id1) {
				return wayReference;
			}
		}

		return null;
	}

	public static Pos getPosById (long id) {
		return NodeIndex.nodes[id];
	}
}
