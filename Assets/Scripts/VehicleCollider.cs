using UnityEngine;
using System.Collections;

public class VehicleCollider: MonoBehaviour {

	void OnTriggerEnter (Collider col) {
		Vehicle parent = transform.parent.gameObject.GetComponent<Vehicle>();
		parent.reportCollision (col, name);
	}
}
