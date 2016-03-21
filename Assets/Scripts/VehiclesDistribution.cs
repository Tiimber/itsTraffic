using UnityEngine;
using System.Collections;

[System.Serializable]
public class VehiclesDistribution {

	public Vehicle vehicle;
	[Range(1f, 100f)]
	public float frequency;
}
