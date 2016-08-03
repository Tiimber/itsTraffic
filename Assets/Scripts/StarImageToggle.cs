using UnityEngine;
using System.Collections;

public class StarImageToggle : MonoBehaviour {

	private bool active = false;

	public void setActiveState(bool active = true) {
		this.active = active;
		updateImage ();
	}

	private void updateImage() {
		GameObject activeStar = transform.FindChild ("active").gameObject;
		GameObject inactiveStar = transform.FindChild ("inactive").gameObject;

		activeStar.SetActive (active);
		inactiveStar.SetActive (!active);
	}

}
