using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NodeWithTags : IdNode {
	public List<Tag> tags = new List<Tag> ();

	public NodeWithTags (long id) : base(id) {
	}

	public List<Tag> getTags () {
		return tags;
	}

	virtual public void addTag (Tag tag) {
		tags.Add (tag);
	}
}
