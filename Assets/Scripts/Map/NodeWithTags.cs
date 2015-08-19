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

	public string getTagValue(string key) {
		string value = null;
		foreach (Tag tag in tags) {
			if (tag.Key == key) {
				value = tag.Value;
				break;
			}
		}
		return value;
	}

	virtual public void addTag (Tag tag) {
		tags.Add (tag);
	}

//	virtual public void processTags () {}
}
