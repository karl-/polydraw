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

			case Draw.DrawStyle.PointMaxVertex:
				script.maxVertices = EditorGUILayout.IntField("Max Allowed Vertices", script.maxVertices);
				break;

			case Draw.DrawStyle.PointClosingDistance:
				script.closingDistance = EditorGUILayout.FloatField("Closing Distance", script.closingDistance);
				break;
		}

		script.inputCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Input Camera", "The camera that listens for mouse input.  Must be orthographic."), script.inputCamera, typeof(Camera), true);

		GUILayout.Space(5);

		GUILayout.Label("Drawing Settings", EditorStyles.boldLabel);

		script.showPointMarkers = EditorGUILayout.BeginToggleGroup("Show Vertex Markers", script.showPointMarkers);
			script.pointMarker = (GameObject)EditorGUILayout.ObjectField("Point Marker", script.pointMarker, typeof(GameObject), true);
		EditorGUILayout.EndToggleGroup();

		script.drawMeshInProgress = EditorGUILayout.Toggle("Draw Preview Mesh", script.drawMeshInProgress);

		script.lineRenderer = (LineRenderer)EditorGUILayout.ObjectField(new GUIContent("Trail Renderer", "If left null, a default trail renderer will be applied automatically."), script.lineRenderer, typeof(LineRenderer), true);
		if(script.lineRenderer == null)
			script.lineWidth = EditorGUILayout.FloatField("Trail Renderer Width", script.lineWidth);

		script.useDistanceCheck = EditorGUILayout.BeginToggleGroup(new GUIContent("Use Distance Check", "If final user set point is greater than `x` distance from origin point, polygon will not be drawn"), script.useDistanceCheck);
			script.maxDistance = EditorGUILayout.FloatField("Max Distance from Origin", script.maxDistance);
		EditorGUILayout.EndToggleGroup();

		GUILayout.Space(5);

		GUILayout.Label("Mesh Settings", EditorStyles.boldLabel);
		
		script.meshName = EditorGUILayout.TextField("Mesh Name", script.meshName);

		script.material = (Material)EditorGUILayout.ObjectField("Material", script.material, typeof(Material), false);

		script.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup("Mesh Anchor", script.anchor);

		script.zPosition = EditorGUILayout.FloatField("Z Origin", script.zPosition);

		script.faceOffset = EditorGUILayout.FloatField(new GUIContent("Z Offset", "Allows for custom offsets.  See docs for details."), script.faceOffset);

		script.generateSide = EditorGUILayout.BeginToggleGroup("Generate Sides", script.generateSide);

			script.sideLength = EditorGUILayout.FloatField("Side Length", script.sideLength);
			script.sideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", script.sideMaterial, typeof(Material), false);

		EditorGUILayout.EndToggleGroup();
		
		script.uvScale = EditorGUILayout.Vector2Field("UV Scale", script.uvScale);

		script.useTag = EditorGUILayout.BeginToggleGroup(new GUIContent("Apply Tag", "Tag must exist prior to assignment."), script.useTag);
			script.tagVal = EditorGUILayout.TextField("Tag", script.tagVal);
		EditorGUILayout.EndToggleGroup();
		
		script.maxAllowedObjects = EditorGUILayout.IntField("Max Meshes Allowed", script.maxAllowedObjects);

		GUILayout.Label("Collision", EditorStyles.boldLabel);

		script.colliderStyle = (Draw.ColliderStyle)EditorGUILayout.EnumPopup(new GUIContent("Collison", "If set to mesh, a Mesh Collider will be applied.  If set to Box, a series of thin box colliders will be generated bordering the edges, allowing for concave interactions.  None means no collider will be applied."), script.colliderStyle);

		if(script.colliderStyle == Draw.ColliderStyle.BoxCollider)
		{
			if(!script.manualColliderDepth)
				script.colDepth = script.sideLength;

			script.manualColliderDepth = EditorGUILayout.BeginToggleGroup(new GUIContent("Manual Collider Depth", "If false, the side length will be used as the depth value."), script.manualColliderDepth);

				script.colDepth = EditorGUILayout.FloatField("Collider Depth", script.colDepth);

			EditorGUILayout.EndToggleGroup();
		}

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