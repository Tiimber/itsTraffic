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
			DateTime now = DateTime.Now;
			dateOfBirth = new DateTime(UnityEngine.Random.Range(now.Year - 80, now.Year - 17), UnityEngine.Random.Range(1, 13), UnityEngine.Random.Range(1, 32));
		}
		if (money == 0f) {
			money = UnityEngine.Random.Range (0f, 500f);
		}
		Debug.Log("New human: " + name);
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
			StopCoroutine (coroutine);
			coroutine = null;
			information = null;
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
