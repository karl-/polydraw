using UnityEngine;
using System.Collections.Generic;

namespace Polydraw {

public static class PolydrawExtensions
{

#region Conversion

	public static List<Vector2> ToVector2(this List<Vector3> v, Axis axis)
	{
		List<Vector2> p = new List<Vector2>();
		for(int i = 0; i < v.Count; i++)
		{
			p.Add(v[i].ToVector2(axis));
		}
		return p;
	}

	public static Vector2[] ToVector2(this Vector3[] v, Axis axis)
	{
		Vector2[] p = new Vector2[v.Length];
		for(int i = 0; i < v.Length; i++)
		{
			p[i] = v[i].ToVector2(axis);
		}
		return p;
	}

	public static Vector2 ToVector2(this Vector3 v3, Axis axis)
	{
		switch(axis)
		{
			case Axis.Up:
				return new Vector2(v3.x, v3.z);

			case Axis.Right:
				return new Vector2(v3.z, v3.y);

			default:
				return v3;
		}
	}

	public static Vector3 ToVector3(this Vector2 v2, Axis axis, float z)
	{
		switch(axis)
		{
			case Axis.Up:
				return new Vector3(v2.x, z, v2.y);

			case Axis.Right:
				return new Vector3(z, v2.y, v2.x);
		}

		return new Vector3(v2.x, v2.y, z);
	}

	public static Vector3[] ToVector3(this Vector2[] v2, Axis axis, float z)
	{
		Vector3[] v = new Vector3[v2.Length];
		for(int i = 0; i < v.Length; i++)
			v[i] = v2[i].ToVector3(axis, z);
		return v;
	}

	public static List<Vector3> ToVector3(this List<Vector2> v2, Axis axis, float z)
	{
		List<Vector3> v = new List<Vector3>();
		for(int i = 0; i < v2.Count; i++)
			v.Add(v2[i].ToVector3(axis, z));
		return v;
	}

	public static Vector3[] ToWorldSpace(this Transform t, Vector3[] v)
	{
		Vector3[] nv = new Vector3[v.Length];
		for(int i = 0; i < nv.Length; i++)
			nv[i] = t.TransformPoint(v[i]);
		return nv;
	}

	public static Vector3[] ToWorldSpace(this Transform t, List<Vector3> v)
	{
		Vector3[] nv = new Vector3[v.Count];
		for(int i = 0; i < nv.Length; i++)
			nv[i] = t.TransformPoint(v[i]);
		return nv;
	}
#endregion

#region Mesh

	public static Mesh CopyCollisionMesh(Mesh InMesh)
	{
		if(InMesh == null)
			return null;
			
		Mesh m = new Mesh();
		m.vertices = InMesh.vertices;
		m.uv = InMesh.uv;
		m.triangles = InMesh.triangles;
		m.normals = InMesh.normals;

		return m;
	}
#endregion

#region GameObject 


	// TODO -- account for scale and rotation
	public static void CenterPivot(this PolydrawObject poly)
	{
		if(!poly.isValid) return;

		Vector3[] v = poly.transform.ToWorldSpace(poly.points.ToVector3(poly.drawSettings.axis, poly.drawSettings.zPosition));

		Vector3 avg = Vector3.zero;
		
		for(int i = 0; i < v.Length; i++)
			avg += v[i];
		
		avg /= (float)v.Length;

		for(int i = 0; i < v.Length; i++)
			v[i] -= avg;

		poly.points = new List<Vector2>(v.ToVector2(poly.drawSettings.axis));

		poly.Refresh();

		// go.GetComponent<MeshFilter>().sharedMesh.vertices = v;
		poly.transform.position = avg;
	}

	/**
	 *	\brief Sets the mesh on the supplied object and destroys the current.
	 */
	public static void SetMesh(this GameObject go, Mesh m)
	{
		MeshFilter mf = go.GetComponent<MeshFilter>();
		if(!mf) return;
		if(mf.sharedMesh != null)
			GameObject.DestroyImmediate(mf.sharedMesh);
		
		mf.sharedMesh = m;
	}

	/**
	 *	\brief Sets the collision mesh on the supplied object and destroys the current.
	 */
	public static void SetMeshCollider(this GameObject go, Mesh m)
	{
		MeshCollider mc = go.GetComponent<MeshCollider>();
		
		if(!mc)
		{
			mc = go.AddComponent<MeshCollider>();
		}
		else
		{
			if(mc.sharedMesh != null)
				GameObject.DestroyImmediate(mc.sharedMesh);
		}

		mc.sharedMesh = m;
	}
#endregion
}
}
