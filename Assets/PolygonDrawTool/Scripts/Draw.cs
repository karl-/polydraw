/// <summary>
/// Draws a mesh from user input.
/// 
/// Known Issues:
/// -	No hole support.
/// 
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Draw : MonoBehaviour {

	List<Vector2> userPoints = new List<Vector2>();
	
	/// <summary>
	/// User Settings
	/// </summary>
	public Material material;
	public GameObject pointMarker;
	public DrawStyle drawStyle = DrawStyle.Continuous;
	public int maxVertices = 5;		// Only applies to non-continuous drawing
	public float samplingRate = .1f;
	public bool drawMeshInProgress = true;
	public bool useDistanceCheck = false;
	public float closingDistance = 5;
	public float lineWidth = .1f;
	public int maxAllowedObjects = 2;
	
	///Final Mesh Settings
	public bool generateCollider = true;
	public bool forceConvex = false;
	public bool applyRigidbody = true;
	public float mass = 25f;
	public bool useGravity = true;
	public bool isKinematic = false;
	public bool useTag = false;
	public string tag = "drawnMesh";
	public float zPosition = 0;
	public Vector2 uvScale = new Vector2(1f, 1f);
	
	/// <summary>
	/// Internal
	/// </summary>
	bool placingPoint = false;
	Vector3 previousMousePosition;
	private float timer = 0f;
	private List<GameObject> generatedMeshes = new List<GameObject>();
	private int windingOrder; // Positive = CC , Negative = CW
	enum Polygon {
		ConvexClockwise,
		ConvexCounterClockwise,
		ConcaveClockwise,
		ConcaveCounterClockwise
	}
	Camera mainCamera;

	public enum DrawStyle {
		Continuous,
		Continuous_ClosingDistance,
		Point_MaxVertice,
		Point_ClosingDistance
	}
	
	void Start() {
		mainCamera = Camera.main;
	}
	
	/// <summary>
	/// Debug GUI Items
	/// </summary>
	void OnGUI()
	{
		if(GUILayout.Button("Destroy All Meshes", GUILayout.MinHeight(32)))
			DestroyAllGeneratedMeshes();
	}
	
	void Update() {
		
		if(drawStyle == DrawStyle.Point_MaxVertice || drawStyle == DrawStyle.Point_ClosingDistance) {
			if(Input.GetMouseButtonDown(0)) {
				Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);

				DrawLineRenderer(userPoints.ToArray());
				
				AddPoint(worldPos);
				
				placingPoint = true;
			}
			
			if(Input.mousePosition != previousMousePosition && placingPoint) {			
				previousMousePosition = Input.mousePosition;
				Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);
				
				userPoints[userPoints.Count - 1]  = worldPos;
				RefreshPreview();
			}
	
			if(Input.GetMouseButtonUp(0)) {
				placingPoint = false;
				
				if(drawStyle == DrawStyle.Point_ClosingDistance && userPoints.Count > 2) {
					if( (userPoints[0] - userPoints[userPoints.Count - 1]).sqrMagnitude < (closingDistance) )
						DrawFinalMesh(userPoints.ToArray());
				}
		
				if(userPoints.Count > maxVertices && drawStyle == DrawStyle.Point_MaxVertice)
					DrawFinalMesh(userPoints.ToArray());
			}
		}
		else
		if(drawStyle == DrawStyle.Continuous || drawStyle == DrawStyle.Continuous_ClosingDistance)
		{
			if(Input.GetMouseButton(0)) {
				if(timer > samplingRate || Input.GetMouseButtonDown(0)) {
					timer = 0f;
					
					// The triangulation algorithm in use doesn't like multiple verts
					// sharing the same world space, so don't let it happen!
					if(Input.mousePosition != previousMousePosition) {			
						previousMousePosition = Input.mousePosition;
						
						Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
						worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);
					
						AddPoint(worldPos);
						
						if(drawStyle == DrawStyle.Continuous_ClosingDistance && userPoints.Count > 2) {
							if( (userPoints[0] - userPoints[userPoints.Count - 1]).sqrMagnitude < (closingDistance) )
								DrawFinalMesh(userPoints.ToArray());
						}//drawstyle
					}//mousepos check
				}//mousedown			
				
				timer += 1 * Time.deltaTime;
			}
			
			if(Input.GetMouseButtonUp(0)) {
				DrawFinalMesh(userPoints.ToArray());
				DestroyTempGameObject();
				DestroyLineRenderer();
			}
		}
	}
	
	void DrawLineRenderer(Vector2[] v) {
		LineRenderer lineRenderer;
		
		if(!gameObject.GetComponent<LineRenderer>())
			lineRenderer = gameObject.AddComponent<LineRenderer>();
		else
			lineRenderer = gameObject.GetComponent<LineRenderer>();			

		lineRenderer.material = new Material (Shader.Find("Particles/Additive"));
     	lineRenderer.SetColors(Color.green, Color.green);
     	lineRenderer.SetWidth(lineWidth,lineWidth);
		
		if(v.Length > 1) {
			lineRenderer.SetVertexCount(v.Length);
			
			for(int i = 0; i < v.Length; i++) {
			//	if(i == v.Length)		// Draws the connecting line to beginning point
			//		gameObject.GetComponent<LineRenderer>().SetPosition(i, new Vector3(v[0].x, v[0].y, zPosition) );
			//	else
					lineRenderer.SetPosition(i, new Vector3(v[i].x, v[i].y, zPosition) );
			}
		}
	}
	
	void CleanUp() {
		userPoints.Clear();
		DestroyLineRenderer();
		DestroyTempGameObject();
	}
	
	void DestroyLineRenderer() {
		if(gameObject.GetComponent<LineRenderer>() != null)
			Destroy(gameObject.GetComponent<LineRenderer>());
	}
	
	void AddPoint(Vector3 position) {
		userPoints.Add(position);
		
		if(userPoints.Count > 1) {
			if(drawMeshInProgress)
				DrawTempMesh(userPoints.ToArray());
			
			DrawLineRenderer(userPoints.ToArray());
		}
	}
	
	void RefreshPreview() {
		if(userPoints.Count > 1) {			
			if(drawMeshInProgress)
				DrawTempMesh(userPoints.ToArray());

			DrawLineRenderer(userPoints.ToArray());
		}
	}
	
	/// <summary>
	/// Draw Mesh - will use only on GameObject and re-write itself.
	/// </summary>
	GameObject go;
	Mesh mesh;
	void DrawTempMesh(Vector2[] points) {		
		if( points.Length < 2 ) {
			CleanUp();
			return;
		}
		
		Polygon convexity = Convexity(points);
		
		// This should probably be accounted for in the Triangulation method using a more
		// reliable fill algorithm, but for non-intersecting geometry this works adequately 
		// well.
		if(convexity == Polygon.ConcaveClockwise || convexity == Polygon.ConvexClockwise)
			Array.Reverse(points);
		
		// Use the triangulator to get indices for creating triangles
        Triangulator tr = new Triangulator(points);
        int[] indices = tr.Triangulate();
       
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[points.Length];
        for (int i=0; i<vertices.Length; i++) {
            vertices[i] = new Vector3(points[i].x, points[i].y, zPosition);
        }
       
        // Create the mesh
		if(go == null) {
			go = new GameObject();
			go.AddComponent<MeshFilter>();
			go.GetComponent<MeshFilter>().sharedMesh = new Mesh();
			go.AddComponent<MeshRenderer>();
		}
		
		mesh = go.GetComponent<MeshFilter>().sharedMesh;
		mesh.Clear();
		mesh.vertices = vertices;
        mesh.triangles = indices;
		mesh.uv = CalculateUVs(vertices, uvScale);
		mesh.RecalculateNormals();
        mesh.RecalculateBounds();
       
        // Set up game object with mesh;
       	go.GetComponent<MeshFilter>().sharedMesh = mesh;
		go.GetComponent<MeshRenderer>().material = material;
	}
	
	void DestroyTempGameObject() 
	{
		if(go)
			Destroy(go);
	}
	
	void ChechMaxMeshes() 
	{
		if(generatedMeshes.Count >= maxAllowedObjects && maxAllowedObjects > 0)
		{
			GameObject g = generatedMeshes[0];
			generatedMeshes.RemoveAt(0);
			Destroy(g);
		}
	}
	
	void DestroyAllGeneratedMeshes()
	{
		for(int i = 0; i < generatedMeshes.Count; i++)
		{
			Destroy(generatedMeshes[i]);
		}
		generatedMeshes.Clear();
	}
	
	/// <summary>
	/// Draws the final mesh, creating a new GameObject.
	/// </summary>
	/// <param name='points'>
	/// Points.
	/// </param>
	void DrawFinalMesh(Vector2[] points) {
		DestroyTempGameObject();
		if(points.Length < 3 || ((points[0] - points[points.Length - 1]).sqrMagnitude > (closingDistance) && useDistanceCheck) ) {
			CleanUp();	
			return;
		}
		ChechMaxMeshes();
		
		Polygon convexity = Convexity(points);

		if(convexity == Polygon.ConcaveClockwise || convexity == Polygon.ConvexClockwise)
			Array.Reverse(points);
		
		/*** Generate Front Face ***/
        Triangulator tr = new Triangulator(points);
        int[] front_indices = tr.Triangulate();
       
        // Create the Vector3 vertices
        Vector3[] front_vertices = new Vector3[points.Length];
        for (int i=0; i<front_vertices.Length; i++) {
            front_vertices[i] = new Vector3(points[i].x, points[i].y, 0f);
        }
		/*** Finish Front Face ***/
		
        /*** Generate Sides ***/
        List<Vector3> side_vertices = new List<Vector3>();
		for (int i=0; i<points.Length; i++) {
   			side_vertices.Add( new Vector3( points[i].x, points[i].y, 10) );
			side_vertices.Add ( new Vector3( points[i].x, points[i].y, -10) );
		}	
        
		side_vertices.Add( new Vector3( points[0].x, points[0].y, 10) );
		side_vertices.Add ( new Vector3( points[0].x, points[0].y, -10) );
		
		// +6 connects it to the first 2 verts
		int[] side_indices = new int[(side_vertices.Count*3)];
		
		windingOrder = 1; // assume counter-clockwise
		
		int v = 0;
		for(int i = 0; i < side_indices.Length - 6; i+=3)
		{			
			// 0 is for clockwise winding order, anything else is CC
			if(i%2!=windingOrder) {
				side_indices[i+0] = v;
				side_indices[i+1] = v + 1;
				side_indices[i+2] = v + 2;
			}else{
				side_indices[i+2] = v;
				side_indices[i+1] = v + 1;
				side_indices[i+0] = v + 2;
			}
			v++;
		}
		/*** Finish Generating Sides ***/
		
		/*** Concat Front + Side ***/		
		List<Vector3> all_vertices = new List<Vector3>(front_vertices);
		List<int> all_indices = new List<int>(front_indices);
		
		all_vertices.AddRange(side_vertices);
		for(int i = 0; i < side_indices.Length; i++)
			side_indices[i] += front_vertices.Length;
		
		all_indices.AddRange(side_indices);
		/*** Finish Front and Side Concatenation ***/
		
		GameObject f_go = new GameObject();
		if(useTag)
			f_go.tag = tag;		
		f_go.AddComponent<MeshFilter>();
		f_go.GetComponent<MeshFilter>().sharedMesh = new Mesh();
		Mesh mesh = f_go.GetComponent<MeshFilter>().sharedMesh;
		f_go.AddComponent<MeshRenderer>();
		mesh.name = "SideMesh";
		mesh.Clear();
		mesh.vertices = all_vertices.ToArray();
        mesh.triangles = all_indices.ToArray();
		mesh.uv = CalculateUVs(all_vertices.ToArray(), uvScale);
		mesh.RecalculateNormals();
        mesh.RecalculateBounds();
       
        // Set up game object with mesh;
       	f_go.GetComponent<MeshFilter>().sharedMesh = mesh;
		f_go.GetComponent<MeshRenderer>().material = material;
	
		if(generateCollider)
		{
			f_go.AddComponent<MeshCollider>();
			
			if(applyRigidbody)
			{
				Rigidbody rigidbody = f_go.AddComponent<Rigidbody>();
			
				if( (convexity == Polygon.ConcaveCounterClockwise || convexity == Polygon.ConcaveClockwise) && forceConvex == false)
					f_go.GetComponent<MeshCollider>().convex = false;
				else
					f_go.GetComponent<MeshCollider>().convex = true;

				rigidbody.mass = 25f;
				rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
				
				if(useGravity)
					rigidbody.useGravity = true;
				else
					rigidbody.useGravity = false;
				
				if(isKinematic)
					rigidbody.isKinematic = true;
				else
					rigidbody.isKinematic = false;
			}
		}
		generatedMeshes.Add (f_go);
		CleanUp();
	}
	
	/// <summary>
	/// Utility Methods
	/// </summary>		
	Vector2[] CalculateUVs(Vector3[] v, Vector2 uvScale)
	{
		Vector2[] uvs = new Vector2[v.Length];
		for(var i = 0; i < v.Length; i++)
		{
			uvs[i] = new Vector2( (v[i].x * uvScale.x), (v[i].y * uvScale.y));
		}
		return uvs;
	}
	
	
	// http://paulbourke.net/geometry/clockwise/index.html
	Polygon Convexity(Vector2[] p)
	{
		bool isConcave = false;
		
		int n = p.Length;
	   	int i,j,k;
		double wind = 0;
		int flag = 0;
		double z;
		
		if (n < 3)
		return(0);
		
		for (i=0;i<n;i++) {
			j = (i + 1) % n;
			k = (i + 2) % n;
			z  = (p[j].x - p[i].x) * (p[k].y - p[j].y);
			z -= (p[j].y - p[i].y) * (p[k].x - p[j].x);
			wind += z;
			if (z < 0)
				flag |= 1;
			else if (z > 0)
				flag |= 2;
						
			if (flag == 3)
				isConcave = true;
		}
		
		if(isConcave == true || flag == 0) 
		{
			if(wind >= 0)
				return Polygon.ConcaveCounterClockwise;
			else
				return Polygon.ConcaveClockwise;
		}
		else
		{
			if(wind >= 0)
				return(Polygon.ConvexCounterClockwise);
			else
				return(Polygon.ConvexClockwise);
		}
	}
}
