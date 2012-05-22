///	## Version 1.3

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

			case Draw.DrawStyle.ContinuousClosingDistance:
				script.samplingRate = EditorGUILayout.FloatField("Sample Rate", script.samplingRate);
				script.closingDistance = EditorGUILayout.FloatField("Closing Distance", script.closingDistance);
				break;

			case Draw.DrawStyle.PointMaxVertice:
				script.maxVertices = EditorGUILayout.IntField("Max Allowed Vertices", script.maxVertices);
				break;

			case Draw.DrawStyle.PointClosingDistance:
				script.closingDistance = EditorGUILayout.FloatField("Closing Distance", script.closingDistance);
				break;
		}

		GUILayout.Space(5);

		GUILayout.Label("Drawing Settings", EditorStyles.boldLabel);

		script.showPointMarkers = EditorGUILayout.BeginToggleGroup("Show Vertice Markers", script.showPointMarkers);
			script.pointMarker = (GameObject)EditorGUILayout.ObjectField("Point Marker", script.pointMarker, typeof(GameObject), true);
		EditorGUILayout.EndToggleGroup();

		script.drawMeshInProgress = EditorGUILayout.Toggle("Draw Preview Mesh", script.drawMeshInProgress);

		script.lineWidth = EditorGUILayout.FloatField("Trail Renderer Width", script.lineWidth);

		script.useDistanceCheck = EditorGUILayout.BeginToggleGroup(new GUIContent("Use Distance Check", "If final user set point is greater than `x` distance from origin point, polygon will not be drawn"), script.useDistanceCheck);
			script.maxDistance = EditorGUILayout.FloatField("Max Distance from Origin", script.maxDistance);
		EditorGUILayout.EndToggleGroup();

		GUILayout.Space(5);

		GUILayout.Label("Mesh Settings", EditorStyles.boldLabel);
		
		script.meshName = EditorGUILayout.TextField("Mesh Name", script.meshName);

		script.generateSide = EditorGUILayout.Toggle("Generate Sides", script.generateSide);

		script.material = (Material)EditorGUILayout.ObjectField("Material", script.material, typeof(Material), true);
		
		script.uvScale = EditorGUILayout.Vector2Field("UV Scale", script.uvScale);

		script.useTag = EditorGUILayout.BeginToggleGroup("Apply Tag", script.useTag);
			script.tagVal = EditorGUILayout.TextField("Tag", script.tagVal);
		EditorGUILayout.EndToggleGroup();
		
		script.maxAllowedObjects = EditorGUILayout.IntField("Max Meshes Allowed", script.maxAllowedObjects);

		script.colliderStyle = (Draw.ColliderStyle)EditorGUILayout.EnumPopup("Collison", script.colliderStyle);

		switch(script.colliderStyle) {
			case Draw.ColliderStyle.BoxCollider:
				break;
			case Draw.ColliderStyle.MeshCollider:
				script.forceConvex = EditorGUILayout.Toggle("Force Convex", script.forceConvex);
				break;
			case Draw.ColliderStyle.None:
				break;
		}

		script.applyRigidbody = EditorGUILayout.BeginToggleGroup("Apply Rigidbody", script.applyRigidbody);

			script.useGravity = EditorGUILayout.Toggle("Use Gravity", script.useGravity);

			script.isKinematic = EditorGUILayout.Toggle("Is Kinematic", script.isKinematic);

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