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
	public bool showPointMarkers = false;					///< If true, PolyDraw will instantiate #pointMarker's at vertex points while recieving input. 
	public GameObject pointMarker;							///< GameObjects to be placed at vertex points as the input is recieved.  See also #showPointMarkers.
	private List<GameObject> pointMarkers = new List<GameObject>();
	private List<Rect> ignoreRect = new List<Rect>();

	public bool drawLineRenderer = true;					///< If true, a LineRenderer will be drawn while taking input.  See also #lineRenderer and #lineWidth.
	public LineRenderer lineRenderer;						///< The LineRenderer to use when drawing.  If left null, a default line renderer will be created.
	public float lineWidth = .1f;							///< If #lineRenderer is left unassigned, this is the width that will be used for an automatically generated LineRenderer.

	public Material material;								///< The material to be applied to the front face of the mesh.
	public int maxAllowedObjects = 4;						///< The maximum amount of meshes allowed on screen at any time.  Meshes will be deleted as new objects are drawn, in the order of oldest to newest.

	// Sides
	public bool generateSide = false;						///< If true, sides will be created along with the front face.
	public float sideLength = 5f;							///< How long the sides will be.
	public Anchor anchor = Anchor.Center;					///< Where is the pivot point of this mesh?  See #Anchor for more information.
	public float faceOffset = 0f;							///< This value is used to offset the anchor.  As an example, a faceOffset of 1f with a #zPosition of 0f would set the front face at Vector3(x, y, 1f).  With #SideAnchor Center and a faceOffset of 0, the front face is set to exactly 1/2 negative distance (towards the camera) of sideLength.   
	public Material sideMaterial;							///< The material to be applied to the sides of the mesh.

	public bool forceConvex = false;						///< If a MeshCollider is used, this can force the collision bounds to convex.
	public bool applyRigidbody = true;						///< If true, a RigidBody will be applied to the final mesh.  Does not apply to preview mesh.
	public bool areaRelativeMass = true;					///< If true, the mass of this final object will be relative to the area of the front face.  Mass is calculated as (area * #massModifier).
	public float massModifier = 1f;							///< The amount to multipy mesh area by when calculating mass.  See also #areaRelativeMass.
	public float mass = 25f;								///< If #areaRelativeMass is false, this value will be used when setting RigidBody mass.  See also #applyRigidbody.
	public bool useGravity = true;							///< If #applyRigidbody is true, this determines if gravity will be applied.
	public bool isKinematic = false;						///< If #applyRigidbody is true, this sets the isKinematic bool.
	public bool useTag = false;								///< If true, the finalized mesh will have its tag set to #tagVal.  Note: Tag must exist prior to assignment.
	public string tagVal = "drawnMesh";						///< The tag to applied to the final mesh.  See also #useTag.
	public float zPosition = 0;								///< The Z position for all vertices.  Z is local to the Draw object, and thus it is recommended that the Draw object remain at world coordinates (0, 0, 0).  By default, this done for you in the Start method.
	public Vector2 uvScale = new Vector2(1f, 1f);			///< The scale to applied when creating UV coordinates.  Different from a material scale property (though that will also affect material layout).
	public string meshName = "Drawn Mesh";					///< What the finalized mesh will be named.

	public ColliderStyle colliderStyle = ColliderStyle.BoxCollider;	///< The #ColliderStyle to be used.
	public bool manualColliderDepth = false;				///< If #ColliderStyle is set to BoxCollider, this can override the #sideLength property to set collision depth.  See also #colDepth.
	public float colDepth = 5f;								///< If #manualColliderDepth is toggled, this value will be used to determine depth of colliders.

	bool placingPoint = false;
	Vector3 previousMousePosition;
	private float timer = 0f;
	private List<GameObject> generatedMeshes = new List<GameObject>();
	private int windingOrder; // Positive = CC , Negative = CW
	public Camera inputCamera;								///< If using a Perspective camera, you will also need an orthographic camera to recieve input.  Assign an orthographic camera here.  Ortho camera Culling Mask should be set to Nothing, with Clear Flags set to Depth Only.
	public float boxColliderSize = .01f;					///< Determines the thickness of the BoxColliders.  See also #ColliderStyle, #colDepth, #manualColliderDepth.

	///< Returns the last drawn object, or null if no objects drawn.
	public GameObject LastDrawnObject { get { return (generatedMeshes.Count > 0) ? generatedMeshes[generatedMeshes.Count-1] : null; } }

#endregion

#region ENUM
	/** Describes polygon convexity and winding order.
	 *
	 */
	public enum PolygonType {
		ConvexClockwise,
		ConvexCounterClockwise,
		ConcaveClockwise,
		ConcaveCounterClockwise
	}

	/** Dictates how user input is interpreted.
	 *	See also #drawStyle.
	 */
	public enum DrawStyle {
		Continuous,						///< Input read at samples (#samplingRate) per second, finalizing mesh on the end of an input event (OnMouseUp, or may be adapted to listen for the end of a touch phase).
		ContinuousClosingDistance,		///< Input read at samples (#samplingRate) per second, and finalizes mesh either on input ceasing or based on a distance check.  Also see #useDistanceCheck.
		PointMaxVertex,					///< Input is read per click, finalizing when a Maximum Vertex amount is met.  See also #maxVertices.
		PointClosingDistance			///< Input is read per click, finalizing when a point is placed within the closing distance (#closingDistance) of the initial point.
	}
	
	/** \enum Anchor 
	 *  \brief If sides are enabled, this determines where the pivot point will be placed in relation to the #zPosition.
	 */
	public enum Anchor {
		Center,							///< Sides will extend in equal distance on both Z axes.
		Back,							///< Sides will extend from origin (#zPosition + #faceOffset) on the negative Z axis only.
		Front 							///< Sides will extend from origin (#zPosition + #faceOffset) on the positive Z axis only.
	}

	/** \enum ColliderStyle
	 *	\brief Determines what style collider to apply to the final object.
	 */
	public enum ColliderStyle {
		BoxCollider,					///< A series of thin box colliders will be created around the edge of the final object.  This allows for concave collisions, though may be slightly more prone to "snagging" on objects.  See also #boxColliderSize (only available via scripting interface).
		MeshCollider,					///< A standard Unity MeshCollider will be applied to this object.  See also #applyRigidbody.
		None							///< A sphere collider will be applied to the object.  <br />Just kidding.  It does what it sounds like, no collider will be applied to the object.
	}
#endregion

#region INITIALIZATION
	void Start() {

		transform.parent = null;
		transform.position = Vector3.zero;

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
					DestroyPreviewMesh();
					DestroyLineRenderer();
				}
				break;
		}
	}
#endregion

#region PREVIEW MESH AND LINE RENDERER
	/**
	 *	\brief Draws the applied LineRenderer in world space.
	 *	To extract world points from #userPoints, see VerticesInWorldSpace().
	 *	@param _vertices The vertices to be fed to the LineRenderer.  Must be in world space.  See also VerticesInWorldSpace().
	 *	@param _connectToStart If true, the LineRenderer will form a complete loop around all points, ending at the first point.  If false, the final point will not connect to the first.
	 */
	public void DrawLineRenderer(Vector3[] _vertices, bool _connectToStart)
	{

		if(lineRenderer == null)
		{	
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.material = new Material (Shader.Find("Particles/Additive"));
			lineRenderer.SetColors(Color.green, Color.green);
			lineRenderer.SetWidth(lineWidth,lineWidth);
		}

		lineRenderer.useWorldSpace = true;

		if(_vertices.Length > 1)
		{
			if(_connectToStart)
				lineRenderer.SetVertexCount(_vertices.Length+1);
			else
				lineRenderer.SetVertexCount(_vertices.Length);
			
			for(int i = 0; i < _vertices.Length; i++)
					lineRenderer.SetPosition(i, _vertices[i]);

			if(_connectToStart)
				lineRenderer.SetPosition(_vertices.Length, _vertices[0]);
		}
	}

	/** 
	 *	\brief Destroys all currently drawn point markers.
	 *	Automatically called when finalizing mesh.  See also CleanUp().
	 */
	public void DestroyPointMarkers()
	{
		for(int i = 0; i < pointMarkers.Count; i++)
		{
			Destroy(pointMarkers[i]);
		}
		pointMarkers.Clear();
	}

	/**
	 *	\brief Sets #lineRenderer vertex count to 0, hiding it from view.
	 */
	public void DestroyLineRenderer()
	{
		if(lineRenderer != null)
			lineRenderer.SetVertexCount(0);		
	}
	
	/**
	 *	\brief Adds a point to the list of vertices to be converted to a mesh.
	 *	This method should be used in place of manually adding points to the internal point cache, as it also handles drawing of the preview mesh, #lineRenderer, and #pointMarker.
	 *	@param _position Point in world space to add to the list of input points.  Assumed to be in world space (use ScreenToWorldPoint(screenPoint) where screenPoint is typically Input.mousePosition )
	 */
	public void AddPoint(Vector3 _position)
	{
		if(showPointMarkers)
			pointMarkers.Add( (GameObject)GameObject.Instantiate(pointMarker, _position, new Quaternion(0f,0f,0f,0f)) );

		userPoints.Add(_position);
		
		RefreshPreview();
	}
	
	/**
	 *	\brief Refreshes the preview mesh, point markers (#pointMarker), and line renderer (#lineRenderer).
	 */
	public void RefreshPreview()
	{
		if(showPointMarkers)
			pointMarkers[pointMarkers.Count-1].transform.position = userPoints[userPoints.Count-1];

		if(userPoints.Count > 1)
		{			
			if(drawMeshInProgress)
				DrawPreviewMesh(userPoints);

			if(drawLineRenderer)
				DrawLineRenderer( VerticesInWorldSpace(userPoints, zPosition + faceOffset), true );		
		}
	}	
	
	/**
	 *	\brief Clears the user point list, destroys all preview materials.  Called during mesh finalization by default.
	 * This should be called any time that a mesh is finalized or cancelled (either due to intersecting lines, or user cancel).
	 */
	public void CleanUp() {
		userPoints.Clear();
		DestroyPointMarkers();
		DestroyLineRenderer();
		DestroyPreviewMesh();
	}

	/**
	 *	\brief Destroy the preview mesh if there is one.
	 */
	public void DestroyPreviewMesh() 
	{
		if(previewGameObject)
			Destroy(previewGameObject);
	}
#endregion
	
#region MESH CREATION

	GameObject previewGameObject;

	/**
	 *	\brief Draws a preview mesh with no collisions from a List<Vector2> of user points.
	 *	This function accepts 2d points and converts them to world point vertices, triangulates, and draws a mesh.  It does not create collisions, and will be deleted on finalizing an object.  See also DestroyPreviewMesh(), CleanUp(), RefreshPreview().
	 *	@param _points List of user points.  Assumed to be in world space (use ScreenToWorldPoint(screenPoint) where screenPoint is typically Input.mousePosition ).
	 */
	public void DrawPreviewMesh(List<Vector2> _points) {		
		if( _points.Count < 2 ) {
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
		
		PolygonType convexity = Convexity(_points);

		Mesh m, c;
		MeshWithPoints(out m, out c, convexity);

		previewGameObject.GetComponent<MeshFilter>().sharedMesh = m;
		Material[] mats = (generateSide) ? 
			new Material[2] {material, sideMaterial} :
			new Material[1] { material };
		previewGameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;
	}
	
	/**
	 *	\brief Creates a mesh and applies all collisions and physics components.
	 *	This method accepts user points and creates a new gameObject with a mesh composed from point data.  As opposed to DrawPreviewMesh(), this function will check against all user specified rules.  If conditions are met, CheckMaxMeshes() is called, and the new gameObject is added to the internal cache of all PolyDraw created objects.
	 *	@param _points List of user points.  Assumed to be in world space (use ScreenToWorldPoint(screenPoint) where screenPoint is typically Input.mousePosition ).
	 */
	public void DrawFinalMesh(List<Vector2> _points)
	{
		DestroyPreviewMesh();

		if(_points.Count < 3 || ((_points[0] - _points[_points.Count - 1]).sqrMagnitude > maxDistance && useDistanceCheck) )
		{
			CleanUp();	
			return;
		}

		// Check for self intesection 
		if(SelfIntersectTest(_points))
		{
			CleanUp();
			return;
		}

		// If we're over max, delete the earliest drawn mesh
		CheckMaxMeshes();

		// Calculate this here because the collision code needs it too
		PolygonType convexity = Convexity(_points);

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
						rigidbody.mass = Mathf.Abs(Triangulator.Area(_points.ToArray()) * massModifier);
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
						rigidbody.mass = Triangulator.Area(_points.ToArray()) * massModifier;
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

				for(int i = 0; i < _points.Count; i++)
				{
					float x1, x2, y1, y2;
					x1 = _points[i].x;
					y1 = _points[i].y;

					if(i > _points.Count-2) {
						x2 = _points[0].x;
						y2 = _points[0].y;
					}
					else {
						x2 = _points[i+1].x;
						y2 = _points[i+1].y;			
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

		DrawEdgePlanes(_points, convexity, new Vector2(.4f, 1.2f));

		CleanUp();
	}

	/**
	 *	\brief Triangulates userPoints and sets mesh data.
	 *	This method should not be called directly unless you absolutely need to.  Use DrawPreviewMesh() or DrawFinalMesh() instead.
	 *	@param m Mesh to be used for graphics.
	 *	@param c Mesh to be used for collisions.
	 *	@param convexity The #PolygonType.  Necessary for producing the correct face orientation.
	 */
	public void MeshWithPoints(out Mesh m, out Mesh c, PolygonType convexity)
	{
		// Assumes local space.  Returns graphics mesh in the following submesh order:
		// 0 - Front face
		// 1 - Sides (optional)
		// 2 - Back face (planned)

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
		Triangulator tr = new Triangulator(points);
		int[] front_indices = tr.Triangulate();
	   
		// Create the Vector3 vertices
		List<Vector3> front_vertices = new List<Vector3>(VerticesWithPoints(points, zOrigin - halfSideLength));

		List<Vector2> front_uv = ArrayMultiply(points, uvScale);

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

		List<Vector2> side_uv = CalcSideUVs(side_vertices);

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

	/**
	 *	\brief Returns a quad.
	 *	\returns A quad plane.
	 */
	public Mesh MeshPlane()
	{
		Mesh m = new Mesh();
		m.vertices = new Vector3[4] {
			new Vector3(-.5f, 0f, 0f),
			new Vector3(.5f, 0f, 0f),
			new Vector3(-.5f, -1f, 0f),
			new Vector3(.5f, -1f, 0f)
		};

		m.triangles = new int[6] {
			2, 1, 0,
			1, 3, 2
		};

		m.RecalculateNormals();

		return m;
	}
#endregion

#region FLOURISHES

	/**
	 *	\brief Draws planes around the edges of a mesh.
	 *	@param _points The points to use as a guide.
	 *	@param _modifier Multiply plane X and Y dimensions component wise.
	 */
	public void DrawEdgePlanes(List<Vector2> _points, PolygonType convexity, Vector2 _modifier)
	{
		Vector2[] points = _points.ToArray();

		if(convexity == PolygonType.ConcaveClockwise || convexity == PolygonType.ConvexClockwise)
			Array.Reverse(points);

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

			GameObject boxColliderObj = new GameObject();
			
			boxColliderObj.AddComponent<MeshFilter>().sharedMesh = MeshPlane();
			boxColliderObj.AddComponent<MeshRenderer>();

			boxColliderObj.name = "EdgePlane " + i;

			boxColliderObj.transform.position = new Vector3( ((x1 + x2)/2f), ((y1+y2)/2f), zPosition);

			Vector2 vectorLength = new Vector2( Mathf.Abs(x1 - x2),  Mathf.Abs(y1 - y2) );
			
			float length = Mathf.Sqrt( ( Mathf.Pow((float)vectorLength.x, 2f) + Mathf.Pow(vectorLength.y, 2f) ) );

			float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;
		
			Vector3 nrml = Vector3.Cross(new Vector3(x1, y1, 0f), new Vector3(x2, y2, 0f));
			
			Debug.Log( (int)angle%360 );

			boxColliderObj.transform.localScale = new Vector3(length, 1f, 2f);
			boxColliderObj.transform.rotation = Quaternion.Euler( new Vector3(0f, 0f, angle) );

			boxColliderObj.transform.parent = LastDrawnObject.transform;
		}		
	}
#endregion

#region MESH UTILITY

	/**
	 *	\brief Checks the count of generated objects against #maxAllowedObjects.
	 *	If the amount of generated objects is greater than the #maxAllowedObjects count, the earliest drawn object is deleted.
	 */
	public void CheckMaxMeshes() 
	{
		if(generatedMeshes.Count >= maxAllowedObjects && maxAllowedObjects > 0)
		{
			GameObject g = generatedMeshes[0];
			generatedMeshes.RemoveAt(0);
			Destroy(g);
		}
	}
	
	/**
	 *	\brief Destroys all PolyDraw generated gameObjects.
	 */
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

	/**
	 *	\brief Exports an OBJ file to the supplied path.
	 *	Files sharing a path name will not be ovewritten.
 	 *	\returns The path to the generated OBJ file.
	 *	@param path The file path to save the resulting OBJ to.
	 *	@param mf The MeshFilter to convert to an OBJ.
	 */
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

	/**
	 *	\brief Exports all generated meshes to an OBJ file.
	 *	\returns The path to the generated OBJ file.
	 *	@param path The file path to save resulting OBJs to.
	 */
	public string ExportOBJ(string path)
	{
		for(int i = 0; i < generatedMeshes.Count; i++)
			ExportOBJ(path, i);
		
		return path;
	}

	string ExportOBJ(string path, int index)
	{
		if(index < generatedMeshes.Count)
			return ExportOBJ(path, generatedMeshes[index]);
		else
			return "Index out of bounds.";
	}

	/**
	 *	\brief Accepts a gameObject and returns the resulting file path.
  	 *	\returns The path to the generated OBJ file.
	 *	@param path The file path to save resulting OBJ to.
	 *	@param go The gameObject to extract mesh data from.
	 */
	public string ExportOBJ(string path, GameObject go)
	{
		if(go.GetComponent<MeshFilter>())
			return ExportOBJ(path, go.GetComponent<MeshFilter>());
		else
			return "No mesh filter found.";
	}

	/**
	 *	\brief Exports the last drawn object to the specified path.
	 *	\returns The path to the generated OBJ file.
	 *	@param path The file path to save resulting OBJ to.
	 */
	public string ExportCurrent(string path)
	{
		return ExportOBJ(path, generatedMeshes[generatedMeshes.Count-1]);
	}
#endregion

#region IGNORE RECTS
	
	/**
	 *	\brief Adds passed Rect to the list of screen rects to ignore input from.
	 *	Input will not be taken from all specified rects in the ignore list.
	 *	@param rect The rect to add to the ignore list.
	 */	
	public void IgnoreRect(Rect rect)
	{
		ignoreRect.Add(rect);
	}

	/**
	 *	\brief Removes supplied Rect from ignore rect list if it exists.
	 *	@param rect The rect value to remove.
	 */
	public void RemoveFromIgnoreList(Rect rect)
	{
		ignoreRect.Remove(rect);
	}

	/**
	 * \brief Clears all rects from ignore list.
	 */
	public void ClearIgnoreRects()
	{
		ignoreRect.Clear();
	}
#endregion

#region UV
	
	// A little hacky, yes, but it works well enough to pass
	List<Vector2> CalcSideUVs(List<Vector3> v)
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

		return ArrayMultiply(uvs, uvScale);
	}

	List<Vector2> ArrayMultiply(Vector2[] _uvs, Vector2 _mult)
	{
		List<Vector2> uvs = new List<Vector2>();
		for(int i = 0; i < _uvs.Length; i++) {
			uvs.Add( Vector2.Scale(_uvs[i], _mult) );
		}
		return uvs;
	}
#endregion

#region MESH MATH UTILITY

	/**
	 *	\brief Takes list of user points and converts them to world points.
	 *	\returns A Vector3 array of resulting world points.
	 *	@param _points The user points to convert to world space.  Relative to Draw gameObject.
	 *	@param _zPosition The Z position to anchor points to.  Not affected by #faceOffset at this point.
	 */
	public Vector3[] VerticesInWorldSpace(List<Vector2> _points, float _zPosition)
	{
		Vector3[] v = new Vector3[_points.Count];

		for(int i = 0; i < _points.Count; i++)
			v[i] = transform.TransformPoint(new Vector3(_points[i].x, _points[i].y, _zPosition));
		
		return v;			
	}

	/**
	 *	\brief Takes list of user points and converts them to Vector3 points with supplied Z value.
	 *	\returns A Vector3 array of resulting points.
	 *	@param _points The user points to convert to world space.  Relative to Draw gameObject.
	 *	@param _zPosition The Z position to anchor points to.  Not affected by #faceOffset at this point.
	 */
	public Vector3[] VerticesWithPoints(Vector2[] _points, float _zPosition)
	{
		Vector3[] v = new Vector3[_points.Length];
		
		for(int i = 0; i < _points.Length; i++)
			v[i] = new Vector3(_points[i].x, _points[i].y, _zPosition);
		return v;
	}

	int[] ShiftTriangles(int[] tris, int offset)
	{
		int[] shifted = new int[tris.Length];

		for(int i = 0; i < shifted.Length; i++)
			shifted[i] = tris[i] + offset;

		return shifted;
	}
	
	/**
	 *	\brief Given a set of points, this method determines both the convexity and winding order of the object.
	 *	\returns The #PolygonType for the set of points.
	 *	@param p The points to read.
	 */
	PolygonType Convexity(List<Vector2> p)
	{
		// http://paulbourke.net/geometry/clockwise/index.html

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

	/**
	 *	\brief Given a set of 2d points, this returns true if any lines will intersect.
	*	\returns True if points will interect, false if not.
	 *	@param vertices The points to read.
	 */
	public bool SelfIntersectTest(List<Vector2> vertices)
	{
		// http://www.gamedev.net/topic/548477-fast-2d-PolygonType-self-intersect-test/
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
	
	/**
	 *	\brief Sets the input camera dimensions to match the screen dimensions of a perspective at given zPosition.
	 *	In order for input to be read correctly, an orthographic camera must be used.  If you wish to render your game using a perspective camera, this method allows you perform all rendering via perspective camera while still receiving input through a separate #inputCamera.  The #inputCamera should be set to Cull nothing, and clear Depth Only.  In addition, the orthographic camera should have the same coordinates as the rendering camera.  The easiest way to do this is simply to parent the orthographic camera to your perspective cam.
	 *	@param perspCam The rendering camera to base dimension calculations on.
	 *	@param zPos The Z position at which objects will be created.
	 */
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
