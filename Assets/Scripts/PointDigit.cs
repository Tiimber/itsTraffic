using UnityEngine;
using System.Collections;

public class PointDigit : MonoBehaviour {

    public static Vector3 NumberScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Points3D parent;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void remove() {
		Destroy (gameObject);
	}

	public void setDigit(GameObject digit) {
        Destroy (transform.GetChild(0).gameObject);

        GameObject number3DObj = Instantiate (digit, transform, false) as GameObject;
        number3DObj.transform.localPosition = Vector3.zero;
        number3DObj.transform.localScale = PointDigit.NumberScale;
	}
}
