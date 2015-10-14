using UnityEditor;
using UnityEngine;
using System;
using Polydraw;
using System.Collections;
using System.Collections.Generic;

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
	GUIContent gc_smoothAngle = new GUIContent("Smooth Angle", "Edges that form an angle less than this value will have their normals averaged.  Creates a smooth lighting effect.");
	GUIContent gc_drawNormals = new GUIContent("Draw Normals", "Draws the edge normals in the sceneview.  Does not affect the object at runtime.  Useful when trying to get smooth edges");

	// textures

	// physics
	GUIContent gc_colliderType = new GUIContent("Collider Type", "Polydraw has the ability to create compound colliders.  This is pretty cool - it builds thin box colliders around the edges of your object.  Or you could be boring and use old fashioned MeshColliders.  Whatever floats your boat.");

	// collisions
	GUIContent gc_colDepth = new GUIContent("Collision Depth", "How long depth-wise the mesh collider should be.");
	GUIContent gc_colAnchor = new GUIContent("Collision Mesh Anchor", "Where should the collision mesh start, and how shoud it align itself?");
	GUIContent gc_boxColliderSize = new GUIContent("BoxCollider Size", "How thick should box colliders generate themselves.");
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
	[MenuItem("Window/Polydraw/Polydraw Object &p")]
	public static void MenuCreatePolydrawObject()
	{
		CreatePolydrawObject();
	}

	public static void CreatePolydrawObject()
	{
		PolydrawObject polydrawObject = PolydrawObject.CreateInstance();

		polydrawObject.drawSettings.frontMaterial = (Material)AssetDatabase.LoadAssetAtPath(
			"Assets/Polydraw/Default Textures/Cardboard.mat", typeof(Material));
		
		polydrawObject.drawSettings.sideMaterial = (Material)AssetDatabase.LoadAssetAtPath(
			"Assets/Polydraw/Default Textures/Cardboard Grass.mat", typeof(Material));
		
		Selection.activeTransform = polydrawObject.transform;
	}

	[MenuItem("Window/Polydraw/Clean Up Unused Assets")]
	public static void CleanUp()
	{
		#if UNITY_5
		EditorUtility.UnloadUnusedAssetsImmediate();
		#else
		EditorUtility.UnloadUnusedAssets();
		#endif
	} 

	private void OnEnable()
	{
		HANDLE_ICON_NORMAL = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/HandleIcon-Normal.png", typeof(Texture2D));
		HANDLE_ICON_ACTIVE = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/HandleIcon-Active.png", typeof(Texture2D));
		INSERT_ICON_ACTIVE = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/InsertPoint-Active.png", typeof(Texture2D));
		INSERT_ICON_NORMAL = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/InsertPoint-Normal.png", typeof(Texture2D));
		DELETE_ICON_ACTIVE = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/DeletePoint-Active.png", typeof(Texture2D));
		DELETE_ICON_NORMAL = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Polydraw/Icons/DeletePoint-Normal.png", typeof(Texture2D));

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

		bool guiChanged = false;

		if(GUI_EditSettings())
			guiChanged = true;
		
		EditorGUI.BeginChangeCheck();
		poly.drawSettings.generateBackFace = EditorGUILayout.Toggle("Generate Back Face", poly.drawSettings.generateBackFace);
		
		/**
		 *	\brief Draw Settings
		 */
		poly.drawSettings.generateSide = EditorGUILayout.Toggle("Generate Sides", poly.drawSettings.generateSide);
		if(EditorGUI.EndChangeCheck())
			guiChanged = true;

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

	private bool GUI_EditSettings()
	{
		bool _snapEnabled = snapEnabled;
		bool changed = false;
		float _snapValue = snapValue;

		poly.drawStyle = (DrawStyle)EditorGUILayout.EnumPopup("Draw Style", poly.drawStyle);

		if(poly.drawStyle == DrawStyle.Continuous)
		{
			poly.drawSettings.minimumDistanceBetweenPoints = EditorGUILayout.FloatField(new GUIContent("Min Distance Between Points", "Points will only be added to the polygon if they are this distance from every other point in the shape."), poly.drawSettings.minimumDistanceBetweenPoints);
			poly.drawSettings.minimumDistanceBetweenPoints = Mathf.Clamp(poly.drawSettings.minimumDistanceBetweenPoints, 0f, 100f);
		}

		GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Snap Enabled");
			_snapEnabled = EditorGUILayout.Toggle(_snapEnabled);
		GUILayout.EndHorizontal();

		EditorGUI.BeginChangeCheck();
		poly.drawSettings.axis = (Axis)EditorGUILayout.EnumPopup("Axis", poly.drawSettings.axis);
		if(EditorGUI.EndChangeCheck())
			changed = true;
		
		_snapValue = EditorGUILayout.FloatField("Snap Value", _snapValue);

		if(snapEnabled != _snapEnabled)
			SetSnapEnabled( _snapEnabled );

		if(snapValue != _snapValue)
			SetSnapValue( _snapValue );

		return changed;
	}

	private bool GUI_SideSettings()
	{
		EditorGUI.BeginChangeCheck();

		poly.drawSettings.anchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_anchor, poly.drawSettings.anchor);
		poly.drawSettings.faceOffset = EditorGUILayout.FloatField(gc_faceOffset, poly.drawSettings.faceOffset);
		poly.drawSettings.sideLength = EditorGUILayout.FloatField(gc_sideLength, poly.drawSettings.sideLength);
		
		GUILayout.BeginHorizontal();
		poly.drawSettings.smoothAngle = EditorGUILayout.FloatField(gc_smoothAngle, poly.drawSettings.smoothAngle, GUILayout.MaxWidth(200));
		poly.drawSettings.smoothAngle = GUILayout.HorizontalSlider(poly.drawSettings.smoothAngle, 0f, 90f);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		poly.drawSettings.drawNormals = EditorGUILayout.Toggle(gc_drawNormals, poly.drawSettings.drawNormals);
		if(!poly.drawSettings.drawNormals) GUI.enabled = false;
		poly.drawSettings.normalLength = Mathf.Clamp(EditorGUILayout.FloatField("Length", poly.drawSettings.normalLength), 0f, 100f);
		GUI.enabled = true;
		GUILayout.EndHorizontal();

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

		if(poly.drawSettings.colliderType != Draw.ColliderType.PolygonCollider2d)
		{
			poly.drawSettings.colDepth = EditorGUILayout.FloatField(gc_colDepth, poly.drawSettings.colDepth);
			poly.drawSettings.colAnchor = (Draw.Anchor)EditorGUILayout.EnumPopup(gc_colAnchor, poly.drawSettings.colAnchor); 

			if(poly.drawSettings.colliderType != Draw.ColliderType.BoxCollider)
				GUI.enabled = false;

			poly.drawSettings.boxColliderSize = EditorGUILayout.FloatField(gc_boxColliderSize, poly.drawSettings.boxColliderSize);
			
			GUI.enabled = true;
		}
		
		return EditorGUI.EndChangeCheck();
	}
#endregion

#region OnSceneGUI

	public void OnSceneGUI()
	{		
		Event e = Event.current;
		
		if(poly && !poly.isEditable) return;

		// Force orthographic camera and x/y axis
		SceneView sceneView = SceneView.lastActiveSceneView;
		if(!sceneView) return;

		sceneView.orthographic = true;
		switch(poly.drawSettings.axis)
		{
			case Axis.Forward:
				sceneView.rotation = Quaternion.identity;
				break;

			case Axis.Up:
				sceneView.rotation = Quaternion.Euler(Vector3.right*90f);
				break;

			case Axis.Right:
				sceneView.rotation = Quaternion.Euler(-Vector3.up*90f);
				break;
		}

		Vector3[] points = poly.transform.ToWorldSpace(poly.points.ToVector3(poly.drawSettings.axis, poly.drawSettings.zPosition));

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

		switch(poly.drawStyle)
		{
			case DrawStyle.Point:
				PointDrawStyleInput(sceneView.camera, e, points);
				break;
			case DrawStyle.Continuous:
				ContinuousDrawStyleInput(sceneView.camera, e, points);
				break;
			default:
				break;
		}

	}

	private bool DrawInsertPointGUI(Vector3[] points)
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
					Undo.RecordObject( poly, "Add Point" );
					
					if(snapEnabled)
						poly.lastIndex = poly.AddPoint( Round(avg.ToVector2(poly.drawSettings.axis), snapValue), n );
					else
						poly.lastIndex = poly.AddPoint( avg.ToVector2(poly.drawSettings.axis), n );
					
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

			Undo.RecordObject(poly, "Delete Point");

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
				GUI.Label(handleRect, HANDLE_ICON_ACTIVE);

				if(GUI.Button(new Rect(g.x+10, g.y-40, 25, 25), "", deletePointStyle))
				{
					Undo.RecordObject(poly, "Delete Point");

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
					Undo.RecordObject(poly, "Add Point");

					if(snapEnabled)
						poly.lastIndex = poly.AddPoint( Round( GetWorldPoint(cam, e.mousePosition).ToVector2(poly.drawSettings.axis), snapValue ), insertPoint);
					else
						poly.lastIndex = poly.AddPoint( GetWorldPoint(cam, e.mousePosition).ToVector2(poly.drawSettings.axis), insertPoint);

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
					Undo.RecordObject(poly, "Move Point");

					if(snapEnabled)
						poly.SetPoint(poly.lastIndex, Round(GetWorldPoint(cam, e.mousePosition + poly.handleOffset).ToVector2(poly.drawSettings.axis), snapValue));
					else
						poly.SetPoint(poly.lastIndex, GetWorldPoint(cam, e.mousePosition + poly.handleOffset).ToVector2(poly.drawSettings.axis));
				}

				poly.Refresh();
				SceneView.RepaintAll();
			break;
		}
	}

	private void ContinuousDrawStyleInput(Camera cam, Event e, Vector3[] points)
	{
		if(!e.isMouse) return;

		switch(e.type)
		{
			case EventType.MouseDown:
			{
				/**
				 * Check it we're clicking on an existing object
				 */
				for(int i = 0; i < points.Length; i++)
				{
					Vector2 g = HandleUtility.WorldToGUIPoint(points[i]);
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
					Undo.RecordObject(poly, "New Drawn Shape");

					poly.ClearPoints();

					poly.handleOffset = Vector2.zero;
					// poly.isDraggingPoint = true;

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
					Undo.RecordObject(poly, "Move Point");

					if(snapEnabled)
						poly.SetPoint(poly.lastIndex, Round(GetWorldPoint(cam, e.mousePosition + poly.handleOffset).ToVector2(poly.drawSettings.axis), snapValue));
					else
						poly.SetPoint(poly.lastIndex, GetWorldPoint(cam, e.mousePosition + poly.handleOffset).ToVector2(poly.drawSettings.axis));
				}
				else
				{
					// drawin' stuff
					Vector3 newPoint = snapEnabled ? Round( GetWorldPoint(cam, e.mousePosition).ToVector2(poly.drawSettings.axis), snapValue ) : GetWorldPoint(cam, e.mousePosition).ToVector2(poly.drawSettings.axis);


					for(int i = 0; i < points.Length; i++)
						if( Vector3.Distance(newPoint, points[i]) < poly.drawSettings.minimumDistanceBetweenPoints )
							return;
	
					poly.lastIndex = poly.AddPoint(newPoint, insertPoint);


					// if(	poly.drawStyle == DrawStyle.ContinuousProximity &&
					// 	poly.points.Count > 3 && 
					// 	Vector3.Distance(points[0], newPoint) < poly.drawSettings.minimumDistanceBetweenPoints)
					// {
					// 	Debug.Log("dist : "  + Vector3.Distance(points[0], newPoint) );
					// 	m_ignore = true;
					// }
				}

				poly.Refresh();
				SceneView.RepaintAll();
			break;
		}
	}
#endregion

#region Event

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

	private Vector3 GetWorldPoint(Camera cam, Vector2 pos)
	{
		pos.y = Screen.height - pos.y - SCENEVIEW_HEADER;
		return cam.ScreenToWorldPoint(pos);
	}
#endregion
}
