using UnityEngine;
using System.Collections;

public class Demo : MonoBehaviour {

	Draw draw;
	public GameObject drawGameObject;
	const int horizontalSize = 250;
	const int selectionButtonHeight = 32;

	void Start () {
		draw = (Draw)drawGameObject.GetComponent<Draw>();
		if(!draw) {
			draw = GameObject.Find("Draw").GetComponent<Draw>();
		}
	}
	
	void OnGUI() {
	
	GUI.Box(new Rect(0, 0, horizontalSize, Screen.height), "");
	
	GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
			if(GUILayout.Button("Continuous", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.Continuous);
			}
			
			if(GUILayout.Button("Continuous - Closing Distance", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.ContinuousClosingDistance);
			}
			
			if(GUILayout.Button("Point - Max Vertice", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.PointMaxVertice);
			}
			
			if(GUILayout.Button("Point - Closing Distance", GUILayout.MaxWidth(horizontalSize), GUILayout.MinHeight(selectionButtonHeight))) {
				SetDrawMode(Draw.DrawStyle.PointClosingDistance);
			}
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
				
			case Draw.DrawStyle.PointMaxVertice:
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

	GUILayout.Space(5);

	if(GUILayout.Button("Clear Screen", GUILayout.MaxWidth(horizontalSize)))
		draw.DestroyAllGeneratedMeshes();

	GUILayout.Space(10);

	GUILayout.Label("There are more user settings available,\ncheck the documentation for more\ninformation!");

	GUILayout.Space(4);

	GUILayout.Label("Max Allowed Objects  " + draw.maxAllowedObjects);
	draw.maxAllowedObjects = (int)GUILayout.HorizontalSlider(draw.maxAllowedObjects, 1, 10, GUILayout.MaxWidth(horizontalSize));
	
	GUILayout.Space(2);

	draw.generateBoxColliders = GUILayout.Toggle(draw.generateBoxColliders, "Generate Box Colliders");
	if(draw.generateBoxColliders) {
		draw.generateMeshCollider = false;
		draw.colliderStyle = Draw.ColliderStyle.BoxCollider;
	}

	draw.generateMeshCollider = GUILayout.Toggle(draw.generateMeshCollider, "Generate Mesh Collider");
	if(draw.generateMeshCollider) {
		draw.generateBoxColliders = false;
		draw.colliderStyle = Draw.ColliderStyle.MeshCollider;

		draw.forceConvex = GUILayout.Toggle(draw.forceConvex, "Force Convex");
		draw.isKinematic = GUILayout.Toggle(draw.isKinematic, "Is Kinematic");
	}

	draw.showPointMarkers = GUILayout.Toggle(draw.showPointMarkers, "Show Point Markers");

	GUILayout.BeginHorizontal();
		switch(draw.drawStyle) {
			case Draw.DrawStyle.Continuous:
			GUILayout.BeginVertical();
				GUILayout.Label("Sampling Rate");
				draw.samplingRate = GUILayout.HorizontalSlider (draw.samplingRate, 0.01f, 1f, GUILayout.MaxWidth(horizontalSize));
			GUILayout.EndVertical();
				break;
			
			case Draw.DrawStyle.ContinuousClosingDistance:
			GUILayout.BeginVertical();
				GUILayout.Label("Sampling Rate");
				draw.samplingRate = GUILayout.HorizontalSlider (draw.samplingRate, 0.01f, 1f, GUILayout.MaxWidth(horizontalSize));
			GUILayout.EndVertical();
				break;
				
			case Draw.DrawStyle.PointMaxVertice:
			GUILayout.BeginVertical();
				GUILayout.Label("Max Vertices " + draw.maxVertices);
				draw.maxVertices = (int)GUILayout.HorizontalSlider(draw.maxVertices, 3, 30, GUILayout.MaxWidth(horizontalSize));
			GUILayout.EndVertical();
				break;
			
			case Draw.DrawStyle.PointClosingDistance:
				GUILayout.Label("");
				break;

			default:
				break;
		}
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
				
			case Draw.DrawStyle.PointMaxVertice:
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
