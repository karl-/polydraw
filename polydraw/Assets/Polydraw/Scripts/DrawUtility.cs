using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Polydraw {

public static class DrawUtility
{
#region MESH GENERATION

	/**
	 *	\brief Triangulates userPoints and sets mesh data.
	 *	This method should not be called directly unless you absolutely need to.  Use DrawPreviewMesh() or DrawFinalMesh() instead.
	 *	@param m Mesh to be used for graphics.
	 *	@param c Mesh to be used for collisions.
	 *	@param convexity The #PolygonType.  Necessary for producing the correct face orientation.
	 */
	public static bool MeshWithPoints(List<Vector2> _points, DrawSettings drawSettings, out Mesh m, out Mesh c, out Draw.PolygonType convexity)
	{
		// Assumes local space.  Returns graphics mesh in the following submesh order:
		// 0 - Front face
		// 1 - Sides (optional)
		// 2 - Back face (planned)

		List<Vector2> points = new List<Vector2>(_points);

//		Debug.Log(drawSettings.ToString());

		m = new Mesh();
		c = new Mesh();

		m.name = "Graphics";
		c.name = "Collisions";

		convexity = Convexity(points);

		if(convexity == Draw.PolygonType.ConcaveClockwise || convexity == Draw.PolygonType.ConvexClockwise)
			points.Reverse();

		// Run a self-intersect test
		if(SelfIntersectTest(points))
		{
			#if DEBUG
			Debug.LogWarning("Polydraw: Mesh generation failed on self-intersect test.");
			#endif
			return false;
		}

		float zOrigin = 0f;
		float collisionOrigin = 0f;
		float halfSideLength = drawSettings.sideLength/2f;
		float colHalfSideLength = drawSettings.colDepth/2f;
		
		switch(drawSettings.anchor)
		{
			case Draw.Anchor.Front:
			 	zOrigin = drawSettings.zPosition + drawSettings.faceOffset + halfSideLength;
				break;
		
			case Draw.Anchor.Back:
			 	zOrigin = drawSettings.zPosition + drawSettings.faceOffset - halfSideLength;
				break;

			case Draw.Anchor.Center:
			default:
			 	zOrigin = drawSettings.zPosition + drawSettings.faceOffset;
				break;	
		}
		
		switch(drawSettings.colAnchor)
		{
			case Draw.Anchor.Front:
			 	collisionOrigin = drawSettings.zPosition + drawSettings.faceOffset + colHalfSideLength;
				break;
		
			case Draw.Anchor.Back:
			 	collisionOrigin = drawSettings.zPosition + drawSettings.faceOffset - colHalfSideLength;
				break;

			case Draw.Anchor.Center:
			default:
			 	collisionOrigin = drawSettings.zPosition + drawSettings.faceOffset;
				break;	
		}


		/*** Generate Front Face ***/
		Triangulator tr = new Triangulator(points);
		int[] front_indices = tr.Triangulate();
	   
		// Create the Vector3 vertices
		List<Vector3> front_vertices = VerticesWithPoints(points, zOrigin - halfSideLength);

		List<Vector2> front_uv = points.Multiply(drawSettings.uvScale);

		/*** Finish Front Face ***/
		
		/*** Generate Sides ***/
		
		// For use with graphics mesh
		List<Vector3> side_vertices = new List<Vector3>();
		// For use with collision mesh
		List<Vector3> collison_vertices = new List<Vector3>();
		
		for (int i=0; i < points.Count; i++) {
			side_vertices.Add( new Vector3( points[i].x, points[i].y, zOrigin + halfSideLength) );
			side_vertices.Add( new Vector3( points[i].x, points[i].y, zOrigin - halfSideLength) );

			collison_vertices.Add( new Vector3( points[i].x, points[i].y, collisionOrigin + colHalfSideLength) );
			collison_vertices.Add( new Vector3( points[i].x, points[i].y, collisionOrigin - colHalfSideLength) );
		}
		
		// these sit right on the first two.  they don't share cause that would screw with
		// the lame way uvs are made.
		side_vertices.Add( new Vector3( points[0].x, points[0].y, zOrigin + halfSideLength) );
		side_vertices.Add( new Vector3( points[0].x, points[0].y, zOrigin - halfSideLength) );
	
		collison_vertices.Add( new Vector3( points[0].x, points[0].y, collisionOrigin + colHalfSideLength) );
		collison_vertices.Add( new Vector3( points[0].x, points[0].y, collisionOrigin - colHalfSideLength) );
		
		// +6 connects it to the first 2 verts
		int[] side_indices = new int[(side_vertices.Count*3)];
		
		int windingOrder = 1; // assume counter-clockwise, cause y'know, we set it that way
		
		int v = 0;
		for(int i = 0; i < side_indices.Length - 6; i+=3)
		{			
			// 0 is for clockwise winding order, anything else is CC
			if(i%2!=windingOrder)
			{
				side_indices[i+0] = v;
				side_indices[i+1] = v + 1;
				side_indices[i+2] = v + 2;
			} else {
				side_indices[i+2] = v;
				side_indices[i+1] = v + 1;
				side_indices[i+0] = v + 2;
			}
			v++;
		}
		/*** Finish Generating Sides ***/

		List<Vector2> side_uv = CalcSideUVs(side_vertices, drawSettings.uvScale);
		m.Clear();
		m.vertices = drawSettings.generateSide ? front_vertices.Concat(side_vertices).ToArray() : front_vertices.ToArray();
		if(drawSettings.generateSide) {
			m.subMeshCount = 2;
			m.SetTriangles(front_indices, 0);
			m.SetTriangles(ShiftTriangles(side_indices, front_vertices.Count), 1);
		} else {
			m.triangles = front_indices;
		}
		m.uv = drawSettings.generateSide ? front_uv.Concat(side_uv).ToArray() : front_uv.ToArray();
		m.RecalculateNormals();
		m.RecalculateBounds();
		m.Optimize();

		c.Clear();
		c.vertices = collison_vertices.ToArray();
		c.triangles = side_indices;
		c.uv = side_uv.ToArray();
		c.RecalculateNormals();
		c.RecalculateBounds();
		return true;
	}
#endregion

#region UV
	
	// A little hacky, yes, but it works well enough to pass
	static List<Vector2> CalcSideUVs(List<Vector3> v, Vector2 uvScale)
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

	public static List<Vector2> ArrayMultiply(Vector2[] _uvs, Vector2 _mult)
	{
		List<Vector2> uvs = new List<Vector2>();
		for(int i = 0; i < _uvs.Length; i++) {
			uvs.Add( Vector2.Scale(_uvs[i], _mult) );
		}
		return uvs;
	}

	public static List<Vector2> Multiply(this List<Vector2> l, Vector2 v)
	{
		List<Vector2> uvs = new List<Vector2>();
		for(int i = 0; i < l.Count; i++) {
			uvs.Add( Vector2.Scale(l[i], v) );
		}
		return uvs;
	}
#endregion

#region MESH MATH UTILITY
	
	/**
	 *	\brief Returns the area of the currently drawn polygon.
	 */
	public static float GetArea(List<Vector2> points)
	{
		if(points != null && points.Count > 2)
			return Mathf.Abs(Triangulator.Area(points));
		else
			return 0f;
	}

	/**
	 *	\brief Takes list of user points and converts them to world points.
	 *	\returns A Vector3 array of resulting world points.
	 *	@param _points The user points to convert to world space.  Relative to Draw gameObject.
	 *	@param _zPosition The Z position to anchor points to.  Not affected by #faceOffset at this point.
	 */
	public static Vector3[] VerticesInWorldSpace(this Transform t, List<Vector2> _points, float _zPosition)
	{
		Vector3[] v = new Vector3[_points.Count];

		for(int i = 0; i < _points.Count; i++)
			v[i] = t.TransformPoint(new Vector3(_points[i].x, _points[i].y, _zPosition));
		
		return v;			
	}

	/**
	 *	\brief Takes list of user points and converts them to Vector3 points with supplied Z value.
	 *	\returns A Vector3 array of resulting points.
	 *	@param _points The user points to convert to world space.  Relative to Draw gameObject.
	 *	@param _zPosition The Z position to anchor points to.  Not affected by #faceOffset at this point.
	 */
	public static List<Vector3> VerticesWithPoints(List<Vector2> _points, float _zPosition)
	{
		List<Vector3> v = new List<Vector3>();
		
		for(int i = 0; i < _points.Count; i++)
			v.Add(new Vector3(_points[i].x, _points[i].y, _zPosition));
		return v;
	}

	static int[] ShiftTriangles(int[] tris, int offset)
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
	static Draw.PolygonType Convexity(List<Vector2> p)
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
		
		Draw.PolygonType convexity;
		if(isConcave == true || flag == 0) 
		{
			if(wind > 0)
				convexity = Draw.PolygonType.ConcaveCounterClockwise;
			else
				convexity = Draw.PolygonType.ConcaveClockwise;
		}
		else
		{
			if(wind > 0)
				convexity = Draw.PolygonType.ConvexCounterClockwise;
			else
				convexity = Draw.PolygonType.ConvexClockwise;
		}

		return convexity;
	}

	/**
	 *	\brief Given a set of 2d points, this returns true if any lines will intersect.
	*	\returns True if points will interect, false if not.
	 *	@param vertices The points to read.
	 */
	public static bool SelfIntersectTest(List<Vector2> points)
	{
		List<Vector2> vertices = new List<Vector2>(points);
		// http://www.gamedev.net/topic/548477-fast-2d-PolygonType-self-intersect-test/
		int count = vertices.Count;

		for (int i = 0; i < count; ++i)
		{
			if (i < count - 1)
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

			if(count <= 3) continue;

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

#region Debug

	public static String ToFormattedString<T>(this List<T> val, string seperator)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for(int i = 0; i < val.Count-1; i++)
		{
			sb.Append(val[i].ToString() + seperator);
		}
		sb.Append(val[val.Count-1].ToString());
		return sb.ToString();
	}
#endregion
}
}