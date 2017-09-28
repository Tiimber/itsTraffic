using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rerouting : MonoBehaviour, IPubSub {

    private const float NUMBER_HALOS_MAX = 50f;
    private const float NUMBER_HALOS_MIN = 15f;
    private const float DISTANCE_FOR_REROUTE = 40f;

    private static int numberOfHalosInPath = 15;

    private static List<GameObject> originalPathHalos = new List<GameObject>();
    private static List<GameObject> reroutePathHalos = new List<GameObject>();


    public GameObject haloObject;
    public GameObject originalParentObj;
    public GameObject originalPartObj;
    public GameObject rerouteParentObj;
    public GameObject reroutePartObj;

   	private Vector3 mouseDownPosition =  Misc.VECTOR3_NULL;
   	private Pos mousePosition;

    void Start() {
		PubSub.subscribe("RClick", this);
		PubSub.subscribe("RMove", this);
		PubSub.subscribe("Graphics:quality", this);

        createHaloObjects();
        inactivateHaloObjects(originalPathHalos);
        inactivateHaloObjects(reroutePathHalos);

        // TODO - This should be published at startup - have it here for now
        float quality = Game.instance.graphicsQuality;
        numberOfHalosInPath = Mathf.FloorToInt(NUMBER_HALOS_MIN + (quality * (NUMBER_HALOS_MAX - NUMBER_HALOS_MIN)));
    }

    private void createHaloObjects() {
        reroutePathHalos.Add(reroutePartObj);
        for (int i = 1; i < NUMBER_HALOS_MAX; i++) {
			GameObject clone = Instantiate(reroutePartObj, rerouteParentObj.transform);
            reroutePathHalos.Add(clone);
        }

        originalPathHalos.Add(originalPartObj);
        for (int i = 1; i < NUMBER_HALOS_MAX; i++) {
			GameObject clone = Instantiate(originalPartObj, originalParentObj.transform);
            originalPathHalos.Add(clone);
        }
    }

//    private void activateHaloObjects(List<GameObject> pathHalos) {
//        float quality = Game.instance.graphicsQuality;
//        numberOfHalosInPath = Mathf.FloorToInt(NUMBER_HALOS_MIN + (quality * (NUMBER_HALOS_MAX - NUMBER_HALOS_MIN)));
//        for (int i = 0; i < pathHalos.Count; i++) {
//            pathHalos[i].SetActive(i < numberOfHalosInPath);
             // TODO - Can only set color on Halos if they are not Halos, but rather Lights (Halo lights)
//        }
//    }

    private void inactivateHaloObjects(List<GameObject> pathHalos) {
        for (int i = 0; i < NUMBER_HALOS_MAX; i++) {
            pathHalos[i].SetActive(false);
        }
    }

	public PROPAGATION onMessage(string message, object data) {
        if (message == "RClick") {
			Vector3 position = (Vector3)data;
            if (mouseDownPosition == Misc.VECTOR3_NULL) {
            	// Right click when no object is active for rerouting
				InformationBase selectedObject = InformationBase.GetInformationBaseAtPosition(position);
				if (selectedObject != null) {
                    GameObject rerouteGameObject = selectedObject.gameObject;
                    IReroute componentIReroute = rerouteGameObject.GetComponent<IReroute>();
                    if (componentIReroute.isRerouteOk()) {
                        mouseDownPosition = position;
                        RerouteInfo.gameObject = rerouteGameObject;
                        RerouteInfo.iReroute = componentIReroute;
                        RerouteInfo.isVehicle = selectedObject.type == InformationBase.TYPE_VEHICLE;
                        RerouteInfo.iReroute.pauseMovement();
                        RerouteInfo.isReroute = false;
                        RerouteInfo.originalPath = RerouteInfo.iReroute.getPath();
                        RerouteInfo.positionOriginalPathHalos();
                        Vector3 objectPosition = rerouteGameObject.transform.position;
                        haloObject.transform.localPosition = new Vector3(objectPosition.x, objectPosition.y, 0f);
                        haloObject.SetActive(true);
                        originalParentObj.SetActive(true);
                        rerouteParentObj.SetActive(false);
                        inactivateHaloObjects(reroutePathHalos);
                    } else {
                        // Re-routing not accepted, play sound
                        GenericSoundEffects.playRerouteUnavailable();
                    }
				}
            } else {

                if (RerouteInfo.isReroute && RerouteInfo.reroutePath.Count > 0) {
                    RerouteInfo.iReroute.setPath(RerouteInfo.reroutePath);
                }
                haloObject.SetActive(false);
                originalParentObj.SetActive(false);
                rerouteParentObj.SetActive(false);
                inactivateHaloObjects(originalPathHalos);
                inactivateHaloObjects(reroutePathHalos);
                RerouteInfo.iReroute.resumeMovement();

                RerouteInfo.gameObject = null;
                RerouteInfo.iReroute = null;
                mouseDownPosition = Misc.VECTOR3_NULL;
            }
        } else if (message == "RMove") {
            if (RerouteInfo.gameObject != null) {
                Vector3 mousePos = (Vector3)data;
                Vector3 objectScreenPos = Game.instance.objectToScreenPos(RerouteInfo.gameObject);
                float moveFromObject = Misc.getDistance(mousePos, objectScreenPos);
                if (moveFromObject >= DISTANCE_FOR_REROUTE) {
// Get re-route pos object
                    Vector2 mousePosVector2 = Game.instance.screenToWorldPosInBasePlane(mousePos);
                    Pos pos = NodeIndex.getPosClosestTo(mousePosVector2, RerouteInfo.isVehicle);

                    int pathPoints = RerouteInfo.originalPath.Count;
                    Pos addToPathPos = null;
                    Pos targetPos = RerouteInfo.originalPath[RerouteInfo.originalPath.Count - 1];
                    if (targetPos.Id == -1L) {
                        addToPathPos = targetPos;
                        targetPos = RerouteInfo.originalPath[RerouteInfo.originalPath.Count - 2];
                        pathPoints--;
                    }

                    if (pathPoints > 2) {
                        Pos startPos = RerouteInfo.originalPath[1];
                        RerouteInfo.reroutePath = Game.calculateCurrentPaths(startPos, targetPos, RerouteInfo.originalPath[0], pos, RerouteInfo.isVehicle, !RerouteInfo.isVehicle);

                        if (RerouteInfo.reroutePath.Count > 0) {
                            RerouteInfo.reroutePath.Insert(0, Game.createTmpPos(RerouteInfo.gameObject.transform.position));
                            if (addToPathPos != null) {
                                // Remove last path, if endPos is on this wayReference
                                if (!RerouteInfo.isVehicle && RerouteInfo.gameObject.GetComponent<HumanLogic>().endWay.hasNodes(RerouteInfo.reroutePath[RerouteInfo.reroutePath.Count - 2], RerouteInfo.reroutePath[RerouteInfo.reroutePath.Count - 1])) {
                                    RerouteInfo.reroutePath.RemoveAt(RerouteInfo.reroutePath.Count - 1);
                                }

                                // Human should walk to end pos (tmp Pos object)
                                RerouteInfo.reroutePath.Add(addToPathPos);
                            }
//                        if (!RerouteInfo.isVehicle && isGoingFrontAndBackOnWay(RerouteInfo.originalPath)) {
//                            RerouteInfo.reroutePath.RemoveAt(1);
//                        }
                            RerouteInfo.isReroute = true;
                            RerouteInfo.positionReroutePathHalos();
                            rerouteParentObj.SetActive(true);
                            originalParentObj.SetActive(false);
                            inactivateHaloObjects(originalPathHalos);
                        } else {
                            RerouteInfo.isReroute = false;
                            originalParentObj.SetActive(true);
                            rerouteParentObj.SetActive(false);
                            inactivateHaloObjects(reroutePathHalos);
                        }
                    }
                }
            } else if (RerouteInfo.isReroute) {
                RerouteInfo.isReroute = false;
                originalParentObj.SetActive(true);
                rerouteParentObj.SetActive(false);
                inactivateHaloObjects(reroutePathHalos);
            }
//            Debug.Log(objectScreenPos + " - " + mousePos);

//            Vector3 objectPosition = Game.getCameraPosition(pos);
//            haloObject.transform.localPosition = new Vector3(objectPosition.x, objectPosition.y, 0f);
//            haloObject.SetActive(true);


        } else if (message == "Graphics:quality") {
            float quality = Game.instance.graphicsQuality;
            numberOfHalosInPath = Mathf.FloorToInt(NUMBER_HALOS_MIN + (quality * (NUMBER_HALOS_MAX - NUMBER_HALOS_MIN)));
//            activateHaloObjects(originalPathHalos);
//            activateHaloObjects(reroutePathHalos);
        }
		return PROPAGATION.DEFAULT;
	}

    private class RerouteInfo {
        public static bool isVehicle;
        public static GameObject gameObject;
        public static IReroute iReroute;

        public static bool isReroute; // TODO - Maybe just use "currentReroutePoint" below instead
        public static Pos currentReroutePoint;
        public static List<Pos> originalPath;
        public static List<Pos> reroutePath;

        public static void positionOriginalPathHalos() {
            positionHalos(originalPath, originalPathHalos);
        }

        public static void positionHalos(List<Pos> path, List<GameObject> pathHalos) {
            // Calculate total length from object to target point
            float totalPathLength = 0;
            Vector3 originalObjectPos = gameObject.transform.position;
            Vector3 objectPos = new Vector3(originalObjectPos.x, originalObjectPos.y, 0f);
            Vector3 previousPos = objectPos;
            for (int i = 1; i < path.Count; i++) {
                Vector3 currentPos = Game.getCameraPosition(path[i]);
                totalPathLength += (currentPos - previousPos).magnitude;
                previousPos = currentPos;
            }

//            Debug.Log("Total path length: " + totalPathLength);

            // Distribute points along the way
            float eachPointDistance = totalPathLength / numberOfHalosInPath;
//            Debug.Log("Number of halos: " + numberOfHalosInPath);
//            Debug.Log("Each point distance: " + eachPointDistance);
            float lastPointDistance = 0f;
            totalPathLength = 0;
            previousPos = objectPos;
            int currentHaloIndex = 0;
            for (int i = 1; i < path.Count; i++) {
                Vector3 currentPos = Game.getCameraPosition(path[i]);
                float startOfPosLength = totalPathLength;
                totalPathLength += (currentPos - previousPos).magnitude;
                while (lastPointDistance + eachPointDistance <= totalPathLength) {
                    lastPointDistance += eachPointDistance;
//                    Debug.Log("[" + currentHaloIndex + "] Positioning at: " + lastPointDistance);
                    setHaloPosition(pathHalos[currentHaloIndex++], previousPos, currentPos, startOfPosLength, totalPathLength, lastPointDistance);
                }
                previousPos = currentPos;
            }
        }

        public static void positionReroutePathHalos() {
            positionHalos(reroutePath, reroutePathHalos);
        }

        private static void setHaloPosition(GameObject haloObject, Vector3 startVector, Vector3 endVector, float startPos, float totalPathLength, float placeHaloAtPos) {
            float length = totalPathLength - startPos;
            float percentOfLength = (placeHaloAtPos - startPos) / length;
            Vector3 haloPosition = startVector + (endVector - startVector) * percentOfLength;
            haloObject.transform.localPosition = haloPosition;
            haloObject.SetActive(true);
        }
    }
}

// TODO - Next time:
// 1. Draw loops in reroute (right click without moving mouse)
// 2. Potentially fix bug with reroute then move back mouse to original position - drawing one halo only