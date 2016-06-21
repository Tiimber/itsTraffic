using UnityEngine;
using System.Collections;

public class CircleTouch {
	private Vector2 center;
	private float radius;

	public CircleTouch(Vector2 center, float radius) {
		this.center = center;
		this.radius = radius;
	}

	public bool isInside(Vector2 pos) {
		return (center - pos).magnitude <= radius;
	}

	public bool isCloser (Vector2 pos, CircleTouch other) {
		return other == null || (this.center - pos).magnitude < (other.center - pos).magnitude;
	}
}
