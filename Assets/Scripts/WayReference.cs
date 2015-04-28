using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WayReference : MonoBehaviour {
	public static int WayId = 0;

	public int Id { set; get; }
	public Way way;
	public Pos node1 { set; get; }
	public Pos node2 { set; get; }

	// TODO - Temporary
	public Color OriginalColor { set; get; }
	public WayReference () : base() {
		OriginalColor = Color.magenta;
	}

	void OnMouseEnter () {
		Game.CurrentWayReference = this;
	}

	void OnMouseExit () {
		if (OriginalColor != Color.magenta) {
			gameObject.GetComponent<Renderer>().material.color = OriginalColor;
			OriginalColor = Color.magenta;
		}
		Game.CurrentWayReference = null;

		List<WayReference> node1Connections = NodeIndex.nodeWayIndex[node1.Id];
		List<WayReference> node2Connections = NodeIndex.nodeWayIndex[node2.Id];
		foreach (WayReference node1Connection in node1Connections) {
			if (node1Connection != this) {
				GameObject node1WayObject = node1Connection.gameObject;
				if (node1Connection.OriginalColor != Color.magenta) {
					node1WayObject.gameObject.GetComponent<Renderer>().material.color = node1Connection.OriginalColor;
					node1Connection.OriginalColor = Color.magenta;
				}
			}
		}
		foreach (WayReference node2Connection in node2Connections) {
			if (node2Connection != this) {
				GameObject node2WayObject = node2Connection.gameObject;
				if (node2Connection.OriginalColor != Color.magenta) {
					node2WayObject.gameObject.GetComponent<Renderer>().material.color = node2Connection.OriginalColor;
					node2Connection.OriginalColor = Color.magenta;
				}
			}
		}

	}
}
