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

	// TODO - Temporary
	public Color OriginalColor { set; get; }
	public WayReference () : base() {
		OriginalColor = Color.magenta;
	}

	void OnMouseOver () {
		Pos selectedPos = getSelectedPos ();
		Game.CurrentWayReference = new KeyValuePair<Pos, WayReference> (selectedPos, this);
		Game.CurrentPath = null;
	}

	void OnMouseEnter () {
		Pos selectedPos = getSelectedPos ();
		Game.CurrentWayReference = new KeyValuePair<Pos, WayReference>(selectedPos, this);
		Game.CurrentPath = null;
	}
	
	private Pos getSelectedPos () {
		RaycastHit hit = new RaycastHit();
		bool isLeft = true;
		Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity);
		if (hit.collider != null) {
			List<Collider> colliders = gameObject.GetComponents<Collider> ().ToList ();
			bool hasFour = colliders.Count == 4;
			isLeft = hit.collider == colliders[0] || (hasFour && hit.collider == colliders[2]);
		}
		return isLeft ? node1 : node2;
	}

	void OnMouseExit () {
		if (OriginalColor != Color.magenta) {
			gameObject.GetComponent<Renderer>().material.color = OriginalColor;
			OriginalColor = Color.magenta;
		}
		Game.CurrentWayReference = new KeyValuePair<Pos, WayReference>();
		Game.CurrentPath = null;

		if (NodeIndex.nodeWayIndex.ContainsKey (node1.Id)) {
			List<WayReference> node1Connections = NodeIndex.nodeWayIndex [node1.Id];
			List<WayReference> node2Connections = NodeIndex.nodeWayIndex [node2.Id];
			foreach (WayReference node1Connection in node1Connections) {
				if (node1Connection != this) {
					GameObject node1WayObject = node1Connection.gameObject;
					if (node1Connection.OriginalColor != Color.magenta) {
						node1WayObject.gameObject.GetComponent<Renderer> ().material.color = node1Connection.OriginalColor;
						node1Connection.OriginalColor = Color.magenta;
					}
				}
			}
			foreach (WayReference node2Connection in node2Connections) {
				if (node2Connection != this) {
					GameObject node2WayObject = node2Connection.gameObject;
					if (node2Connection.OriginalColor != Color.magenta) {
						node2WayObject.gameObject.GetComponent<Renderer> ().material.color = node2Connection.OriginalColor;
						node2Connection.OriginalColor = Color.magenta;
					}
				}
			}
		}
	}

	void OnMouseDown () {
		Pos selectedPos = getSelectedPos ();
		Game.CurrentTarget = new KeyValuePair<Pos, WayReference>(selectedPos, this);
	}

	public bool isNode1 (Pos pos) {
		return pos.Id == node1.Id;
	}

	public Pos getOtherNode (Pos pos) {
		return isNode1(pos) ? node2 : node1;
	}

	public bool hasNodes (Pos pos1, Pos pos2) {
		return (node1 == pos1 && node2 == pos2) || (node1 == pos2 && node2 == pos1);
	}
}
