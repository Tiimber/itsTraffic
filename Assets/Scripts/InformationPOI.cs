using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class InformationPOI : InformationBase {

	/*
	 * Name
	 * Group
	 * Address
	 *
	 * (People going here?)
	 */

	private Coroutine coroutine;
	private List<KeyValuePair<string, object>> information;

	public string group;
	public string address;
	public List<InformationHuman> peopleGoingHere;

	// Use this for initialization
	void Start() {
		type = TYPE_POI;
		// Check for stored information in POIIcon
		POIIcon poi = GetComponent<POIIcon> ();
		base.name = poi.getName ();
		group = poi.getGroup ();
		address = poi.getAddress ();
        peopleGoingHere = poi.getPeopleGoingHere();
	}

	public override List<KeyValuePair<string, object>> getInformation(bool onlyName = false) {
		if (onlyName) {
			return base.getInformation(onlyName);
		}

		if (information == null) {
			information = base.getInformation ();

			information.Add (new KeyValuePair<string, object> ("Type", group));
			if (address != null) {
				information.Add (new KeyValuePair<string, object> ("Adress", address));
			}

            information.Add (new KeyValuePair<string, object>("Person coming here", peopleGoingHere));

//            keepInformationUpToDate (true, information);
        }

		return information;
	}

//    private void keepInformationUpToDate (bool start, List<KeyValuePair<string, object>> information = null) {
//        if (start) {
//            if (coroutine == null) {
//                coroutine = StartCoroutine (checkAndUpdatePOIInfo (information));
//            }
//        } else {
//            if (coroutine != null) {
//                StopCoroutine (coroutine);
//                coroutine = null;
//                this.information = null;
//            }
//        }
//    }
//
//    private IEnumerator checkAndUpdatePOIInfo (List<KeyValuePair<string, object>> information) {
//        // Forever loop
//        while (true) {
//            int indexPeopleGoingHere = information.FindIndex (pair => pair.Key == "Person coming here");
//            information.RemoveAt (indexPeopleGoingHere);
//            information.Insert (indexPeopleGoingHere, new KeyValuePair<string, object>("Person coming here", peopleGoingHere));
//        }
//    }
//
//    public override void disposeInformation () {
//        keepInformationUpToDate (false);
//    }
}