using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HumanCollider : MonoBehaviour {

	public static Dictionary<string, string> colliderNamesForGameObjectName = new Dictionary<string, string>{
		{"shirtmesh", "BODY"},
		{"skinmesh", "VISION"}
	};

	void OnTriggerEnter (Collider col) {
		HumanLogic human = GetComponentInParent<HumanLogic>();
		human.reportCollision (col, HumanCollider.colliderNamesForGameObjectName[name]);
	}

	void OnTriggerExit (Collider col) {
		HumanLogic human = GetComponentInParent<HumanLogic>();
		human.reportColliderExit (col, HumanCollider.colliderNamesForGameObjectName[name]);
	}
}
