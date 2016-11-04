using UnityEngine;

[System.Serializable]
public class VehiclesDistribution {

    public string brand;
	public Vehicle vehicle;
	[Range(1f, 100f)]
	public float frequency;

    public VehiclesDistribution(string brand, float frequency, Vehicle vehicle) {
    	this.brand = brand;
        this.frequency = frequency;
		this.vehicle = vehicle;
	}

}
