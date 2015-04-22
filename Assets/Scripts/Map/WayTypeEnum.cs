using System.Collections.Generic;

public class WayTypeEnum
{
	public const float HIGHWAY_PRIMARY = 2.5f;
	public const float HIGHWAY_PRIMARY_LINK = 2.0f;
	public const float HIGHWAY_SECONDARY = 2f;
	public const float HIGHWAY_SECONDARY_LINK = 1.7f;
	public const float HIGHWAY_TERTIARY = 1.5f;
	public const float HIGHWAY_TERTIARY_LINK = 1.2f;
	public const float MOTORWAY = 1.2f;
	public const float MOTORWAY_LINK = 1f;
	public const float PLATFORM = 0.7f;
	public const float PEDESTRIAN = 0.4f;
	public const float FOOTWAY = 0.3f;
	public const float LIVING_STREET = 0.6f;
	public const float PATH = 0.3f;
	public const float SERVICE = 1f;
	public const float CYCLEWAY = 0.4f;
	public const float STEPS = 0.4f;
	public const float RESIDENTIAL = 0.4f;
	public const float UNCLASSIFIED = 0.05f;

	public static List<float> WayTypes = new List<float>() {
		HIGHWAY_PRIMARY,
		HIGHWAY_SECONDARY,
		HIGHWAY_TERTIARY,
		MOTORWAY,
		MOTORWAY_LINK,
		PLATFORM,
		PEDESTRIAN,
		FOOTWAY,
		LIVING_STREET,
		PATH,
		SERVICE,
		CYCLEWAY,
		STEPS,
		RESIDENTIAL,
		UNCLASSIFIED
	};

	static WayTypeEnum () {
		WayTypes.Sort ();
	}

	public static float getLower(float value) {
		return getNext (value, false);
	}

	public static float getHigher(float value) {
		return getNext (value, true);
	}

	private static float getNext(float original, bool reverse) {
		float previous = original;
		bool pickNext = false;
		foreach (float current in WayTypes) {
			if (pickNext) {
				return current;
			}
			if (current == original) {
				if (reverse) {
					return previous;
				} else {
					pickNext = true;
				}
			}

			previous = current;
		}
		return original;
	}
}
