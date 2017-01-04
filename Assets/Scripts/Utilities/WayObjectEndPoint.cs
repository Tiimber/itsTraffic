using UnityEngine;
using System.Collections;

public class WayObjectEndPoint {

	public static void create (long key, WayReference endPoint, string materialId) {
		bool isNode1 = endPoint.isNode1 (NodeIndex.getPosById (key));
		bool isSmall = endPoint.SmallWay;

		Vector3 originalScale = endPoint.gameObject.transform.localScale;
		Vector3 nodeCameraPos = Game.getCameraPosition (isNode1 ? endPoint.node1 : endPoint.node2);

		Vector3 fromPos = nodeCameraPos - new Vector3(0f, originalScale.y / 2f, 0f);
		Vector3 toPos = fromPos + new Vector3((isSmall ? originalScale.x / 2f : originalScale.y / 2f), originalScale.y, 0f);

		GameObject endPointObj = MapSurface.createPlaneMeshForPoints (fromPos, toPos, MapSurface.Anchor.LEFT_CENTER);
		endPointObj.name = "End of way (" + key + ")";
		Vector3 zOffset = new Vector3 (0, 0, Game.WAYS_Z_POSITION);
		endPointObj.transform.position = endPointObj.transform.position + zOffset - (isNode1 ? Vector3.zero : endPoint.transform.rotation * new Vector3 (isSmall ? originalScale.x / 2f : originalScale.y / 2f, 0f, 0f));
        endPointObj.transform.parent = Game.instance.waysParent;
		endPointObj.transform.rotation = endPoint.transform.rotation;
		AutomaticMaterialObject endPointMaterialObject = endPointObj.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
		endPointMaterialObject.requestMaterial (materialId, null); // TODO - Default material

		// Add rigidbody and mesh collider, so that they will fall onto the underlying plane
		Misc.AddGravityToWay(endPointObj);
	}
}
