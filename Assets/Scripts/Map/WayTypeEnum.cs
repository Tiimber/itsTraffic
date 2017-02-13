using System.Collections.Generic;

public class WayTypeEnum
{
	public const float HIGHWAY_PRIMARY = 2.01f;
	public const float HIGHWAY_PRIMARY_LINK = 1.71f;
	public const float HIGHWAY_SECONDARY = 1.53f;
	public const float HIGHWAY_SECONDARY_LINK = 1.23f;
	public const float HIGHWAY_TERTIARY = 1.52f;
	public const float HIGHWAY_TERTIARY_LINK = 1.22f;
	public const float MOTORWAY = 1.51f;
	public const float MOTORWAY_LINK = 1.21f;

	// Keep all "Non CarWays" under 1.00f and above 0.00f
	public const float PLATFORM = 0.1f; // Bus stop?
	public const float EXPANDED_PLATFORM = 0.22f; // Draw width for platforms that are lines, not areas
	public const float PEDESTRIAN = 0.44f;
	public const float FOOTWAY = 0.13f;
	public const float LIVING_STREET = 0.21f;
	public const float PATH = 0.11f;
	public const float SERVICE = 0.31f;
	public const float CYCLEWAY = 0.12f;
	public const float STEPS = 0.41f;
	public const float RESIDENTIAL = 0.51f;
	public const float UNCLASSIFIED = 1.11f;

	public const float OTHER_FOOTWAY = 0.10f;

	public const float ALL = 0.00f;

	public static List<float> WayTypes = new List<float>() {
		HIGHWAY_PRIMARY,
		HIGHWAY_SECONDARY,
		HIGHWAY_TERTIARY,
		MOTORWAY,
		MOTORWAY_LINK,
		//PLATFORM, // Should not render intersections
		PEDESTRIAN,
		FOOTWAY,
		LIVING_STREET,
		PATH,
		SERVICE,
		CYCLEWAY,
		STEPS,
		RESIDENTIAL,
		UNCLASSIFIED,

		OTHER_FOOTWAY,

		ALL
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
				if (pickNext && original == current) {
					continue;
				}
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
