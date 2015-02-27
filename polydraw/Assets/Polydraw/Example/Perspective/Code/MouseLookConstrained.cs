using UnityEngine;
using System.Collections;

namespace PolyDraw.Demo
{

	public class MouseLookConstrained : MonoBehaviour
	{
		public Vector2 sensitivity = new Vector2(.3f, .3f);
		public ClampValues clampX = new ClampValues(-10f, 10f);
		public ClampValues clampY = new ClampValues(-10f, 10f);

		private float rotationX, rotationY;
		
		public class ClampValues
		{
			public float Minimum = -10f;
			public float Maximum = 10f;
			public bool Lock360 = true;

			public ClampValues(float min, float max)
			{
				Minimum = min;
				Maximum = max;
			}

			public ClampValues(float min, float max, bool lock360)
			{
				Minimum = min;
				Maximum = max;
				Lock360 = lock360;
			}

			public float Clamp(float val)
			{
				if(Lock360)
				{
					if (val < -360)
						val += 360;
					else if (val > 360)
						val -= 360;
			 
					return Mathf.Clamp(val, Minimum, Maximum);			
				}
				else
				{
					return Mathf.Clamp(val, Minimum, Maximum);
				}
			}
		}

		public void LateUpdate()
		{
				// Read the mouse input axis
				rotationX += Input.GetAxis("Mouse X") * sensitivity.x;
				rotationY += Input.GetAxis("Mouse Y") * sensitivity.y;
	 
				rotationX = clampX.Clamp(rotationX);
				rotationY = clampY.Clamp(rotationY);

				transform.localRotation = Quaternion.AngleAxis (rotationX, Vector3.up);
				transform.localRotation *= Quaternion.AngleAxis (rotationY, Vector3.left);
	 	}
	}
}