using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WayReference : MonoBehaviour {
	public static int WayId = 0;

	public int Id { set; get; }
	public Way way;
	public Pos node1 { set; get; }
	public Pos node2 { set; get; }
	public bool SmallWay;

	// TODO - This is named wrong!
	public float fieldsFromPos1ToPos2 = 1f; 
	public float fieldsFromPos2ToPos1 = 1f;

	public float getNumberOfFields () {
		return fieldsFromPos1ToPos2 + fieldsFromPos2ToPos1;
	}

	public float getNumberOfFieldsInDirection (Pos toPosition) {
		return isNode1 (toPosition) ? fieldsFromPos1ToPos2 : fieldsFromPos2ToPos1;
	}

	public float getNumberOfFieldsInDirection (bool toPos) {
		return toPos ? fieldsFromPos1ToPos2 : fieldsFromPos2ToPos1;
	}

	private float carCost = -1f;
	private float getCarCost() {
		if (carCost == -1f) {
            if (way.WayWidthFactor < WayTypeEnum.PEDESTRIAN) {
                carCost = 1000f;
            } else {
                carCost = transform.localScale.magnitude / way.WayWidthFactor;
            }
		}
		return carCost;
	}

	private float walkCost = -1f;
	private float getWalkCost() {
		if (walkCost == -1f) {
			if (way.WayWidthFactor <= WayTypeEnum.PEDESTRIAN) {
				walkCost = transform.localScale.magnitude;
			} else {
				walkCost = transform.localScale.magnitude * (1f + way.WayWidthFactor);
			}
		}
		return walkCost;
	}

	public float getTravelCost(bool isVehicle) {
		if (isVehicle) {
			return getCarCost ();
		} else {
			return getWalkCost ();
		}
	}

	// TODO - Flag if one-way (and if so, in which direction)

	public bool isNode1 (Pos pos) {
		return pos.Id == node1.Id;
	}

	public Pos getOtherNode (Pos pos) {
		return isNode1(pos) ? node2 : node1;
	}

	public bool hasNodes (Pos pos1, Pos pos2) {
		return (node1 == pos1 && node2 == pos2) || (node1 == pos2 && node2 == pos1);
	}

	public Pos getClosestNode(Vector3 position) {
		return (position - Game.getCameraPosition(node1)).magnitude <= (position - Game.getCameraPosition(node2)).magnitude ? node1 : node2;
	}
}
