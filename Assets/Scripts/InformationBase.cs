using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class InformationBase : MonoBehaviour {

	public bool passive = false;
	protected string type;
	protected new string name;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public virtual List<KeyValuePair<string, object>> getInformation () {
		
		return new List<KeyValuePair<string, object>> {
			new KeyValuePair<string, object> ("Name", name)
		};
	}

	public virtual void disposeInformation() {
		
	}
}
