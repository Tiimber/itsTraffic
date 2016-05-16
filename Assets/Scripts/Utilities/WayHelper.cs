using UnityEngine;

public class WayHelper
{
	public const float BEZIER_RESOLUTION = 20f;
	public static Quaternion ONEEIGHTY_DEGREES = Quaternion.Euler (new Vector3 (0, 0, 180f));
	public static float LIMIT_WAYWIDTH = WayTypeEnum.PEDESTRIAN;
	public const float MINIMUM_DRIVE_WAY = WayTypeEnum.PEDESTRIAN;

	public static float CROSSING_LINE_PERCENTAGE = 0.9f;
	public static Vector3 DEGREES_90_VECTOR = new Vector3 (0f, 0f, 90f);
	public static Vector3 DEGREES_270_VECTOR = new Vector3 (0f, 0f, 270f);
}
