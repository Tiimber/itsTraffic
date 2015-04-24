using UnityEngine;
using System.Collections;

public class WayReference : MonoBehaviour {
	public Way way;

	// TODO - Temporary
	public Color OriginalColor { set; get; }
	public WayReference () : base() {
		OriginalColor = Color.magenta;
	}
}
