using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EmergencyDispatch : MonoBehaviour, IPubSub {

    public static EmergencyDispatch instance;

    public enum EMERGENCY_TYPE {
        POLICE,
        FIRE_FIGHTER,
        AMBULANCE
    }

    public enum UNIT_STATUS {
        INVOLVED_IN_EMERGENCY,

        SITUATION_RESOLVED,
        ARRIVED_AT_SCENE,
        ON_THE_WAY,
        AWAITING_SPAWN
    }

    public const string REPORT_MAJOR_CRASH = "Report:majorCrash";
    public const string REPORT_FIRE = "Report:fire";
    public const string REPORT_INJURY = "Report:injury";

    public enum SEVERITY {
        MINOR,
        MEDIUM,
        MAJOR,
        EXTREME
    }

    public long emergencyId = 0L;
    private Dictionary<long, EmergencyInfo> emergencies = new Dictionary<long, EmergencyInfo>();

    void Start() {
        EmergencyDispatch.instance = this;
        PubSub.subscribe("Report:majorCrash", this);
    }

    public PROPAGATION onMessage(string message, object data) {
        bool offMap = false;
        Vector3 targetPosition = Misc.VECTOR3_NULL;
        Vehicle vehicleRef = null;
        if (data != null && typeof(Vehicle) == data.GetType()) {
            vehicleRef = (Vehicle)data;
            targetPosition = vehicleRef.transform.position;
        }
        emergencyId++;
        switch (message) {
            case REPORT_MAJOR_CRASH:
                int neededPolice = 2;
                // TODO - When going to a specific point, "offMap" parameter need to be false
                registerEmergency(emergencyId, message, offMap, 20f);
                grabOrSpawn(neededPolice, EMERGENCY_TYPE.POLICE, targetPosition, vehicleRef);
                break;
            case REPORT_FIRE:
                break;
            case REPORT_INJURY:
                break;
            // TODO - Vehicle report status when queued for spawn, resolving emergency and acknowledging when received alert
        }
        return PROPAGATION.DEFAULT;
    }

    private List<Vehicle> pickUnits(int number, Vector3 closestToPosition, List<Vehicle> availableUnits) {
        availableUnits.Sort((vehicle1, vehicle2) => (vehicle1.transform.position - closestToPosition).magnitude - (vehicle2.transform.position - closestToPosition).magnitude < 0 ? -1 : 1);
        availableUnits.RemoveRange(number, availableUnits.Count - number);
        return availableUnits;
    }

    private BoxCollider createCollider(Vector3 position, Vehicle potentialVehicle) {
        // WayReference of emergency
        WayReference closestWayReference = NodeIndex.getClosestWayReference(position);

        GameObject colliderGameObject = new GameObject();
        colliderGameObject.name = "EmergencyCollider:" + emergencyId;
        colliderGameObject.transform.rotation = closestWayReference.transform.rotation * Quaternion.Euler(0f, 0f, 90f);
        colliderGameObject.transform.position = Misc.GetProjectedPointOnLine(Misc.NoZ(position), Game.getCameraPosition(closestWayReference.node1), Game.getCameraPosition(closestWayReference.node2));
        BoxCollider collider = colliderGameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(closestWayReference.transform.localScale.y,0.1f, 3f);
        return collider;
    }

    private void growCollider (BoxCollider collider) {
        float growSize = 0.0025f;
        collider.size = new Vector3(collider.size.x + growSize, collider.size.y + growSize, collider.size.z);
    }

    private void grabOrSpawn(int numberOfUnits, EMERGENCY_TYPE type, Vector3 closestToPosition, Vehicle potentialVehicle) {

        // TODO - Based on "type"
        string brand = "Po-liz";
        List<Vehicle> availableEmergencyUnits = Misc.GetVehicles(brand);

        // If position is not on map... pretend it's on of our endpoints
        if (closestToPosition == Misc.VECTOR3_NULL) {
            long endPointNode = Misc.pickRandomKey(NodeIndex.endPointDriveWayIndex);
            if (endPointNode != 0L) {
                closestToPosition = Game.getCameraPosition(NodeIndex.getPosById(endPointNode));
            }
        } else {
            // Make a collider, this will grow once emergency vehicles are standing still - when dispatching vehicle sees this, it has arrived
            BoxCollider collider = createCollider(closestToPosition, potentialVehicle);
            registerCollider(emergencyId, collider);
        }

        // Filter the units that are already on the way to other emergencies - and can re-route to given position
        availableEmergencyUnits = availableEmergencyUnits.FindAll(vehicle => !vehicle.areSirensOn() && vehicle.canReroute(closestToPosition));

        // If there are too many, pick out some, either by position, or randomly...
        if (availableEmergencyUnits.Count > numberOfUnits) {
            availableEmergencyUnits = pickUnits(numberOfUnits, closestToPosition, availableEmergencyUnits);
        }

        // Dispatch units on map
        dispatchUnits(availableEmergencyUnits, closestToPosition);

        // If there aren't units on the map, spawn "the rest"
        spawnUnits(numberOfUnits - availableEmergencyUnits.Count, type, closestToPosition);

//        Debug.Log(availableEmergencyUnits.Count);
    }

    [InspectorButton("callPoliceFn")]
    public bool callPolice;
    [InspectorButton("callPoliceDjakneFn")]
    public bool callPoliceDjakne;
    [InspectorButton("callFireFighterFn")]
    public bool callFireFighter;
    [InspectorButton("callAmbulanceFn")]
    public bool callAmbulance;

    private void callPoliceFn() {
        PubSub.publish(REPORT_MAJOR_CRASH);
    }

    private void callPoliceDjakneFn() {
        GameObject vehicleGameObject = GameObject.Find ("Vehicle (id:10001)");
        Vehicle vehicle = vehicleGameObject.GetComponent<Vehicle>();
        vehicle.makeCrash();
    }

    private void callFireFighterFn() {
        PubSub.publish(REPORT_FIRE);
    }

    private void callAmbulanceFn() {
        PubSub.publish(REPORT_INJURY);
    }

    private void dispatchUnits(List<Vehicle> units, Vector3 targetPosition) {
        units.ForEach(vehicle => vehicle.dispatchTo(targetPosition, emergencyId));
    }

    private void spawnUnits(int numberOfUnits, EMERGENCY_TYPE type, Vector3 targetPosition) {
        for (int i = 0; i < numberOfUnits; i++) {
            spawnUnit(type, targetPosition);
        }
        EmergencyDispatch.Report(UNIT_STATUS.AWAITING_SPAWN, null, emergencyId);
    }

    private void spawnUnit(EMERGENCY_TYPE type, Vector3 targetPosition) {
        List<Pos> endPositions = getEndPosForVector(targetPosition);

        Pos startPos;
        do {
            startPos = NodeIndex.getPosById(Misc.pickRandomKey(NodeIndex.endPointDriveWayIndex));
        } while (startPos.Id == endPositions[0].Id || (endPositions.Count == 2 && startPos.Id == endPositions[1].Id));

        // Find out which is the closest of the two end points
        Pos endPos = null;
        Pos secondLastPos = null;
        if (endPositions.Count == 1) {
            endPos = endPositions[0];
        } else {
            if (Game.isPathToFirstClosest(startPos, endPositions[0], endPositions[1])) {
                endPos = endPositions[1];
                secondLastPos = endPositions[0];
            } else {
                endPos = endPositions[0];
                secondLastPos = endPositions[1];
            }
        }

        // TODO - When having Ambulance or Fire Truck, change some of these
        // TODO - Which of these SHOULD be "Po-liz", and which shouldn't
        float speedFactor = Misc.randomRange (1.1f, 1.5f);
        float acceleration = Misc.randomRange (3f, 3.5f);
        long newId = 0L;
        string newName = "Po-liz";
        float newTime = GameTimer.elapsedTime() + Misc.randomRange(5f, 15f);
        List<long> newWayPoints = secondLastPos != null ? new List<long>(){secondLastPos.Id} : new List<long>();
        string newBrand = "Po-liz";
        string newType = "Po-liz";
        List<long> newPassengerIds = new List<long>();
        List<float> newMood = new List<float>(){1f, 0f, 1.5f};
        Setup.VehicleSetup vehicleSetup = new Setup.VehicleSetup(newId, newName, newTime, startPos.Id, endPos.Id, null, false, false, newWayPoints, newBrand, null, newType, 0, 0F, 1F, 0L, newPassengerIds, speedFactor, acceleration, 0F, 0F, 0f, null, null, newMood, 0.1F, 0.05F, true, true);
        vehicleSetup.emergencyId = emergencyId;

        CustomObjectCreator.instance.addVehicle(vehicleSetup);
    }

    private List<Pos> getEndPosForVector(Vector3 vector) {
        // First check in the end-point register
        foreach (KeyValuePair<long, List<WayReference>> pair in NodeIndex.endPointDriveWayIndex) {
            if (Game.getCameraPosition(NodeIndex.getPosById(pair.Key)) == vector) {
                return new List<Pos>(){
                    NodeIndex.getPosById(pair.Key)
                };
            }
        }

        // If not found as end-point, get closest WayReference
        WayReference closestWayReference = NodeIndex.getClosestWayReference(vector);
        return new List<Pos>(){
            closestWayReference.node1,
            closestWayReference.node2
        };
    }

    private void registerEmergency(long emergencyId, string message, bool offMap, float timeToResolve) {
        emergencies.Add(emergencyId, new EmergencyInfo(message, offMap, timeToResolve));
    }

    public static bool Report (UNIT_STATUS status, Vehicle vehicle, long emergencyId, bool newSpawn = false) {
        if (EmergencyDispatch.instance.emergencies.ContainsKey(emergencyId)) {
            EmergencyDispatch.instance.emergencies[emergencyId].register(status, vehicle, newSpawn);
            return true;
        }
        return false;
    }

    public static void ReportStandingStill (long emergencyId, Vehicle vehicle) {
        if (EmergencyDispatch.instance.emergencies.ContainsKey(emergencyId)) {
            if (EmergencyDispatch.instance.emergencies[emergencyId].getVehicleStatus(vehicle) == UNIT_STATUS.ON_THE_WAY) {
                EmergencyDispatch.instance.growCollider(EmergencyDispatch.instance.emergencies[emergencyId].collider);
            }
        }
    }

    public static bool IsInvolvedInAccident (long emergencyId, Vehicle vehicle) {
        if (EmergencyDispatch.instance.emergencies.ContainsKey(emergencyId)) {
            return EmergencyDispatch.instance.emergencies[emergencyId].getVehicleStatus(vehicle) == UNIT_STATUS.INVOLVED_IN_EMERGENCY;
        }
        return false;
    }

    private void registerCollider (long emergencyId, BoxCollider collider) {
        if (emergencies.ContainsKey(emergencyId)) {
            emergencies[emergencyId].collider = collider;
        }
    }

    public IEnumerator unregisterCollider(EmergencyInfo emergencyInfo) {
        emergencyInfo.collider.size = new Vector3(0.001f, 0.001f, 0.001f);
        emergencyInfo.collider.transform.position = new Vector3(10000f, 10000f, 10000f);
        yield return null;
        Destroy(emergencyInfo.collider.gameObject);
        emergencyInfo.collider = null;
    }

    public IEnumerator clearUpDispatchUnits(EmergencyInfo emergencyInfo) {
        yield return new WaitForSeconds(0.25f);
        List<Vehicle> emergencyVehicles = emergencyInfo.vehicleStatus.Where(keyValuePair => keyValuePair.Value != UNIT_STATUS.INVOLVED_IN_EMERGENCY).Select(p => p.Key).ToList();
        foreach (Vehicle emergencyVehicle in emergencyVehicles) {
            emergencyVehicle.cancelDispatch();
        }

        emergencyInfo.vehicleStatus.Clear();
    }

    public IEnumerator countdownEmergency(EmergencyInfo emergencyInfo) {
        float timeToSleep = 0.25f;
        do {
            yield return new WaitForSecondsRealtime(timeToSleep);
            emergencyInfo.timeToResolve -= timeToSleep * emergencyInfo.numberOfArrivedUnits;
            // TODO - Maybe some graphics to show progress (or police radio?)
        } while (emergencyInfo.timeToResolve > 0f);
        emergencyInfo.situationResolved();
    }

    public class EmergencyInfo {
        private string emergencyType;
        private bool offMap = false;
        private bool emergencyResolved = false;
        private int numberAwaitingSpawn = 0;
        public float timeToResolve = 0f;
        public int numberOfArrivedUnits = 0;
        public Dictionary<Vehicle, UNIT_STATUS> vehicleStatus = new Dictionary<Vehicle, UNIT_STATUS>();
        public BoxCollider collider;

        Coroutine countdown = null;

        public EmergencyInfo(string emergencyType, bool offMap, float timeToResolve) {
            this.emergencyType = emergencyType;
            this.offMap = offMap;
            this.timeToResolve = timeToResolve;
        }

        public void register(UNIT_STATUS status, Vehicle vehicle, bool newSpawn) {
            if (!emergencyResolved) {
                if (newSpawn) {
                    numberAwaitingSpawn--;
                }
                if (status == UNIT_STATUS.AWAITING_SPAWN) {
                    numberAwaitingSpawn++;
                } else if (status == UNIT_STATUS.SITUATION_RESOLVED) {
                    emergencyResolved = true;
                    vehicleStatus.Clear();
                } else if (status == UNIT_STATUS.ARRIVED_AT_SCENE && offMap) {
                    vehicleStatus.Remove(vehicle);
                    if (vehicleStatus.Count == 0 && numberAwaitingSpawn == 0) {
                        emergencyResolved = true;
                    }
                } else {
                    if (!vehicleStatus.ContainsKey(vehicle)) {
                        vehicleStatus.Add(vehicle, status);
                    } else {
                        vehicleStatus[vehicle] = status;
                    }

                    if (status == UNIT_STATUS.ARRIVED_AT_SCENE) {
                        numberOfArrivedUnits++;
                        if (countdown == null) {
                            countdown = EmergencyDispatch.instance.StartCoroutine(EmergencyDispatch.instance.countdownEmergency(this));
                        }
                    }
                }
            }
//            string[] vehicleStatusString = vehicleStatus.Select(kvp => kvp.Key.name + ": " + kvp.Value.ToString()).ToArray<string>();
//            Debug.Log(string.Join(Environment.NewLine, vehicleStatusString));
        }

        public void situationResolved () {
            EmergencyDispatch.instance.StopCoroutine(countdown);
            countdown = null;

            emergencyResolved = true;

            List<Vehicle> involvedVehicles = vehicleStatus.Where(keyValuePair => keyValuePair.Value == UNIT_STATUS.INVOLVED_IN_EMERGENCY).Select(p => p.Key).ToList();

            // Fade out involved vehicles (and remove danger halo)
            foreach (Vehicle involvedVehicle in involvedVehicles) {
                involvedVehicle.fadeOutAndDestroy();
                vehicleStatus.Remove(involvedVehicle);
                PubSub.publish ("Vehicle:removeDangerHalo", involvedVehicle);
            }

            // Clear off the collider
            EmergencyDispatch.instance.StartCoroutine(EmergencyDispatch.instance.unregisterCollider(this));

            // Reset involved cars, after a delay...
            EmergencyDispatch.instance.StartCoroutine(EmergencyDispatch.instance.clearUpDispatchUnits(this));
        }

        public UNIT_STATUS getVehicleStatus(Vehicle vehicle) {
            if (vehicleStatus.ContainsKey(vehicle)) {
                return vehicleStatus[vehicle];
            }
            return UNIT_STATUS.ARRIVED_AT_SCENE;
        }
    }

}
