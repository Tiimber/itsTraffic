using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.IO;

public class MaterialManager {

	private static string materialBaseUrl = "http://samlingar.com/itsTraffic/";
	private static string[] MaterialTypes = new string[]{"Outdoors", "Roof", "Street"};

	// Materials that we have available in the app
	private static Dictionary<string, Material> MaterialResources = new Dictionary<string, Material>();

	// List of materials available to download
	private static Dictionary<string, string> MaterialAvailable = new Dictionary<string, string>();
	private static Dictionary<string, KeyValuePair<int, int>> MaterialAvailableSizes = new Dictionary<string, KeyValuePair<int, int>> ();

	// Materials that are indexed for use
	public static Dictionary<string, Material> MaterialIndex = new Dictionary<string, Material>();

	public static IEnumerator<WWW> Init () {
		LoadLocalMaterials ();

		WWW connection = new WWW (materialBaseUrl + "list.txt");
		yield return connection;
		GetListOfRemoteMaterials (connection);
	}

	public static IEnumerator<WWW> LoadMaterial (string id, string type) {
		if (!MaterialIndex.ContainsKey (id)) {
			string materialKey = type + "-" + id;
			// First check if it exists in loaded resources...
			if (MaterialResources.ContainsKey (materialKey)) {
				// Resource found - just add it to our index
				Material material = MaterialResources [materialKey];
				MaterialIndex.Add (id, material);
				yield return null;
			} else if (MaterialAvailable.ContainsKey (materialKey)) {
				// Material is available, need to download and construct the material from the texture
				WWW connection = new WWW (materialBaseUrl + MaterialAvailable[materialKey]);
				yield return connection;

				DownloadAndCreateMaterial (connection, id, type, MaterialAvailable [materialKey], materialKey);
			} else {
				// Material is missing, what to do?
				Debug.LogWarning ("Couldn't find material for ID: " + id + " (" + type+ ")");
				yield return null;
			}
		} else {
			yield return null;
		}
	}

	private static void LoadLocalMaterials () {
		// Load all resources that are embedded in the application (also includes materials downloaded, created and saved)
		foreach (string type in MaterialTypes) {
			UnityEngine.Object[] resources = Resources.LoadAll (type + "/Materials");
			for (int i = 0; i < resources.Length; i++) {
				if (resources[i].GetType() == typeof(Material)) {
					string numberPrefix = GetNumberPrefix(resources[i].name);
					MaterialResources.Add(type + "-" + numberPrefix, (Material)resources[i]);
				}
			}	
		}
	}

	private static void GetListOfRemoteMaterials (WWW connection) {
		string[] lines = connection.text.Split(new char[] {'\n'});
		foreach (string line in lines) {
			if (line.Length > 0 && !line.Trim().StartsWith("#") && line.Contains("|")) {
				string[] parts = line.Split (new char[] {'|'}, 4);
				if (parts.Length >= 2) {
					string type = parts[0];
					string file = parts[1];
					int width = 32;
					int height = 32;
					if (parts.Length > 2) {
						width = Convert.ToInt32 (parts[2]);
					}
					if (parts.Length > 3) {
						height = Convert.ToInt32 (parts[3]);
					}
					string filename = type + "/" + file;
					string numberPrefix = GetNumberPrefix(file);
					string entryKey = type + "-" + numberPrefix;

					if (!MaterialAvailable.ContainsKey(entryKey)) {
						MaterialAvailable.Add(entryKey, filename);
						MaterialAvailableSizes.Add (entryKey, new KeyValuePair<int, int>(width, height));
					}
				}
			}
		}
	}

	private static void DownloadAndCreateMaterial (WWW connection, string id, string type, string filename, string materialKey) {
//		string savePath = Application.persistentDataPath + "/";
//		if (!System.IO.Directory.Exists (savePath + type)) {
//			System.IO.Directory.CreateDirectory (savePath + type);
//			System.IO.Directory.CreateDirectory (savePath + type + "/Materials");
//		}
//		System.IO.File.WriteAllBytes ("Assets/Resources/" + filename, connection.bytes);
		// Now create the Texture from the image data
		KeyValuePair<int, int> size = MaterialAvailableSizes [materialKey];
		Texture2D materialTexture = new Texture2D (size.Key, size.Value);
		materialTexture.LoadImage (connection.bytes);
		byte[] pngData = materialTexture.EncodeToPNG ();
		string textureFullFilePath = "Assets/Resources/" + filename;
		File.WriteAllBytes(textureFullFilePath, pngData);
//		DestroyImmediate(materialTexture);
		AssetDatabase.Refresh ();

		// Now to create a simple material with the texture
//		Material material = new Material (Shader.Find ("Custom/PlainShader"));
//		material.mainTexture = materialTexture;
		string materialFullFilePath = "Assets/Resources/" + type + "/Materials/" + StripTypeAndExtension (filename) + ".mat";
		AssetDatabase.CreateAsset (new Material (Shader.Find ("Custom/PlainShader")), materialFullFilePath);
		Material material = (Material) (AssetDatabase.LoadAssetAtPath (materialFullFilePath, typeof(Material)));
		material.mainTexture = (Texture2D)(AssetDatabase.LoadAssetAtPath (textureFullFilePath, typeof(Texture2D)));

		// Save the texture and material to files in our resources folder, for quick load next time
//		Debug.Log (filename);
//		AssetDatabase.CreateAsset (materialTexture, "Assets/Resources/" + filename);
//		AssetDatabase.CreateAsset (material, "Assets/Resources/" + type + "/Materials/" + StripTypeAndExtension(filename) + ".mat");
		AssetDatabase.SaveAssets ();

		// Add it to our index
		MaterialIndex.Add (id, material);
		Debug.Log ("Material indexed: " + id);
	}

	private static string StripTypeAndExtension (string name) {
		string withouttype = name.Substring (name.IndexOf ('/') + 1);
		string withoutending = withouttype.Substring (0, withouttype.LastIndexOf ('.'));
		return withoutending;
	}

	private static string GetNumberPrefix (string name) {
		string[] parts = name.Split (new char[] {'-'});
		return parts[0];
	}
}
