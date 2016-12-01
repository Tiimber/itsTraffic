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
//	public List<InformationHuman> peopleGoingHere; // TODO


	// Use this for initialization
	void Start() {
		type = TYPE_POI;
		// Check for stored information in POIIcon
		POIIcon poi = GetComponent<POIIcon> ();
		base.name = poi.getName ();
		group = poi.getGroup ();
		address = poi.getAddress ();
	}

	public override List<KeyValuePair<string, object>> getInformation() {
		if (information == null) {
			information = base.getInformation ();

			information.Add (new KeyValuePair<string, object> ("Type", group));
			if (address != null) {
				information.Add (new KeyValuePair<string, object> ("Adress", address));
			}
		}

		return information;
	}
}