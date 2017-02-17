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

    public static InformationBase GetInformationBaseAtPosition(Vector3 position, bool ignorePOI = false) {
        InformationBase clickedInformationBase = null;

        // Check for clicking to get information on actual informationBase-objects
        InformationBase[] informationBaseObjects = FindObjectsOfType<InformationBase> ();

        CircleTouch clickedCircleTouch = null;

        foreach (InformationBase informationBaseObject in informationBaseObjects) {
            if (!informationBaseObject.passive) {
                // Get click position (x,y) in a plane of the objects' Z position
                Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, informationBaseObject.gameObject.transform.position.z));
                Vector2 clickPos = Game.instance.screenToWorldPosInPlane(position, plane);

                float thresholdSurroundingArea = 0.1f * 3f; // Click 0.1 (vehicle length) multiplied by three
                if (informationBaseObject.type == InformationBase.TYPE_POI) {
                    if (ignorePOI) {
                        continue;
                    }
                    thresholdSurroundingArea = 0.07f; // Click area around POI is smaller
                }
                CircleTouch informationObjectTouch = new CircleTouch (informationBaseObject.transform.position, thresholdSurroundingArea);
                if (informationObjectTouch.isInside (clickPos)) {

                    if (informationObjectTouch.isCloser (clickPos, clickedCircleTouch)) {
                        clickedCircleTouch = informationObjectTouch;
                        clickedCircleTouch.setDistance(clickPos);
                        clickedInformationBase = informationBaseObject;
                    }
                }
            }
        }

        return clickedInformationBase;
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
