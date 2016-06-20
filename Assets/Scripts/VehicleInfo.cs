using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public class VehicleInfo : MonoBehaviour {

	public string brand;
	public string model { get; set; }
	public int year;

	// Size is number of people in the car (including driver)
	// Value inside each element is relative probability of each number of passengers
	// Element 0 - No passengers probability
	// Element n - Probability for n passengers
	public List<int> passengerFrequency;

	private int numberOfPassengers = -1;

	// Use this for initialization
	void Start () {
		model = ModelGeneratorVehicles.generate (brand);
		year = DateTime.Now.Year - UnityEngine.Random.Range (0, 10);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	private void randomizeNumberOfPassengers () {
		int index = 0;
		int sum = passengerFrequency.Sum ();
		int random = UnityEngine.Random.Range (0, sum);
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
