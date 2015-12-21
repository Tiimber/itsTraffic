using UnityEngine;
using System.Collections;

public class PointDigit : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void remove() {
		Destroy (gameObject);
	}

	public void setDigit(int digit) {
		TextMesh text = GetComponent<TextMesh> ();
		text.text = "" + digit;
	}
}
