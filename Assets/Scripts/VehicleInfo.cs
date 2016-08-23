using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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
			brand = data.brand;
			model = data.model;
			year = data.year;
			numberOfPassengers = data.passengerIds.Count;
			if (materialGameObjectName != null && data.color != null) {
				GameObject materialGameObject = transform.FindChild (materialGameObjectName).gameObject;
				MeshRenderer meshRenderer = materialGameObject.GetComponent<MeshRenderer> ();

				Material mainColorMaterial = meshRenderer.materials [mainMaterialIndex];
				Color color = Misc.parseColor (data.color);
				mainColorMaterial.SetColor ("_Color", color);
			}
		} else {			
			model = ModelGeneratorVehicles.generate (brand);
			year = DateTime.Now.Year - Misc.randomRange (0, 10);
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
}
