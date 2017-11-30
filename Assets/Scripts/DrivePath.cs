using System.Collections.Generic;
using UnityEngine;

public class DrivePath {

    const float BEZIER_MAX_RESOLUTION = 20f;
    const float DEGREES_TO_BLINK_THRESHOLD = 30f;
    const float DISTANCE_TO_PREPARE_FOR_TURN = 0.3f;

    public Vector3 startVector;
    public Vector3 endVector;
    public long startId;
    public long endId;
    public float fullLength;
    public float breakFactor;
    public float originalBreakFactor;
    public float wayWidthFactor;
    public string blinkDirection = null;
    public float blinkStart = -1f;
    public float breakStart = -1f;
    public TrafficLightLogic upcomingTrafficLight = null;
    public bool isBacking = false;

    private DrivePath() {}

    private DrivePath (DrivePath drivePath) {
        this.startVector = drivePath.startVector;
        this.endVector = drivePath.endVector;
        this.startId = drivePath.startId;
        this.endId = drivePath.endId;
        this.fullLength = drivePath.fullLength;
        this.breakFactor = drivePath.breakFactor;
        this.originalBreakFactor = drivePath.originalBreakFactor;
        this.wayWidthFactor = drivePath.wayWidthFactor;
        this.blinkDirection = drivePath.blinkDirection;
        this.blinkStart = drivePath.blinkStart;
        this.breakStart = drivePath.breakStart;
        this.upcomingTrafficLight = drivePath.upcomingTrafficLight;
        this.isBacking = drivePath.isBacking;
    }

    private void setUpcomingTrafficLight(long currTargetId, long prevTargetId) {
        if (TrafficLightIndex.TrafficLightsForPos.ContainsKey(currTargetId)) {
            upcomingTrafficLight = TrafficLightIndex.TrafficLightsForPos[currTargetId].Find(trafficLight => trafficLight.getOtherPos().Id == prevTargetId);
        }
    }

    public void adjustStartTo(DrivePath other) {
        startVector = other.endVector;
        fullLength = (endVector - startVector).magnitude;
    }

    public void shortenToPct(float pct) {
        endVector = startVector + (endVector - startVector) * pct;
        fullLength = (endVector - startVector).magnitude;
    }

    public static List<DrivePath> Build(List<Vector3> path, List<Pos> posObjs) {
//        Debug.Log("NEW");
        List<DrivePath> drivePaths = new List<DrivePath>();

        Vector3 previousPoint = path[0];
        WayReference previousWayReference = null;
        float previousWidth = 0f;
        float previousLength = 0f;
        for (int i = 1; i < path.Count; i++) {
            bool hadPrevious = i > 1;
            bool hasNext = i < path.Count - 1;

            // DrivePath for current way
            DrivePath currentPath = new DrivePath();
            currentPath.startVector = path[i - 1];
            currentPath.endVector = path[i];
            currentPath.startId = posObjs[i - 1].Id;
            currentPath.endId = posObjs[i].Id;

            WayReference wayReference = NodeIndex.getWayReference(currentPath.startId, currentPath.endId);
            float wayWidth = wayReference.transform.localScale.y;
            float wayLength = (currentPath.endVector - currentPath.startVector).magnitude;

            currentPath.setUpcomingTrafficLight(posObjs[i].Id, posObjs[i - 1].Id);

            if (hadPrevious) {
                Vector3 originalStartVector = currentPath.startVector;
                // If we had a previous path, we want to add an extra bezier from last way "half way width from the end" to current road "half way width from the start"
                Vector3 endOfPrevious = currentPath.startVector;
                if (previousWidth > previousLength) {
                    endOfPrevious += (previousPoint - currentPath.startVector) / 2f;
                } else {
                    float percentageOfPreviousLength = (previousWidth / 2f) / previousLength;
                    endOfPrevious += (previousPoint - currentPath.startVector) * percentageOfPreviousLength;
                }

                Vector3 startOfCurrent = currentPath.startVector;
                if (wayWidth > wayLength) {
                    startOfCurrent += (currentPath.endVector - currentPath.startVector) / 2f;
                } else {
                    float percentageOfCurrentLength = (wayWidth / 2f) / wayLength;
                    startOfCurrent += (currentPath.endVector - currentPath.startVector) * percentageOfCurrentLength;
                }

                // Since this moves our start-point, reduce the "straight way" drive length
                currentPath.startVector = startOfCurrent;

                // Make the bezier
                float previousRotation = Misc.GetZRotation(previousPoint, currentPath.startVector);
                float currentRotation = Misc.GetZRotation(currentPath.startVector, currentPath.endVector);
                float rotationDiff = currentRotation - previousRotation;

                if (rotationDiff > 90f) {
                    rotationDiff = 90f - (rotationDiff-90f);
                } else if (rotationDiff < -90f) {
                    rotationDiff = -90f - (rotationDiff+90f);
                }
                float absRotationDiff = Mathf.Abs(rotationDiff);

//                Debug.Log("PART");
//                DebugFn.print(previousPoint);
//                DebugFn.print(currentPath.startVector);
//                DebugFn.print(currentPath.endVector);
//                Debug.Log(previousRotation);
//                Debug.Log(currentRotation);
//                Debug.Log("Rotation: " + rotationDiff + " (" + absRotationDiff + ")");
                float bezierResolution = BEZIER_MAX_RESOLUTION;
                string blinkDirection = null;
                if (absRotationDiff > DEGREES_TO_BLINK_THRESHOLD) {
//                    drivePaths[drivePaths.Count - 1].blinkDirection = rotationDiff < 0 ? "right" : "left";
                    bool pointOnRightSideOfLine = Math3d.PointOnRightSideOfLine(endOfPrevious, path[i - 1], path[i]);
                    blinkDirection = pointOnRightSideOfLine ? "right" : "left";
                    drivePaths[drivePaths.Count - 1].blinkDirection = blinkDirection;
                    drivePaths[drivePaths.Count - 1].blinkStart = DISTANCE_TO_PREPARE_FOR_TURN;
//                    Debug.Log("Blink: " + drivePaths[drivePaths.Count - 1].blinkDirection);
                } else if (absRotationDiff < 1f) {
                    bezierResolution = 2f;
                } else if (absRotationDiff < 5f) {
                    bezierResolution = 4f;
                } else if (absRotationDiff < 10f) {
                    bezierResolution = 6f;
                } else if (absRotationDiff < 20f) {
                    bezierResolution = 12f;
                }
                float breakFactor = GetTurnBreakFactorForDegrees(absRotationDiff);
                MakeBezier(drivePaths, endOfPrevious, originalStartVector - endOfPrevious, startOfCurrent, startOfCurrent - originalStartVector, currentPath.startId, breakFactor, wayReference.way.WayWidthFactor, bezierResolution, blinkDirection);
            }

            previousPoint = currentPath.startVector;
            previousWayReference = wayReference;
            previousWidth = wayWidth;
            previousLength = wayLength;

            if (hadPrevious && hasNext && wayLength < wayWidth) {
                // Special case, if way is too short, don't use the "mid" part of it, only the turn between previous and next way
                continue;
            }

            if (hasNext) {
                float percentageOfCurrentLength = (wayWidth / 2f) / (currentPath.endVector - currentPath.startVector).magnitude;
                currentPath.endVector -= (currentPath.endVector - currentPath.startVector) * percentageOfCurrentLength;
            }

            currentPath.wayWidthFactor = wayReference.way.WayWidthFactor;
            currentPath.fullLength = (currentPath.endVector - currentPath.startVector).magnitude;
//            Debug.Log(currentPath.fullLength);
            currentPath.breakFactor = 1.0f;
            currentPath.originalBreakFactor = currentPath.breakFactor;

            // Put the path for the "mid" way

            drivePaths.Add(currentPath);
        }

        // If needed, make them all z = 0
        drivePaths.ForEach(dp => {
            dp.startVector = new Vector3(dp.startVector.x, dp.startVector.y, 0f);
            dp.endVector = new Vector3(dp.endVector.x, dp.endVector.y, 0f);
        });

        // All should be connected... if not, do that!
        DrivePath previous = null;
        for (int i = 0; i < drivePaths.Count; i++) {
            DrivePath current = drivePaths[i];
            if (previous != null) {
                if (previous.endVector != current.startVector) {
                    Vector3 midVector = Misc.GetMidVector(previous.endVector, current.startVector);
                    previous.endVector = midVector;
                    previous.fullLength = (previous.endVector - previous.startVector).magnitude;
                    current.startVector = midVector;
                    current.fullLength = (current.endVector - current.startVector).magnitude;
                }
            }
            if (i < drivePaths.Count - 1 && current.breakFactor == 1f) {
                DrivePath next = drivePaths[i+1];
                if (next.breakFactor < 1f) {
                    current.breakStart = DISTANCE_TO_PREPARE_FOR_TURN;
                }
            }
            previous = current;
        }

        return drivePaths;
    }


    private const float a = 1f;
    private const float b = -0.01197f;
    private const float c = 3.65e-5f;
//	private const float d = 2.74e-7f;
    public static float GetTurnBreakFactorForDegrees(float x)
    {
// TODO - Make break factor working smoothly
//		return - (b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3));
//		return a + b * x + c * Mathf.Pow(x, 2) + d * Mathf.Pow(x, 3);
        return a + b * x + c * Mathf.Pow(x, 2);
    }

    private static void MakeBezier(List<DrivePath> drivePaths, Vector3 start, Vector3 startDirection, Vector3 end, Vector3 endDirection, long posId, float breakFactor, float wayWidthFactor, float bezierResolution, string blinkDirection) {
        bezierResolution = Mathf.Min(bezierResolution, BEZIER_MAX_RESOLUTION);
        Vector3 intersection = Vector3.zero;
        bool intersects = Math3d.LineLineIntersection (out intersection, start, startDirection, end, endDirection);
        if (intersects) {
			Vector3 prev = Vector3.zero;
			for (float t = 0.0f; t <= 1.0f; t+= 1f / bezierResolution) {
				Vector3 curr = Math3d.GetVectorInBezierAtTime(t, start, intersection, end);
				if (prev != Vector3.zero) {
                    DrivePath bezierDrivePath = new DrivePath();
                    bezierDrivePath.startVector = prev;
                    bezierDrivePath.endVector = curr;
                    bezierDrivePath.startId = posId;
                    bezierDrivePath.endId = posId;
                    bezierDrivePath.fullLength = (curr - prev).magnitude;
                    bezierDrivePath.breakFactor = breakFactor;
                    bezierDrivePath.originalBreakFactor = breakFactor;
                    bezierDrivePath.wayWidthFactor = wayWidthFactor;
                    bezierDrivePath.blinkDirection = blinkDirection;
                    bezierDrivePath.blinkStart = 0f;
                    drivePaths.Add(bezierDrivePath);
				}
				prev = curr;
			}
        } else {
            // Something is weird, just drive straight...
            DrivePath straightDrivePath = new DrivePath();
            straightDrivePath.startVector = start;
            straightDrivePath.endVector = end;
            straightDrivePath.startId = posId;
            straightDrivePath.endId = posId;
            straightDrivePath.fullLength = (end - start).magnitude;
            straightDrivePath.breakFactor = 1.0f;
            straightDrivePath.originalBreakFactor = straightDrivePath.breakFactor;
            straightDrivePath.wayWidthFactor = wayWidthFactor;
            straightDrivePath.blinkDirection = blinkDirection;
            straightDrivePath.blinkStart = 0f;
            drivePaths.Add(straightDrivePath);
        }
    }

    public static void AddBacking(List<DrivePath> drivePaths, Vehicle vehicle) {
        // Debug.Log("AddBacking" + vehicle.vehicleId);
        DrivePath drivePath = new DrivePath(drivePaths[0]);
        drivePath.upcomingTrafficLight = null;
        drivePath.isBacking = true;
        drivePath.wayWidthFactor /= 2f;
        Vector3 backingVector = drivePath.startVector - Misc.NoZ(vehicle.transform.position);
        drivePath.startVector = Misc.NoZ(vehicle.transform.position);
        backingVector = backingVector.normalized / 4f;
        drivePath.endVector = drivePath.startVector + backingVector;
        drivePath.fullLength = backingVector.magnitude;
        drivePaths[0].fullLength += drivePath.fullLength;
        drivePaths.Insert(0, drivePath);
    }
}
