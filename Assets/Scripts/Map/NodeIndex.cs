using System.Collections.Generic;
using System.Linq;

public class NodeIndex
{
	public static Dictionary<long, Pos> nodes = new Dictionary<long, Pos> ();
	public static Dictionary<long, Pos> nodesOfInterest = new Dictionary<long, Pos> ();

	public static List<long> uninterestingNodeId = new List<long>();

	public static Dictionary<long, List<WayReference>> nodeWayIndex = new Dictionary<long, List<WayReference>>();
	
	public static Dictionary<long, List<WayReference>> endPointIndex = new Dictionary<long, List<WayReference>>();
	public static Dictionary<long, List<WayReference>> straightWayIndex = new Dictionary<long, List<WayReference>>();
	public static Dictionary<long, List<WayReference>> intersectionWayIndex = new Dictionary<long, List<WayReference>>();

	public static Dictionary<long, List<WayReference>> endPointDriveWayIndex = new Dictionary<long, List<WayReference>>();
	public static Dictionary<long, List<WayReference>> nodeWayWalkPathIndex = new Dictionary<long, List<WayReference>>();
	public static List<Pos> walkNodes = new List<Pos>();

	public static Dictionary<long, List<WayReference>> buildingOutlines = new Dictionary<long, List<WayReference>>();

	public static List<long> buildingWayIds = new List<long> ();
	public static HashSet<long> nodeIdsForBuildingWays = new HashSet<long> ();
	
	public static void addWayReferenceToNode (long id, WayReference partOfWay) {
		if (!nodeWayIndex.ContainsKey (id)) {
			nodeWayIndex.Add(id, new List<WayReference>());
		}
		nodeWayIndex [id].Add (partOfWay);
	}
		
	public static void addUninterestingNodeId (long id) {
		uninterestingNodeId.Add (id);
	}

	public static void calculateIndexes () {
		endPointIndex = nodeWayIndex.Where (p => p.Value.Count == 1 && !p.Value[0].way.EndPointImpossible).ToDictionary (p => p.Key, p => p.Value);
		straightWayIndex = nodeWayIndex.Where (p => p.Value.Count == 2).ToDictionary (p => p.Key, p => p.Value);
		intersectionWayIndex = nodeWayIndex.Where (p => p.Value.Count > 2).ToDictionary (p => p.Key, p => p.Value);

		endPointDriveWayIndex = endPointIndex.Where (p => p.Value[0].way.WayWidthFactor >= WayTypeEnum.MINIMUM_DRIVE_WAY).ToDictionary (p => p.Key, p => p.Value);

		foreach (KeyValuePair<long, List<WayReference>> nodeWithWays in nodeWayIndex) {
			List<WayReference> walkPaths = nodeWithWays.Value.Where (p => p.way.WayWidthFactor <= WayTypeEnum.MINIMUM_DRIVE_WAY && p.way.WayWidthFactor > WayTypeEnum.OTHER_FOOTWAY).ToList ();
			if (walkPaths.Count > 0) {
				nodeWayWalkPathIndex.Add (nodeWithWays.Key, walkPaths);
			}
		}

		walkNodes = nodes.Where (p => nodeWayWalkPathIndex.ContainsKey (p.Key)).ToDictionary (p => p.Key, p => p.Value).Values.ToList();

		// Places where people may spawn
		nodesOfInterest = nodes.Where (p => (
				!uninterestingNodeId.Contains(p.Value.Id) &&
				!nodeIdsForBuildingWays.Contains(p.Value.Id) &&
				!nodeWayIndex.ContainsKey (p.Value.Id) 
			) || (
			endPointIndex.ContainsKey(p.Value.Id) && endPointIndex[p.Value.Id][0].way.WayWidthFactor <= WayTypeEnum.MINIMUM_DRIVE_WAY && endPointIndex[p.Value.Id][0].way.WayWidthFactor > WayTypeEnum.OTHER_FOOTWAY
			)
		).ToDictionary(p => p.Key, p => p.Value);
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
