using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rerouting : MonoBehaviour, IPubSub {

    private static Vector3 VECTOR3_NULL = new Vector3(0, 0, -10000f);

    public GameObject haloObject;

    private GameObject activeRerouteObj;
	private IReroute activeIRerouteObj;
    private bool isVehicle;
   	private Vector3 mouseDownPosition =  VECTOR3_NULL;
   	private Pos mousePosition;

    void Start() {
		PubSub.subscribe("RClick", this);
		PubSub.subscribe("RMove", this); // TODO
    }

	// Update is called once per frame
	void Update () {
        if (activeRerouteObj != null) {
            // TODO - Halo on object

            // TODO - Breadcrumb halos to mouse position

            // TODO - Breadcrumb halos to target position
        }
	}

	public PROPAGATION onMessage(string message, object data) {
        if (message == "RClick") {
			Vector3 position = (Vector3)data;
            if (mouseDownPosition == VECTOR3_NULL) {
            	// Right click when no object is active for rerouting
				InformationBase selectedObject = InformationBase.GetInformationBaseAtPosition(position);
				if (selectedObject != null) {
					mouseDownPosition = position;
					activeRerouteObj = selectedObject.gameObject;
                    activeIRerouteObj = activeRerouteObj.GetComponent<IReroute>();
					isVehicle = selectedObject.type == InformationBase.TYPE_VEHICLE;
                    activeIRerouteObj.pauseMovement();
                    List<Pos> currentPath = activeIRerouteObj.getPath();
					Vector3 objectPosition = selectedObject.gameObject.transform.position;
					haloObject.transform.localPosition = new Vector3(objectPosition.x, objectPosition.y, 0f);
                    haloObject.SetActive(true);
				}
            } else {
                // TODO - Rerouting active, cancel if movement is small, otherwise apply rerouting
//                activeRerouteObj.setPath();
//                activeRerouteObj.resumeMovement();
            }
//            Vector3 position = (Vector3)data;
//            Vector3 mouseWorldPoint = screenToWorldPosInBasePlane (position);
//            Pos pos = NodeIndex.getPosClosestTo (mouseWorldPoint);
        }
		return PROPAGATION.DEFAULT;
	}
}
