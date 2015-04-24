using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IdNode : BaseNode {
	public long Id { set; get; }

	public IdNode (long id) {
		this.Id = id;
	}
}
