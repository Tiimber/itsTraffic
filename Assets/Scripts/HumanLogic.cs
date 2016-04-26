using UnityEngine;
using System.Collections;

public class HumanLogic : MonoBehaviour {

	public static System.Random HumanRNG = new System.Random ((int)Game.randomSeed);

	// Use this for initialization
	void Start () {
		DataCollector.Add ("Total # of people", 1f);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
