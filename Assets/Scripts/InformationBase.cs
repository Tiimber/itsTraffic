using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class InformationBase : MonoBehaviour {

	public bool passive = false;
	public string type;
	protected new string name;

    public const string TYPE_POI = "Point Of Interest";
    public const string TYPE_HUMAN = "Human";
    public const string TYPE_VEHICLE = "Vehicle";

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public virtual List<KeyValuePair<string, object>> getInformation (bool onlyName = false) {
		
		return new List<KeyValuePair<string, object>> {
			new KeyValuePair<string, object> ("Name", name)
		};
	}

	public virtual void disposeInformation() {
		
	}
}
