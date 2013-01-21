using UnityEngine;
using System.Collections;

public class IntersectTesting : MonoBehaviour {

	// Use this for initialization
	void Start () {
		// SelfIntersectTestFast(new Vector2[0]{});
	
	}

	// http://www.bryceboe.com/2006/10/23/line-segment-intersection-algorithm/
	bool CounterClockwise(Vector2 A, Vector2 B, Vector2 C)
	{
		return (C.y-A.y)*(B.x-A.x) > (B.y-A.y)*(C.x-A.x);
	}

	bool Intersecting(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
	{
        return CounterClockwise(A,C,D) != CounterClockwise(B,C,D) && CounterClockwise(A,B,C) != CounterClockwise(A,B,D);
	}

	public bool SelfIntersectTestFast(Vector2[] p)
	{
		for(int i = 0; i < p.Length; i++)
		{
			for(int n = 0; n < p.Length-1; n++)
			{
				if(n == i)
					continue;

				if(Intersecting(p[i], p[i+1], p[n], p[n+1]))
					return true;
			}

			if(i != p.Length-1 && Intersecting(p[i], p[i+1], p[p.Length-1], p[0]))
				return true;
		}

		return false;

		// Vector2 A = new Vector2(-1f, -1f);
		// Vector2 B = new Vector2(1f, 1f);

		// Vector2 C = new Vector2(1f, -1f);
		// Vector2 D = new Vector2(1f, 1f);

		// Debug.DrawLine(A.ToVector3(), B.ToVector3(), Color.green, 5f, false);
		// Debug.DrawLine(C.ToVector3(), D.ToVector3(), Color.red, 5f, false);

		// Debug.Log("Intersecting? " + Intersect(A, B, C, D));
	}
}
