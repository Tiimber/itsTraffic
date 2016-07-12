public class VehicleCollisionObj : CollisionObj {
	public static string NAME = "VEHICLE_COLLISION_OBJ";

	public Vehicle Vehicle { get; set; }

	public VehicleCollisionObj (Vehicle vehicle, string collisionObjType) : base(collisionObjType, VehicleCollisionObj.NAME) {
		this.Vehicle = vehicle;
	}
}