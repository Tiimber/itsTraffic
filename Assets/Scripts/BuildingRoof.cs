using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuildingRoof : MapSurface, IPubSub, IExplodable {
	public static Vector3 bodyPositionWhileRising = new Vector3 (0, 0, 0.1f);
	public static Vector3 bodyPositionAfterRising = new Vector3 (0, 0, 0.01f);

	public bool slave = false;
	public BuildingRoof parent = null;
	private List<BuildingRoof> slaves = new List<BuildingRoof>();

    private string id;
    private GameObject roof;

	float height = 0f;
	float heightProperty = 0f;
	float constructionHeight = 0f;
	float constructionTime = 0f;
	float constructionTimeEnd = Mathf.PI/2f;
	float delayTime;
	
	private bool hasSlaves() {
		return slaves.Count > 0;
	}

	private void setConstructionHeight() {
		if (constructionTime >= constructionTimeEnd) {
			constructionTime = constructionTimeEnd;
			constructionHeight = height;
		} else {
			constructionHeight = Mathf.Sin(constructionTime) * height;	
		}
	}

	void Start () {
		if (!slave) {
			PubSub.subscribe ("gameIsReady", this);
			delayTime = Misc.randomRange (0.3f, 0.8f);
			
			foreach (BuildingRoof slaveRoof in slaves) {
				slaveRoof.delayTime = delayTime;
			}
		}
	}

	void Update () {
		if (!hasSlaves()) {
			if (height != constructionHeight) {
				if (delayTime > 0) {
					delayTime -= Time.deltaTime;
				} else {
					constructionTime += Time.deltaTime;
					setConstructionHeight ();
					raiseBuilding ();
					if (height == constructionHeight) {
						Game.instance.removeInitAnimationRequest ();
					}
				}
			}
		}
	}

	public void createBuildingWithXMLNode(XmlNode xmlNode) {
		if (xmlNode != null) {
//			this.gameObject.name = "BuildingRoof (" + xmlNode.Attributes.GetNamedItem ("id").Value + ")";
            this.id = xmlNode.Attributes.GetNamedItem ("id").Value;
			this.gameObject.name = "Building (" + id + ")";

            // Create the roof and set its layer
            roof = new GameObject ("BuildingRoof (" + xmlNode.Attributes.GetNamedItem ("id").Value + ")");
            roof.transform.parent = this.transform;
            roof.transform.localPosition = Vector3.zero;
			MapSurface roofSurface = roof.AddComponent<MapSurface>();
            roof.AddComponent<BuildingRoofLayer>();
            roofSurface.createMesh (xmlNode);
            roofSurface.createMeshCollider(false);
        }
	}

	public bool createSplitMeshes(List<Vector3> outer, List<Vector3> inner) {
		Rect rectOfOuter = Misc.GetRectOfVectorList(outer);
		// Vector3 centerOuter = Misc.GetCenterOfVectorList(outer);
		Vector3 centerInner = Misc.GetCenterOfVectorList(inner);

		if (Misc.IsPointInsideRect(centerInner, rectOfOuter)) {
			// Now try to split this with a line going from this point straight left and right (x axis)
			Vector3 intersectionCheckPoint = centerInner - new Vector3(rectOfOuter.width, 0f, 0f);
			Vector3 intersectionCheckLine = new Vector3(rectOfOuter.width * 2, 0f, 0f);

			// Loop through outers in pairs and keep them in separate lists
			List<Vector3> beforeSplitOuter = new List<Vector3>();
			List<Vector3> afterSplitOuter = new List<Vector3>();
			List<Vector3> outerIntersectionPoints = new List<Vector3>();
			bool beforeSplit = true;
			for (int i = 0; i < outer.Count - 1; i++) {				
				Vector3 vec1 = outer[i];
				Vector3 vec2 = outer[i+1];
				// Add current vector
				if (beforeSplit)  {
					beforeSplitOuter.Add(vec1);
				} else {
					afterSplitOuter.Add(vec1);
				}
				
				Vector3 intersectionPoint;
				bool intersected = Math3d.LineLineIntersection(out intersectionPoint, vec1, vec2 - vec1, intersectionCheckPoint, intersectionCheckLine);
				if (intersected) {
					// We have crossed the intersection point, add this to both and shift the boolean value, to push coming values to the other list
					beforeSplitOuter.Add(intersectionPoint);
					afterSplitOuter.Add(intersectionPoint);
					// Also keep track of the actual intersection points
					outerIntersectionPoints.Add(intersectionPoint);
					beforeSplit = !beforeSplit;
				}
			}

			// Loop through inners in pairs and keep them in separate lists
			List<Vector3> beforeSplitInner = new List<Vector3>();
			List<Vector3> afterSplitInner = new List<Vector3>();
			List<Vector3> innerIntersectionPoints = new List<Vector3>();
			beforeSplit = true;
			for (int i = 0; i < inner.Count - 1; i++) {				
				Vector3 vec1 = inner[i];
				Vector3 vec2 = inner[i+1];
				// Add current vector
				if (beforeSplit)  {
					beforeSplitInner.Add(vec1);
				} else {
					afterSplitInner.Add(vec1);
				}
				
				Vector3 intersectionPoint;
				bool intersected = Math3d.LineLineIntersection(out intersectionPoint, vec1, vec2 - vec1, intersectionCheckPoint, intersectionCheckLine);
				if (intersected) {
					// We have crossed the intersection point, add this to both and shift the boolean value, to push coming values to the other list
					beforeSplitInner.Add(intersectionPoint);
					afterSplitInner.Add(intersectionPoint);
					// Also keep track of the actual intersection points
					innerIntersectionPoints.Add(intersectionPoint);
					beforeSplit = !beforeSplit;
				}
			}

			// TODO - If we have more than two intersection points for either inner or outer, we should try and rotate the intersectionCheckLine and redo above
			// - If eg. outer would happen to be irregular, a third intersection could occur, probably failing everything

			// We now have two lists for outer and two lists for inner, and one list for each to know where the intersections are at
			List<Vector3> topMostOuters = Misc.GetTopMost(beforeSplitOuter, afterSplitOuter);
			List<Vector3> topMostInners = Misc.GetTopMost(beforeSplitInner, afterSplitInner);
			List<Vector3> bottomMostOuters = topMostOuters == beforeSplitOuter ? afterSplitOuter : beforeSplitOuter;
			List<Vector3> bottomMostInners = topMostInners == beforeSplitInner ? afterSplitInner : beforeSplitInner;

			// Tie together topmost outer and inner, so they make one solid block
			List<Vector3> topPart = Misc.TieTogetherOuterAndInner(topMostOuters, topMostInners, outerIntersectionPoints, innerIntersectionPoints);
			// Tie together bottommost outer and inner, so they make one solid block
			List<Vector3> bottomPart = Misc.TieTogetherOuterAndInner(bottomMostOuters, bottomMostInners, outerIntersectionPoints, innerIntersectionPoints);

			if (topPart != null && bottomPart != null) {
				// Create the mesh
				// DebugFn.print(intersectionCheckPoint);
				// DebugFn.print(intersectionCheckLine);
				// Debug.Log(outerIntersectionPoints.Count);
				// Debug.Log(innerIntersectionPoints.Count);
				// DebugFn.square(topPart[0]);
				// DebugFn.square(bottomPart[0]);
				// DebugFn.print(topPart);
				// DebugFn.print(bottomPart);
				// DebugFn.DebugPath(topPart);
				// DebugFn.DebugPath(bottomPart);

				GameObject top = createMesh(topPart, "top", this.transform);
				BuildingRoof topBR = top.AddComponent<BuildingRoof>();
				topBR.createMeshCollider(false);
				topBR.slave = true;
				topBR.parent = this;
				slaves.Add(topBR);

				GameObject bottom = createMesh(bottomPart, "bottom", this.transform);
				BuildingRoof bottomBR = bottom.AddComponent<BuildingRoof>();
				bottomBR.createMeshCollider(false);
				bottomBR.slave = true;
				bottomBR.parent = this;
				slaves.Add(bottomBR);

				return true;
			}
		}

		return false;
	}

	private void raiseBuilding () {
		transform.position = new Vector3(transform.position.x, transform.position.y, -constructionHeight);
	}

	private void extrude () {
		height = heightProperty * Game.heightFactor;

		MeshFilter filter = roof.GetComponent<MeshFilter>() as MeshFilter;
		Mesh msh = filter.mesh;

		Matrix4x4 [] extrusionPath = new Matrix4x4 [2];
		extrusionPath[0] = roof.transform.worldToLocalMatrix * Matrix4x4.TRS(roof.transform.position, Quaternion.identity, Vector3.one);
		extrusionPath[1] = roof.transform.worldToLocalMatrix * Matrix4x4.TRS(roof.transform.position + new Vector3(0, 0, height), Quaternion.identity, Vector3.one);

		Mesh extrudedmesh = new Mesh ();
//		MeshExtrusion.ExtrudeMesh(msh, GetComponent<MeshFilter>().mesh, extrusionPath, false);
		MeshExtrusion.ExtrudeMesh(msh, extrudedmesh, extrusionPath, false);

		// Add the sides to this gameobject
		MeshFilter sidesMeshFilter = gameObject.AddComponent<MeshFilter> ();
		sidesMeshFilter.mesh = extrudedmesh;
		MeshRenderer sidesMeshRenderer = gameObject.AddComponent<MeshRenderer> ();
		sidesMeshRenderer.material.color = new Color (1f, 0, 0);
		transform.SetParent (this.gameObject.transform);

        transform.localPosition += bodyPositionWhileRising;
        roof.transform.localPosition -= bodyPositionWhileRising;
	}
	
	public void setProperties (Dictionary<string, string> properties) {
		if (hasSlaves()) {			
			foreach (BuildingRoof slaveRoof in slaves) {
				slaveRoof.setProperties(properties);
			}
			return;
		}

		string materialId = properties["material"];
		string wallMaterialId = properties.ContainsKey("wall") ? properties ["wall"] : null;

		heightProperty = Convert.ToInt64 (properties ["height"]);
		Game.instance.addInitAnimationRequest ();

		if (MaterialManager.MaterialIndex.ContainsKey (materialId) && (wallMaterialId == null || MaterialManager.MaterialIndex.ContainsKey (wallMaterialId))) {
			Material material = MaterialManager.MaterialIndex [materialId];
			Material wallMaterial = wallMaterialId != null ? MaterialManager.MaterialIndex [wallMaterialId] : null;

			extrude ();
			applyMaterials (material, wallMaterial);
		} else {
			StartCoroutine (applyMaterialsWhenAvailableThenExtrude (materialId, wallMaterialId));
		}
	}

	private void applyMaterials (Material material, Material wallMaterial) {
		MeshRenderer meshRenderer = roof.GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;

		if (wallMaterial != null) {
			MeshRenderer wallMeshRenderer = transform.gameObject.GetComponent<MeshRenderer> ();
			Renderer wallRenderer = wallMeshRenderer.GetComponent<Renderer> ();
			wallRenderer.material = wallMaterial;

		}
	}

	private IEnumerator applyMaterialsWhenAvailableThenExtrude (string materialId, string wallMaterialId) {
		while (!MaterialManager.MaterialIndex.ContainsKey (materialId) || (wallMaterialId != null && !MaterialManager.MaterialIndex.ContainsKey(wallMaterialId))) {
			yield return new WaitForSeconds (0.5f);
//			Debug.Log ("Waiting for materials: " + materialId + ", " + wallMaterialId);
		}

//		Debug.Log ("Got materials: " + materialId + ", " + wallMaterialId);
		Material material = MaterialManager.MaterialIndex [materialId];
		Material wallMaterial = wallMaterialId != null ? MaterialManager.MaterialIndex [wallMaterialId] : null;

		if (hasSlaves()) {			
			foreach (BuildingRoof slaveRoof in slaves) {
				slaveRoof.extrude ();
				slaveRoof.applyMaterials (material, wallMaterial);
			}
		} else {
			extrude ();
			applyMaterials (material, wallMaterial);
		}
	}

	public PROPAGATION onMessage (string message, object data) {
		if (hasSlaves()) {
			foreach (BuildingRoof slaveRoof in slaves) {
				slaveRoof.onMessage(message, data);
			}
		} else {
			if (message == "gameIsReady") {
				roof.transform.localPosition = -bodyPositionAfterRising;
			}
		}
		return PROPAGATION.DEFAULT;
	}

    public float getTargetHeight() {
        return height + bodyPositionWhileRising.z;
    }

    void OnDestroy() {
        PubSub.unsubscribeAllForSubscriber(this);
    }

    public void turnOnExplodable() {
        Misc.SetGravityState (gameObject, true);
    }

}
	