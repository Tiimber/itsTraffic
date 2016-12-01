using System;
using System.Collections.Generic;

public class NodeWithTags : IdNode {
	public List<Tag> tags = new List<Tag> ();

	public NodeWithTags (long id) : base(id) {
	}

	public List<Tag> getTags () {
		return tags;
	}

    public bool hasTag(string key) {
        return getTagValue(key) != null;
    }

    public bool hasTagWithValues(string key, params string[] values) {
		string tagValue = getTagValue (key);
        return Array.IndexOf(values, tagValue) != -1;
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

	public string printTags () {
		string printValue = "";
		foreach (Tag tag in tags) {
			printValue += (printValue.Length > 0 ? ", " : "") + printTag (tag);
		}
		return printValue;
	}

	public string printTag (Tag tag) {
		return tag.Key + ": " + tag.Value;
	}
}
