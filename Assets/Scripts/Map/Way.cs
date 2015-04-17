using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Way : NodeWithTags {
	private List<Pos> nodes = new List<Pos> ();

	public Way (long id) : base(id) {
	}

	public List<Pos> getPoses () {
		return nodes;
	}

	public void addPos (Pos node) {
		nodes.Add (node);
	}
}
