using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PolyDraw.Demo
{
[CustomEditor(typeof(Button))]
public class ButtonEditor : Editor
{
	Button b;

	// [MenuItem("Window/Refersh Buttons")]
	public static void RefreshButtons()
	{
		foreach(Button butt in TransformExtensions.GetComponents<Button>(Selection.transforms))
		{
			if(butt.primitive == Button.Primitive.Plane)
				butt.CreateSprite(butt.normal);
		}
	}

	[MenuItem("GameObject/Create Other/Button")]
	public static void NewButton()
	{
		GameObject go = new GameObject();
		go.transform.position = new Vector3(0f, 0f, 5f);
		go.layer = 9;
		go.transform.parent = ((Menu)FindObjectsOfType(typeof(Menu))[0]).transform;
		go.AddComponent<Button>();
		go.name = "New Button !!!";
		Selection.activeTransform = go.transform;
	}

	public override void OnInspectorGUI()
	{
		b = (Button)target;

		GUI.changed = false;
			b.normal = (Texture2D)EditorGUILayout.ObjectField("Normal", b.normal, typeof(Texture2D), false);
			b.primitive = (Button.Primitive)EditorGUILayout.EnumPopup("Primitive", b.primitive);
		if(GUI.changed) {
			if(b.primitive == Button.Primitive.Plane)
				b.CreateSprite(b.normal);
		}
		
		b.hover = (Texture2D)EditorGUILayout.ObjectField("Hover", b.hover, typeof(Texture2D), false);
		b.down = (Texture2D)EditorGUILayout.ObjectField("Down", b.down, typeof(Texture2D), false);

		b.arg = EditorGUILayout.ObjectField("Arg", b.arg, typeof(Object), false);

		GUI.backgroundColor = Color.green;
			if(GUILayout.Button("Refresh Mesh"))
			{
				if(b.primitive == Button.Primitive.Plane)
					b.CreateSprite(b.normal);
			}
		GUI.backgroundColor = Color.white;

		GUILayout.Label("Tweening", EditorStyles.boldLabel);

		if(GUILayout.Button("Go to Destination Frame"))
			b.transform.position = b.destination;

		if(GUILayout.Button("Go to Start Frame"))
			b.transform.position = b.start;

		GUILayout.Space(5);


		if(GUILayout.Button("Set Destination Frame"))
			b.destination = b.transform.position;

		if(GUILayout.Button("Set Start Frame"))
			b.start = b.transform.position;
	}
}

public static class TransformExtensions
{
	public static T[] GetComponents<T>(Transform[] t_arr) where T : Component
	{
		List<T> c = new List<T>();
		foreach(Transform t in t_arr)
		{
			if(t.GetComponent<T>())
				c.Add(t.GetComponent<T>());
		}
		return (T[])c.ToArray();
	}
}
}