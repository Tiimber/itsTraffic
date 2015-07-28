using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuildingRoof : MonoBehaviour {
	long height;

	public void createMesh(XmlNode xmlNode) {
		if (xmlNode != null) {
			this.gameObject.name = "BuildingRoof (" + xmlNode.Attributes.GetNamedItem("id").Value + ")";
			XmlNodeList nodeRefs = xmlNode.SelectNodes ("nd/@ref");
			Vector2[] vertices2D = new Vector2[nodeRefs.Count-1];
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
			int[] indices = tr.Triangulate();
			
			// Create the Vector3 vertices
			Vector3[] vertices = new Vector3[vertices2D.Length];
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
			extrude(msh);
		}
	}

	private void extrude(Mesh msh) {
		//MeshFilter filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
		//Mesh msh = filter.mesh;
		for (int i = 0; i<msh.vertices.Length; i++) {
			msh.vertices[i].z = -height;
		}
		msh.RecalculateNormals();
		msh.RecalculateBounds();
		Matrix4x4 [] extrusionPath = new Matrix4x4 [2];
		extrusionPath[0] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
		extrusionPath[1] = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position + new Vector3(0, 0, 10f), Quaternion.identity, Vector3.one);
		MeshExtrusion.ExtrudeMesh(msh, GetComponent<MeshFilter>().mesh, extrusionPath, false);
	}
	
	public void setProperties (Dictionary<string, string> properties) {
		string materialId = properties["material"];
		Material material = Game.MaterialIndex [materialId];
		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;

		height = Convert.ToInt64 (properties["height"]);
		//extrude();
	}
}
	


