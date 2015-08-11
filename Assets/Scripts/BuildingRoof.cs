using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuildingRoof : MonoBehaviour {
	float height = 0f;
	float constructionHeight = 0f;
	float constructionTime = 0f;
	float constructionTimeEnd = Mathf.PI/2f;
	float delayTime;

	Vector3[] vertices;
	int[] indices;
	Vector2[] vertices2D;

	private void setConstructionHeight() {
		if (constructionTime >= constructionTimeEnd) {
			constructionTime = constructionTimeEnd;
			constructionHeight = height;
		} else {
			constructionHeight = Mathf.Sin(constructionTime) * height;	
		}
	}

	void Start () {
		delayTime = UnityEngine.Random.Range (0.3f, 0.8f);
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

	public void createMesh(XmlNode xmlNode) {
		if (xmlNode != null) {
			this.gameObject.name = "BuildingRoof (" + xmlNode.Attributes.GetNamedItem("id").Value + ")";
			XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
			vertices2D = new Vector2[nodeRefs.Count-1];
			int i = 0;
			foreach (XmlAttribute refAttribute in nodeRefs) {
				Pos pos = NodeIndex.nodes [Convert.ToInt64 (refAttribute.Value)];
				Vector3 worldPos = Game.getCameraPosition(pos);
				vertices2D[i++] = worldPos;
				if (i==nodeRefs.Count-1) {
					break;
				}
			}

			// Use the triangulator to get indices for creating triangles
			Triangulator tr = new Triangulator(vertices2D);
			indices = tr.Triangulate();
			
			// Create the Vector3 vertices
			vertices = new Vector3[vertices2D.Length];
			for (i=0; i<vertices.Length; i++) {
				vertices[i] = new Vector3(vertices2D[i].x, vertices2D[i].y, 0);
			}
			
			// Create the mesh
			Mesh msh = new Mesh();
			msh.vertices = vertices;
			msh.triangles = indices;
			msh.uv = vertices2D;
			msh.RecalculateNormals();
			msh.RecalculateBounds();
			
			// Set up game object with mesh;
			gameObject.AddComponent(typeof(MeshRenderer));
			MeshFilter filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
			filter.mesh = msh;
		}
	}
	
	private void raiseBuilding () {
		transform.position = new Vector3(transform.position.x, transform.position.y, -constructionHeight);
	}

	private void extrude() {
		MeshFilter filter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
		Mesh msh = filter.mesh;

		Matrix4x4 [] extrusionPath = new Matrix4x4 [2];
		extrusionPath[0] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
		extrusionPath[1] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position + new Vector3(0, 0, height), Quaternion.identity, Vector3.one);
		MeshExtrusion.ExtrudeMesh(msh, GetComponent<MeshFilter>().mesh, extrusionPath, false);
	}
	
	public void setProperties (Dictionary<string, string> properties) {
		string materialId = properties["material"];

		long heightProperty = Convert.ToInt64 (properties ["height"]);
		height = heightProperty * Game.heightFactor;

		Game.instance.addInitAnimationRequest ();
		if (MaterialManager.MaterialIndex.ContainsKey (materialId)) {
			Material material = MaterialManager.MaterialIndex [materialId];
			applyMaterial (material);

			extrude ();
		} else {
			StartCoroutine (applyMaterialWhenAvailableThenExtrude(materialId));
		}
	}

	private void applyMaterial (Material material) {
		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;
	}

	private IEnumerator applyMaterialWhenAvailableThenExtrude (string materialId) {
		while (!MaterialManager.MaterialIndex.ContainsKey (materialId)) {
			yield return new WaitForSeconds (0.5f);
			Debug.Log ("Waiting for material: " + materialId);
		}

		Debug.Log ("Got material: " + materialId);
		Material material = MaterialManager.MaterialIndex [materialId];
		applyMaterial (material);
	}
}
	