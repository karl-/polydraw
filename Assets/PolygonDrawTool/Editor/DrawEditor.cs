using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Draw))]

public class DrawEditor : Editor {

	override public void OnInspectorGUI() {
		Draw script = (Draw)target;

		GUILayout.Space(5);

		GUILayout.Label("Draw Method", EditorStyles.boldLabel);

		script.drawStyle = (Draw.DrawStyle)EditorGUILayout.EnumPopup("Draw Style", script.drawStyle);

		// Style Specific Options	
		switch(script.drawStyle) {
			case Draw.DrawStyle.Continuous:
				script.samplingRate = EditorGUILayout.FloatField("Sample Rate", script.samplingRate);
				break;

			case Draw.DrawStyle.Continuous_ClosingDistance:
				script.samplingRate = EditorGUILayout.FloatField("Sample Rate", script.samplingRate);
				break;

			case Draw.DrawStyle.Point_MaxVertice:
				script.maxVertices = EditorGUILayout.IntField("Max Allowed Vertices", script.maxVertices);
				break;

			case Draw.DrawStyle.Point_ClosingDistance:
				break;
		}

		GUILayout.Space(5);

		GUILayout.Label("Drawing Settings", EditorStyles.boldLabel);

		script.drawMeshInProgress = EditorGUILayout.Toggle("Draw Preview Mesh", script.drawMeshInProgress);

		script.lineWidth = EditorGUILayout.FloatField("Trail Renderer Width", script.lineWidth);

		script.useDistanceCheck = EditorGUILayout.BeginToggleGroup(new GUIContent("Use Distance Check", "If final user set point is greater than `x` distance from origin point, polygon will not be drawn"), script.useDistanceCheck);
			script.closingDistance = EditorGUILayout.FloatField("Max Distance from Origin", script.closingDistance);
		EditorGUILayout.EndToggleGroup();

		GUILayout.Space(5);

		GUILayout.Label("Mesh Settings", EditorStyles.boldLabel);
	
		script.material = (Material)EditorGUILayout.ObjectField("Material", script.material, typeof(Material), true);
		
		script.uvScale = EditorGUILayout.Vector2Field("UV Scale", script.uvScale);

		script.useTag = EditorGUILayout.BeginToggleGroup("Apply Tag", script.useTag);
			script.tagVal = EditorGUILayout.TextField("Tag", script.tagVal);
		EditorGUILayout.EndToggleGroup();
		
		script.maxAllowedObjects = EditorGUILayout.IntField("Max Meshes Allowed", script.maxAllowedObjects);

		script.generateCollider = EditorGUILayout.BeginToggleGroup("Generate Collider", script.generateCollider);
			script.forceConvex = EditorGUILayout.Toggle("Force Convex", script.forceConvex);
			script.useGravity = EditorGUILayout.Toggle("Use Gravity", script.useGravity);
			script.isKinematic = EditorGUILayout.Toggle("Is Kinematic", script.isKinematic);
			script.applyRigidbody = EditorGUILayout.Toggle("Apply Rigidbody", script.applyRigidbody);

			script.areaRelativeMass = EditorGUILayout.Toggle("Area Relative Mass", script.areaRelativeMass);
			if(script.areaRelativeMass)
				script.massModifier = EditorGUILayout.FloatField("Mass Modifier", script.massModifier);
			else
				script.mass = EditorGUILayout.FloatField("Mass", script.mass);
	
			EditorGUILayout.EndToggleGroup();

		if(GUI.changed)
			EditorUtility.SetDirty(script);
	}
}

	/// <summary>
	/// User Settings
	/// </summary>
	/*
	public GameObject pointMarker;
	
	///Final Mesh Settings
	public string tag = "drawnMesh";
	public float zPosition = 0;
	public Vector2 uvScale = new Vector2(1f, 1f);
	*/