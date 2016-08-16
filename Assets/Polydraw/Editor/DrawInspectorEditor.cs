/// Parabox LLC
/// Support Email - karl@paraboxstudios.com
/// paraboxstudios.com
///	## Version 2.0
///
///	Provides a neatly organized user interface for Draw.cs

using UnityEngine;
using UnityEditor;
using System.Collections;
using Polydraw;

[CustomEditor(typeof(Draw))]
class DrawInspectorEditor : Editor {

	bool showDrawSettings = false;
	bool showMeshSettings = false;
	bool showGameObjectSettings = false;
	bool showCollisionSettings = false;

	[MenuItem("Window/Polydraw/Open Documentation", false, 30)]
	public static void OpenURL()
	{
	    Application.OpenURL ("http://www.paraboxstudios.com/polydraw/docs/index.html");
	}

	[MenuItem("Window/Polydraw/Export Selected", false, 31)]
	public static void ExportSelected()
	{
		string path = EditorUtility.SaveFilePanelInProject("Save selected meshes as OBJ",
			"PolyDrawMesh.obj",
			"obj",
			"Enter saved mesh name");
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

		script.drawSettings.axis = (Polydraw.Axis)EditorGUILayout.EnumPopup(script.drawSettings.axis);

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

			script.drawSettings.requireMinimumArea = EditorGUILayout.BeginToggleGroup(new GUIContent("Require Minimum Area to Draw Object", "If true, the drawn polygon must have an area greater than this value in order to be generated.  Perfect for use with Drawing Style Continuous."), script.drawSettings.requireMinimumArea);
				script.drawSettings.minimumAreaToDraw = EditorGUILayout.FloatField("Minimum Area", script.drawSettings.minimumAreaToDraw);
			EditorGUILayout.EndToggleGroup();

			script.useDistanceCheck = EditorGUILayout.BeginToggleGroup(new GUIContent("Use Distance Check", "If final user set point is greater than `x` distance from origin point, polygon will not be drawn"), script.useDistanceCheck);
				script.maxDistance = EditorGUILayout.FloatField("Max Distance from Origin", script.maxDistance);
			EditorGUILayout.EndToggleGroup();
		}

		showMeshSettings = EditorGUILayout.Foldout(showMeshSettings, "Mesh Settings");
		if(showMeshSettings)
		{
			script.drawSettings.meshName = EditorGUILayout.TextField("Mesh Name", script.drawSettings.meshName);

			script.drawSettings.frontMaterial = (Material)EditorGUILayout.ObjectField("Material", script.drawSettings.frontMaterial, typeof(Material), false);

			script.drawSettings.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup("Mesh Anchor", script.drawSettings.anchor);

			script.drawSettings.zPosition = EditorGUILayout.FloatField("Z Origin", script.drawSettings.zPosition);

			script.drawSettings.faceOffset = EditorGUILayout.FloatField(new GUIContent("Z Offset", "Allows for custom offsets.  See docs for details."), script.drawSettings.faceOffset);

			script.drawSettings.generateBackFace = EditorGUILayout.Toggle("Generate Back", script.drawSettings.generateBackFace);

			script.drawSettings.generateSide = EditorGUILayout.BeginToggleGroup("Generate Sides", script.drawSettings.generateSide);

				script.drawSettings.sideLength = EditorGUILayout.FloatField("Side Length", script.drawSettings.sideLength);
				script.drawSettings.sideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", script.drawSettings.sideMaterial, typeof(Material), false);

			EditorGUILayout.EndToggleGroup();

			script.drawSettings.drawEdgePlanes = EditorGUILayout.BeginToggleGroup("Draw Edge Planes", script.drawSettings.drawEdgePlanes);

				script.drawSettings.edgeMaterial = (Material)EditorGUILayout.ObjectField("Edge Material", script.drawSettings.edgeMaterial, typeof(Material), false);
				script.drawSettings.edgeLengthModifier = EditorGUILayout.FloatField(new GUIContent("Edge Length Modifier", "Multiply the length of the plane by this amount.  1 = no change, 1.3 = 33% longer."), script.drawSettings.edgeLengthModifier);
				script.drawSettings.edgeHeight  = EditorGUILayout.FloatField(new GUIContent("Edge Height", "Absolute value; how tall to make edge planes."), script.drawSettings.edgeHeight);
				script.drawSettings.minLengthToDraw  = EditorGUILayout.FloatField(new GUIContent("Minimum Edge Length", "Edges with a length less than this amount will not be drawn."), script.drawSettings.minLengthToDraw);
				script.drawSettings.edgeOffset  = EditorGUILayout.FloatField(new GUIContent("Edge Z Offset", "Value to scoot mesh + or - from Z Position."), script.drawSettings.edgeOffset);
				script.drawSettings.maxAngle = EditorGUILayout.FloatField(new GUIContent("Maximum Slope", "Edge Z rotation must be less than this amount to be drawn.  Set to 180 to draw a complete loop, or leave at 45 to only draw up facing planes."), script.drawSettings.maxAngle);
				script.drawSettings.areaRelativeHeight = EditorGUILayout.Toggle(new GUIContent("Area Relative Height", "If true, the height of each plane will be modified by how large the finalized mesh is."), script.drawSettings.areaRelativeHeight);
				if(script.drawSettings.areaRelativeHeight)
				{
					script.drawSettings.minEdgeHeight = EditorGUILayout.FloatField("Minimum Edge Height", script.drawSettings.minEdgeHeight);
					script.drawSettings.maxEdgeHeight = EditorGUILayout.FloatField("Maximum Edge Height", script.drawSettings.maxEdgeHeight);
				}

			EditorGUILayout.EndToggleGroup();
			
			script.drawSettings.uvScale = EditorGUILayout.Vector2Field("UV Scale", script.drawSettings.uvScale);
		}

		showGameObjectSettings = EditorGUILayout.Foldout(showGameObjectSettings, "GameObject Settings");
		if(showGameObjectSettings)
		{
			script.drawSettings.useTag = EditorGUILayout.BeginToggleGroup(new GUIContent("Apply Tag", "Tag must exist prior to assignment."), script.drawSettings.useTag);
				script.drawSettings.tagVal = EditorGUILayout.TextField("Tag", script.drawSettings.tagVal);
			EditorGUILayout.EndToggleGroup();
			
			script.maxAllowedObjects = EditorGUILayout.IntField("Max Drawn Objects Allowed", script.maxAllowedObjects);
		}

		showCollisionSettings = EditorGUILayout.Foldout(showCollisionSettings, "Physics Settings");
		if(showCollisionSettings)
		{
			script.drawSettings.colliderType = (Draw.ColliderType)EditorGUILayout.EnumPopup(new GUIContent("Collison", "If set to mesh, a Mesh Collider will be applied.  If set to Box, a series of thin box colliders will be generated bordering the edges, allowing for concave interactions.  None means no collider will be applied."), script.drawSettings.colliderType);

			if(script.drawSettings.colliderType == Draw.ColliderType.BoxCollider)
			{
				if(!script.drawSettings.manualColliderDepth)
					script.drawSettings.colDepth = script.drawSettings.sideLength;

				script.drawSettings.manualColliderDepth = EditorGUILayout.BeginToggleGroup(new GUIContent("Manual Collider Depth", "If false, the side length will be used as the depth value."), script.drawSettings.manualColliderDepth);

					script.drawSettings.colDepth = EditorGUILayout.FloatField("Collider Depth", script.drawSettings.colDepth);

				EditorGUILayout.EndToggleGroup();
			}

			switch(script.drawSettings.colliderType) {
				case Draw.ColliderType.BoxCollider:
					break;
				case Draw.ColliderType.MeshCollider:
					script.drawSettings.forceConvex = EditorGUILayout.Toggle("Force Convex", script.drawSettings.forceConvex);
					break;
				case Draw.ColliderType.None:
					break;
			}

			script.drawSettings.applyRigidbody = EditorGUILayout.BeginToggleGroup("Apply Rigidbody", script.drawSettings.applyRigidbody);

				script.drawSettings.useGravity = EditorGUILayout.Toggle("Use Gravity", script.drawSettings.useGravity);

				script.drawSettings.isKinematic = EditorGUILayout.Toggle("Is Kinematic", script.drawSettings.isKinematic);

				script.drawSettings.areaRelativeMass = EditorGUILayout.Toggle("Area Relative Mass", script.drawSettings.areaRelativeMass);
				if(script.drawSettings.areaRelativeMass)
					script.drawSettings.massModifier = EditorGUILayout.FloatField("Mass Modifier", script.drawSettings.massModifier);
				else
					script.drawSettings.mass = EditorGUILayout.FloatField("Mass", script.drawSettings.mass);
			EditorGUILayout.EndToggleGroup();
		}

		if(GUI.changed)
			EditorUtility.SetDirty(script);
	}
}
