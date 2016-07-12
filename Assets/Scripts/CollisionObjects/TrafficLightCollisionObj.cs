public class TrafficLightCollisionObj : CollisionObj {
	public static string NAME = "TRAFFIC_LIGHT_COLLISION_OBJ";

	public TrafficLightLogic TrafficLightLogic { get; set; }

	public TrafficLightCollisionObj (TrafficLightLogic trafficLightLogic, string collisionObjType) : base(collisionObjType, TrafficLightCollisionObj.NAME) {
		this.TrafficLightLogic = trafficLightLogic;
	}
}