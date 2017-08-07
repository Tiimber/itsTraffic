public class CollisionObj {
	public string CollisionObjType { get; set; }
	public string typeName;

	public const string WAY_COLLIDER = "WC";
	public const string VEHICLE_FRONT_AWARE_COLLIDER = "FAC";
	public const string VEHICLE_PANIC_COLLIDER = "PC";
	public const string VEHICLE_COLLIDER = "CAR";
	public const string VEHICLE_BACK_COLLIDER = "BC";
	public const string TRAFFIC_LIGHT_YELLOW = "Yellow";
	public const string TRAFFIC_LIGHT_RED = "Red";
	public const string TRAFFIC_LIGHT_GREEN = "Green";

	protected CollisionObj (string collisionObjType, string typeName) {
		this.CollisionObjType = collisionObjType;
		this.typeName = typeName;
	}
}