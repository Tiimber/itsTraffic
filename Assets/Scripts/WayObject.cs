using UnityEngine;

public class WayObject : MonoBehaviour, IExplodable {

	public void turnOnExplodable() {
        Misc.SetGravityState (gameObject, true);
    }

    public void setWeight() {
        float weightPerSquareMeter = 0.135f; // We calculate with ways being 10cm thick
        MeshArea meshArea = gameObject.GetComponent<MeshArea>();
        float squareMeters = meshArea.area * 1000f;
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        rigidbody.mass = squareMeters * weightPerSquareMeter;
    }
}
