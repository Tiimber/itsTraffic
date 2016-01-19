using UnityEngine;

public class WayHelper
{
	public const float BEZIER_RESOLUTION = 20f;
	public static Quaternion ONEEIGHTY_DEGREES = Quaternion.Euler (new Vector3 (0, 0, 180f));
	public static float LIMIT_WAYWIDTH = WayTypeEnum.PEDESTRIAN;

}
