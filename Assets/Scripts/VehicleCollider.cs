using UnityEngine;
using System.Collections;

public class VehicleCollider: MonoBehaviour {

	void OnTriggerEnter (Collider col) {
		Vehicle parent = GetComponentInParent<Vehicle>();
		parent.reportCollision (col, name);
	}

	void OnTriggerExit (Collider col) {
		Vehicle parent = GetComponentInParent<Vehicle>();
		parent.reportColliderExit (col, name);
	}

	void OnMouseDown () {
		if (name == "CAR") {
			Vehicle parent = GetComponentInParent<Vehicle>();
			parent.setDebug ();
		}
	}
}
