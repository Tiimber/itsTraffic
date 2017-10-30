using System;

public class EmergencyCollisionObj : CollisionObj {
	public static string NAME = "EMERGENCY_COLLISION_OBJ";

    public long emergencyId;

	public EmergencyCollisionObj (string name) : base(name, EmergencyCollisionObj.NAME) {
        emergencyId = Convert.ToInt64(name.Substring("EmergencyCollider:".Length));
	}
}