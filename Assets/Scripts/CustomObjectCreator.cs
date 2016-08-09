using System.Collections;
using UnityEngine;

public class CustomObjectCreator {

	private static CustomObjectCreator instance = null;

	private Setup setup;
	private Coroutine placePeople;
	private Coroutine placeVehicles;
	private float time;

	public CustomObjectCreator (Setup setup) {
		this.setup = setup;
		this.time = Time.time;
		if (setup.people.Count > 0) {
			placePeople = Singleton<Game>.Instance.StartCoroutine(placeOutPeople ());
		}
		if (setup.vehicles.Count > 0) {
			placeVehicles = Singleton<Game>.Instance.StartCoroutine(placeOutVehicles ());
		}
	}

	public void destroy() {
		Singleton<Game>.Instance.StopCoroutine (placePeople);
		Singleton<Game>.Instance.StopCoroutine (placeVehicles);
		setup = null;
	}

	public IEnumerator placeOutPeople() {
		yield return null;
		while (setup.people.Count > 0 && setup.people [0].time == -1) {
			Setup.PersonSetup person = setup.people [0];
			Game.instance.giveBirth (person);
			setup.people.RemoveAt (0);
		}
		while (setup.people.Count > 0) {
			Setup.PersonSetup person = setup.people [0];
			float inTime = person.time - (Time.time - time);
			yield return new WaitForSeconds (inTime);
			Game.instance.giveBirth (person);
			setup.people.RemoveAt (0);
		}
	}

	public IEnumerator placeOutVehicles() {
		yield return null;
		while (setup.vehicles.Count > 0 && setup.vehicles [0].time == -1) {
			Setup.VehicleSetup vehicle = setup.vehicles [0];
			Game.instance.createNewCar (vehicle);
			setup.vehicles.RemoveAt (0);
		}
		while (setup.vehicles.Count > 0) {
			Setup.VehicleSetup vehicle = setup.vehicles [0];
			float inTime = vehicle.time - (Time.time - time);
			yield return new WaitForSeconds (inTime);
			Game.instance.createNewCar (vehicle);
			setup.vehicles.RemoveAt (0);
		}
	}

	public static void initWithSetup(Setup setup) {
		instance = new CustomObjectCreator (setup);
	}
		
	public static void Destroy() {
		if (instance != null) {
			instance.destroy ();
			instance = null;
		}
	}
}
