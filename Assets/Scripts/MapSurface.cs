using UnityEngine;
using System.Collections;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Linq;

public class MapSurface : MonoBehaviour {

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

//		Vector3[] normals = msh.normals;
//		for (int i = 0; i < normals.Length; i++) {
//			normals[i] = normal;
//		}
//		msh.SetNormals (normals.ToList ());
////		msh.RecalculateNormals();
////		msh.RecalculateBounds();

		// Set up game object with mesh;
		gameObject.AddComponent(typeof(MeshRenderer));
		MeshFilter filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
		filter.mesh = msh;
	}

	public enum Anchor {
		CENTER,
		LEFT_CENTER
	}

	public static GameObject createPlaneMeshForPoints(Vector3 from, Vector3 to, Anchor anchor = Anchor.CENTER) {
		Vector3 offset = from + (to - from) / 2;
		from -= offset;
		to -= offset;

		switch (anchor) {
			case Anchor.LEFT_CENTER: 
				offset += new Vector3(from.x, 0f, 0f); 
				to -= new Vector3(from.x, 0f, 0f); 
				from -= new Vector3(from.x, 0f, 0f); 
				break;
			default: break;
		}

		Vector2[] points = new Vector2[]{
			new Vector2(from.x, from.y),
			new Vector2(to.x, from.y),
			new Vector2(to.x, to.y),
			new Vector2(from.x, to.y)
		};

		return createPlaneMeshForVector2 (points, offset);
	}

	public static GameObject createPlaneMeshForPoints(List<Vector3> points) {
		Vector2[] vector2Points = new Vector2[points.Count];
		int i = 0;
		foreach (Vector3 point in points) {
			vector2Points[i++] = point;
		}
		return createPlaneMeshForVector2 (vector2Points, Vector3.zero);
	}

	private static GameObject createPlaneMeshForVector2 (Vector2[] points, Vector3 offset) {
		GameObject planeMesh = new GameObject ("Plane Mesh For Points");
		
		addMeshToGameObject(planeMesh, points);
		
		planeMesh.transform.position = offset;
		
		return planeMesh;
	}
	
		
}
	