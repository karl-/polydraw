using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using Polydraw;

public class OrthoDemo : MonoBehaviour
{
	Draw draw;
	public GameObject drawGameObject;
	const int horizontalSize = 250;
	const int selectionButtonHeight = 25;
	Rect guiRect;

	void Start () {
		draw = (Draw)drawGameObject.GetComponent<Draw>();
		if(!draw) {
			draw = GameObject.Find("Draw").GetComponent<Draw>();
		}

		guiRect = new Rect(0, 0, horizontalSize, selectionButtonHeight * 9);

		draw.IgnoreRect(new Rect(
			guiRect.xMin,
			Screen.height - guiRect.yMin - guiRect.height,
			guiRect.width,
			guiRect.height
			));
	}
	
	void OnGUI() {
	
	GUI.Box(guiRect, "");
	
	GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
			if(GUILayout.Button("Continuous", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.Continuous);
			}
			
			if(GUILayout.Button("Continuous - Closing Distance", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.ContinuousClosingDistance);
			}
			
			if(GUILayout.Button("Point - Max Vertex Amount", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.PointMaxVertex);
			}
			
			if(GUILayout.Button("Point - Closing Distance", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.PointClosingDistance);
			}

			// Saves all current meshes
#if UNITY_EDITOR
			if(GUILayout.Button("Save", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight)))
				draw.ExportOBJ("Assets/Mesh.obj");
			AssetDatabase.Refresh();

#elif UNITY_STANDALONE_WIN
			if(GUILayout.Button("Save", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight)))				
			draw.ExportOBJ(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Mesh.obj");

#elif UNITY_STANDALONE_OSX	// Please note that this is not tested.  I'm assuming that relative paths will translate though.  If this doesn't work, you might also try "$HOME/Desktop/".
			if(GUILayout.Button("Save", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight)))				
			draw.ExportOBJ("~/Desktop/" + "Mesh.obj");
#endif

		GUILayout.Space(5);

		if(GUILayout.Button("Clear Screen", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight)))
			draw.DestroyAllGeneratedMeshes();

		GUILayout.Space(4);

		GUILayout.Label("Max Allowed Objects  " + draw.maxAllowedObjects, GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight));
		draw.maxAllowedObjects = (int)GUILayout.HorizontalSlider(draw.maxAllowedObjects, 1, 10, GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight));

		GUILayout.EndVertical();

		GUILayout.BeginVertical();
			GUILayout.Label("Current Draw Style -- " + draw.drawStyle.ToString());
			switch(draw.drawStyle) {
				case Draw.DrawStyle.Continuous:
					GUILayout.Label("Click and drag to draw points.  Let the mouse up to finalize the mesh.  Don't cross the streams!");
				break;
			
			case Draw.DrawStyle.ContinuousClosingDistance:
					GUILayout.Label("Click and drag to draw points.  Let the mouse up to finalize the mesh, or draw a point near the origin.");
				break;
				
			case Draw.DrawStyle.PointMaxVertex:
				GUILayout.Label("Click to place points.  The mesh will finalize itself once the max number of Vertices has been reached.");
				break;
			
			case Draw.DrawStyle.PointClosingDistance:
				GUILayout.Label("Place a point next to the origin point to finalize mesh.");
				break;

			default:
				break;
		}

		GUILayout.EndVertical();
		
		GUILayout.EndHorizontal();
	}

	void SetDrawMode(Draw.DrawStyle drawStyle) {
		
		draw.CleanUp();

		draw.drawStyle = drawStyle;

		switch(drawStyle) {
			case Draw.DrawStyle.Continuous:
				draw.showPointMarkers = false;
				break;
			
			case Draw.DrawStyle.ContinuousClosingDistance:
				draw.showPointMarkers = true;			
				break;
				
			case Draw.DrawStyle.PointMaxVertex:
				draw.showPointMarkers = true;
				break;
			
			case Draw.DrawStyle.PointClosingDistance:
				draw.showPointMarkers = true;
				break;

			default:
				break;
		}
	}

}
