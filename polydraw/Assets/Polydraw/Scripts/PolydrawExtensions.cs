using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Polydraw {

public static class PolydrawExtensions
{

#region Conversion

	public static List<Vector2> ToVector2(this Vector3[] v)
	{
		List<Vector2> p = new List<Vector2>();
		for(int i = 0; i < v.Length; i++)
			p.Add(v[i]);
		return p;
	}

	public static Vector3 ToVector3(this Vector2 v2)
	{
		return new Vector3(v2.x, v2.y, 0f);
	}

	public static Vector3 ToVector3(this Vector2 v2, float z)
	{
		return new Vector3(v2.x, v2.y, z);
	}

	public static Vector3[] ToVector3(this List<Vector2> v2, float z)
	{
		Vector3[] v = new Vector3[v2.Count];
		for(int i = 0; i < v.Length; i++)
			v[i] = new Vector3(v2[i].x, v2[i].y, z);
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

#region GameObject 


	// TODO -- account for scale and rotation
	public static void CenterPivot(this PolydrawObject poly)
	{
		if(!poly.isValid) return;

		Vector3[] v = poly.transform.ToWorldSpace(poly.points.ToVector3(poly.drawSettings.zPosition));

		Vector3 avg = Vector3.zero;
		
		for(int i = 0; i < v.Length; i++)
			avg += v[i];
		
		avg /= (float)v.Length;

		for(int i = 0; i < v.Length; i++)
			v[i] -= avg;

		poly.points = v.ToVector2();

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
			go.AddComponent<MeshCollider>().sharedMesh = m;
		}
		else
		{
			if(mc.sharedMesh != null)
				GameObject.DestroyImmediate(mc.sharedMesh);
			mc.sharedMesh = m;
		}
	}
#endregion
}
}