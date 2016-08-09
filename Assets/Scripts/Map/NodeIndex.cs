using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	public static Dictionary<long, Tuple3<Pos, WayReference, Vector3>> humanSpawnPointsInfo = new Dictionary<long, Tuple3<Pos, WayReference, Vector3>>();

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

		endPointDriveWayIndex = endPointIndex.Where (p => p.Value[0].way.WayWidthFactor >= WayHelper.MINIMUM_DRIVE_WAY).ToDictionary (p => p.Key, p => p.Value);

		foreach (KeyValuePair<long, List<WayReference>> nodeWithWays in nodeWayIndex) {
			List<WayReference> walkPaths = nodeWithWays.Value.Where (p => p.way.WayWidthFactor <= WayHelper.MINIMUM_DRIVE_WAY && p.way.WayWidthFactor > WayTypeEnum.OTHER_FOOTWAY).ToList ();
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
			endPointIndex.ContainsKey(p.Value.Id) && endPointIndex[p.Value.Id][0].way.WayWidthFactor <= WayHelper.MINIMUM_DRIVE_WAY && endPointIndex[p.Value.Id][0].way.WayWidthFactor > WayTypeEnum.OTHER_FOOTWAY
			)
		).ToDictionary(p => p.Key, p => p.Value);

		// Calculate the human spawn positions
		foreach (long nodeOfInterest in nodesOfInterest.Keys) {
			humanSpawnPointsInfo.Add (nodeOfInterest, GetHumanSpawnInfo (nodeOfInterest));
		}
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

	private static Tuple3<Pos, WayReference, Vector3> GetHumanSpawnInfo (long nodeId) {
		Pos spawnPos = nodes [nodeId];
		// Position to spawn/despawn
		Pos spawnNode;
		WayReference closestWay = null;
		Vector3 closestPoint = Vector3.zero;
		if (NodeIndex.nodeWayWalkPathIndex.ContainsKey (spawnPos.Id)) {
			// Spawn in a node
			spawnNode = spawnPos;
			closestWay = NodeIndex.nodeWayWalkPathIndex [spawnPos.Id] [0];
			closestPoint = Game.getCameraPosition (spawnNode);
		} else {
			// Calculate which node is closest
			spawnNode = PosHelper.getClosestNode (spawnPos, NodeIndex.walkNodes);
			// Pick closest wayReference
			Vector3 spawnPosVector = Game.getCameraPosition (spawnPos);
			float closestDistance = float.MaxValue;
			List<WayReference> ways = NodeIndex.nodeWayIndex [spawnNode.Id];
			foreach (WayReference way in ways) {
				Vector3 wayStart = Game.getCameraPosition (way.node1);
				Vector3 wayEnd = Game.getCameraPosition (way.node2);
				Vector3 projectedPoint = Math3d.ProjectPointOnLineSegment (wayStart, wayEnd, spawnPosVector);
				float distance = PosHelper.getVectorDistance (spawnPosVector, projectedPoint);
				if (distance < closestDistance) {
					closestDistance = distance;
					closestWay = way;
					closestPoint = projectedPoint;
				}
			}
//			DebugFn.arrow (spawnPosVector, closestPoint);
		}
//		DebugFn.square (closestPoint);

		return Tuple3.New (spawnNode, closestWay, closestPoint);
	}

	public static Pos getPosClosestTo (Vector3 mouseWorldPoint, bool driveWay = true) {
		long closestNodeId = -1L;
		float closestNodeDistance = float.MaxValue;
		foreach (long id in nodes.Keys) {
			Pos node = nodes[id];
			if (driveWay && nodeIdHasDriveway(id) || !driveWay && walkNodes.Contains(node)) {
				Vector3 wayPoint = Game.getCameraPosition (node); 
				float nodeDistance = Misc.getDistance(mouseWorldPoint, wayPoint);
				if (nodeDistance < closestNodeDistance) {
					closestNodeId = id;
					closestNodeDistance = nodeDistance;
				}
			}
		}
		return closestNodeId != -1L ? nodes [closestNodeId] : null;
	}

	private static bool nodeIdHasDriveway (long id) {
		if (nodeWayIndex.ContainsKey (id)) {
			List<WayReference> wayReferences = nodeWayIndex [id];
			foreach (WayReference wayReference in wayReferences) {
				if (wayReference.way.WayWidthFactor >= WayHelper.MINIMUM_DRIVE_WAY) {
					return true;
				}
			}
		}
		return false;
	}
}
