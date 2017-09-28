using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EmergencyDispatch : MonoBehaviour, IPubSub {

    public enum EMERGENCY_TYPE {
        POLICE,
        FIRE_FIGHTER,
        AMBULANCE
    }

    public enum UNIT_STATUS {
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

    private long emergencyId = 0L;
    private Dictionary<long, EmergencyInfo> emergencies = new Dictionary<long, EmergencyInfo>();

    void Start() {
        PubSub.subscribe("Report:majorCrash", this);
    }

    public PROPAGATION onMessage(string message, object data) {
        emergencyId++;
        switch (message) {
            case REPORT_MAJOR_CRASH:
                int neededPolice = 2;
                registerEmergency(emergencyId, message);
                grabOrSpawn(neededPolice, EMERGENCY_TYPE.POLICE, Misc.VECTOR3_NULL);
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

    private void grabOrSpawn(int numberOfUnits, EMERGENCY_TYPE type, Vector3 closestToPosition) {
        // TODO - Based on "type"
        string brand = "Po-liz";
        List<Vehicle> availableEmergencyUnits = Misc.GetVehicles(brand);

        // If position is not on map... pretend it's on of our endpoints
        if (closestToPosition == Misc.VECTOR3_NULL) {
            long endPointNode = Misc.pickRandomKey(NodeIndex.endPointDriveWayIndex);
            if (endPointNode != 0L) {
                closestToPosition = Game.getCameraPosition(NodeIndex.getPosById(endPointNode));
            }
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
    [InspectorButton("callFireFighterFn")]
    public bool callFireFighter;
    [InspectorButton("callAmbulanceFn")]
    public bool callAmbulance;

    private void callPoliceFn() {
        PubSub.publish(REPORT_MAJOR_CRASH);
    }

    private void callFireFighterFn() {
        PubSub.publish(REPORT_FIRE);
    }

    private void callAmbulanceFn() {
        PubSub.publish(REPORT_INJURY);
    }

    private void dispatchUnits(List<Vehicle> units, Vector3 targetPosition) {
        units.ForEach(vehicle => vehicle.dispatchTo(targetPosition));
        // TODO - Acknowledge going to emergency
    }

    private void spawnUnits(int numberOfUnits, EMERGENCY_TYPE type, Vector3 targetPosition) {
        for (int i = 0; i < numberOfUnits; i++) {
            spawnUnit(type, targetPosition);
        }
        // TODO - Acknowledge planning to spawn
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
        long newId = 0L;
        string newName = "Po-liz";
        float newTime = GameTimer.elapsedTime() + Misc.randomRange(5f, 15f);
        List<long> newWayPoints = secondLastPos != null ? new List<long>(){secondLastPos.Id} : new List<long>();
        string newBrand = "Po-liz";
        string newType = "Po-liz";
        List<long> newPassengerIds = new List<long>();
        List<float> newMood = new List<float>(){1f, 0f, 1.5f};
        Setup.VehicleSetup vehicleSetup = new Setup.VehicleSetup(newId, newName, newTime, startPos.Id, endPos.Id, null, false, false, newWayPoints, newBrand, null, newType, 0, 0F, 1F, 0L, newPassengerIds, 0F, 0F, 0F, 0F, 0f, null, null, newMood, 0.1F, 0.05F, true, true);

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

    private void registerEmergency(long emergencyId, string message) {
        emergencies.Add(emergencyId, new EmergencyInfo(message));
    }

    public class EmergencyInfo {
        private string emergencyType;

        public EmergencyInfo(string emergencyType) {
            this.emergencyType = emergencyType;
        }
    }

}
