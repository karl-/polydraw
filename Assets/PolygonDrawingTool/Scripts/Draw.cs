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
using System.Linq;
using System.IO;

public class Draw : MonoBehaviour
{
#region MEMBERS
	List<Vector2> userPoints = new List<Vector2>();
	
	public DrawStyle drawStyle = DrawStyle.Continuous;		///< The DrawStyle to be used.
	public int maxVertices = 4;								///< Maximum amount of vertices allowed per geometry.
	public float samplingRate = 30f;						///< How many point samples to read per second.  Only applies to Continuous DrawStyles.


	public bool drawMeshInProgress = true;					///< Should a preview mesh be constructed as the user draws?
	public bool useDistanceCheck = false;					///< If true, the final user point must be less than closingDistance away from origin point in order for a mesh to be drawn.  Does not affect preview meshes.
	public float maxDistance = 5f;							///< If #useDistanceCheck is set to true, the final point must be within this distance of the origin point in order for a final mesh to constructed.  When using #DrawStyle ::ContinuousClosingDistance, this is automatically set to true.
	public float closingDistance = .5f;						///< If #drawStyle is set to ContinuousClosingDistance (see #DrawStyle), the mesh will automatically finalize itself once a point is detected within the closingDistance of the origin point.
	public float lineWidth = .1f;							///< If #lineRenderer is left unassigned, this is the width that will be used for an automatically generated LineRenderer.
	public bool showPointMarkers = false;					///< If true, PolyDraw will instantiate #pointMarker's at vertex points while recieving input. 
	public GameObject pointMarker;							///< GameObjects to be placed at vertex points as the input is recieved.  See also #showPointMarkers.
	private List<GameObject> pointMarkers = new List<GameObject>();
	private List<Rect> ignoreRect = new List<Rect>();
	public LineRenderer lineRenderer;


	///Final Mesh Settings
	public Material material;
	public int maxAllowedObjects = 4;
	public bool generateMeshCollider = true;

	// Sides
	public bool generateSide = false;
	public float sideLength = 5f;
	public Anchor anchor = Anchor.Center;
	public float faceOffset = 0f;							///< This value is used to offset the anchor.  As an example, a faceOffset of 1f with a #zPosition of 0f would set the front face at Vector3(x, y, 1f).  With #SideAnchor Center and a faceOffset of 0, the front face is set to exactly 1/2 negative distance (towards the camera) of sideLength.   
	public Material sideMaterial;

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
	public bool manualColliderDepth = false;
	public float colDepth = 5f;

	bool placingPoint = false;
	Vector3 previousMousePosition;
	private float timer = 0f;
	private List<GameObject> generatedMeshes = new List<GameObject>();
	private int windingOrder; // Positive = CC , Negative = CW
	public Camera inputCamera;
	private float boxColliderSize = .01f;

#endregion

#region ENUM
	enum PolygonType {
		ConvexClockwise,
		ConvexCounterClockwise,
		ConcaveClockwise,
		ConcaveCounterClockwise
	}

	/**
	 *  \brief Dictates how user input is interpreted.
	 *	See also #drawStyle.
	 */
	public enum DrawStyle {
		Continuous,
		ContinuousClosingDistance,		///< Input read at samples (#samplingRate) per second, and finalizes mesh either on input ceasing or based on a distance check.  Also see #useDistanceCheck.
		PointMaxVertex,
		PointClosingDistance
	}
	
	public enum Anchor {
		Center,
		Back,
		Front
	}

	public enum ColliderStyle {
		BoxCollider,
		MeshCollider,
		None
	}
#endregion

#region INITIALIZATION
	void Start() {
		if(inputCamera == null)
			inputCamera = Camera.main;

		// If we're not rendering with the camera used for input, set the ortho cam coordinates to match
		// the coordinates drawn at the Z position of the perspective camera.
		if(inputCamera != Camera.main)
			SetOrthographioCameraDimensions(Camera.main, zPosition);
	}
#endregion

#region UPDATE
	void Update()
	{
		// If mouse is in an ignoreRect, don't affect  mesh drawing.
		if(ignoreRect.Count > 0)
		{
			foreach(Rect r in ignoreRect)
			{
				if(r.Contains(Input.mousePosition))
					return;
			}
		}

		switch(drawStyle)
		{
			case DrawStyle.PointMaxVertex:
			case DrawStyle.PointClosingDistance:
				if(Input.GetMouseButtonDown(0))
				{
					Vector3 worldPos = inputCamera.ScreenToWorldPoint(Input.mousePosition);
					worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);

					// if(generateSide) {
					// 	// switch(anchor)
					// 	// {
					// 	// 	case Anchor.Center:
					// 	// 		DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset - sideLength/2f) );
					// 	// 		break;
					// 	// 	case Anchor.Front:
					// 	// 		DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset - sideLength/2f) );
					// 	// 		break;
					// 	// 	case Anchor.Back:
					// 	// 		DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset + sideLength) );
					// 	// 		break;
					// 	// }
					// 	DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset) );

					// } else
					// 	DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset) );

					AddPoint(worldPos);
					
					placingPoint = true;
				}
				
				if(Input.mousePosition != previousMousePosition && placingPoint)
				{			
					previousMousePosition = Input.mousePosition;
					Vector3 worldPos = inputCamera.ScreenToWorldPoint(Input.mousePosition);
					worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);
					
					userPoints[userPoints.Count - 1] = worldPos;

					RefreshPreview();
				}
		
				if(Input.GetMouseButtonUp(0))
				{
					placingPoint = false;
					
					// CLosing Distance
					if(drawStyle == DrawStyle.PointClosingDistance && userPoints.Count > 2)
					{
						if( (userPoints[0] - userPoints[userPoints.Count - 1]).sqrMagnitude < closingDistance )
						{
							userPoints.RemoveAt(userPoints.Count-1);
							DrawFinalMesh(userPoints);
						}
					}

					// Max Vertice
					if(userPoints.Count >= maxVertices && drawStyle == DrawStyle.PointMaxVertex) {
						DrawFinalMesh(userPoints);
					}
				}
				break;

			case DrawStyle.Continuous:
			case DrawStyle.ContinuousClosingDistance:
				if(Input.GetMouseButton(0))
				{
					if(timer > 1f/samplingRate || Input.GetMouseButtonDown(0))
					{
						timer = 0f;
						
						// The triangulation algorithm in use doesn't like multiple verts
						// sharing the same world space, so don't let it happen!
						if(Input.mousePosition != previousMousePosition)
						{			
							previousMousePosition = Input.mousePosition;
							
							Vector3 worldPos = inputCamera.ScreenToWorldPoint(Input.mousePosition);
							worldPos = new Vector3(worldPos.x, worldPos.y, zPosition);
						
							AddPoint(worldPos);
							
							if(drawStyle == DrawStyle.ContinuousClosingDistance && userPoints.Count > 2)
							{
								if( (userPoints[0] - userPoints[userPoints.Count - 1]).sqrMagnitude < (closingDistance) )
									DrawFinalMesh(userPoints);
							}//drawstyle
						}//mousepos check
					}//mousedown			
						
					timer += 1 * Time.deltaTime;
				}
					
				if(Input.GetMouseButtonUp(0))
				{
					DrawFinalMesh(userPoints);
					DestroyTempGameObject();
					DestroyLineRenderer();
				}
				break;
		}
	}
#endregion

#region PREVIEW MESH AND LINE RENDERER
	void DrawLineRenderer(Vector3[] v)
	{

		if(lineRenderer == null)
		{	
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.material = new Material (Shader.Find("Particles/Additive"));
			lineRenderer.SetColors(Color.green, Color.green);
			lineRenderer.SetWidth(lineWidth,lineWidth);
		}

		lineRenderer.useWorldSpace = true;

		if(v.Length > 1) {
			lineRenderer.SetVertexCount(v.Length);
			
			for(int i = 0; i < v.Length; i++) {
			//	if(i == v.Length)		// Draws the connecting line to beginning point
			//		gameObject.GetComponent<LineRenderer>().SetPosition(i, new Vector3(v[0].x, v[0].y, zPosition) );
			//	else
					lineRenderer.SetPosition(i, v[i]);
			}
		}
	}

	public void DestroyPointMarkers()
	{
		for(int i = 0; i < pointMarkers.Count; i++)
		{
			Destroy(pointMarkers[i]);
		}
		pointMarkers.Clear();
	}

	public void DestroyLineRenderer()
	{
		// if(gameObject.GetComponent<LineRenderer>() != null)
			// Destroy(gameObject.GetComponent<LineRenderer>());
		if(lineRenderer != null)
		{
			lineRenderer.SetVertexCount(0);		
		}
	}
	
	void AddPoint(Vector3 position)
	{
		if(showPointMarkers)
			pointMarkers.Add( (GameObject)GameObject.Instantiate(pointMarker, position, new Quaternion(0f,0f,0f,0f)) );

		userPoints.Add(position);
		
		RefreshPreview();
	}
	
	void RefreshPreview()
	{
		if(showPointMarkers)
			pointMarkers[pointMarkers.Count-1].transform.position = userPoints[userPoints.Count-1];

		if(userPoints.Count > 1)
		{			
			if(drawMeshInProgress)
				DrawTempMesh(userPoints);

			DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset) );		
		}
	}	
	
	public void CleanUp() {
		userPoints.Clear();
		DestroyPointMarkers();
		DestroyLineRenderer();
		DestroyTempGameObject();
	}

	void DestroyTempGameObject() 
	{
		if(previewGameObject)
			Destroy(previewGameObject);
	}
#endregion
	
#region MESH CREATION
	/// <summary>
	/// Draw Mesh - will use only on GameObject and re-write itself.
	/// </summary>
	GameObject previewGameObject;
	void DrawTempMesh(List<Vector2> points) {		
		if( points.Count < 2 ) {
			CleanUp();
			return;
		}
		
		// Create the mesh
		if(previewGameObject == null) {
			previewGameObject = new GameObject();
			previewGameObject.AddComponent<MeshFilter>();
			previewGameObject.GetComponent<MeshFilter>();
			previewGameObject.AddComponent<MeshRenderer>();
		}
		
		PolygonType convexity = Convexity(points);

		Mesh m, c;
		MeshWithPoints(out m, out c, convexity);

		previewGameObject.GetComponent<MeshFilter>().sharedMesh = m;
		Material[] mats = (generateSide) ? 
			new Material[2] {material, sideMaterial} :
			new Material[1] { material };
		previewGameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;
	}
	
	/// <summary>
	/// Draws the final mesh, creating a new GameObject.
	/// </summary>
	/// <param name='points'>
	/// A list of X,Y points in local space to be translated into vertex data.
	/// </param>
	void DrawFinalMesh(List<Vector2> points)
	{
		DestroyTempGameObject();

		if(points.Count < 3 || ((points[0] - points[points.Count - 1]).sqrMagnitude > maxDistance && useDistanceCheck) )
		{
			CleanUp();	
			return;
		}

		// Check for self intesection 
		if(SelfIntersectTest(points))
		{
			CleanUp();
			return;
		}

		// If we're over max, delete the earliest drawn mesh
		CheckMaxMeshes();

		// Calculate this here because the collision code needs it too
		PolygonType convexity = Convexity(points);

		// graphics = any mesh that you can see, collision = the side mesh
		Mesh graphics, collision;

		MeshWithPoints(out graphics, out collision, convexity);
				
		GameObject finalMeshGameObject = new GameObject();
		finalMeshGameObject.name = meshName;

		if(useTag)
			finalMeshGameObject.tag = tagVal;	

		finalMeshGameObject.AddComponent<MeshFilter>();
		finalMeshGameObject.GetComponent<MeshFilter>().sharedMesh = graphics;
		finalMeshGameObject.AddComponent<MeshRenderer>();

		Material[] mats = (generateSide) ? 
			new Material[2] {material, sideMaterial} :
			new Material[1] { material };

		finalMeshGameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;

		switch(colliderStyle)	
		{
			case ColliderStyle.MeshCollider:
				finalMeshGameObject.AddComponent<MeshCollider>();
				
				finalMeshGameObject.GetComponent<MeshCollider>().sharedMesh = collision;

				if(applyRigidbody)
				{
					Rigidbody rigidbody = finalMeshGameObject.AddComponent<Rigidbody>();
				
					if( (convexity == PolygonType.ConcaveCounterClockwise || convexity == PolygonType.ConcaveClockwise) && forceConvex == false)
						finalMeshGameObject.GetComponent<MeshCollider>().convex = false;
					else
						finalMeshGameObject.GetComponent<MeshCollider>().convex = true;

					if(areaRelativeMass)
						rigidbody.mass = Mathf.Abs(Triangulator.Area(points.ToArray()) * massModifier);
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
					BoxCollider parent_collider = finalMeshGameObject.AddComponent<BoxCollider>();

					// the parent collider - don't allow it to be seen, just use it for
					// mass and other settings
					parent_collider.size = new Vector3(.01f, .01f, .01f);

					Rigidbody rigidbody = finalMeshGameObject.AddComponent<Rigidbody>();

					if(areaRelativeMass)
						rigidbody.mass = Triangulator.Area(points.ToArray()) * massModifier;
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

				if(!manualColliderDepth)
					colDepth = sideLength;

				float zPos_collider = zPosition + faceOffset;
				switch(anchor)
				{
					case Anchor.Front:
						zPos_collider += sideLength/2f;
						break;
					case Anchor.Back:
						zPos_collider -= sideLength/2f;
						break;
					default:
						break;
				}

				for(int i = 0; i < points.Count; i++)
				{
					float x1, x2, y1, y2;
					x1 = points[i].x;
					y1 = points[i].y;

					if(i > points.Count-2) {
						x2 = points[0].x;
						y2 = points[0].y;
					}
					else {
						x2 = points[i+1].x;
						y2 = points[i+1].y;			
					}

					GameObject boxColliderObj = new GameObject();
					boxColliderObj.name = "BoxCollider" + i;
					boxColliderObj.AddComponent<BoxCollider>();
					
					boxColliderObj.transform.position = new Vector3( ((x1 + x2)/2f), ((y1+y2)/2f), zPos_collider);

					Vector2 vectorLength = new Vector2( Mathf.Abs(x1 - x2),  Mathf.Abs(y1 - y2) );
					
					float length = Mathf.Sqrt( ( Mathf.Pow((float)vectorLength.x, 2f) + Mathf.Pow(vectorLength.y, 2f) ) );
					float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;

					boxColliderObj.transform.localScale = new Vector3(length, boxColliderSize, colDepth);
					boxColliderObj.transform.rotation = Quaternion.Euler( new Vector3(0f, 0f, angle) );

					boxColliderObj.transform.parent = finalMeshGameObject.transform;
				}
			break;

			default:
			break;

		}

		generatedMeshes.Add (finalMeshGameObject);
		CleanUp();
	}

	// Assumes local space.  Returns graphics mesh in the following submesh order:
	// 0 - Front face
	// 1 - Sides (optional)
	// 2 - Back face (planned)
	void MeshWithPoints(out Mesh m, out Mesh c, PolygonType convexity)
	{
		m = new Mesh();
		c = new Mesh();

		Vector2[] points = userPoints.ToArray();
		float zOrigin;
		float halfSideLength = sideLength/2f;
		switch(anchor)
		{
			case Anchor.Front:
			 	zOrigin = zPosition + faceOffset + sideLength / 2f;
				break;
		
			case Anchor.Back:
			 	zOrigin = zPosition + faceOffset - sideLength / 2f;
				break;

			case Anchor.Center:
			default:
			 	zOrigin = zPosition + faceOffset;
				break;	
		}

		if(convexity == PolygonType.ConcaveClockwise || convexity == PolygonType.ConvexClockwise)
			Array.Reverse(points);
		
		/*** Generate Front Face ***/
		Triangulator tr = new Triangulator(userPoints.ToArray());
		int[] front_indices = tr.Triangulate();
	   
		// Create the Vector3 vertices
		List<Vector3> front_vertices = new List<Vector3>(VerticesWithPoints(userPoints, zOrigin - halfSideLength));

		List<Vector2> front_uv = ListMultiply(userPoints, uvScale);

		/*** Finish Front Face ***/
		
		/*** Generate Sides ***/
		List<Vector3> side_vertices = new List<Vector3>();
		
		for (int i=0; i < points.Length; i++) {
			side_vertices.Add( new Vector3( points[i].x, points[i].y, zOrigin + halfSideLength) );
			side_vertices.Add( new Vector3( points[i].x, points[i].y, zOrigin - halfSideLength) );
		}
		
		// these sit right on the first two.  they don't share cause that would screw with
		// the lame way uvs are made.
		side_vertices.Add( new Vector3( points[0].x, points[0].y, zOrigin + halfSideLength) );
		side_vertices.Add( new Vector3( points[0].x, points[0].y, zOrigin - halfSideLength) );
		
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

		List<Vector2> side_uv = new List<Vector2>(CalcSideUVs(side_vertices));

		m.Clear();
		m.vertices = generateSide ? front_vertices.Concat(side_vertices).ToArray() : front_vertices.ToArray();
		if(generateSide) {
			m.subMeshCount = 2;
			m.SetTriangles(front_indices, 0);
			m.SetTriangles(ShiftTriangles(side_indices, front_vertices.Count), 1);
		} else {
			m.triangles = front_indices;
		}
		m.uv = generateSide ? front_uv.Concat(side_uv).ToArray() : front_uv.ToArray();
		m.RecalculateNormals();
		m.RecalculateBounds();
		m.Optimize();

		c.Clear();
		c.vertices = side_vertices.ToArray();
		c.triangles = side_indices;
		c.uv = side_uv.ToArray();
		c.RecalculateNormals();
		c.RecalculateBounds();
	}
#endregion

#region MESH UTILITY

	void CheckMaxMeshes() 
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
#endregion

#region OBJ EXPORT
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

	public string ExportOBJ(string path, GameObject previewGameObject)
	{
		if(previewGameObject.GetComponent<MeshFilter>())
			return ExportOBJ(path, previewGameObject.GetComponent<MeshFilter>());
		else
			return "No mesh filter found.";
	}

	public string ExportCurrent(string path)
	{
		return ExportOBJ(path, generatedMeshes[generatedMeshes.Count-1]);
	}
#endregion

#region IGNORE RECTS
	/// <summary>
	///	Ignore rect methods.
	/// </summary>
	public void IgnoreRect(Rect rect)
	{
		ignoreRect.Add(rect);
	}

	public void ClearIgnoreRects()
	{
		ignoreRect.Clear();
	}
#endregion

#region UV
	
	// A little hacky, yes, but it works well enough to pass
	Vector2[] CalcSideUVs(List<Vector3> v)
	{
		// we konw that vertices are generated in rows, in a ccwise manner.
		// this method figures out dist between rows, and uses the known 
		// side length to generate properly scaled uvs.
		
		Vector2[] uvs = new Vector2[v.Count];

		float curX = 0f;

		uvs[0] = new Vector2(0f, v[0].z);
		uvs[1] = new Vector2(0f, v[1].z);

		for(int i = 2; i < v.Count; i+=2)
		{
			curX += Vector3.Distance(v[i], v[i-2]);

			uvs[i+0] = new Vector2(curX, v[i+0].z);
			uvs[i+1] = new Vector2(curX, v[i+1].z);
		}

		return uvs;
	}

	List<Vector2> ListMultiply(List<Vector2> uvs, Vector2 mult)
	{
		for(int i = 0; i < uvs.Count; i++)
			uvs[i].Scale(mult);
		return uvs;
	}
#endregion

#region MESH MATH UTILITY
	public Vector3[] VerticesInWorldSpace(List<Vector2> points, float zPos)
	{
		Vector3[] v = new Vector3[points.Count];

		for(int i = 0; i < points.Count; i++)
			v[i] = transform.TransformPoint(new Vector3(points[i].x, points[i].y, zPos));
		
		return v;			
	}

	public Vector3[] VerticesWithPoints(List<Vector2> points, float zPos)
	{
		Vector3[] v = new Vector3[points.Count];
		
		for(int i = 0; i < points.Count; i++)
			v[i] = new Vector3(points[i].x, points[i].y, zPos);
		return v;
	}

	public int[] ShiftTriangles(int[] tris, int offset)
	{
		int[] shifted = new int[tris.Length];

		for(int i = 0; i < shifted.Length; i++)
			shifted[i] = tris[i] + offset;

		return shifted;
	}
	
	// http://paulbourke.net/geometry/clockwise/index.html
	PolygonType Convexity(List<Vector2> p)
	{
		bool isConcave = false;
		
		int n = p.Count;
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
		
		PolygonType convexity;
		if(isConcave == true || flag == 0) 
		{
			if(wind > 0)
				convexity = PolygonType.ConcaveCounterClockwise;
			else
				convexity = PolygonType.ConcaveClockwise;
		}
		else
		{
			if(wind > 0)
				convexity = PolygonType.ConvexCounterClockwise;
			else
				convexity = PolygonType.ConvexClockwise;
		}

		return convexity;
	}

	// http://www.gamedev.net/topic/548477-fast-2d-PolygonType-self-intersect-test/
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
#endregion

#region CAMERA
	
	public void SetOrthographioCameraDimensions(Camera perspCam, float zPos)
	{
		Vector3 tr = perspCam.ScreenToWorldPoint(new Vector3(perspCam.pixelWidth, perspCam.pixelHeight, zPos - perspCam.transform.position.z));
		Vector3 bl = perspCam.ScreenToWorldPoint(new Vector3(0, 0, zPos - perspCam.transform.position.z));

		inputCamera.orthographic = true;

		// orthographicSize is Y
		inputCamera.orthographicSize = (tr.y - bl.y) / 2f;
	}
#endregion
}
