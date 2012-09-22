/// <summary>
/// Parabox LLC 
/// Support Email - karl@paraboxstudios.com
/// paraboxstudios.com
///	## Version 1.4
///
/// Draws a mesh from user input.
/// 
/// Known Issues:
/// -	No hole support.
///	-	No fill rule implementation, meaning on the (rare) occasion
///		that a mesh doesn't get it's winding squared away it will fill
///		improperly.
/// 
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Draw : MonoBehaviour {

	List<Vector2> userPoints = new List<Vector2>();
	
	/// <summary>
	/// Draw Styles
	/// </summary>
	public DrawStyle drawStyle = DrawStyle.Continuous;
	public int maxVertices = 4;		// Only applies to non-continuous drawing
	public float samplingRate = .1f;

	/// <summary>
	/// Mesh In Progress
	/// </summary>
	public bool drawMeshInProgress = true;
	public bool useDistanceCheck = false;
	public float maxDistance = 5f;
	public float closingDistance = .5f;
	public float lineWidth = .1f;
	public bool showPointMarkers = false;
	public GameObject pointMarker;
	private List<GameObject> pointMarkers = new List<GameObject>();
	private List<Rect> ignoreRect = new List<Rect>();

	///Final Mesh Settings
	public Material material;
	public int maxAllowedObjects = 4;
	public bool generateMeshCollider = true;
	public bool generateSide = false;
	public bool forceConvex = false;
	public bool applyRigidbody = true;
	public bool areaRelativeMass = true;
	public float massModifier = 1f;
	public float mass = 25f;
	public bool useGravity = true;
	public bool isKinematic = false;
	public bool useTag = false;
	public string tagVal = "drawnMesh";
	public float zPosition = 0;
	public Vector2 uvScale = new Vector2(1f, 1f);
	public string meshName = "Drawn Mesh";
	public bool generateBoxColliders = false;
	public ColliderStyle colliderStyle = ColliderStyle.BoxCollider;

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
		ContinuousClosingDistance,
		PointMaxVertice,
		PointClosingDistance
	}
	
	public enum ColliderStyle {
		BoxCollider,
		MeshCollider,
		None
	}

	void Start() {
		mainCamera = Camera.main;
	}

	void Update() {
		// If mouse is in an ignoreRect, don't affect  mesh drawing.
		if(ignoreRect.Count > 0) {
			foreach(Rect r in ignoreRect) {
				if(r.Contains(Input.mousePosition))
					return;
			}
		}

		if(drawStyle == DrawStyle.PointMaxVertice || drawStyle == DrawStyle.PointClosingDistance) {
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
				
				userPoints[userPoints.Count - 1] = worldPos;

				RefreshPreview();
			}
	
			if(Input.GetMouseButtonUp(0)) {
				placingPoint = false;
				
				// CLosing Distance
				if(drawStyle == DrawStyle.PointClosingDistance && userPoints.Count > 2) {
					if( (userPoints[0] - userPoints[userPoints.Count - 1]).sqrMagnitude < closingDistance ) {
						userPoints.RemoveAt(userPoints.Count-1);
						DrawFinalMesh(userPoints.ToArray());
					}
				}

				// Max Vertice
				if(userPoints.Count >= maxVertices && drawStyle == DrawStyle.PointMaxVertice) {
					DrawFinalMesh(userPoints.ToArray());
				}
			}
		}
		else
		if(drawStyle == DrawStyle.Continuous || drawStyle == DrawStyle.ContinuousClosingDistance)
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
						
						if(drawStyle == DrawStyle.ContinuousClosingDistance && userPoints.Count > 2) {
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
	
	public void CleanUp() {
		userPoints.Clear();
		DestroyPointMarkers();
		DestroyLineRenderer();
		DestroyTempGameObject();
	}
	
	void DestroyPointMarkers() {
		for(int i = 0; i < pointMarkers.Count; i++) {
			Destroy(pointMarkers[i]);
		}
		pointMarkers.Clear();
	}

	void DestroyLineRenderer() {
		if(gameObject.GetComponent<LineRenderer>() != null)
			Destroy(gameObject.GetComponent<LineRenderer>());
	}
	
	void AddPoint(Vector3 position) {
		if(showPointMarkers)
			pointMarkers.Add( (GameObject)GameObject.Instantiate(pointMarker, position, new Quaternion(0f,0f,0f,0f)) );

		userPoints.Add(position);
		
		if(userPoints.Count > 1) {
			if(drawMeshInProgress)
				DrawTempMesh(userPoints.ToArray());
			
			DrawLineRenderer(userPoints.ToArray());
		}
	}
	
	void RefreshPreview() {
		if(showPointMarkers)
			pointMarkers[pointMarkers.Count-1].transform.position = userPoints[userPoints.Count-1];

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
		
		Polygon convexity = Convexity(points, false);
		
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
	
	public void DestroyAllGeneratedMeshes()
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
		if(points.Length < 3 || ((points[0] - points[points.Length - 1]).sqrMagnitude > (maxDistance) && useDistanceCheck) ) {
			CleanUp();	
			return;
		}
		ChechMaxMeshes();
		
		// Check for self intesection 
		if(SelfIntersectTest(new List<Vector2>(points))) {
			CleanUp();
			return;
		}

		Polygon convexity = Convexity(points, true);

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
			side_vertices.Add( new Vector3( points[i].x, points[i].y, -10) );
		}	
        
		side_vertices.Add( new Vector3( points[0].x, points[0].y, 10) );
		side_vertices.Add( new Vector3( points[0].x, points[0].y, -10) );
		
		// +6 connects it to the first 2 verts
		int[] side_indices = new int[(side_vertices.Count*3)];
		
		windingOrder = 1; // assume counter-clockwise, cause y'know, we set it that way
		
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
		List<Vector3> all_vertices = new List<Vector3>();
		List<int> all_indices = new List<int>();
		if(generateSide) {
			all_vertices = new List<Vector3>(front_vertices);
			all_indices = new List<int>(front_indices);
			
			all_vertices.AddRange(side_vertices);
			for(int i = 0; i < side_indices.Length; i++)
				side_indices[i] += front_vertices.Length;
			
			all_indices.AddRange(side_indices);
		}
		/*** Finish Front and Side Concatenation ***/
		
		/*** Make Side Collider Mesh Seperately ***/
		Mesh col = new Mesh();
		if(!generateSide) {
			col.Clear();
			col.vertices = side_vertices.ToArray();
			col.triangles = side_indices;
			col.RecalculateBounds();
			col.Optimize();
		}
		else
			Destroy(col);
		/*** End Side Collider Mesh ***/

		GameObject f_go = new GameObject();
		f_go.name = meshName;

		if(useTag)
			f_go.tag = tagVal;	

		f_go.AddComponent<MeshFilter>();
		f_go.GetComponent<MeshFilter>().sharedMesh = new Mesh();
		Mesh mesh = f_go.GetComponent<MeshFilter>().sharedMesh;
		f_go.AddComponent<MeshRenderer>();
		mesh.name = "Mesh";
		mesh.Clear();

		if(generateSide) {
			mesh.vertices = all_vertices.ToArray();
	        mesh.triangles = all_indices.ToArray();
			mesh.uv = CalculateUVs(all_vertices.ToArray(), uvScale);
		} else {
			mesh.vertices = front_vertices;
	        mesh.triangles = front_indices;
			mesh.uv = CalculateUVs(front_vertices, uvScale);
		}
		
		mesh.Optimize();
		mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Set up game object with mesh;
       	f_go.GetComponent<MeshFilter>().sharedMesh = mesh;
		f_go.GetComponent<MeshRenderer>().material = material;

		switch(colliderStyle)	
		{
			case ColliderStyle.MeshCollider:
				f_go.AddComponent<MeshCollider>();
				
				if(!generateSide)
					f_go.GetComponent<MeshCollider>().sharedMesh = col;

				if(applyRigidbody)
				{
					Rigidbody rigidbody = f_go.AddComponent<Rigidbody>();
				
					if( (convexity == Polygon.ConcaveCounterClockwise || convexity == Polygon.ConcaveClockwise) && forceConvex == false)
						f_go.GetComponent<MeshCollider>().convex = false;
					else
						f_go.GetComponent<MeshCollider>().convex = true;

					if(areaRelativeMass)
						rigidbody.mass = Triangulator.Area(points) * massModifier;
					else
						rigidbody.mass = mass;

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
			break;

			case ColliderStyle.BoxCollider:
				if(applyRigidbody)
				{
					BoxCollider parent_collider = f_go.AddComponent<BoxCollider>();

					// the parent collider - don't allow it to be seen, just use it for
					// mass and other settings
					parent_collider.size = new Vector3(.1f, .1f, .1f);

					Rigidbody rigidbody = f_go.AddComponent<Rigidbody>();

					if(areaRelativeMass)
						rigidbody.mass = Triangulator.Area(points) * massModifier;
					else
						rigidbody.mass = mass;

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

				for(int i = 0; i < points.Length; i++)
				{
					float x1, x2, y1, y2;
					x1 = points[i].x;
					y1 = points[i].y;

					if(i > points.Length-2) {
						x2 = points[0].x;
						y2 = points[0].y;
					}
					else {
						x2 = points[i+1].x;
						y2 = points[i+1].y;			
					}

					GameObject go = new GameObject();
					go.name = "BoxCollider" + i;
					go.AddComponent<BoxCollider>();
					
					go.transform.position = new Vector3( ((x1 + x2)/2f), ((y1+y2)/2f), zPosition);

					Vector2 vectorLength = new Vector2( Mathf.Abs(x1 - x2),  Mathf.Abs(y1 - y2) );
					
					float length = Mathf.Sqrt( ( Mathf.Pow((float)vectorLength.x, 2f) + Mathf.Pow(vectorLength.y, 2f) ) );
					float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;

					go.transform.localScale = new Vector3(length, .0001f, 1f);
					go.transform.rotation = Quaternion.Euler( new Vector3(0f, 0f, angle) );

					go.transform.parent = f_go.transform;
				}
			break;

			default:
			break;

		}

		generatedMeshes.Add (f_go);
		CleanUp();
	}

	///	<summary>
	///	OBJ Export methods
	///	</summary>
	public string ExportOBJ(string path, MeshFilter mf)
	{
		if(File.Exists(path)) {
			int i = 0;
			while(File.Exists(path)) {
				path = path.Replace(".obj","");
				path = path + i + ".obj";
				i++;
			}
		}
		ObjExporter.MeshToFile(mf, path);
		return path;
	}

	// Export all meshes
	public string ExportOBJ(string path)
	{
		for(int i = 0; i < generatedMeshes.Count; i++)
			ExportOBJ(path, i);
		
		return path;
	}

	public string ExportOBJ(string path, int index)
	{
		if(index < generatedMeshes.Count)
			return ExportOBJ(path, generatedMeshes[index]);
		else
			return "Index out of bounds.";
	}

	public string ExportOBJ(string path, GameObject go)
	{
		if(go.GetComponent<MeshFilter>())
			return ExportOBJ(path, go.GetComponent<MeshFilter>());
		else
			return "No mesh filter found.";
	}

	public string ExportCurrent(string path)
	{
		return ExportOBJ(path, generatedMeshes[generatedMeshes.Count-1]);
	}

	/// <summary>
	///	Ingore rect methods.
	/// </summary>
	public void IgnoreRect(Rect rect)
	{
		ignoreRect.Add(rect);
	}

	public void ClearIgnoreRects()
	{
		ignoreRect.Clear();
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
	Polygon Convexity(Vector2[] p, bool final)
	{
//		string cheese = "";
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

//			cheese += z + "		" + flag + "\n";
		}
		
		Polygon convexity;
		if(isConcave == true || flag == 0) 
		{
			if(wind > 0)
				convexity = Polygon.ConcaveCounterClockwise;
			else
				convexity = Polygon.ConcaveClockwise;
		}
		else
		{
			if(wind > 0)
				convexity = Polygon.ConvexCounterClockwise;
			else
				convexity = Polygon.ConvexClockwise;
		}

//		if(final)
//			Debug.Log(convexity + "\n" + "winding  " + wind + "\n" + cheese);

		return convexity;
	}

	// http://www.gamedev.net/topic/548477-fast-2d-polygon-self-intersect-test/
	public bool SelfIntersectTest(List<Vector2> vertices)
	{
	    for (int i = 0; i < vertices.Count; ++i)
	    {
	        if (i < vertices.Count - 1)
	        {
	            for (int h = i + 1; h < vertices.Count; ++h)
	            {
	                // Do two vertices lie on top of one another?
	                if (vertices[i] == vertices[h])
	                {
	                    return true;
	                }
	            }
	        }

	        int j = (i + 1) % vertices.Count;
	        Vector2 iToj = vertices[j] - vertices[i];
	        Vector2 iTojNormal = new Vector2(iToj.y, -iToj.x);
	        // i is the first vertex and j is the second
	        int startK = (j + 1) % vertices.Count;
	        int endK = (i - 1 + vertices.Count) % vertices.Count;
	        endK += startK < endK ? 0 : startK + 1;
	        int k = startK;
	        Vector2 iTok = vertices[k] - vertices[i];
	        bool onLeftSide = Vector2.Dot(iTok, iTojNormal) >= 0;
	        Vector2 prevK = vertices[k];
	        ++k;
	        for (; k <= endK; ++k)
	        {
	            int modK = k % vertices.Count;
	            iTok = vertices[modK] - vertices[i];
	            if (onLeftSide != Vector2.Dot(iTok, iTojNormal) >= 0)
	            {
	                Vector2 prevKtoK = vertices[modK] - prevK;
	                Vector2 prevKtoKNormal = new Vector2(prevKtoK.y, -prevKtoK.x);
	                if ((Vector2.Dot(vertices[i] - prevK, prevKtoKNormal) >= 0) != (Vector2.Dot(vertices[j] - prevK, prevKtoKNormal) >= 0))
	                {
	                    return true;
	                }
	            }
	            onLeftSide = Vector2.Dot(iTok, iTojNormal) > 0;
	            prevK = vertices[modK];
	        }
	    }
	    return false;
	}
}
