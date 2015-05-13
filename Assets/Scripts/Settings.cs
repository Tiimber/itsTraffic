using System.Collections;

public class Settings {
	public static float wayLengthFactor = 10f;
	public static float currentMapWidthFactor = 5f;
	// Playback, normal, faster, really fast... normal = 0.1
	public static float playbackSpeed = 0.1f;

	// Related to waywidth factor, 75km/h 
	public static float speedFactor = 75f / playbackSpeed;

}
