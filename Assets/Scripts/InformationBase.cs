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

        // only name means it's only a reference to this object - which should be linked
		return new List<KeyValuePair<string, object>> {
			onlyName ? new KeyValuePair<string, object> ("Name", new InformationLink(this)) : new KeyValuePair<string, object> ("Name", name)
		};
	}

	public virtual void disposeInformation() {
		
	}

    public class InformationLink {
        public string name;
        public InformationBase informationBase;

        public InformationLink(InformationBase obj) {
            this.name = obj.name;
            this.informationBase = obj;
        }
    }
}
