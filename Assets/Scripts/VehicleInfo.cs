using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class VehicleInfo : MonoBehaviour {

	public string brand;
	public string model { get; set; }
	public int year;
	public float vehicleWidth = 0.05f;
	public string materialGameObjectName;
	public int mainMaterialIndex;

	// Size is number of people in the car (including driver)
	// Value inside each element is relative probability of each number of passengers
	// Element 0 - No passengers probability
	// Element n - Probability for n passengers
	public List<int> passengerFrequency;

	public int numberOfPassengers = -1;

	// Use this for initialization
	void Start () {
		Setup.VehicleSetup data = GetComponent<Vehicle> ().characteristics;
		if (data != null) {
//			name = data.name;
//			brand = brand != null ? brand : data.brand;

            if (data.model != null) {
                model = data.model;

            } else {
                model = ModelGeneratorVehicles.generate (brand);
            }
            if (data.year != 0) {
				year = data.year;
			} else {
                year = DateTime.Now.Year - Misc.randomRange (0, 10);
            }
			numberOfPassengers = data.passengerIds.Count;

            // Set color
			GameObject materialGameObject = transform.Find (materialGameObjectName).gameObject;
			MeshRenderer meshRenderer = materialGameObject.GetComponent<MeshRenderer> ();

			Material mainColorMaterial = getMainColorMaterial (meshRenderer.materials);
			if (mainColorMaterial != null) {
				if (data.color != null) {
					Color color = Misc.parseColor (data.color);
					// Parse and set this color from data
					mainColorMaterial.SetColor ("_Color", color);
				} else {
					// Try to get color to use from level setup - if not specified, will use default color
					string colorForBrand = Game.instance.loadedLevel.vehicleColors.getRandomColorForBrand (brand);
					if (colorForBrand != null) {
						Color color = Misc.parseColor (colorForBrand);
						mainColorMaterial.SetColor ("_Color", color);
					}
				}
			}
		} else {
			model = ModelGeneratorVehicles.generate (brand);
			year = DateTime.Now.Year - Misc.randomRange (0, 10);

			// Parse and set this color from data
			GameObject materialGameObject = transform.Find (materialGameObjectName).gameObject;
			MeshRenderer meshRenderer = materialGameObject.GetComponent<MeshRenderer> ();

			Material mainColorMaterial = getMainColorMaterial (meshRenderer.materials);
			if (mainColorMaterial != null) {
				// Try to get color to use from level setup - if not specified, will use default color
				string colorForBrand = Game.instance.loadedLevel.vehicleColors.getRandomColorForBrand (brand);
				if (colorForBrand != null) {
					Color color = Misc.parseColor (colorForBrand);
					mainColorMaterial.SetColor ("_Color", color);
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	private void randomizeNumberOfPassengers () {
		int index = 0;
		int sum = passengerFrequency.Sum ();
		int random = Misc.randomRange (0, sum);
		int current = 0;
		foreach (int part in passengerFrequency) {
			current += part;
			if (random < current) {
				break;
			}
			index++;
		}
		numberOfPassengers = index;
	}

	public int getNumberOfPassengers() {
		if (numberOfPassengers == -1) {
			randomizeNumberOfPassengers ();
		}
		return numberOfPassengers;
	}

	public Material getMainColorMaterial(Material[] materials) {
		return Array.Find<Material> (materials, material => Regex.IsMatch (material.name, "((^car.*)|(.*_car))(\\s\\(Instance\\))?", RegexOptions.IgnoreCase));
	}
}
