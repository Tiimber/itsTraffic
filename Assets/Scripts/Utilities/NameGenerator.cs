using UnityEngine;
using System.Collections.Generic;

public class NameGenerator {

	private const string DEFAULT_COUNTRY = "se";

	public static string generate(string country = DEFAULT_COUNTRY) {
		char sex = Random.value <= 0.5f ? 'f' : 'm';
		string name = "";
		if (NameGeneratorNames.firstNamesByNationalityAndSex.ContainsKey (country)) {
			// First name
			Dictionary<char, List<string>> firstnamesForCountry = NameGeneratorNames.firstNamesByNationalityAndSex[country];
			List<string> firstnames;
			if (firstnamesForCountry.ContainsKey (sex)) {
				firstnames = firstnamesForCountry [sex];
			} else {
				firstnames = firstnamesForCountry ['*'];
			}
			string firstname = Misc.pickRandom (firstnames);

			// Last name
			Dictionary<char, List<string>> lastnamesForCountry = NameGeneratorNames.lastNamesByNationalityAndSex[country];
			List<string> lastnames;
			if (lastnamesForCountry.ContainsKey (sex)) {
				lastnames = lastnamesForCountry [sex];
			} else {
				lastnames = lastnamesForCountry ['*'];
			}
			string lastname = Misc.pickRandom (lastnames);

			name = firstname + ' ' + lastname;		
		} else {
			// TODO - Handle differently
			name = "No names for this country";
		}
		return name;
	}


}
