using UnityEngine;
using System.Collections;

public class AutomaticMaterialObject : MonoBehaviour, IPubSub {
	private string requestedMaterialId;

	public void requestMaterial (string materialId, Material defaultMaterial = null) {
		string[] idParts = materialId.Split(new char[] {'-'});
		string materialIdNumber = idParts[0];
		requestedMaterialId = materialIdNumber;
		if (MaterialManager.MaterialIndex.ContainsKey (materialIdNumber)) {
			applyMaterial (MaterialManager.MaterialIndex[materialIdNumber]);
		} else {
//			Debug.Log ("Requested Material: " + materialIdNumber);
			PubSub.subscribe ("Material-" + materialIdNumber, this);
			StartCoroutine (MaterialManager.LoadMaterial (materialIdNumber, idParts[1]));
			if (defaultMaterial != null) {
				applyMaterial (defaultMaterial);
			}
		}
	}

	public void applyMaterial (Material material) {
		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;
	}

	public PROPAGATION onMessage (string message, object data) {
//		Debug.Log ("Material for way: " + message);
		applyMaterial (MaterialManager.MaterialIndex[requestedMaterialId]);
		return PROPAGATION.DEFAULT;
	}
}
