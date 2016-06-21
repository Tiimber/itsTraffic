using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class InformationHuman : InformationBase {

	/*
	 * Age (dob)
	 * Money
	 * Mood
	 * Distance walking
	 * Destination(s)
	 */

	private static DateTime notBornYet = new DateTime(0);

	private Coroutine coroutine;
	private List<KeyValuePair<string, object>> information;

	protected DateTime dateOfBirth = notBornYet;
	protected float money;
	protected string mood;
	protected float distance;
//	protected List<InformationNode> destination; // TODO


	// Use this for initialization
	void Start () {
		init ();
	}

	public void init() {
		type = "Human";
		if (name == null) {
			name = NameGenerator.generate ();
		}
		if (dateOfBirth == notBornYet) {
			// TODO - Make sure random ranges are valid dates
			DateTime now = DateTime.Now;
			int daysOld = UnityEngine.Random.Range (6574, 29220); // 18-80 years old in days
			dateOfBirth = new DateTime(now.Ticks - Misc.daysToTicks(daysOld)); // Days to ticks
		}
		if (money == 0f) {
			money = UnityEngine.Random.Range (0f, 500f);
		}
//		Debug.Log("New human: " + name);
	}

	public override List<KeyValuePair<string, object>> getInformation () {
		if (information == null) {
			HumanLogic human = GetComponent<HumanLogic> ();
			if (human != null) {
				distance = Mathf.FloorToInt (human.totalWalkingDistance);
//				mood = calculateMood (human.mood); // TODO
			}

			information = base.getInformation ();

			information.Add (new KeyValuePair<string, object> ("Date of birth", dateOfBirth));
			information.Add (new KeyValuePair<string, object> ("Money", Misc.getMoney (money)));
//			information.Add (new KeyValuePair<string, object> ("Mood", mood));
			information.Add (new KeyValuePair<string, object> ("Have walked", Misc.getDistance (distance)));

			keepInformationUpToDate (true, information);
		}

		return information;		
	}

	private void keepInformationUpToDate (bool start, List<KeyValuePair<string, object>> information = null) {
		if (start) {
			if (coroutine == null) {
				coroutine = StartCoroutine (checkAndUpdateHumanInfo (information));
			}
		} else {
			if (coroutine != null) {
				StopCoroutine (coroutine);
				coroutine = null;
				this.information = null;
			}
		}
	}

	private IEnumerator checkAndUpdateHumanInfo (List<KeyValuePair<string, object>> information) {
		HumanLogic human = GetComponent<HumanLogic> ();
		while (human != null) {
			distance = Mathf.FloorToInt(human.totalWalkingDistance);
//			mood = calculateMood (human.mood); // TODO
			int indexHaveWalked = information.FindIndex (pair => pair.Key == "Have walked");
			information.RemoveAt (indexHaveWalked);
			information.Insert (indexHaveWalked, new KeyValuePair<string, object>("Have walked", Misc.getDistance(distance)));
			// TODO - Mood
			yield return new WaitForSeconds(1f); 
		}
	}

	public override void disposeInformation () {
		keepInformationUpToDate (false);
	}
}
