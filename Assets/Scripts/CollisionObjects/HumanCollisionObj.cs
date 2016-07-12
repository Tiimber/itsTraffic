public class HumanCollisionObj : CollisionObj {
	public static string NAME = "HUMAN_COLLISION_OBJ";

	public HumanLogic Human { get; set; }

	public HumanCollisionObj (HumanLogic human, string collisionObjType) : base(collisionObjType, HumanCollisionObj.NAME) {
		this.Human = human;
	}
}