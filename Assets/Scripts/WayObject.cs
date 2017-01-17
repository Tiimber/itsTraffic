using UnityEngine;

public class WayObject : MonoBehaviour, IExplodable {

	public void turnOnExplodable() {
        Misc.SetGravityState (gameObject, true);
    }

}
