using UnityEngine;
using System.Collections;

namespace Polydraw {

[System.Serializable]
public class DrawSettings
{
	public DrawSettings()
	{
	}

	public Axis			axis;							///< On which axis will this object be created?  (Front, Up, or Right)

	// Textures
	public Material 	frontMaterial;					///< The frontMaterial to be applied to the front face of the mesh.
	public Material 	sideMaterial;					///< The frontMaterial to be applied to the sides of the mesh.
	public Vector2 		uvScale = new Vector2(1f, 1f);	///< The scale to applied when creating UV coordinates.  Different from a frontMaterial scale property (though that will also affect frontMaterial layout).
	public Vector2 		uvOffset = new Vector2(0f, 0f);	///< 
	public float 		uvRotation = 0f;				///< Rotation of texture in degrees.

	// Sides
	public bool 		generateSide = true;			///< If true, sides will be created along with the front face.
	public float 		sideLength = 5f;				///< How long the sides will be.
	public Draw.Anchor 	anchor = Draw.Anchor.Center;	///< Where is the pivot point of this mesh?  See #Anchor for more information.
	public float 		faceOffset = 0f;				///< This value is used to offset the anchor.  As an example, a faceOffset of 1f with a #zPosition of 0f would set the front face at Vector3(x, y, 1f).  With #SideAnchor Center and a faceOffset of 0, the front face is set to exactly 1/2 negative distance (towards the camera) of sideLength.   

	public bool 		generateBackFace = false;		///< If true, a back face will be generated.

	public float 		zPosition = 0;					///< The Z position for all vertices.  Z is local to the Draw object, and thus it is recommended that the Draw object remain at world coordinates (0, 0, 0).  By default, this done for you in the Start method.
	public bool 		requireMinimumArea = false;		///< Polygon must have an area greater than #minimumAreaToDraw in order to be drawn.  Best when used in conjunction with a Continuous point drawing style.
	public float 		minimumAreaToDraw = 1f;			///< Polygon must have an area greater than this value in order to be drawn.
	public float 		smoothAngle = 45f;				///< Edges where the meeting planes form an angle less than this value will have their normals averaged to create a smooth seam.

	// Edges
	public bool 		drawEdgePlanes = false;			///< If true, edge planes will be drawn bordering the final mesh.
	public Material 	edgeMaterial;					///< The frontMaterial to be applied to the edge planes of the mesh.
	public float 		edgeLengthModifier = 1.2f;		///< Multiply the edge length by this amount to determine the final length of plane.
	public float 		edgeHeight = .5f;				///< How tall the plane should be.  Will be modified if #areaRelativeHeight is true.
	public float 		minLengthToDraw = .4f;			///< The minimum length that a plane must be in order to be drawn.
	public float 		edgeOffset = .2f;				///< A Z modifier determining how far offset this plane will be from the #zPosition.
	public float 		maxAngle = 45;					///< The maximum angle steepness allowed in order for a mesh to be drawn.
	public bool 		areaRelativeHeight = false;		///< If true, the #edgeHeight will be multiplied by 1/10th the area value.
	public float 		minEdgeHeight = .1f;			///< The minimum edge height for a plane.  Only taken into account when #areaRelativeHeight is true.
	public float 		maxEdgeHeight = 1f;				///< The maximum edge height for a plane.  Only taken into account when #areaRelativeHeight is true.	
	public float 		minimumDistanceBetweenPoints = .02f; ///< The minimum allowable distance that points may be separated by to generate a new point.

	// Physics
	public bool 		forceConvex = false;			///< If a MeshCollider is used, this can force the collision bounds to convex.
	public bool 		applyRigidbody = true;			///< If true, a RigidBody will be applied to the final mesh.  Does not apply to preview mesh.
	public bool 		areaRelativeMass = true;		///< If true, the mass of this final object will be relative to the area of the front face.  Mass is calculated as (area * #massModifier).
	public float 		massModifier = 1f;				///< The amount to multipy mesh area by when calculating mass.  See also #areaRelativeMass.
	public float 		mass = 25f;						///< If #areaRelativeMass is false, this value will be used when setting RigidBody mass.  See also #applyRigidbody.
	public bool 		useGravity = true;				///< If #applyRigidbody is true, this determines if gravity will be applied.
	public bool 		isKinematic = false;			///< If #applyRigidbody is true, this sets the isKinematic bool.
		
	// GameObject
	public bool 		useTag = false;					///< If true, the finalized mesh will have its tag set to #tagVal.  Note: Tag must exist prior to assignment.
	public string 		tagVal = "drawnMesh";			///< The tag to applied to the final mesh.  See also #useTag.
	public string 		meshName = "Drawn Mesh";		///< What the finalized mesh will be named.

	// Collisions
	public Draw.ColliderType colliderType = Draw.ColliderType.MeshCollider;	///< The #ColliderStyle to be used.
	public bool 		manualColliderDepth = false;	///< If #ColliderStyle is set to BoxCollider, this can override the #sideLength property to set collision depth.  See also #colDepth.
	public Draw.Anchor	colAnchor = Draw.Anchor.Center;	///< If #manualColliderDepth is toggled, this value will be used to determine where meshcollider will anchor itself.  \sa #Draw:Anchor
	public float 		colDepth = 5f;					///< If #manualColliderDepth is toggled, this value will be used to determine depth of colliders.
	public float 		boxColliderSize = .1f;			///< How thick should box colliders generate themselves.
	
	// Debug
	public bool 		drawNormals = false;			///< If true, selected polydraw objects will draw their normals in the SceneView.
	public float 		normalLength = .3f;				///< If drawNormals == true, this affects the length of the normals.

	public override string ToString()
	{
		return
			"frontMaterial: " + frontMaterial + "\n" +
			"sideMaterial: " + sideMaterial + "\n" +
			"uvScale: " + uvScale + "\n" +
			"generateSide: " + generateSide + "\n" +
			"sideLength: " + sideLength + "\n" +
			"anchor: " + anchor + "\n" +
			"faceOffset: " + faceOffset + "\n" +
			"zPosition: " + zPosition + "\n" +
			"drawEdgePlanes: " + drawEdgePlanes + "\n" +
			"edgeMaterial: " + edgeMaterial + "\n" +
			"edgeLengthModifier: " + edgeLengthModifier + "\n" +
			"edgeHeight: " + edgeHeight + "\n" +
			"minLengthToDraw: " + minLengthToDraw + "\n" +
			"edgeOffset: " + edgeOffset + "\n" +
			"maxAngle: " + maxAngle + "\n" +
			"areaRelativeHeight: " + areaRelativeHeight + "\n" +
			"minEdgeHeight: " + minEdgeHeight + "\n" +
			"maxEdgeHeight: " + maxEdgeHeight + "\n" +
			"forceConvex: " + forceConvex + "\n" +
			"Apply Rigidbody: " + applyRigidbody + "\n" +
			"Area Relative Mass: " + areaRelativeMass + "\n" +
			"Mass Modifier: " + massModifier + "\n" +
			"mass: " + mass + "\n" +
			"useGravity: " + useGravity + "\n" +
			"isKinematic: " + isKinematic + "\n" +
			"useTag: " + useTag + "\n" +
			"tagVal: " + tagVal + "\n" +
			"meshName: " + meshName + "\n" +
			"Collider Type: " + colliderType + "\n" +
			"manualColliderDepth: " + manualColliderDepth + "\n" +
			"colDepth: " + colDepth + "\n";

	}
}
}