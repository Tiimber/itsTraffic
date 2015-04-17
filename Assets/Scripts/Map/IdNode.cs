using UnityEngine;
using System.Collections;

public class IdNode : BaseNode {
	public long Id { set; get; }

	public IdNode (long id) {
		this.Id = id;
	}
}
