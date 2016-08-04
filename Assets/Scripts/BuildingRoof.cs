using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuildingRoof : MapSurface, IPubSub {
	private static Vector3 bodyPositionWhileRising = new Vector3 (0, 0, 0.1f);
	private static Vector3 bodyPositionAfterRising = new Vector3 (0, 0, 0.001f);

	float height = 0f;
	float heightProperty = 0f;
	float constructionHeight = 0f;
	float constructionTime = 0f;
	float constructionTimeEnd = Mathf.PI/2f;
	float delayTime;
	
	private void setConstructionHeight() {
		if (constructionTime >= constructionTimeEnd) {
			constructionTime = constructionTimeEnd;
			constructionHeight = height;
		} else {
			constructionHeight = Mathf.Sin(constructionTime) * height;	
		}
	}

	void Start () {
		PubSub.subscribe ("mainCameraActivated", this);
		delayTime = Misc.randomRange (0.3f, 0.8f);
	}

	void Update () {
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

	public void createBuildingWithXMLNode(XmlNode xmlNode) {
		if (xmlNode != null) {
			this.gameObject.name = "BuildingRoof (" + xmlNode.Attributes.GetNamedItem ("id").Value + ")";
			createMesh (xmlNode);
		}
	}
	
	private void raiseBuilding () {
		transform.position = new Vector3(transform.position.x, transform.position.y, -constructionHeight);
	}

	private void extrude () {
		height = heightProperty * Game.heightFactor;

		MeshFilter filter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
		Mesh msh = filter.mesh;

		Matrix4x4 [] extrusionPath = new Matrix4x4 [2];
		extrusionPath[0] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
		extrusionPath[1] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position + new Vector3(0, 0, height), Quaternion.identity, Vector3.one);

		Mesh extrudedmesh = new Mesh ();
//		MeshExtrusion.ExtrudeMesh(msh, GetComponent<MeshFilter>().mesh, extrusionPath, false);
		MeshExtrusion.ExtrudeMesh(msh, extrudedmesh, extrusionPath, false);

		// Create the sides as separate gameobject
		GameObject sides = new GameObject ();
		sides.name = "Building side";
		MeshFilter sidesMeshFilter = sides.AddComponent<MeshFilter> ();
		sidesMeshFilter.mesh = extrudedmesh;
		MeshRenderer sidesMeshRenderer = sides.AddComponent<MeshRenderer> ();
		sidesMeshRenderer.material.color = new Color (1f, 0, 0);
		sides.transform.SetParent (this.gameObject.transform);
		sides.transform.localPosition = bodyPositionWhileRising;
	}
	
	public void setProperties (Dictionary<string, string> properties) {
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
		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;

		if (wallMaterial != null) {
			Transform sidesTransform = transform.FindChild ("Building side");
			MeshRenderer wallMeshRenderer = sidesTransform.gameObject.GetComponent<MeshRenderer> ();
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

		extrude ();
		applyMaterials (material, wallMaterial);
	}

	public PROPAGATION onMessage (string message, object data) {
		if (message == "mainCameraActivated") {
			Transform sidesTransform = transform.FindChild ("Building side");
			sidesTransform.localPosition = bodyPositionAfterRising;
		}
		return PROPAGATION.DEFAULT;
	}
}
	