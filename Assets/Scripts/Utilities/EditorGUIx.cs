using UnityEngine;

public static class EditorGUIx {
	private static Texture2D s_lineTex;

	static EditorGUIx() {
		s_lineTex = new Texture2D(1, 3, TextureFormat.ARGB32, true);
		s_lineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
		s_lineTex.SetPixel(0, 1, Color.white);
		s_lineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
		s_lineTex.Apply();
	}

	public static void DrawLine(Vector2 p_pointA, Vector2 p_pointB, float p_width) {
		Matrix4x4 _saveMatrix = GUI.matrix;
		Color _saveColor = GUI.color;

		Vector2 _delta = p_pointB - p_pointA;
		GUIUtility.ScaleAroundPivot(new Vector2(_delta.magnitude, p_width), Vector2.zero);
		GUIUtility.RotateAroundPivot(Vector2.Angle(_delta, Vector2.right) * Mathf.Sign(_delta.y), Vector2.zero);
		GUI.matrix = Matrix4x4.TRS(p_pointA, Quaternion.identity, Vector3.one) * GUI.matrix;

		GUI.DrawTexture(new Rect(Vector2.zero, Vector2.one), s_lineTex);

		GUI.matrix = _saveMatrix;
		GUI.color = _saveColor;
	}
}