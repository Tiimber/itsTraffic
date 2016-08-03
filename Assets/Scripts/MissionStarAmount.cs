using UnityEngine;
using System.Collections;

public class MissionStarAmount : MonoBehaviour {

	private int initialStarAmount = 0;
	private int currentStarAmount = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void setInitialStarAmount(int amount) {
		initialStarAmount = amount;
		setStarAmount (amount);
	}

	public void setStarAmount(int amount) {
		currentStarAmount = amount;
		updateStars ();
	}

	private void updateStars() {
		// TODO - animate in when initialStarAmount and currentStarAmount differs
		initialStarAmount = currentStarAmount;
		for (int i = 1; i <= 5; i++) {
			GameObject starObject = transform.FindChild ("star_" + i).gameObject;
			StarImageToggle starToggle = starObject.GetComponent<StarImageToggle> ();
			starToggle.setActiveState (currentStarAmount >= i);
		}
	}
}
