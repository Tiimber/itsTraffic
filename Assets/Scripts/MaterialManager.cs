using UnityEngine;
using System.Collections.Generic;

public class MaterialManager {

	private static string[] MaterialTypes = new string[]{"Outdoors", "Roof", "Street"};

	private static Dictionary<string, Material> MaterialResources = new Dictionary<string, Material>();

	public static Dictionary<string, Material> MaterialIndex = new Dictionary<string, Material>();

	public static void LoadMaterial (string id, string type) {
		if (MaterialResources.Count == 0) {
			LoadLocalMaterials ();
		}

		if (!MaterialIndex.ContainsKey (id)) {
			// First check if it exists in loaded resources...
			if (MaterialResources.ContainsKey(type + "-" + id)) {
				Material material = MaterialResources[type + "-" + id];
				MaterialIndex.Add (id, material);
			}
			// TODO - Else download and save it to our materials
		}
	}

	private static void LoadLocalMaterials () {
		foreach (string type in MaterialTypes) {
			Object[] resources = Resources.LoadAll (type + "/Materials");
			for (int i = 0; i < resources.Length; i++) {
				if (resources[i].GetType() == typeof(Material)) {
					string numberPrefix = GetNumberPrefix(resources[i].name);
					MaterialResources.Add(type + "-" + numberPrefix, (Material)resources[i]);
				}
			}	
		}
	}

	private static string GetNumberPrefix (string name) {
		string[] parts = name.Split (new char[] {'-'});
		return parts[0];
	}
}
