using System.Collections;
using System.Xml;
using System.Collections.Generic;
using System;

public class Setup {

	public List<PersonSetup> people = new List<PersonSetup>();
	public List<VehicleSetup> vehicles = new List<VehicleSetup>();

	public Setup(XmlNode setupNode) {
		XmlNodeList personNodes = setupNode.SelectNodes("people/person");
		foreach (XmlNode personNode in personNodes) {
			people.Add (createPerson (personNode));
		}

		XmlNodeList vehicleNodes = setupNode.SelectNodes("vehicles/vehicle");
		foreach (XmlNode vehicleNode in vehicleNodes) {
			vehicles.Add (createVehicle (vehicleNode));
		}
	}

	private PersonSetup createPerson (XmlNode personNode) {
		XmlAttributeCollection personAttributes = personNode.Attributes;
		long id = Misc.xmlLong(personAttributes.GetNamedItem ("id"));
		string name = Misc.xmlString(personAttributes.GetNamedItem ("name"));
		float time = Misc.xmlFloat(personAttributes.GetNamedItem ("time"));
		bool refOnly = Misc.xmlBool(personAttributes.GetNamedItem ("refOnly"));
		string dob = Misc.xmlString(personAttributes.GetNamedItem ("dob"));
		float money = Misc.xmlFloat(personAttributes.GetNamedItem ("money"));

		return new PersonSetup (id, name, time, refOnly, dob, money);
	}

	private VehicleSetup createVehicle (XmlNode vehicleNode) {
		XmlAttributeCollection vehicleAttributes = vehicleNode.Attributes;
		long id = Misc.xmlLong(vehicleAttributes.GetNamedItem ("id"));
		string name = Misc.xmlString(vehicleAttributes.GetNamedItem ("name"));
		float time = Misc.xmlFloat(vehicleAttributes.GetNamedItem ("time"));

		long startPos = Misc.xmlLong(vehicleAttributes.GetNamedItem ("startPos"));
		long endPos = Misc.xmlLong(vehicleAttributes.GetNamedItem ("endPos"));
		string type = Misc.xmlString(vehicleAttributes.GetNamedItem ("type"));
		int year = Misc.xmlInt(vehicleAttributes.GetNamedItem ("year"));
		float distance = Misc.xmlFloat(vehicleAttributes.GetNamedItem ("distance"));
		float condition = Misc.xmlFloat(vehicleAttributes.GetNamedItem ("condition"));
		long driverId = Misc.xmlLong(vehicleAttributes.GetNamedItem ("driverId"));
		string passengerIdsStr = Misc.xmlString(vehicleAttributes.GetNamedItem ("passengerIds"));
		List<long> passengerIds = Misc.parseLongs (passengerIdsStr);

		return new VehicleSetup (id, name, time, startPos, endPos, type, year, distance, condition, driverId, passengerIds);
	}

	public class InstanceSetup {
		public long id;
		public string name;
		public float time;

		public InstanceSetup(long id, string name, float time) {
			this.id = id;
			this.name = name;
			this.time = time;
		}
	}

	public class PersonSetup : InstanceSetup {
		public bool refOnly;
		public string dob;
		public float money;

		public PersonSetup(long id, string name, float time, bool refOnly, string dob, float money) : base(id, name, time) {
			this.refOnly = refOnly;
			this.dob = dob;
			this.money = money;
		}
	}

	public class VehicleSetup : InstanceSetup {
		public long startPos;
		public long endPos;
		public string type;
		public int year;
		public float distance;
		public float condition;
		public long driverId;
		public List<long> passengerIds;

		public VehicleSetup(long id, string name, float time, long startPos, long endPos, string type, int year, float distance, float condition, long driverId, List<long> passengerIds) : base(id, name, time) {
			this.startPos = startPos;
			this.endPos = endPos;
			this.type = type;
			this.year = year;
			this.distance = distance;
			this.condition = condition;
			this.driverId = driverId;
			this.passengerIds = passengerIds;
		}
	}
}
