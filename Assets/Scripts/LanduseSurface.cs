﻿using UnityEngine;
using System.Collections;
using System.Xml;
using System.Collections.Generic;

public class LanduseSurface : MapSurface {

	private const string DEFAULT_TYPE = "DEFAULT";

	private static Dictionary<string, Color> colors = new Dictionary<string, Color> () {
		{"commercial", new Color (0, 0, 0.2f)},
		{"residential", new Color (0.2f, 0, 0)},
		{"industrial", new Color (0.2f, 0.2f, 0)},
		{"retail", new Color (0.2f, 0, 0.2f)},
		{"cemetery", new Color (0.43f, 0.62f, 0.46f)},
		{"school", new Color (0.8f, 0.8f, 0.35f)},
		{"park", new Color (0.22f, 0.78f, 0.29f)},
		{"grass", new Color (0.10f, 8f, 0.17f)},
		{"soccerfield", new Color (0.10f, 8f, 0.17f)},
		{"platform", new Color (0.73f, 0.73f, 0.73f)},

		// Different kinds of water
		{"river", new Color (0.13f, 0.47f, 0.76f)},
		{"reservoir", new Color (0.13f, 0.47f, 0.76f)},
		{"water", new Color (0.13f, 0.47f, 0.76f)},

		{"background", new Color (0.03f, 0.10f, 0.05f)}, // TODO - Change
		{"_country", new Color(0.51f, 0.76f, 0.28f)},
		{"_sea", new Color(0.13f, 0.47f, 0.76f)},

		{DEFAULT_TYPE, new Color (0, 1f, 0)}
	};

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void createLanduseWithXMLNode (XmlNode xmlNode, Way way, string overrideType = null) {
		if (xmlNode != null) {
			string landuseType = "";
			if (overrideType != null) {
				landuseType = overrideType;
			} else if (way.getTagValue ("landuse") != null) {
				landuseType = way.getTagValue ("landuse");
			}
			this.gameObject.name = "Landuse - " + landuseType + " (" + way.printTags () + ")";
			createMesh (xmlNode);
//            createMeshCollider();
			setLanduseMaterial (landuseType);
		}
	}

	public void createLanduseAreaWithXMLNode (XmlNode xmlNode, Way way, string overrideType = null, float overrideWayWidthFactor = Mathf.Infinity) {
		if (xmlNode != null) {
			float wayWidthFactor;

			if (overrideWayWidthFactor != Mathf.Infinity) {
				wayWidthFactor = overrideWayWidthFactor;
			} else {
				wayWidthFactor = way.WayWidthFactor;
			}
			string landuseType = "";
			if (overrideType != null) {
				landuseType = overrideType;
			} else if (way.getTagValue ("landuse") != null) {
				landuseType = way.getTagValue ("landuse");
			}
			this.gameObject.name = "Landuse - " + landuseType + " (" + way.printTags () + ")";
			createMeshArea (xmlNode, wayWidthFactor);
			setLanduseMaterial (landuseType);

//            createMeshCollider();
		}
	}

	public void createLanduseAreaWithVectors (List<Vector3> vectors, string landuseType = null) {
		this.gameObject.name = "Landuse - " + landuseType;
		GameObject planeMesh = createPlaneMeshForPoints (vectors);
		planeMesh.transform.parent = this.transform;
		setLanduseMaterial (landuseType, planeMesh);
	} 

	public void createBackgroundLanduse () {
		this.gameObject.name = "Landuse - Background";
		List<Vector3> backgroundBounds = new List<Vector3> {
			new Vector3(-20f, -20f, 0),
			new Vector3(20f, -20f, 0),
			new Vector3(20f, 20f, 0),
			new Vector3(-20, 20f, 0)
		};
		GameObject planeMeshObj = createPlaneMeshForPoints (backgroundBounds);
		planeMeshObj.transform.parent = transform;

		Material material = new Material (Shader.Find ("Custom/PlainShader"));
		material.color = colors["background"];

		MeshRenderer meshRenderer = planeMeshObj.GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;

//        createMeshCollider();
    }

	private void setLanduseMaterial (string type, GameObject go = null) {
		Material material = new Material (Shader.Find ("Custom/PlainShader"));
		if (!colors.ContainsKey (type)) {
			type = DEFAULT_TYPE;
		}
		material.color = colors[type];

		MeshRenderer meshRenderer = go != null ? go.GetComponent<MeshRenderer> () : this.GetComponent<MeshRenderer> ();
		Renderer renderer = meshRenderer.GetComponent<Renderer> ();
		renderer.material = material;
	}
}
