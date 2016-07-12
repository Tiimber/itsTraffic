public class WayCollisionObj : CollisionObj {
	public static string NAME = "WAY_COLLISION_OBJ";

	public WayReference WayReference { get; set; }
	public Pos Pos { get; set; }

	public WayCollisionObj (WayReference wayReference, string collisionObjType, Pos pos) : base(collisionObjType, WayCollisionObj.NAME) {
		this.WayReference = wayReference;
		this.Pos = pos;
	}
}