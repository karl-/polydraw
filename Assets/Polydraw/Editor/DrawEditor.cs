#define GOLDBLUM

using UnityEditor;
using UnityEngine;
using System;
using Polydraw;
using System.Collections;
using System.Collections.Generic;

// todo 
//	- undo for points and deleting
//	- make points relative to transform, not world space.
//		would allow for moving of objects without screwing
//		with editing.
//	- normalize and scale uv options

[CustomEditor(typeof(PolydrawObject))]
public class DrawEditor : Editor
{

#region Members

	const int SCENEVIEW_HEADER = 40;	// accounts for the tabs and menubar at the top of the sceneview.

	// draw settings
	public enum DrawStyle
	{
		Continuous,
		Point
	};
	private DrawStyle drawStyle = DrawStyle.Point;
	private int insertPoint = -1;

	private PolydrawObject poly;
#endregion

#region GUIContent Strings

	// sides
	GUIContent gc_anchor = new GUIContent("Pivot", "Determines how the sides will be generated.  Ex: If Front, the pivot of this object will be at the z position and sides will extend back in the positive Z direction.");
	GUIContent gc_faceOffset = new GUIContent("Pivot Offset", "This value is used to determine where the front of the mesh will begin relative to the z position.  As an example, a value of -1 and a 'Front' pivot will place the front face of the mesh 1m in front of the z position with sides extending backwards towards position z axis.");
	GUIContent gc_sideLength = new GUIContent("Side Length", "How long the sides will be.");

	// textures

	// physics
	GUIContent gc_colliderType = new GUIContent("Collider Type", "Polydraw has the ability to create compound colliders.  This is pretty cool - it builds thin box colliders around the edges of your object.  Or you could be boring and use old fashioned MeshColliders.  Whatever floats your boat.");

	// collisions
	GUIContent gc_colDepth = new GUIContent("Collision Depth", "How long depth-wise the mesh collider should be.");
	GUIContent gc_colAnchor = new GUIContent("Collision Mesh Anchor", "Where should the collision mesh start, and how shoud it align itself?");
#endregion

#region Convenient

	public bool earlyOut
	{
		get
		{
			return (
				Event.current.alt || 
				Tools.current == Tool.View || 
				GUIUtility.hotControl > 0 || 
				(Event.current.isMouse ? Event.current.button > 1 : false) ||
				Tools.viewTool == ViewTool.FPS ||
				Tools.viewTool == ViewTool.Orbit);
		}
	}
#endregion

#region Window Lifecycle

	[MenuItem("GameObject/Create Other/Polydraw Object &p")]
	public static void GameObjctInit()
	{
		CreatePolydrawObject();
	}

	[MenuItem("Window/Polydraw/Polydraw Object &p")]
	public static void windowinit()
	{
		CreatePolydrawObject();
	}

	public static void CreatePolydrawObject()
	{
		PolydrawObject polydrawObject = PolydrawObject.CreateInstance();

		polydrawObject.drawSettings.frontMaterial = (Material)Resources.LoadAssetAtPath(
			"Assets/Polydraw/Default Textures/Cardboard.mat", typeof(Material));
		
		polydrawObject.drawSettings.sideMaterial = (Material)Resources.LoadAssetAtPath(
			"Assets/Polydraw/Default Textures/Cardboard Grass.mat", typeof(Material));
		
		Selection.activeTransform = polydrawObject.transform;
	}

	[MenuItem("Window/Polydraw/Clean Up Unused Assets")]
	public static void CleanUp()
	{
		EditorUtility.UnloadUnusedAssets();
	} 

	public void OnEnable()
	{
		poly = (PolydrawObject)target;
	}	
#endregion

#region Interface

	public override void OnInspectorGUI()
	{
		string onoff = poly.isEditable ? "Lock Editing" : "Edit Polydraw Object";
		GUI.backgroundColor = poly.isEditable ? Color.red : Color.green;
		if(GUILayout.Button(onoff, GUILayout.MinHeight(25)))
			ToggleEditingEnabled();
		GUI.backgroundColor = Color.white;

		GUILayout.Space(10);

		// drawStyle = (DrawStyle)EditorGUILayout.EnumPopup("Draw Style", drawStyle);

		GUI.changed = false;

		/**
		 *	\brief Draw Settings
		 */
		GUI_InputSettings();

		poly.drawSettings.generateSide = EditorGUILayout.Toggle("Generate Sides", poly.drawSettings.generateSide);

		poly.t_showSideSettings = EditorGUILayout.Foldout(poly.t_showSideSettings, "Side Settings");
		if(poly.t_showSideSettings)
			GUI_SideSettings();

		poly.t_showTextureSettings = EditorGUILayout.Foldout(poly.t_showTextureSettings, "Texture Settings");
		if(poly.t_showTextureSettings)
			GUI_TextureSettings();

		poly.t_showCollisionSettings = EditorGUILayout.Foldout(poly.t_showCollisionSettings, "Collision Settings");
		if(poly.t_showCollisionSettings)
			GUI_CollisionSettings();

		if(GUI.changed) 
		{
			EditorUtility.SetDirty(poly);
			poly.Refresh();
		}

		GUILayout.Space(10);

		GUI.backgroundColor = Color.cyan;
		if(GUILayout.Button("Clear Points"))
		{
			poly.ClearPoints();
			poly.DestroyMesh();
			SceneView.RepaintAll();
		}
		GUI.backgroundColor = Color.white;
	}
#endregion

#region DrawSettings GUI 	// since there are so many settings, this serves as a way to keep the OnGUI function "light"

	private void GUI_InputSettings()
	{
		EditorGUILayout.HelpBox("A negative value inserts new points at the end of the line.", MessageType.Info);
		insertPoint = EditorGUILayout.IntField("Insert Point", insertPoint);

	}

	private void GUI_SideSettings()
	{
		poly.drawSettings.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_anchor, poly.drawSettings.anchor);
		poly.drawSettings.faceOffset = EditorGUILayout.FloatField(gc_faceOffset, poly.drawSettings.faceOffset);
		poly.drawSettings.sideLength = EditorGUILayout.FloatField(gc_sideLength, poly.drawSettings.sideLength);
	}

	private void GUI_TextureSettings()
	{
		poly.drawSettings.frontMaterial = (Material)EditorGUILayout.ObjectField("Front Material", poly.drawSettings.frontMaterial, typeof(Material), true);
		GUI.enabled = poly.drawSettings.generateSide;
		poly.drawSettings.sideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", poly.drawSettings.sideMaterial, typeof(Material), true);
		GUI.enabled = true;
	}

	private void GUI_CollisionSettings()
	{
		poly.drawSettings.colliderType = (Draw.ColliderType)EditorGUILayout.EnumPopup(gc_colliderType, poly.drawSettings.colliderType);

		poly.drawSettings.colDepth = EditorGUILayout.FloatField(gc_colDepth, poly.drawSettings.colDepth);
		poly.drawSettings.colAnchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_colAnchor, poly.drawSettings.colAnchor); 

	}
#endregion

#region OnSceneGUI

	public void OnSceneGUI()
	{		
		Event e = Event.current;

		if(e.type == EventType.ValidateCommand)
		{
			OnValidateCommand(Event.current.commandName);
			return;
		}

		SceneViewGUI();

		if(!poly.isEditable) return;
		
		// Force orthographic camera and x/y axis
		SceneView sceneView = SceneView.lastActiveSceneView;
		if(!sceneView) return;

		sceneView.rotation = Quaternion.identity;
		sceneView.orthographic = true;

		Vector3[] points = poly.transform.ToWorldSpace(poly.points.ToVector3(poly.drawSettings.zPosition));

		// listen for shortcuts
		ShortcutListener(e);

		if(poly && !poly.isEditable) return;

		// draw handles
		// draws PositionHandles and delete / point no info
		DrawHandles(points);

		// debug
		DrawLines(points);

		if(earlyOut) return;

		// Give us control of the scene view
		int controlID = GUIUtility.GetControlID(FocusType.Passive);
		HandleUtility.AddDefaultControl(controlID);

		Tools.current = Tool.None;

		switch(drawStyle)
		{
			case DrawStyle.Point:
				PointDrawStyleInput(sceneView.camera, e);
				break;
			case DrawStyle.Continuous:
				ContinuousDrawStyleInput(sceneView.camera, e);
				break;
			default:
				break;
		}
	}

	private void SceneViewGUI()
	{
		Handles.BeginGUI();

		Handles.EndGUI();
	}

	private void ShortcutListener(Event e)
	{
		if(!e.isKey || e.type != EventType.KeyUp) return;
		
		if(e.keyCode == KeyCode.Return)
			poly.SetEditable(false);
	}

	private void DrawHandles(Vector3[] p)
	{
		for(int i = 0; i < p.Length; i++)
		{
			Vector3 p0 = p[i];
			p0 = Handles.PositionHandle(p0, Quaternion.identity);
			if(p0 != p[i])
			{
				Undo.RegisterUndo(poly, "Move Point");
				poly.SetPoint(i, p0);
				poly.Refresh();
			}
		}

		Handles.BeginGUI();
		GUI.backgroundColor = Color.red;
		for(int i = 0; i < p.Length; i++)
		{
			Vector2 g = HandleUtility.WorldToGUIPoint(p[i]);

			if(GUI.Button(new Rect(g.x+10, g.y-50, 25, 25), "x"))
			{
				poly.RemovePointAtIndex(i);
				poly.Refresh();
			}

			GUI.Label(new Rect(g.x+45, g.y-50, 200, 25), "Point: " + i.ToString());	
		}
		GUI.backgroundColor = Color.white;
		Handles.EndGUI();		
	}

	private void DrawLines(Vector3[] p)
	{
		if(p.Length < 2) return;

		for(int i = 0; i < p.Length-1; i++)
		{
			Handles.DrawLine(p[i], p[i+1]);
		}
		Handles.DrawLine(p[p.Length-1], p[0]);
	}
#endregion

#region Draw Style Input

	int lastIndex = 0;
	bool placingPoint = false;

	private void PointDrawStyleInput(Camera cam, Event e)
	{
		if(!e.isMouse) return;

		if(e.type == EventType.MouseDown)
		{
			Undo.RegisterUndo(poly, "Add Point");

			lastIndex = poly.AddPoint(GetWorldPoint(cam, e.mousePosition), insertPoint);
			placingPoint = true;
		}
		else
		if(e.type == EventType.MouseDrag && placingPoint)
		{
			poly.SetPoint(lastIndex, GetWorldPoint(cam, e.mousePosition));
		}
		else
		if(e.type == EventType.MouseUp && placingPoint)
		{
			poly.SetPoint(lastIndex, GetWorldPoint(cam, e.mousePosition));
			placingPoint = false;
		}
		else
		{
			placingPoint = false;
			return;
		}

		poly.Refresh();

		SceneView.RepaintAll();
	}

	private void ContinuousDrawStyleInput(Camera cam, Event e)
	{

	}
#endregion

#region Event

	void OnValidateCommand(string command)
	{
		switch(command)
		{
			case "UndoRedoPerformed":
				if(poly)
					poly.Refresh();
				
				Event.current.Use();

				break;
		}
	}
#endregion

#region Operational Setters

	Tool prevTool = (Tool)Tool.None;
	private void ToggleEditingEnabled()
	{
		poly.SetEditable(!poly.isEditable);
	
		if(poly.isEditable)
		{
			prevTool = Tools.current;
			Tools.current = Tool.None;
			
			poly.CenterPivot();
		}
		else
		{
			Tools.current = prevTool;
		}

		SceneView.RepaintAll();
	}
#endregion

#region Camera Conversions

	private Vector2 GetWorldPoint(Camera cam, Vector2 pos)
	{
		pos.y = Screen.height - pos.y - SCENEVIEW_HEADER;
		return cam.ScreenToWorldPoint(pos);
	}
#endregion
}