/// Parabox LLC
/// Support Email - karl@paraboxstudios.com
/// paraboxstudios.com
///	## Version 2.0
///
///	Provides a neatly organized user interface for Draw.cs

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(Draw))]

class DrawEditor : Editor {

	bool showDrawSettings = false;
	bool showMeshSettings = false;
	bool showGameObjectSettings = false;
	bool showCollisionSettings = false;

	[MenuItem("Window/PolyDraw/Export Selected")]
	public static void ExportSelected()
	{
		string path = EditorUtility.SaveFilePanelInProject("Save selected meshes as OBJ",
			"PolyDrawMesh.obj",
			"obj",
			"Enter saved mesh name");
		Debug.Log(path);
		Draw.ExportOBJ(path, Selection.transforms);
		AssetDatabase.Refresh();
	}

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

		showDrawSettings = EditorGUILayout.Foldout(showDrawSettings, "Drawing Settings");
		if(showDrawSettings)
		{
			script.showPointMarkers = EditorGUILayout.BeginToggleGroup("Show Vertex Markers", script.showPointMarkers);
				script.pointMarker = (GameObject)EditorGUILayout.ObjectField("Point Marker", script.pointMarker, typeof(GameObject), true);
			EditorGUILayout.EndToggleGroup();

			script.drawMeshInProgress = EditorGUILayout.Toggle("Draw Preview Mesh", script.drawMeshInProgress);

			script.drawLineRenderer = EditorGUILayout.BeginToggleGroup("Draw Line Renderer", script.drawLineRenderer);
				script.lineRenderer = (LineRenderer)EditorGUILayout.ObjectField(new GUIContent("Trail Renderer", "If left null, a default trail renderer will be applied automatically."), script.lineRenderer, typeof(LineRenderer), true);
				if(script.lineRenderer == null)
					script.lineWidth = EditorGUILayout.FloatField("Trail Renderer Width", script.lineWidth);
			EditorGUILayout.EndToggleGroup();

			script.useDistanceCheck = EditorGUILayout.BeginToggleGroup(new GUIContent("Use Distance Check", "If final user set point is greater than `x` distance from origin point, polygon will not be drawn"), script.useDistanceCheck);
				script.maxDistance = EditorGUILayout.FloatField("Max Distance from Origin", script.maxDistance);
			EditorGUILayout.EndToggleGroup();
		}

		showMeshSettings = EditorGUILayout.Foldout(showMeshSettings, "Mesh Settings");
		if(showMeshSettings)
		{
			script.meshName = EditorGUILayout.TextField("Mesh Name", script.meshName);

			script.material = (Material)EditorGUILayout.ObjectField("Material", script.material, typeof(Material), false);

			script.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup("Mesh Anchor", script.anchor);

			script.zPosition = EditorGUILayout.FloatField("Z Origin", script.zPosition);

			script.faceOffset = EditorGUILayout.FloatField(new GUIContent("Z Offset", "Allows for custom offsets.  See docs for details."), script.faceOffset);

			script.generateSide = EditorGUILayout.BeginToggleGroup("Generate Sides", script.generateSide);

				script.sideLength = EditorGUILayout.FloatField("Side Length", script.sideLength);
				script.sideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", script.sideMaterial, typeof(Material), false);

			EditorGUILayout.EndToggleGroup();

			script.drawEdgePlanes = EditorGUILayout.BeginToggleGroup("Draw Edge Planes", script.drawEdgePlanes);

				script.edgeMaterial = (Material)EditorGUILayout.ObjectField("Edge Material", script.edgeMaterial, typeof(Material), false);
				script.edgeLengthModifier = EditorGUILayout.FloatField(new GUIContent("Edge Length Modifier", "Multiply the length of the plane by this amount.  1 = no change, 1.3 = 33% longer."), script.edgeLengthModifier);
				script.edgeHeight  = EditorGUILayout.FloatField(new GUIContent("Edge Height", "Absolute value; how tall to make edge planes."), script.edgeHeight);
				script.minLengthToDraw  = EditorGUILayout.FloatField(new GUIContent("Minimum Edge Length", "Edges with a length less than this amount will not be drawn."), script.minLengthToDraw);
				script.edgeOffset  = EditorGUILayout.FloatField(new GUIContent("Edge Z Offset", "Value to scoot mesh + or - from Z Position."), script.edgeOffset);
				script.maxAngle = EditorGUILayout.FloatField(new GUIContent("Maximum Slope", "Edge Z rotation must be less than this amount to be drawn.  Set to 180 to draw a complete loop, or leave at 45 to only draw up facing planes."), script.maxAngle);
				script.areaRelativeHeight = EditorGUILayout.Toggle(new GUIContent("Area Relative Height", "If true, the height of each plane will be modified by how large the finalized mesh is."), script.areaRelativeHeight);
				if(script.areaRelativeHeight)
				{
					script.minEdgeHeight = EditorGUILayout.FloatField("Minimum Edge Height", script.minEdgeHeight);
					script.maxEdgeHeight = EditorGUILayout.FloatField("Maximum Edge Height", script.maxEdgeHeight);
				}

			EditorGUILayout.EndToggleGroup();
			
			script.uvScale = EditorGUILayout.Vector2Field("UV Scale", script.uvScale);
		}

		showGameObjectSettings = EditorGUILayout.Foldout(showGameObjectSettings, "GameObject Settings");
		if(showGameObjectSettings)
		{
			script.useTag = EditorGUILayout.BeginToggleGroup(new GUIContent("Apply Tag", "Tag must exist prior to assignment."), script.useTag);
				script.tagVal = EditorGUILayout.TextField("Tag", script.tagVal);
			EditorGUILayout.EndToggleGroup();
			
			script.maxAllowedObjects = EditorGUILayout.IntField("Max Drawn Objects Allowed", script.maxAllowedObjects);
		}

		showCollisionSettings = EditorGUILayout.Foldout(showCollisionSettings, "Physics Settings");
		if(showCollisionSettings)
		{
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
		}

		if(GUI.changed)
			EditorUtility.SetDirty(script);
	}
}