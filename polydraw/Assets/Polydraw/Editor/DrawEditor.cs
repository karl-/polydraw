#define SPIN_SELECTED_INDEX

#if UNITY_4_3 || UNITY_4_3_0 || UNITY_4_3_1 || UNITY_4_3_2 || UNITY_4_3_3 || UNITY_4_3_4 || UNITY_4_3_5
#define UNITY_4_3
#elif UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_3_0 || UNITY_4_3_1 || UNITY_4_3_2 || UNITY_4_3_3 || UNITY_4_3_4 || UNITY_4_3_5
#define UNITY_4
#elif UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define UNITY_3
#endif

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
	const int HANDLE_SIZE = 32;
	const int INSERT_HANDLE_SIZE = 24;
	private Texture2D HANDLE_ICON_ACTIVE;
	private Texture2D HANDLE_ICON_NORMAL;
	private Texture2D INSERT_ICON_ACTIVE;
	private Texture2D INSERT_ICON_NORMAL;
	private Texture2D DELETE_ICON_NORMAL;
	private Texture2D DELETE_ICON_ACTIVE;
	private GUIStyle insertIconStyle;
	private GUIStyle deletePointStyle;

	// draw settings
	public enum DrawStyle
	{
		Continuous,
		Point
	};

	private DrawStyle drawStyle = DrawStyle.Point;
	public int insertPoint = -1;

	private PolydrawObject poly;

	public bool snapEnabled;
	public float snapValue;
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

	private void OnEnable()
	{
		HANDLE_ICON_NORMAL = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/HandleIcon-Normal.png", typeof(Texture2D));
		HANDLE_ICON_ACTIVE = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/HandleIcon-Active.png", typeof(Texture2D));
		INSERT_ICON_ACTIVE = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/InsertPoint-Active.png", typeof(Texture2D));
		INSERT_ICON_NORMAL = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/InsertPoint-Normal.png", typeof(Texture2D));
		DELETE_ICON_ACTIVE = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/DeletePoint-Active.png", typeof(Texture2D));
		DELETE_ICON_NORMAL = (Texture2D)Resources.LoadAssetAtPath("Assets/Polydraw/Icons/DeletePoint-Normal.png", typeof(Texture2D));

		insertIconStyle = new GUIStyle();
		insertIconStyle.normal.background = INSERT_ICON_NORMAL;
		insertIconStyle.active.background = INSERT_ICON_ACTIVE;
		deletePointStyle = new GUIStyle();
		deletePointStyle.normal.background = DELETE_ICON_NORMAL;
		deletePointStyle.active.background = DELETE_ICON_ACTIVE;

		#if UNITY_4_3
			if(Undo.undoRedoPerformed != this.UndoRedoPerformed)
				Undo.undoRedoPerformed += this.UndoRedoPerformed;
		#endif

		poly = (PolydrawObject)target;

		snapEnabled = EditorPrefs.HasKey("polydraw_snapEnabled") ? EditorPrefs.GetBool("polydraw_snapEnabled") : false;
		snapValue= EditorPrefs.HasKey("polydraw_snapValue") ? EditorPrefs.GetFloat("polydraw_snapValue") : .25f;
	}

	private void OnDisable()
	{
		#if UNITY_4_3
		Undo.undoRedoPerformed -= this.UndoRedoPerformed;
		#endif
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

		GUILayout.Space(5);

		// drawStyle = (DrawStyle)EditorGUILayout.EnumPopup("Draw Style", drawStyle);
		
		GUI_EditSettings();
		
		bool guiChanged = false;

		/**
		 *	\brief Draw Settings
		 */
		poly.drawSettings.generateSide = EditorGUILayout.Toggle("Generate Sides", poly.drawSettings.generateSide);

		poly.t_showSideSettings = EditorGUILayout.Foldout(poly.t_showSideSettings, "Side Settings");
		if(poly.t_showSideSettings)
			if (GUI_SideSettings() )
				guiChanged = true;

		poly.t_showTextureSettings = EditorGUILayout.Foldout(poly.t_showTextureSettings, "Texture Settings");
		if(poly.t_showTextureSettings)
			if (GUI_TextureSettings() )
				guiChanged = true;

		poly.t_showCollisionSettings = EditorGUILayout.Foldout(poly.t_showCollisionSettings, "Collision Settings");
		if(poly.t_showCollisionSettings)
			if( GUI_CollisionSettings() )
				guiChanged = true;
		
		if(guiChanged) 
		{
			EditorUtility.SetDirty(poly);
			poly.Refresh();
			SceneView.RepaintAll();
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

	private void GUI_EditSettings()
	{
		bool _snapEnabled = snapEnabled;
		float _snapValue = snapValue;

		GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Snap Enabled");
			_snapEnabled = EditorGUILayout.Toggle(_snapEnabled);
		GUILayout.EndHorizontal();
		
		_snapValue = EditorGUILayout.FloatField("Snap Value", _snapValue);

		if(snapEnabled != _snapEnabled)
			SetSnapEnabled( _snapEnabled );

		if(snapValue != _snapValue)
			SetSnapValue( _snapValue );
	}

	private bool GUI_SideSettings()
	{
		EditorGUI.BeginChangeCheck();

		poly.drawSettings.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_anchor, poly.drawSettings.anchor);
		poly.drawSettings.faceOffset = EditorGUILayout.FloatField(gc_faceOffset, poly.drawSettings.faceOffset);
		poly.drawSettings.sideLength = EditorGUILayout.FloatField(gc_sideLength, poly.drawSettings.sideLength);
	
		return EditorGUI.EndChangeCheck();
	}

	private bool GUI_TextureSettings()
	{
		EditorGUI.BeginChangeCheck();
	
		poly.drawSettings.frontMaterial = (Material)EditorGUILayout.ObjectField("Front Material", poly.drawSettings.frontMaterial, typeof(Material), true);
		GUI.enabled = poly.drawSettings.generateSide;
		poly.drawSettings.sideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", poly.drawSettings.sideMaterial, typeof(Material), true);
		GUI.enabled = true;

		GUI.changed = false;
		poly.drawSettings.uvOffset = EditorGUILayout.Vector2Field("UV Offset", poly.drawSettings.uvOffset);
		poly.drawSettings.uvScale = EditorGUILayout.Vector2Field("UV Scale", poly.drawSettings.uvScale);
		poly.drawSettings.uvRotation = EditorGUILayout.FloatField("UV Rotation", poly.drawSettings.uvRotation);

		return EditorGUI.EndChangeCheck();
	}

	private bool GUI_CollisionSettings()
	{
		EditorGUI.BeginChangeCheck();
		
		poly.drawSettings.colliderType = (Draw.ColliderType)EditorGUILayout.EnumPopup(gc_colliderType, poly.drawSettings.colliderType);

		poly.drawSettings.colDepth = EditorGUILayout.FloatField(gc_colDepth, poly.drawSettings.colDepth);
		poly.drawSettings.colAnchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_colAnchor, poly.drawSettings.colAnchor); 

		return EditorGUI.EndChangeCheck();
	}
#endregion

#region OnSceneGUI

	public void OnSceneGUI()
	{		
		Event e = Event.current;

		#if !UNITY_4_3
		if(e.type == EventType.ValidateCommand)
		{
			OnValidateCommand(Event.current.commandName);
			return;
		}
		#endif
		
		if(poly && !poly.isEditable) return;

		// Force orthographic camera and x/y axis
		SceneView sceneView = SceneView.lastActiveSceneView;
		if(!sceneView) return;

		sceneView.rotation = Quaternion.identity;
		sceneView.orthographic = true;

		Vector3[] points = poly.transform.ToWorldSpace(poly.points.ToVector3(poly.drawSettings.zPosition));

		// listen for shortcuts
		ShortcutListener(e);


		// draw handles
		// draws PositionHandles and delete / point no info
		DrawHandles(points);

		if( DrawInsertPointGUI(points) )
			return;

		if(earlyOut) return;

		// Give us control of the scene view
		int controlID = GUIUtility.GetControlID(FocusType.Passive);
		HandleUtility.AddDefaultControl(controlID);

		Tools.current = Tool.None;

		switch(drawStyle)
		{
			case DrawStyle.Point:
				PointDrawStyleInput(sceneView.camera, e, points);
				break;
			case DrawStyle.Continuous:
				ContinuousDrawStyleInput(sceneView.camera, e);
				break;
			default:
				break;
		}
	}

	public bool DrawInsertPointGUI(Vector3[] points)
	{
		Handles.BeginGUI();

			int n = 0;
			for(int i = 0; i < points.Length; i++)
			{
				n = (i >= points.Length-1) ? 0 : i+1;

				Vector3 avg = (points[i]+points[n])/2f;
				Vector2 g = HandleUtility.WorldToGUIPoint( avg );

				Rect handleRect = new Rect(g.x-INSERT_HANDLE_SIZE/2f, g.y-INSERT_HANDLE_SIZE/2f, INSERT_HANDLE_SIZE, INSERT_HANDLE_SIZE);
				if( GUI.Button(handleRect, "", insertIconStyle))
				{
					#if UNITY_4_3
					Undo.RecordObject( poly, "Add Point" );
					#else
					Undo.RegisterUndo( poly, "Add Point" );
					#endif
					
					if(snapEnabled)
						poly.lastIndex = poly.AddPoint( Round(avg, snapValue), n );
					else
						poly.lastIndex = poly.AddPoint( avg, n );
					
					Handles.EndGUI();
					
					return true;
				}
			}

		Handles.EndGUI();
		
		return false;
	}

	private void ShortcutListener(Event e)
	{
		if(!e.isKey || e.type != EventType.KeyUp) return;
		
		if(e.keyCode == KeyCode.Return)
			poly.SetEditable(false);
		
		if(e.keyCode == KeyCode.Backspace)
		{
			if(poly.lastIndex < 0)
				return;

			#if UNITY_4_3
			Undo.RecordObject(poly, "Delete Point");
			#else
			Undo.RegisterUndo(poly, "Delete Point");
			#endif

			poly.RemovePointAtIndex(poly.lastIndex);
			poly.Refresh();
		}
	}

	// private Vector2 handleOffset = Vector2.zero;

	private void DrawHandles(Vector3[] p)
	{
		Handles.BeginGUI();
		GUI.backgroundColor = Color.red;
		for(int i = 0; i < p.Length; i++)
		{
			Vector2 g = HandleUtility.WorldToGUIPoint(p[i]);
			Rect handleRect = new Rect(g.x-HANDLE_SIZE/2f, g.y-HANDLE_SIZE/2f, HANDLE_SIZE, HANDLE_SIZE);
			
			if(i == poly.lastIndex)
			{
				#if SPIN_SELECTED_INDEX
				{
					float ro = Time.realtimeSinceStartup;
					ro = (ro % 360) * 100f;
					GUIUtility.RotateAroundPivot(ro, g);
						GUI.Label(handleRect, HANDLE_ICON_ACTIVE);
					GUIUtility.RotateAroundPivot(-ro, g);
				}
				#else
					GUI.Label(handleRect, HANDLE_ICON_ACTIVE);
				#endif


				if(GUI.Button(new Rect(g.x+10, g.y-40, 25, 25), "", deletePointStyle))
				{
					#if UNITY_4_3
					Undo.RecordObject(poly, "Delete Point");
					#else
					Undo.RegisterUndo(poly, "Delete Point");
					#endif

					poly.RemovePointAtIndex(i);
					poly.Refresh();
				}
			}
			else
				GUI.Label(handleRect, HANDLE_ICON_NORMAL);
		}

		GUI.backgroundColor = Color.white;
		Handles.EndGUI();		

		SceneView.RepaintAll();
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

	private float Round(float val, float snap)
	{
		return snap * Mathf.Round(val / snap);
	}
	
	private Vector2 Round(Vector2 val, float snap)
	{
		return new Vector2(snap * Mathf.Round(val.x / snap), snap * Mathf.Round(val.y / snap));
	}

	private Vector3 Round(Vector3 val, float snap)
	{
		return new Vector3(snap * Mathf.Round(val.x / snap), snap * Mathf.Round(val.y / snap), snap * Mathf.Round(val.z / snap));
	}

#endregion

#region Draw Style Input

	private void PointDrawStyleInput(Camera cam, Event e, Vector3[] p)
	{
		if(!e.isMouse) return;

		switch(e.type)
		{
			case EventType.MouseDown:
			{
				for(int i = 0; i < p.Length; i++)
				{
					Vector2 g = HandleUtility.WorldToGUIPoint(p[i]);
					Rect handleRect = new Rect(g.x-HANDLE_SIZE/2f, g.y-HANDLE_SIZE/2f, HANDLE_SIZE, HANDLE_SIZE);
					
					if(handleRect.Contains(e.mousePosition))
					{
						poly.isDraggingPoint = true;
						poly.lastIndex = i;
						poly.handleOffset = g-e.mousePosition;
					}
				}

				if(!poly.isDraggingPoint)
				{
					#if UNITY_4_3
					Undo.RecordObject(poly, "Add Point");
					#else
					Undo.RegisterUndo(poly, "Add Point");
					#endif

					if(snapEnabled)
						poly.lastIndex = poly.AddPoint( Round( GetWorldPoint(cam, e.mousePosition), snapValue ), insertPoint);
					else
						poly.lastIndex = poly.AddPoint( GetWorldPoint(cam, e.mousePosition), insertPoint);

					poly.handleOffset = Vector2.zero;
					poly.isDraggingPoint = true;

					poly.Refresh();
					SceneView.RepaintAll();
				}
			}
			break;

			case EventType.MouseUp:
			{
				if(!poly.isDraggingPoint)
					break;
		
				poly.isDraggingPoint = false;
			}
			break;

			case EventType.MouseDrag:

				if(poly.isDraggingPoint)
				{
					#if UNITY_4_3
					Undo.RecordObject(poly, "Move Point");
					#else
					Undo.RegisterUndo(poly, "Move Point");
					#endif

					if(snapEnabled)
						poly.SetPoint(poly.lastIndex, Round(GetWorldPoint(cam, e.mousePosition + poly.handleOffset), snapValue));
					else
						poly.SetPoint(poly.lastIndex, GetWorldPoint(cam, e.mousePosition + poly.handleOffset));
				}

				poly.Refresh();
				SceneView.RepaintAll();
			break;
		}
	}

	private void ContinuousDrawStyleInput(Camera cam, Event e)
	{

	}
#endregion

#region Event

#if !UNITY_4_3
	void OnValidateCommand(string command)
	{
		switch(command)
		{
			case "UndoRedoPerformed":
				if(poly)
				{
					poly.isDraggingPoint = false;
					poly.Refresh();
				}
				
				Event.current.Use();

				SceneView.RepaintAll();	

				break;
		}
	}
#endif

	void UndoRedoPerformed()
	{
		if(poly)
		{
			poly.isDraggingPoint = false;
			poly.Refresh();
		}

		SceneView.RepaintAll();
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

	public void SetSnapEnabled(bool enable)
	{
		snapEnabled = enable;
		EditorPrefs.SetBool("polydraw_snapEnabled", snapEnabled);
	}

	public void SetSnapValue(float snapV)
	{
		snapValue = snapV;
		EditorPrefs.SetFloat("polydraw_snapValue", snapValue);
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