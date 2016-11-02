using UnityEngine;
using System.Collections;

[System.Serializable]
public class VehiclesDistribution {

    public string brand;
	public Vehicle vehicle;
	[Range(1f, 100f)]
	public float frequency;
}
