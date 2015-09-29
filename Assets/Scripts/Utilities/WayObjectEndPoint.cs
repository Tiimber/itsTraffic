using UnityEngine;
using System.Collections;

public class WayObjectEndPoint {

	public static void create (long key, WayReference endPoint) {
		bool isNode1 = endPoint.isNode1 (NodeIndex.getPosById (key));

		Vector3 originalScale = endPoint.gameObject.transform.localScale;
		Vector3 nodeCameraPos = Game.getCameraPosition (isNode1 ? endPoint.node1 : endPoint.node2);

		Vector3 fromPos = nodeCameraPos - new Vector3(0f, originalScale.y / 2f, 0f);
		Vector3 toPos = fromPos + new Vector3(originalScale.y / 2f, originalScale.y, 0f);

		GameObject endPointObj = MapSurface.createPlaneMeshForPoints (fromPos, toPos, MapSurface.Anchor.LEFT_CENTER);
		endPointObj.transform.position = endPointObj.transform.position - new Vector3 (0, 0, 0.1f) - (isNode1 ? Vector3.zero : endPoint.transform.rotation * new Vector3 (originalScale.y / 2f, 0f, 0f));
		endPointObj.transform.rotation = endPoint.transform.rotation;
		AutomaticMaterialObject endPointMaterialObject = endPointObj.AddComponent<AutomaticMaterialObject> () as AutomaticMaterialObject;
		endPointMaterialObject.requestMaterial ("2002-Street", null); // TODO - Default material

	}
}
