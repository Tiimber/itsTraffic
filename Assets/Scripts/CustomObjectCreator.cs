using System.Collections;
using UnityEngine;

public class CustomObjectCreator {

	public static CustomObjectCreator instance = null;

	private Setup setup;
	private Coroutine placePeople;
	private Coroutine placeVehicles;

	public CustomObjectCreator (Setup setup) {
		this.setup = setup;
		GameTimer.resetTime();
        restart();
	}

    private void restart() {
        if (setup.people.Count > 0 && placePeople == null) {
            placePeople = Singleton<Game>.Instance.StartCoroutine(placeOutPeople ());
        }
        if (setup.vehicles.Count > 0 && placeVehicles == null) {
            placeVehicles = Singleton<Game>.Instance.StartCoroutine(placeOutVehicles ());
        }
        // TODO - If a vehicle (or human) is inserted before the "next one", we need to restart the coroutine
    }

    public void addPerson(Setup.PersonSetup personSetup) {
        setup.people.Add(personSetup);
        setup.people.Sort((p1, p2) => Mathf.RoundToInt((p1.time - p2.time) * 100));

        if (placePeople == null) {
            restart();
        }
    }

    public void addVehicle(Setup.VehicleSetup vehicleSetup) {
        setup.vehicles.Add(vehicleSetup);
        setup.vehicles.Sort((v1, v2) => Mathf.RoundToInt((v1.time - v2.time) * 100));

        if (placeVehicles == null) {
            restart();
        }
    }

	public void destroy() {
        if (placePeople != null) {
            Singleton<Game>.Instance.StopCoroutine (placePeople);
        }
        if (placeVehicles != null) {
            Singleton<Game>.Instance.StopCoroutine (placeVehicles);
        }
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
			float inTime = person.time - GameTimer.elapsedTime();
			yield return new WaitForSeconds (inTime);
			Game.instance.giveBirth (person);
			setup.people.RemoveAt (0);
		}
        placePeople = null;
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
			float inTime = vehicle.time - GameTimer.elapsedTime();
			yield return new WaitForSeconds (inTime);
			Game.instance.createNewCar (vehicle);
			setup.vehicles.RemoveAt (0);
		}
        placeVehicles = null;
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
