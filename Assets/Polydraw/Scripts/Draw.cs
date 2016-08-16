#pragma warning disable 0642

/// Parabox LLC
/// Support Email - karl@paraboxstudios.com
/// paraboxstudios.com
///
/// Draws a mesh from user input.
/// 
/// Known Issues:
/// -	No hole support.
///	-	No fill rule implementation, meaning on the (rare) occasion
///		that a mesh doesn't get it's winding squared away it will fill
///		improperly, or not at all.
/// 
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Polydraw;

namespace Polydraw
{

public class Draw : MonoBehaviour
{

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
		Point,							///< Input is read per click, finalizing only when the user presses the Enter or Return key.
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

	/** \enum ColliderType
	 *	\brief Determines what style collider to apply to the final object.
	 */
	public enum ColliderType {
		BoxCollider,					///< A series of thin box colliders will be created around the edge of the final object.  This allows for concave collisions, though may be slightly more prone to "snagging" on objects.  See also #boxColliderSize (only available via scripting interface).
		MeshCollider,					///< A standard Unity MeshCollider will be applied to this object.  See also #applyRigidbody.
		PolygonCollider2d,				///< Unity2d polygon collider.
		None							///< A sphere collider will be applied to the object.  <br />Just kidding.  It does what it sounds like, no collider will be applied to the object.
	}
#endregion

#region MEMBERS
	List<Vector2> userPoints = new List<Vector2>();
	
	public DrawStyle drawStyle = DrawStyle.Continuous;		///< The DrawStyle to be used.
	public int maxVertices = 4;								///< Maximum amount of vertices allowed per geometry.
	public float samplingRate = 30f;						///< How many point samples to read per second.  Only applies to Continuous DrawStyles.

	// Generated Mesh Settings
	public DrawSettings drawSettings = new DrawSettings();

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

	public int maxAllowedObjects = 4;						///< The maximum amount of meshes allowed on screen at any time.  Meshes will be deleted as new objects are drawn, in the order of oldest to newest.

	bool placingPoint = false;
	Vector3 previousMousePosition;
	private float timer = 0f;
	private List<GameObject> generatedMeshes = new List<GameObject>();
	private int windingOrder; // Positive = CC , Negative = CW
	public Camera inputCamera;								///< If using a Perspective camera, you will also need an orthographic camera to recieve input.  Assign an orthographic camera here.  Ortho camera Culling Mask should be set to Nothing, with Clear Flags set to Depth Only.
	public float boxColliderSize = .01f;					///< Determines the thickness of the BoxColliders.  See also #ColliderType, #colDepth, #manualColliderDepth.

	///< Returns the last drawn object, or null if no objects drawn.
	public GameObject LastDrawnObject { get { return (generatedMeshes.Count > 0) ? generatedMeshes[generatedMeshes.Count-1] : null; } }
#endregion

#region MEMBER SETTINGS
	
	/**
	 *	\brief Sets the front and side face materials.
	 *	@param mat The material to apply.
	 */
	public void SetMaterial(Material mat)
	{
		drawSettings.frontMaterial = mat;
		drawSettings.sideMaterial = mat;
	}

	/**
	 *	\brief Sets the front face material.
	 *	@param mat The material to apply.
	 */
	public void SetFrontMaterial(Material mat)
	{
		drawSettings.frontMaterial = mat;
	}

	/**
	 *	\brief Sets the side face material.
	 *	@param mat The material to apply.
	 */
	public void SetSideMaterial(Material mat)
	{
		drawSettings.sideMaterial = mat;
	}

	/**
	 *	\brief Sets the edge plane face material.
	 *	@param mat The material to apply.
	 */
	public void SetEdgeMaterial(Material mat)
	{
		drawSettings.edgeMaterial = mat;
	}
#endregion

#region EVENTS

	public delegate void OnObjectCreatedEvent();
	public static event OnObjectCreatedEvent OnObjectCreated;

	public delegate void OnDrawCanceledEvent();
	public static event OnDrawCanceledEvent OnDrawCanceled;


	protected void OnCreatedNewObject() 
	{
		if (OnObjectCreated != null)
			OnObjectCreated();
	}

	protected void OnCanceledObjectCreation()
	{
		if( OnDrawCanceled != null )
			OnDrawCanceled();
	}
#endregion


#region INITIALIZATION
	
	void Start()
	{		
		transform.parent = null;
		transform.position = Vector3.zero;

		if(inputCamera == null)
			inputCamera = Camera.main;

		// If we're not rendering with the camera used for input, set the ortho cam coordinates to match
		// the coordinates drawn at the Z position of the perspective camera.
		if(inputCamera != Camera.main)
			SetOrthographioCameraDimensions(Camera.main, drawSettings.zPosition);
	}
#endregion

#region UPDATE

	void Update()
	{
		if(inputCamera != Camera.main)
			SetOrthographioCameraDimensions(Camera.main, drawSettings.zPosition + drawSettings.faceOffset);
		
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
			case DrawStyle.Point:

				if(Input.GetKeyUp(KeyCode.Return))
					DrawFinalMesh(userPoints);

				if(Input.GetMouseButtonDown(0))
				{
					Vector3 worldPos = inputCamera.ScreenToWorldPoint(new Vector3(
						Input.mousePosition.x, 
						Input.mousePosition.y,
						0f));
					worldPos = new Vector3(worldPos.x, worldPos.y, drawSettings.zPosition + drawSettings.faceOffset);

					AddPoint(worldPos);
					
					RefreshPreview();

					placingPoint = true;
					break;
				}
				
				if(Input.mousePosition != previousMousePosition && placingPoint)
				{			
					previousMousePosition = Input.mousePosition;
					Vector3 worldPos = inputCamera.ScreenToWorldPoint(new Vector3(
						Input.mousePosition.x, 
						Input.mousePosition.y,
						0f));
					worldPos = new Vector3(worldPos.x, worldPos.y, drawSettings.zPosition + drawSettings.faceOffset);
													
					if(userPoints.Count > 0)
					{
						int eol = userPoints.Count - 1;
						userPoints[eol] = worldPos;
						if(pointMarkers.Count == userPoints.Count)
							pointMarkers[eol].transform.position = worldPos;
					}

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

					// Max Vertex
					if(userPoints.Count >= maxVertices && drawStyle == DrawStyle.PointMaxVertex)
					{
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
							
							Vector3 worldPos = inputCamera.ScreenToWorldPoint(new Vector3(
								Input.mousePosition.x, 
								Input.mousePosition.y,
								0f));
							worldPos.z = drawSettings.zPosition + drawSettings.faceOffset;
												
							AddPoint(worldPos);

							RefreshPreview();
							
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
	}
	
	/**
	 *	\brief Refreshes the preview mesh, point markers (#pointMarker), and line renderer (#lineRenderer).
	 */
	public void RefreshPreview()
	{
		if(userPoints.Count > 1)
		{			
			if(drawMeshInProgress && userPoints.Count > 2)
				DrawPreviewMesh(userPoints);

			if(drawLineRenderer)
				DrawLineRenderer( transform.ToWorldSpace(userPoints.ToVector3(drawSettings.axis, drawSettings.zPosition + drawSettings.faceOffset)), true);
		}
	}	
	
	/**
	 *	\brief Clears the user point list, destroys all preview frontMaterials.  Called during mesh finalization by default.
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
		
		if( _points.Count < 3 )
			return;
		
		// Create the mesh
		if(previewGameObject == null) {
			previewGameObject = new GameObject();
			previewGameObject.AddComponent<MeshFilter>();
			previewGameObject.GetComponent<MeshFilter>();
			previewGameObject.AddComponent<MeshRenderer>();
		}
		
		Mesh m, c;
		PolygonType convexity;

		DrawUtility.MeshWithPoints(_points, drawSettings, out m, out c, out convexity);

		previewGameObject.GetComponent<MeshFilter>().sharedMesh = m;

		Material[] mats = (drawSettings.generateSide) ? 
			new Material[2] { drawSettings.frontMaterial, drawSettings.sideMaterial} :
			new Material[1] { drawSettings.frontMaterial };

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

		if(_points.Count < 3)
		{
			CleanUp();
			#if DEBUG
			Debug.LogWarning("Polydraw: Mesh generation failed on points < 3");
			#endif
			OnCanceledObjectCreation();
			return;
		}

 		if((_points[0] - _points[_points.Count - 1]).sqrMagnitude > maxDistance && useDistanceCheck)
		{
			#if DEBUG
			Debug.LogWarning("Mesh generation failed on distance check");
			#endif
			CleanUp();
			OnCanceledObjectCreation();
			return;
		}

		// Calculate this here because the collision code needs it too
		float obj_area = Mathf.Abs(Triangulator.Area(_points));

		if(drawSettings.requireMinimumArea && obj_area < drawSettings.minimumAreaToDraw)
		{
			#if DEBUG
			Debug.Log("Polydraw: Mesh generation failed on requireMinimumArea check.");
			#endif
			CleanUp();
			return;
		}				

		// graphics = any mesh that you can see, collision = the side mesh
		Mesh graphics, collision;
		Draw.PolygonType convexity;

		// Since Draw doesn't expose special collision settings, just the side settings.
		drawSettings.colAnchor = drawSettings.anchor;
		drawSettings.colDepth = drawSettings.sideLength > .01 ? drawSettings.sideLength : .01f;

		if( !DrawUtility.MeshWithPoints(_points, drawSettings, out graphics, out collision, out convexity) )
		{
			if(graphics != null)
				DestroyImmediate(graphics);
			if(collision != null)
				DestroyImmediate(collision);

			CleanUp();

			return;
		}

		// If we're over max, delete the earliest drawn mesh
		CheckMaxMeshes();

		GameObject finalMeshGameObject = new GameObject();
		finalMeshGameObject.name = drawSettings.meshName;

		if(drawSettings.useTag)
			finalMeshGameObject.tag = drawSettings.tagVal;	

		finalMeshGameObject.AddComponent<MeshFilter>();
		finalMeshGameObject.GetComponent<MeshFilter>().sharedMesh = graphics;
		finalMeshGameObject.AddComponent<MeshRenderer>();


		Material[] mats = (drawSettings.generateSide) ? 
			new Material[2] { drawSettings.frontMaterial, drawSettings.sideMaterial} :
			new Material[1] { drawSettings.frontMaterial };

		finalMeshGameObject.GetComponent<MeshRenderer>().sharedMaterials = mats;

		switch(drawSettings.colliderType)	
		{
			case ColliderType.MeshCollider:
				finalMeshGameObject.AddComponent<MeshCollider>();
				
				finalMeshGameObject.GetComponent<MeshCollider>().sharedMesh = collision;

				if(drawSettings.applyRigidbody)
				{
					Rigidbody rigidbody = finalMeshGameObject.AddComponent<Rigidbody>();
				
					if( (convexity == PolygonType.ConcaveCounterClockwise || convexity == PolygonType.ConcaveClockwise) && drawSettings.forceConvex == false)
						finalMeshGameObject.GetComponent<MeshCollider>().convex = false;
					else
						finalMeshGameObject.GetComponent<MeshCollider>().convex = true;

					if(drawSettings.areaRelativeMass)
						rigidbody.mass = obj_area * drawSettings.massModifier;
					else
						rigidbody.mass = drawSettings.mass;

					rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
					
					if(drawSettings.useGravity)
						rigidbody.useGravity = true;
					else
						rigidbody.useGravity = false;
					
					if(drawSettings.isKinematic)
						rigidbody.isKinematic = true;
					else
						rigidbody.isKinematic = false;
				}
			break;

			case ColliderType.BoxCollider:
				if(drawSettings.applyRigidbody)
				{
					BoxCollider parent_collider = finalMeshGameObject.AddComponent<BoxCollider>();

					// the parent collider - don't allow it to be seen, just use it for
					// mass and other settings
					parent_collider.size = new Vector3(.01f, .01f, .01f);

					Rigidbody rigidbody = finalMeshGameObject.AddComponent<Rigidbody>();

					if(drawSettings.areaRelativeMass)
						rigidbody.mass = obj_area * drawSettings.massModifier;
					else
						rigidbody.mass = drawSettings.mass;

					rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
					
					if(drawSettings.useGravity)
						rigidbody.useGravity = true;
					else
						rigidbody.useGravity = false;
					
					if(drawSettings.isKinematic)
						rigidbody.isKinematic = true;
					else
						rigidbody.isKinematic = false;
				}

				if(!drawSettings.manualColliderDepth)
					drawSettings.colDepth = drawSettings.sideLength;

				float zPos_collider = drawSettings.zPosition + drawSettings.faceOffset;

				switch(drawSettings.anchor)
				{
					case Anchor.Front:
						zPos_collider += drawSettings.sideLength/2f;
						break;
					case Anchor.Back:
						zPos_collider -= drawSettings.sideLength/2f;
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

					boxColliderObj.transform.localScale = new Vector3(length, boxColliderSize, drawSettings.colDepth);
					boxColliderObj.transform.rotation = Quaternion.Euler( new Vector3(0f, 0f, angle) );

					boxColliderObj.transform.parent = finalMeshGameObject.transform;
				}
			break;

			case ColliderType.PolygonCollider2d:
				PolygonCollider2D poly = finalMeshGameObject.AddComponent<PolygonCollider2D>();
				finalMeshGameObject.AddComponent<Rigidbody2D>();
				poly.points = _points.ToArray();
				break;

			default:
			break;

		}

		generatedMeshes.Add (finalMeshGameObject);

		if(drawSettings.drawEdgePlanes)
			DrawEdgePlanes(_points, convexity, new Vector2(.4f, 1.2f), obj_area);

		CleanUp();

		OnCreatedNewObject();
	}

	/**
	 *	\brief Returns a quad.
	 *	\returns A quad plane.
	 */
	public Mesh MeshPlane()
	{
		Mesh m = new Mesh();

		m.vertices = new Vector3[4] {
			new Vector3(-.5f, .1f, 0f),
			new Vector3(.5f, .1f, 0f),
			new Vector3(-.5f, -.9f, 0f),
			new Vector3(.5f, -.9f, 0f)
		};

		m.triangles = new int[6] {
			0, 1, 3,
			3, 2, 0
		};

		m.uv = new Vector2[] {
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
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
	public void DrawEdgePlanes(List<Vector2> _points, PolygonType convexity, Vector2 _modifier, float area)
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

			float length = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
			length *= drawSettings.edgeLengthModifier;
			
			if(length < drawSettings.minLengthToDraw)
				continue;
		
			float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;

			if( Mathf.Abs(angle) > 180-drawSettings.maxAngle && Mathf.Abs(angle) < 180+drawSettings.maxAngle)
				;
			else
				continue;

			GameObject boxColliderObj = new GameObject();
			
			boxColliderObj.AddComponent<MeshFilter>().sharedMesh = MeshPlane();
			boxColliderObj.AddComponent<MeshRenderer>();
			boxColliderObj.name = "EdgePlane " + angle;

			boxColliderObj.transform.position = new Vector3( ((x1 + x2)/2f), ((y1+y2)/2f), drawSettings.zPosition + drawSettings.edgeOffset);


			boxColliderObj.GetComponent<MeshRenderer>().sharedMaterial = drawSettings.edgeMaterial;
			Vector2[] uvs = boxColliderObj.GetComponent<MeshFilter>().sharedMesh.uv;

			float imgScale = 1f;
			if(drawSettings.edgeMaterial != null)
				imgScale = ((float)drawSettings.edgeMaterial.mainTexture.width / drawSettings.edgeMaterial.mainTexture.height);

			boxColliderObj.GetComponent<MeshFilter>().sharedMesh.uv = DrawUtility.ArrayMultiply(uvs, new Vector2( length / imgScale, 1f)).ToArray();

			if(drawSettings.areaRelativeHeight)
				boxColliderObj.transform.localScale = new Vector3(
					length,
					Mathf.Clamp(Mathf.Abs(area) * drawSettings.edgeHeight,
						drawSettings.minEdgeHeight,
						drawSettings.maxEdgeHeight),
						2f);
			else
				boxColliderObj.transform.localScale = new Vector3(length, drawSettings.edgeHeight, 2f);

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
	 *	\brief Exports all selected Transform meshes to an OBJ file in the supplied path.
	 *	Files sharing a path name will not be ovewritten.
 	 *	\returns The path to the generated OBJ(s) file.
	 *	@param path The file path to save the resulting OBJ to.
	 *	@param t_arr An array of Transforms to be parsed.  Typical use case for this would be in editor, calling Selection.transforms.
	 */
	public static string ExportOBJ(string path, Transform[] t_arr)
	{
		foreach(Transform t in t_arr)
		{
			if(File.Exists(path)) {
				int i = 0;
				while(File.Exists(path)) {
					path = path.Replace(".obj","");
					path = path + i + ".obj";
					i++;
				}
			}
			if(t.GetComponent<MeshFilter>())
				ObjExporter.MeshToFile(t.GetComponent<MeshFilter>(), path);
		}
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

		Vector3 center = new Vector3( (tr.x + bl.x) / 2f, (tr.y + bl.y) / 2f, zPos);

		inputCamera.transform.position = new Vector3(center.x, center.y, perspCam.transform.position.z);

		inputCamera.orthographic = true;
		inputCamera.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);

		// orthographicSize is Y
		inputCamera.orthographicSize = (tr.y - bl.y) / 2f;
	}
#endregion
}
}
