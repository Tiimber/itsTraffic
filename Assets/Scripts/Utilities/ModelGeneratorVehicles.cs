using System.Collections.Generic;

public class ModelGeneratorVehicles {

	public static string generate(string brand) {
		List<string> models;
		if (modelsForBrand.ContainsKey (brand)) {
			models = modelsForBrand [brand];
		} else {
			models = modelsForBrand [""];
		}
		return Misc.pickRandom (models);
	}

    public static void setBusLines(List<string> busLines) {
		List<string> busData = modelsForBrand ["Bus"];
        busData.Clear();
        busData.AddRange(busLines);
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
        	"Bus", new List<string> {""}
        }, {
        	"Truck", new List<string> {"Sconea", "Evico", "Gluse", "Marcides-Bonz"}
        }, {
        	"Ri-Now", new List<string> {"Captain", "Kilo", "Dasher", "Spacy", "Influence", "Colonoscopy", "Pluse"}
        }, {
        	"Fraud", new List<string> {"Moustache", "Vellus", "Baerder", "Pibuc", "Ficail", "Armpat", "Ack", "Star", "Lence"}
        }
	};
}
