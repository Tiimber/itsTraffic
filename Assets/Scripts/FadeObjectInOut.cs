/*
	FadeObjectInOut.cs
 	Hayden Scott-Baron (Dock) - http://starfruitgames.com
 	6 Dec 2012 
 
	This allows you to easily fade an object and its children. 
	If an object is already partially faded it will continue from there. 
	If you choose a different speed, it will use the new speed. 
 
	NOTE: Requires materials with a shader that allows transparency through color.  
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeObjectInOut : MonoBehaviour
{

	// publically editable speed
	public float fadeDelay = 0.0f; 
	public float fadeTime = 0.5f; 
	public bool fadeInOnStart = false; 
	public bool fadeOutOnStart = false;
	private bool logInitialFadeSequence = false; 
	public string DoneMessage { get; set; }




	// store colours
	private Color[] colors; 

	// allow automatic fading on the start of the scene
	IEnumerator Start ()
	{
		//yield return null; 
		yield return new WaitForSeconds (fadeDelay); 

		if (fadeInOnStart)
		{
			logInitialFadeSequence = true; 
			FadeIn (); 
		}

		if (fadeOutOnStart)
		{
			FadeOut (fadeTime); 
		}
	}




	// check the alpha value of most opaque object
	float MaxAlpha()
	{
		float maxAlpha = 0.0f; 
		Renderer[] rendererObjects = GetComponentsInChildren<Renderer>(); 
		foreach (Renderer item in rendererObjects)
		{
			maxAlpha = Mathf.Max (maxAlpha, item.material.color.a); 
		}
		return maxAlpha; 
	}

	private Material[] getMaterials (Renderer[] renderers) {
		List<Material> materials = new List<Material> ();
		foreach (Renderer renderer in renderers) {
			for (int i = 0; i < renderer.materials.Length; i++) {
				materials.Add (renderer.materials [i]);
			}
		}
		return materials.ToArray ();
	}

	// fade sequence
	IEnumerator FadeSequence (float fadingOutTime)
	{
		// log fading direction, then precalculate fading speed as a multiplier
		bool fadingOut = (fadingOutTime < 0.0f);
		float fadingOutSpeed = 1.0f / fadingOutTime; 

		// grab all child objects
		Renderer[] rendererObjects = GetComponentsInChildren<Renderer>();
		Material[] materials = getMaterials (rendererObjects);

		if (colors == null)
		{
			//create a cache of colors if necessary
			colors = new Color[materials.Length];

			// store the original colours for all child objects
			for (int i = 0; i < materials.Length; i++)
			{
				colors[i] = materials[i].color; 
			}
		}

		// make all objects visible
		for (int i = 0; i < rendererObjects.Length; i++)
		{
			rendererObjects[i].enabled = true;
		}


		// get current max alpha
		float alphaValue = MaxAlpha();  


		// This is a special case for objects that are set to fade in on start. 
		// it will treat them as alpha 0, despite them not being so. 
		if (logInitialFadeSequence && !fadingOut)
		{
			alphaValue = 0.0f; 
			logInitialFadeSequence = false; 
		}

		// iterate to change alpha value 
		while ( (alphaValue >= 0.0f && fadingOut) || (alphaValue <= 1.0f && !fadingOut)) 
		{
			alphaValue += Time.deltaTime * fadingOutSpeed; 

			for (int i = 0; i < materials.Length; i++)
			{
				Color newColor = colors[i];
				newColor.a = Mathf.Clamp (alphaValue, 0.0f, 1.0f); 				
				materials[i].SetColor("_Color", newColor) ; 
			}

			yield return null; 
		}

		// turn objects off after fading out
		if (fadingOut)
		{
			for (int i = 0; i < rendererObjects.Length; i++)
			{
				rendererObjects[i].enabled = false; 
			}
		}

		FadeInterface fadeInterface = GetComponent<FadeInterface> ();
		if (DoneMessage != null && fadeInterface != null) {
			fadeInterface.onFadeMessage (DoneMessage);
		}
	}


	public void FadeIn ()
	{
		FadeIn (fadeTime); 
	}

	public void FadeOut ()
	{
		FadeOut (fadeTime); 		
	}

	public void FadeIn (float newFadeTime)
	{
		StopCoroutine("FadeSequence"); 
		StartCoroutine("FadeSequence", newFadeTime); 
	}

	public void FadeOut (float newFadeTime)
	{
		StopCoroutine("FadeSequence"); 
		StartCoroutine("FadeSequence", -newFadeTime); 
	}


	// These are for testing only. 
//			void Update()
//			{
//				if (Input.GetKeyDown (KeyCode.K) )
//				{
//					FadeIn();
//				}
//				if (Input.GetKeyDown (KeyCode.L) )
//				{
//					FadeOut(); 
//				}
//			}

}