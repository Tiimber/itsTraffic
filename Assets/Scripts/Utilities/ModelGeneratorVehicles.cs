using System.Collections.Generic;
using UnityEngine;

public class ModelGeneratorVehicles {

	public static string generate(string brand) {
		List<string> models;
		if (modelsForBrand.ContainsKey (brand) && modelsForBrand [brand].Count > 0) {
			models = modelsForBrand [brand];
		} else {
            if (modelsForBrand.ContainsKey (brand)) Debug.Log("Did not find brand "+brand);
            else if (modelsForBrand [brand].Count > 0) Debug.Log("No models found for "+brand);
			models = modelsForBrand ["Sportzcar"];
		}
		return Misc.pickRandom (models);
	}

    public static void setBusLines(List<string> busLines) {
		List<string> busData = modelsForBrand ["Bus"];
		if (busLines.Count == 0) {
        	busData.Clear();
        	busData.AddRange(busLines);
        }
    }

	private static Dictionary<string, List<string>> modelsForBrand = new Dictionary<string, List<string>> {
		{
			"Sportzcar", new List<string> {"Prancer", "3310", "Bolt", "Zolt", "ZX", "x86", "NES", "Oldsmobile", "Zpyder", "Camro"}
//		}, {
//			"Wolvo", new List<string> {"TI-81", "C64", "N64", "11-7", "X36.0", "X1", "X2000"}
		}, {
			"Midza", new List<string> {"Carola", "Bettan", "Rachel", "Brittany", "Robin", "Athena", "Domino"}
		}, {
			"Handaj", new List<string> {"Croissant", "Innuendo", "Nontendo", "Bisou", "Citrouille", "Irusu", "Eikel"}
		}, {
        	"Bus", new List<string> {"2X", "Buffalo", "Merit", "Berit", "Gitaro", "Classic", "Cub", "Flash", "Elite", "Falcon"}
        }, {
        	"Truck", new List<string> {"Sconea", "Evico", "Gluse", "Marcides-Bonz", "Beast", "Inferno", "Muscle"}
        }, {
        	"Ri-Now", new List<string> {"Captain", "Kilo", "Dasher", "Spacy", "Influence", "Colonoscopy", "Pluse"}
        }, {
        	"Faith", new List<string> {"Typo", "Zuper", "Popolino", "Barista", "Zport", "Onu", "Ack", "Pundo", "Million", "Zpider", "Basta", "Piano", "Rasta"}
        }, {
        	"Marcides-Bonz", new List<string> {"ELK-Classe", "Mario", "Ü-Classe", "VV123", "Schulz", "Fritz", "Kraut", "SOS OMG", "ß-Classe", "CLS-Classe", "8-D Classe", "Schatzi"}
        }, {
        	"Po-liz", new List<string> {"Paddy", "Prowl", "Panda", "Patrol", "Bacon", "Bobby", "Bulle", "Law", "Popo", "Donut", "Popo"}
        }, {
        	"Fraud", new List<string> {"Moustache", "Vellus", "Baerder", "Pibuc", "Ficail", "Armpat", "Ack", "Star", "Lence"}
        }
	};
}
