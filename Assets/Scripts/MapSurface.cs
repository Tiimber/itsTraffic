using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class MapSurface : MonoBehaviour {
//	Vector3[] vertices;
//	int[] indices;
//	Vector2[] vertices2D;
	
	protected void createMesh(XmlNode xmlNode) {
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

		addMeshToGameObject (gameObject, vertices2D);

	}

	private static void addMeshToGameObject (GameObject gameObject, Vector2[] vertices2D) {
		// Use the triangulator to get indices for creating triangles
		Triangulator tr = new Triangulator(vertices2D);
		int[] indices = tr.Triangulate();
		
		// Create the Vector3 vertices
		Vector3[] vertices = new Vector3[vertices2D.Length];
		for (int i=0; i<vertices.Length; i++) {
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

	public static GameObject createPlaneMeshForPoints(Vector3 from, Vector3 to, Quaternion rotation) {
		Vector3 offset = from + (to - from) / 2;
		from -= offset;
		to -= offset;

		Vector2[] points = new Vector2[]{
			new Vector2(from.x, from.y),
			new Vector2(to.x, from.y),
			new Vector2(to.x, to.y),
			new Vector2(from.x, to.y)
		};

		GameObject planeMesh = new GameObject ();
		planeMesh.name = "Plane Mesh For Points";

		addMeshToGameObject(planeMesh, points);

		planeMesh.transform.position = offset;
		planeMesh.transform.rotation = rotation;

		return planeMesh;
	}
}
	