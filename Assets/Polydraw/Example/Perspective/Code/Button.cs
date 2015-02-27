using UnityEngine;
using System.Collections;

namespace PolyDraw.Demo
{
	public class Button : MonoBehaviour 
	{
		public Texture2D normal, down, hover;
		public Vector3 destination;
		public Vector3 start;

		public Object arg;

		public enum Primitive {
			Plane
		}
		public Primitive primitive = Primitive.Plane;

		public Menu menu;

	#region INPUT
		public void OnMouseEnter()
		{
			SetMaterial(hover);
		}

		public void OnMouseExit()
		{
			SetMaterial(normal);
		}

		public void OnMouseUp()
		{
			StartCoroutine(Down());
			menu.OnClick(gameObject.name, arg);
		}

		public IEnumerator Down()
		{
			SetMaterial(down);
			
			yield return new WaitForSeconds(.3f);

			SetMaterial(normal);
		}
	#endregion

	#region MOVING

		public void MoveToStart()
		{
			gameObject.transform.position = start;
			// StartCoroutine(_MoveToStart(secs));
		}

		public void MoveToDestination()
		{
			gameObject.transform.position = destination;
			// StartCoroutine(_MoveToDestination(secs));
		}

		public void TweenToStart(float secs)
		{
			MoveTo(start, secs);
		}

		public void TweenToDestination(float secs)
		{
			MoveTo(destination, secs);
		}


		public void MoveTo(Vector3 pos, float secs)
		{
			l_end = pos;
			l_start = transform.position;
			StartCoroutine(_MoveToDestination(secs));
		}

		Vector3 l_end, l_start = Vector3.zero;
		private IEnumerator _MoveToDestination(float secs)
		{
			float timer = 0f;
			while(Vector3.Distance(transform.position, l_end) > .1f)
			{
				timer += 1f * Time.deltaTime;
				transform.position = Vector3.Lerp(l_start, l_end, timer/secs);
				yield return null;
			}
			yield return null;
		}
	#endregion

	#region GRAPHICS
		public void SetMaterial(Texture2D tex)
		{
			gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tex;
		}

		public void CreateSprite(Texture2D img)
		{
			Mesh m = new Mesh();
			m.name = img.name;

			float scale = (img) ? (float)img.height/(float)img.width : 1f;
			Vector3[] v = new Vector3[4] {
				new Vector3(-.5f, -.5f * scale, 0f),
				new Vector3(.5f, -.5f * scale, 0f),
				new Vector3(-.5f, .5f * scale, 0f),
				new Vector3(.5f, .5f * scale, 0f)
			};
			int[] t = new int[6] {
				2, 1, 0,
				2, 3, 1
			};
			Vector2[] u = new Vector2[4] {
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(0f, 1f),
				new Vector2(1f, 1f)
			};

			m.vertices = v;
			m.triangles = t;
			m.uv = u;
			m.RecalculateNormals();
			m.RecalculateBounds();
			m.Optimize();

			if(gameObject.GetComponent<MeshFilter>())
			{
				if(gameObject.GetComponent<MeshFilter>().sharedMesh != null)
					DestroyImmediate(gameObject.GetComponent<MeshFilter>().sharedMesh);

				gameObject.GetComponent<MeshFilter>().sharedMesh = m;
			}
			else
				gameObject.AddComponent<MeshFilter>().sharedMesh = m;

			if(!gameObject.GetComponent<MeshRenderer>())
				gameObject.AddComponent<MeshRenderer>();

			Material mat = new Material(Shader.Find("Unlit/Transparent"));
			if(img)	mat.mainTexture = img;
		
			gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);

			gameObject.GetComponent<MeshRenderer>().sharedMaterial = mat;

			if(gameObject.GetComponent<BoxCollider>())
				DestroyImmediate(gameObject.GetComponent<BoxCollider>());
			gameObject.AddComponent<BoxCollider>();
		}

		public void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
				Gizmos.DrawLine(start, destination);
				
				Gizmos.DrawWireSphere(start, .3f);
			Gizmos.color = Color.white;
		}
	#endregion

	}
}