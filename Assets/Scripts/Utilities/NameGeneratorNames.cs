using System.Collections.Generic;

public class NameGeneratorNames {

	public static Dictionary<string, Dictionary<char, List<string>>> firstNamesByNationalityAndSex = new Dictionary<string, Dictionary<char, List<string>>> {
		{"ar", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Juan", "Carlos", "Jorge", "José", "Luis", "Roberto", "Alberto", "Eduardo", "Ricardo", "Miguel"}
			}, {
				'f', new List<string> {"María", "Ana", "Marta", "Susana", "Alicia", "Rosa", "Silvia", "Graciela", "Beatriz", "Norma"}
			}
		}}, 
		{"at", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Lukas", "Florian", "Tobias", "David", "Alexander", "Fabian", "Michael", "Julian", "Daniel", "Simon"}
			}, {
				'f', new List<string> {"Sarah", "Anna", "Julia", "Laura", "Lena", "Hannah", "Lisa", "Katharina", "Leonie", "Vanessa"}
			}
		}},
		{"au", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jack", "Joshua", "Lachlan", "Thomas", "Ethan", "Benjamin", "James", "Riley", "Liam", "Luke"}
			}, {
				'f', new List<string> {"Ella", "Jessica", "Emily", "Chloe", "Sophie", "Olivia", "Grace", "Charlotte", "Isabella", "Lily"}
			}
		}},
		{"ba", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Amar", "Tarik", "Vedad", "Adjin", "Harun", "Adin", "Ahmed", "Faris", "Benjamin", "Muhamed", "Eman", "Hamza", "Eldar", "Emir", "Armin", "Emin"}
			}, {
				'f', new List<string> {"Amina", "Lamija", "Ajna", "Sara", "Ajla", "Emina", "Lejla", "Adna", "Hana", "Nejla", "Ema", "Amila", "Nejra", "Sajra", "Ena"}
			}
		}},
		{"be", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Noah", "Thomas", "Anton", "Lucas", "Louis", "Milan", "Arthur", "Mohamed", "Maxime", "Simon"}
			}, {
				'f', new List<string> {"Emma", "Marie", "Laura", "Julie", "Sarah", "Clara", "Manon", "Léa", "Lisa", "Camille"}
			}
		}},
		{"ca", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Ethan", "Matthew", "Jacob", "Joshua", "Carter", "Liam", "Logan", "Noah", "Nathan", "Andrew"}
			}, {
				'f', new List<string> {"Emma", "Emily", "Hannah", "Olivia", "Madison", "Grace", "Hailey", "Sarah", "Rachel", "Julia"}
			}
		}},
		{"cl", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Benjamín", "Matías", "Vicente", "Martín", "Sebastián", "Diego", "Nicolás", "Juan", "José", "Cristóbal"}
			}, {
				'f', new List<string> {"Constanza", "Catalina", "Valentina", "Javiera", "Martina", "Sofía", "María", "Antonia", "Fernanda", "Francisca"}
			}
		}},
		{"cz", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jan", "Jakub", "Tomáš", "Adam", "Ondřej", "Martin", "Filip", "Lukáš", "Vojtěch", "Matěj"}
			}, {
				'f', new List<string> {"Tereza", "Eliška", "Adéla", "Natálie", "Anna", "Karolína", "Kristýna", "Aneta", "Nikola", "Kateřina"}
			}
		}},
		{"de", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Magnus", "Lucas", "Mathias", "Oliver", "Frederik", "Emil", "Mikkel", "Tobias", "Nikolaj", "Victor"}
			}, {
				'f', new List<string> {"Mathilde", "Emma", "Laura", "Sofie", "Freja", "Caroline", "Ida", "Sara", "Julie", "Anna"}
			}
		}},
		{"es", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Alejandro", "Daniel", "Pablo", "David", "Javier", "Adrián", "Álvaro", "Sergio", "Carlos", "Hugo"}
			}, {
				'f', new List<string> {"Lucía", "María", "Paula", "Laura", "Marta", "Andrea", "Alba", "Sara", "Claudia", "Ana"}
			}
		}},
		{"fi", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Veeti", "Eetu", "Aleksi", "Joona", "Elias", "Juho", "Lauri", "Arttu", "Leevi", "Matias"}
			}, {
				'f', new List<string> {"Emma", "Ella", "Siiri", "Aino", "Anni", "Sara", "Venla", "Aada", "Emilia", "Iida"}
			}
		}},
		{"fr", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Enzo", "Mathis", "Lucas", "Hugo", "Mathéo", "Nathan", "Théo", "Noah", "Mattéo", "Thomas"}
			}, {
				'f', new List<string> {"Emma", "Léa", "Manon", "Clara", "Chloé", "Inès", "Camille", "Sarah", "Océane", "Jade"}
			}
		}},
		{"gb", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jack", "Thomas", "Joshua", "Oliver", "Harry", "James", "William", "Samuel", "Daniel", "Charlie", "Benjamin", "Joseph", "Callum", "George", "Jake", "Alfie", "Luke", "Matthew", "Ethan", "Lewis"}
			}, {
				'f', new List<string> {"Olivia", "Grace", "Jessica", "Ruby", "Emily", "Sophie", "Chloe", "Lucy", "Lily", "Ellie", "Ella", "Charlotte", "Katie", "Mia", "Hannah", "Amelia", "Megan", "Amy", "Isabella", "Millie"}
			}
		}},
		{"ge", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Alexander", "Maximilian", "Leon", "Lukas", "Luca", "Paul", "Jonas", "Felix", "Tim", "David"}
			}, {
				'f', new List<string> {"Marie", "Sophie", "Maria", "Anna", "Leonie", "Lena", "Emily", "Leah", "Julia", "Laura"}
			}
		}},
		{"ie", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jack", "Matthew", "Ryan", "James", "Daniel", "Adam", "Joshua", "Callim", "Ben", "Ethan"}
			}, {
				'f', new List<string> {"Katie", "Grace", "Emma", "Sophie", "Ellie", "Lucy", "Sarah", "Hannah", "Jessica", "Erin"}
			}
		}},
		{"jp", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Shun", "Takumi", "Shō", "Ren", "Shōta", "Sōta", "Kaito", "Kenta", "Daiki", "Yū"}
			}, {
				'f', new List<string> {"Misaki", "Aoi", "Nanami", "Miu", "Riko", "Miyu", "Moe", "Mitsuki", "Yu-ka", "Rin"}
			}
		}},
		{"ke", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Isaac", "James", "George", "Ken", "Bonface", "Paul", "Martin", "Mark", "Freddie", "Anthony"}
			}, {
				'f', new List<string> {"Bushira", "Fadhill", "Amira", "Inaya", "Tosha", "Munira", "Nafula", "Haoniyao", "Nuru", "Muna"}
			}
		}},
		{"ph", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Michael", "Ronald", "Ryan", "Joseph", "Joel", "Jeffrey", "Marlon", "Richard", "Noel", "Jonathan"}
			}, {
				'f', new List<string> {"Maricel", "Michelle", "Jennifer", "Janice", "Mary", "Jocelyn", "Catherine", "Anne", "Rowena", "Grace"}
			}
		}},
		{"pl", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jan", "Andrzej", "Piotr", "Krzysztof", "Stanisław", "Tomasz", "Paweł", "Józef", "Marcin", "Marek"}
			}, {
				'f', new List<string> {"Anna", "Maria", "Katarzyna", "Małgorzata", "Agnieszka", "Krystyna", "Barbara", "Ewa", "Elżbieta", "Zofia"}
			}
		}},
		{"pt", new Dictionary<char, List<string>> {{
				'm', new List<string> {"João", "Tiago", "André", "Pedro", "Ricardo", "José", "Manuel", "Diogo", "Fábio", "Miguel"}
			}, {
				'f', new List<string> {"Maria", "Joana", "Ana", "Catarina", "Inês", "Teresa", "Isabel", "Margarida", "Carolina", "Filipa"}
			}
		}},
		{"ru", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Alexander", "Sergei", "Dmitry", "Andrei", "Alexey", "Maxin", "Evgeny", "Ivan", "Mikhail", "Artyom", "Atrem", "Daniil", "Kirill", "Andrey", "Nikita", "Timofey"}
			}, {
				'f', new List<string> {"Anastasia", "Yelena", "Olga", "Natalia", "Yekaterina", "Anna", "Tatiana", "Maria", "Irina", "Yulia", "Sofiya", "Mariya", "Arina", "Dariya", "Varvara", "Yelizaveta", "Viktoriya", "Polina"}
			}
		}},
		{"se", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Lucas", "Oscar", "William", "Elias", "Filip", "Hugo", "Viktor", "Isak", "Alexander", "Emil"}
			}, {
				'f', new List<string> {"Emma", "Maja", "Agnes", "Julia", "Alva", "Linnéa", "Wilma", "Ida", "Alice", "Elin"}
			}
		}},
		{"ua", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Maxim", "Artyom", "Nazar", "Andrei", "Daniil", "Dmitry", "Nikita", "Vladyslav", "Denys", "Kirill"}
			}, {
				'f', new List<string> {"Alexandra", "Mariya", "Ekaterina", "Anna", "Polina", "Kristina", "Daryna", "Viktoriya", "Elizaveta", "Alina"}
			}
		}},
		{"us", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Jacob", "Michael", "Joshua", "Matthew", "Andrew", "Christopher", "Joseph", "Nicholas", "Daniel", "William", "Ethan", "Anthony", "Ryan", "Tyler", "David", "John", "Alexander", "James", "Zachary", "Brandon"}
			}, {
				'f', new List<string> {"Emily", "Madison", "Hannah", "Emma", "Ashley", "Alexis", "Samantha", "Sarah", "Abigail", "Olivia", "Elizabeth", "Alyssa", "Jessica", "Grace", "Lauren", "Taylor", "Kayla", "Brianna", "Isabella", "Anna"}
			}
		}},
		{"za", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Emmanuel", "Daniel", "Richard", "Patrick", "Emershan", "Prince", "Stan", "Matthew", "Junior", "Jordan", "Kevin", "Zubair", "Christopher", "Nickens", "James"}
			}, {
				'f', new List<string> {"Kayla", "Megan", "Hannah", "Nicole", "Caitlin", "Kelly", "Jessica", "Emma", "Aisha", "Nadine", "Raeesa", "Bianca", "Kimberly", "Mariam", "Melissa"}
			}
		}}
	};

	public static Dictionary<string, Dictionary<char, List<string>>> lastNamesByNationalityAndSex = new Dictionary<string, Dictionary<char, List<string>>> {
		{"ar", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Fernandez", "Rodriguez", "Gonzales", "Garcia", "Lopez", "Martinez", "Perez", "Alvarez", "Gomez", "Sanchez", "Diaz", "Vazquez", "Castro", "Romero", "Suarez", "Blanco", "Ruiz", "Alionso", "Torres", "Dominguez"}
			}
		}}, 
		{"at", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Gruber", "Huber", "Bauer", "Wagner", "Müller", "Pichler", "Steiner", "Moser", "Mayer", "Hofer", "Leitner", "Berger", "Fuchs", "Eder", "Fischer", "Schmid", "Winkler", "Weber", "Schwarz", "Maier", "Schneider", "Reiter", "Mayr", "Schmidt", "Wimmer"}
			}
		}}, 
		{"au", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Morton", "White", "Martin", "Anderson", "Thompson", "Nguyen", "Thomas", "Walker", "Harris", "Lee", "Ryan", "Robinson", "Kelly", "King"}
			}
		}}, 
		{"ba", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Hodžić", "Hadžić", "Čengić", "Delić", "Demirović", "Kovačević", "Tahirović", "Ferhatović", "Muratović", "Ibrahimović", "Hasanović", "Mehmedović", "Salihović", "Terzić", "Ademović", "Adilović", "Delemovic"}
			}
		}}, 
		{"be", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Peeters", "Janssens", "Maes", "Jacobs", "Mertens", "Wilems", "Claes", "Goossens", "Wouters", "De Smet", "Dubois", "Lambert", "Dupont", "Margin", "Simon"}
			}
		}}, 
		{"ca", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Li", "Smith", "Lam", "Martin", "Brown", "Roy", "Tremblay", "Lee", "Gagnon", "Wilson", "Clark", "Johnson", "White", "Williams", "Cote", "Taylor", "Campbell", "Anderson", "Chan", "Jones"}
			}
		}}, 
		{"ch", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Baumgartner", "Bühler", "Gerber", "Meier", "Meyer", "Schneider", "Steiner", "Studer", "Zimmermann", "Fischer", "Kunz", "Kaufmann", "Moser", "Huber", "Suter", "Weber", "Brunner", "Frei", "Graf", "Keller", "Müller", "Roth", "Schmid", "Widmer", "Wyss"}
			}
		}}, 
		{"cl", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Gonzalez", "Munoz", "Rojas", "Diaz", "Perez", "Soto", "Contreras", "Silva", "Martinez", "Sepulveda", "Morales", "Rodriguez", "Lopez", "Fuentes", "Hernandez", "Torres", "Araya", "Flores", "Espinoza", "Valenzuela"}
			}
		}}, 
		{"cz", new Dictionary<char, List<string>> {{
				'm', new List<string> {"Novák", "Svoboda", "Novotný", "Dvořák", "Černý", "Procházka", "Kučera", "Veselý", "Horák", "Němec"}
			}, {
				'f', new List<string> {"Nováková", "Svobodová", "Novotná", "Dvořáková", "Černá", "Procházková", "Kučerová", "Veselá", "Horáková", "Němcová"}
			}
		}},
		{"de", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Jensen", "Nielsen", "Hansen", "Pedersen", "Andersen", "Christensen", "Larsen", "Sørensen", "Rasmussen", "Jørgensen", "Petersen", "Madsen", "Kristensen", "Olsen", "Thomsen", "Christiansen", "Poulsen", "Johansen", "Møller", "Knudsen"}
			}
		}}, 
		{"es", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Garcia", "Fernández", "González", "Rodriquez", "López", "Martinez", "Sánches", "Pérez", "Martin", "Gómez", "Ruiz", "Hernández", "Jiménez", "Diaz", "Álvarez", "Moreno", "Muñoz", "Alonso", "Gutiérrez", "Romero", "Navarro", "Torres", "Domínguez", "Gil", "Vázquez"}
			}
		}}, 
		{"fi", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Korhonen", "Virtanen", "Mäkinen", "Nieminen", "Mäkelä", "Hämäläinen", "Laine", "Heikkinen", "Koskinen", "Järvinen", "Lehtonen", "Lehtinen", "Saarinen", "Salminen", "Heinonen", "Niemi", "Heikkilä", "Salonen", "Kinnunen", "Turunen"}
			}
		}}, 
		{"fr", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent", "Fournier", "Morel", "Girard", "André", "Lefévre", "Mercier", "Dupont", "Lambert", "Bonnet", "François", "Martinez"}
			}
		}}, 
		{"gb", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Walker", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Davies", "Robinson", "Wright", "Thompson", "Evans", "Smith", "White", "Roberts", "Green", "Hall", "Wood", "Jackson", "Clarke", "Wilson", "Campbell", "Kelly", "Owen"}
			}
		}}, 
		{"ge", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Müller", "Schmidt", "Fischer", "Meyer", "Weber", "Schulz", "Wagner", "Becker", "Hoffmann"}
			}
		}}, 
		{"ie", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Murphy", "Kelly", "O'Kelly", "Sullivan", "O'Sullivan", "Walsh", "Smith", "O'Brien", "Byrne", "O'Byrne", "Ryan", "O'Ryan", "O'Connor", "O'Neill", "Reilly", "O'Reilly", "Doyle", "McCarthy", "Gallagher", "O'Gallagher", "Doherty", "O'Doherty", "Kennedy", "Lynch", "Murray", "Quinn", "O'Quinn", "Moore", "O'Moore"}
			}
		}}, 
		{"jp", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Satou", "Suzuki", "Takahashi", "Tanaka", "Watanabe", "Itou", "Nakamura", "Yamamoto", "Kobayashi", "Saitou"}
			}
		}}, 
		{"ke", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Ba", "Bah", "Ballo", "Chahine", "Cisse", "Congo", "Contee", "Conteh", "Dia", "Diallo", "Diop", "Fall", "Fofana", "Gueye", "Jalloh", "Keita", "Kone", "Maalouf", "Mensah", "Ndiaye", "Nwosu", "Okafor", "Okeke", "Okoro", "Osei", "Owusu", "Sall", "Sane", "Sarr", "Sesay", "Sow", "Sy", "Sylla", "Toure", "Traore", "Turay", "Yeboah"}
			}
		}}, 
		{"ph", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Santos", "Reyes", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza", "Torres", "Tomás", "Andrada", "Castillo", "Flores", "Villanueva", "Ramos", "Castro", "Rivera", "Aquino", "Navarro", "Salazar", "Mercado"}
			}
		}}, 
		{"pl", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Nowak", "Kowalski", "Wiśniewski", "Wójcik", "Kowalczyk", "Kamiński", "Lewandowski", "Zieliński", "Szymański", "Woźniak", "Dąbrowski", "Kozłowski", "Jankowski", "Mazur", "Kwiatkowski", "Wojciechowski", "Krawczyk", "Kaczmarek", "Piotrowski", "Grabowski"}
			}
		}}, 
		{"pt", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Silva", "Santos", "Ferreira", "Pereira", "Oliveira", "Costa", "Rodrigues", "Martins", "Jesus", "Sousa", "Fernandes", "Gonçalves", "Gomes", "Lopes", "Marques", "Alves", "Almeida", "Riveiro", "Pinto", "Carvalho"}
			}
		}}, 
		{"ru", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Smirnov", "Ivanov", "Kuznetsov", "Popov", "Sokolov", "Lebedev", "Kozlov", "Novikov", "Morozov", "Petrov", "Volkov", "Solovyov", "Vasilyev", "Zaytsev", "Pavlov", "Semyonov", "Golubev", "Vinogradov", "Bogdanov", "Vorobyov"}
			}
		}}, 
		{"se", new Dictionary<char, List<string>> {{
					'*', new List<string> {"Andersson", "Johansson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson", "Gustafsson", "Pettersson", "Jonsson", "Jansson", "Hansson", "Bengtsson", "Jönsson", "Lindberg", "Jakobsson", "Magnusson", "Olofsson"}
			}
		}}, 
		{"us", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson", "Anderson", "Taylor", "Thomas", "Moore", "Martin", "Jackson", "Thompson", "White", "Lee", "Harris", "Clark", "Lewis", "Robinson", "Walker", "Hall", "Young", "Allen", "Wright", "King", "Scott", "Green", "Baker", "Adams", "Nelson", "Hill", "Campvell", "Mitchell", "Roberts", "Carter", "Philips", "Evans", "Turner", "Parker"}
			}
		}}, 
		{"ua", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Melnyk", "Shevchenko", "Boyko", "Kovalenko", "Bondarenko", "Tkachenko", "Kovalchuk", "Oliynyk", "Shevchuk", "Koval", "Polishchuk", "Bondar", "Tkachuk", "Moroz", "Marchenko", "Lysenko", "Rudenko", "Savchenko", "Petrenko"}
			}
		}}, 
		{"za", new Dictionary<char, List<string>> {{
				'*', new List<string> {"Naidoo", "Govender", "Botha", "Pillay", "Smith", "Pretorius", "Fourie", "Venter", "Nel", "Moodley", "Coetzee", "Jacobs", "Kruger", "Smit", "Singh", "Erasmus", "Meyer", "Chetty", "Joubert", "Williams", "Steyn", "Swanepoel", "Viljoen", "Potgieter", "Swart"}
			}
		}}		
	};
}
