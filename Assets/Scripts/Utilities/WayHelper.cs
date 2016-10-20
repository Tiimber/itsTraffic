using UnityEngine;

public class WayHelper
{
	private const float BEZIER_RESOLUTION_MIN = 12f;
	private const float BEZIER_RESOLUTION_MAX = 100f;
    public static float BEZIER_RESOLUTION {
        get {
			float resolution = BEZIER_RESOLUTION_MIN + (Game.instance.graphicsQuality * (BEZIER_RESOLUTION_MAX - BEZIER_RESOLUTION_MIN));
			return resolution;
        }
    }
	public static Quaternion ONEEIGHTY_DEGREES = Quaternion.Euler (new Vector3 (0, 0, 180f));
	public static float LIMIT_WAYWIDTH = WayTypeEnum.PEDESTRIAN;
	public const float MINIMUM_DRIVE_WAY = WayTypeEnum.PEDESTRIAN;

	public static float CROSSING_LINE_PERCENTAGE = 0.9f;
	public static Vector3 DEGREES_90_VECTOR = new Vector3 (0f, 0f, 90f);
	public static Vector3 DEGREES_270_VECTOR = new Vector3 (0f, 0f, 270f);
}
