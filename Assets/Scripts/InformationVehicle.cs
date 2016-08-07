using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InformationVehicle : InformationBase {

	/*
	 * Year
	 * Distance driving
	 * Condition
	 * 
	 * Destination(s)
	 * 
	 * Driver
	 * Passenger(s)
	 */

	protected int year;
	protected int distance;
	protected string condition;
//	protected List<InformationNode> destination; // TODO
	public InformationHuman driver;
	public List<InformationHuman> passengers;

	private Coroutine coroutine;

	// Use this for initialization
	void Start () {
		type = "Vehicle";
		VehicleInfo info = GetComponent<VehicleInfo> ();
		name = info.brand + " " + info.model;
		year = info.year;
//		Debug.Log("New vehicle: " + name + " (" + year + ")");
	}
	
	public override List<KeyValuePair<string, object>> getInformation () {
		Vehicle vehicle = GetComponent<Vehicle> ();
		if (vehicle != null) {
			distance = Mathf.FloorToInt(vehicle.totalDrivingDistance);
			condition = calculateCondition (vehicle.health, vehicle.startHealth);
		}
		List<KeyValuePair<string, object>> information = base.getInformation ();

		information.Add (new KeyValuePair<string, object>("Year", year));
		information.Add (new KeyValuePair<string, object>("Have driven", Misc.getDistance(distance)));
		information.Add (new KeyValuePair<string, object>("Condition", condition + "%")); // TODO - Readable labels?

//		information.Add (new KeyValuePair<string, object>("Destination", destination));

		information.Add (new KeyValuePair<string, object>("Driver", driver));
		if (passengers.Count > 0) {
			information.Add (new KeyValuePair<string, object>("Passenger", passengers));
		}

		keepInformationUpToDate (true, information);

		return information;		
	}

	private void keepInformationUpToDate (bool start, List<KeyValuePair<string, object>> information = null) {
		if (start) {
			if (coroutine == null) {
				coroutine = StartCoroutine (checkAndUpdateVehicleInfo (information));
			}
		} else {
			if (coroutine != null) {
				StopCoroutine (coroutine);
				coroutine = null;
			}
		}
	}

	private IEnumerator checkAndUpdateVehicleInfo (List<KeyValuePair<string, object>> information) {
		Vehicle vehicle = GetComponent<Vehicle> ();
		while (vehicle != null) {
			distance = Mathf.FloorToInt(vehicle.totalDrivingDistance);
			condition = calculateCondition (vehicle.health, vehicle.startHealth);
			int indexHaveDriven = information.FindIndex (pair => pair.Key == "Have driven");
			information.RemoveAt (indexHaveDriven);
			information.Insert (indexHaveDriven, new KeyValuePair<string, object>("Have driven", Misc.getDistance(distance)));
			int indexCondition = information.FindIndex (pair => pair.Key == "Condition");
			information.RemoveAt (indexCondition);
			information.Insert (indexCondition, new KeyValuePair<string, object>("Condition", condition + "%")); // TODO - Readable labels?
			yield return new WaitForSeconds(1f); 
		}
	}

	public override void disposeInformation () {
		keepInformationUpToDate (false);
		driver.disposeInformation ();
		foreach (InformationHuman passenger in passengers) {
			passenger.disposeInformation ();
		}
	}

	private string calculateCondition (float health, float startHealth) {
		float pctHealth = health / startHealth;
		// TODO - Readable labels?
		return "" + Mathf.FloorToInt(pctHealth * 100);
	}

	void OnDestroy() {
		disposeInformation ();
	}
}
