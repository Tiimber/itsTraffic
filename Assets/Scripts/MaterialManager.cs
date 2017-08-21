using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public class MaterialManager {

	private static string downloadedMaterialsFolder = Application.persistentDataPath + "/downloadedMaterials/";
	private static string localMaterialBaseUrl = "file://" + Application.persistentDataPath + "/downloadedMaterials/";
	private static string remoteMaterialBaseUrl = "http://localhost:4002/static/materials/";
	private static string[] MaterialTypes = new string[]{"Outdoors", "Roof", "Driveway", "Walkway", "Wall"};

	// Materials that we have available in the app
	private static Dictionary<string, Material> MaterialResources = new Dictionary<string, Material>();

	// List of materials available to download
	private static List<string> DownloadingMaterial = new List<string>();
	private static Dictionary<string, string> MaterialAvailable = new Dictionary<string, string>();
	private static Dictionary<string, KeyValuePair<int, int>> MaterialAvailableSizes = new Dictionary<string, KeyValuePair<int, int>> ();
	private static List<string> MaterialAvailableLocally = new List<string>();

	// Materials that are indexed for use
	public static Dictionary<string, Material> MaterialIndex = new Dictionary<string, Material>();

	// Material list synced?
	private static bool isSyncRemoteDone = false;
	private static bool isSyncLocalDone = false;
	// Before list synced, list of requested materials
	private static List<KeyValuePair<string, string>> queuedMaterialsForAfterSync = new List<KeyValuePair<string, string>> ();

	public static IEnumerator<WWW> Init () {
		LoadLocalMaterials ();

		// Ensure list of downloaded materials
		if (!Directory.Exists (downloadedMaterialsFolder)) {
			Directory.CreateDirectory (downloadedMaterialsFolder);
			string listInitData = "# Type|ID-Name.png|width|height\n";
			File.AppendAllText(downloadedMaterialsFolder + "list.txt", listInitData);
		}

		WWW downloadedMaterialsList = CacheWWW.Get(localMaterialBaseUrl + "list.txt", Misc.getTsForReadable("1m"));
		yield return downloadedMaterialsList;
		GetListOfRemoteMaterials (downloadedMaterialsList, false);

		WWW remoteMaterialsList = CacheWWW.Get(remoteMaterialBaseUrl + "list.txt");
		yield return remoteMaterialsList;
		GetListOfRemoteMaterials (remoteMaterialsList);
	}

	public static IEnumerator<WWW> LoadMaterial (string id, string type) {
		if (!MaterialIndex.ContainsKey (id)) {
			string materialKey = type + "-" + id;
			// First check if it exists in loaded resources...
			if (MaterialResources.ContainsKey (materialKey)) {
				// Resource found - just add it to our index
				Material material = MaterialResources [materialKey];
				MaterialIndex.Add (id, material);
//				Debug.Log ("Publishing Material: " + id);
				PubSub.publish("Material-" + id);
				yield return null;
			} else if (MaterialAvailable.ContainsKey (materialKey)) {
				// Material is available, need to download and construct the material from the texture
				if (!DownloadingMaterial.Contains (materialKey)) {
					DownloadingMaterial.Add (materialKey);
					bool isAvailableLocally = MaterialAvailableLocally.Contains (materialKey);
					string baseUrl = isAvailableLocally ? localMaterialBaseUrl : remoteMaterialBaseUrl;

					WWW connection = CacheWWW.Get(baseUrl + MaterialAvailable[materialKey]);
					yield return connection;

					DownloadAndCreateMaterial (connection, id, type, MaterialAvailable [materialKey], materialKey, !isAvailableLocally);
				}
			} else {
				if (isSyncRemoteDone && isSyncLocalDone) {
					// Material is missing, what to do?
					// TODO - This isn't correct - investigate later
//					Debug.LogWarning ("Couldn't find material for ID: " + id + " (" + type+ ")");
					yield return null;
				} else {
					queuedMaterialsForAfterSync.Add (new KeyValuePair<string, string>(id, type));
				}
			}
		} else {
			yield return null;
		}
	}

	private static void LoadLocalMaterials () {
		// Load all resources that are embedded in the application (also includes materials downloaded, created and saved)
		// TODO - Maybe not load in all resources, but only add them to the map and load them when first requested
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

	private static void GetListOfRemoteMaterials (WWW connection, bool isRemoteMaterials = true) {
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
						if (!isRemoteMaterials) {
							MaterialAvailableLocally.Add(entryKey);
						}
					}
				}
			}
		}
		if (isRemoteMaterials) {
			isSyncRemoteDone = true;
		} else {
			isSyncLocalDone = true;
		}
		if (isSyncLocalDone && isSyncRemoteDone) {
			loadQueuedMaterials ();
		}
	}

	private static void loadQueuedMaterials ()
	{
		foreach (KeyValuePair<string, string> materialItem in queuedMaterialsForAfterSync) {
			Singleton<Game>.Instance.StartCoroutine (LoadMaterial (materialItem.Key, materialItem.Value));
		}
	}

	private static void DownloadAndCreateMaterial (WWW connection, string id, string type, string filename, string materialKey, bool loadedFromWeb = true) {
		// Now create the Texture from the image data
		KeyValuePair<int, int> size = MaterialAvailableSizes [materialKey];
		Texture2D materialTexture = new Texture2D (size.Key, size.Value);
		connection.LoadImageIntoTexture (materialTexture);
		byte[] pngData = materialTexture.EncodeToPNG ();

		if (loadedFromWeb) {
			string textureTargetFolder = downloadedMaterialsFolder + type;
			string textureFullFilePath = downloadedMaterialsFolder + filename;
			if (!Directory.Exists (textureTargetFolder)) {
				Directory.CreateDirectory (textureTargetFolder);
			}
			File.WriteAllBytes (textureFullFilePath, pngData);
			string materialListEntry = type + "|" + StripFilename(filename, false) + "|" + size.Key + "|" + size.Value + "\n";
			File.AppendAllText(downloadedMaterialsFolder + "list.txt", materialListEntry);		
		}

		// Now to create a simple material with the texture
		Material material;
		switch (type) {
			case "Driveway": 
		 		material = new Material (Shader.Find ("Custom/DrivewayShader"));
				break;
			case "Walkway":
		 		material = new Material (Shader.Find ("Custom/WalkwayShader"));
				break;
			case "Roof":
				material = new Material(Shader.Find("Custom/PlainShader"));
				material.renderQueue = RenderOrder.BUILDING_ROOF;
				break;
			case "Wall":
				material = new Material(Shader.Find("Custom/PlainShader"));
				material.renderQueue = RenderOrder.BUILDING;
				break;
			default:  
		 		material = new Material (Shader.Find ("Custom/PlainShader"));
				break;
		}
		material.mainTexture = materialTexture;

		// Add it to our index
//		Debug.Log ("Adding to index: " + id);
		MaterialIndex.Add (id, material);
//		Debug.Log ("Publishing Material: " + id);
		PubSub.publish("Material-" + id);
//		Debug.Log ("Material indexed: " + id);
	}

	private static string StripFilename (string name, bool alsoStripExtension = true) {
		string result = name;
		result = result.Substring (result.IndexOf ('/') + 1);
		if (alsoStripExtension) {
			result = result.Substring (0, result.LastIndexOf ('.'));
		}
		return result;
	}

	private static string GetNumberPrefix (string name) {
		string[] parts = name.Split (new char[] {'-'});
		return parts[0];
	}
}
