using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PolyDraw.Demo
{

	public class Menu : MonoBehaviour
	{
		public Rect worldRect = new Rect(0f, 0f, 0f, 0f);
		private Vector2 prevScreenSize = Vector2.zero;

		public GameObject plane;

		public Material[] mats;

		public List<Button> buttons = new List<Button>();

	#region INIT
			
		public void Awake()
		{
			// create material buttons

			foreach(Transform t in transform) {
				if(t.GetComponent<Button>()) {
					buttons.Add(t.GetComponent<Button>());
					buttons[buttons.Count-1].menu = this;
				}
			}
			// buttons = new List<Button>( (Button[])GetComponentsInChildren(typeof(Button)));

			foreach(Button butt in buttons)
				butt.MoveToStart();
		}
	#endregion

	#region OPEN CLOSE
		
		public void OpenMenu()
		{
			foreach(Button butt in buttons)
				butt.TweenToDestination(.2f);
		}

		public void CloseMenu()
		{
			foreach(Button butt in buttons)
				butt.TweenToStart(.3f);
		}
	#endregion

	#region UPDATE

		public void Update()
		{
			if(Screen.width != prevScreenSize.x || Screen.height != prevScreenSize.y)
				OnWindowResize();
		}
	#endregion

	#region ACTION
		
		public void OnClick(string action, Object arg)
		{
			switch(action)
			{
				case "Export":
					GameManager.ExportAll();
					break;

				case "Quit":
					Application.Quit();
					break;
			}

			GameManager.CloseContextMenu();
		}
	#endregion

	#region SCREEN UTILITY

		public void OnWindowResize()
		{
			Camera cam = GetComponent<Camera>();
			Vector2 bl 	= cam.ScreenToWorldPoint(Vector2.zero);
			Vector2 tr 	= cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, cam.pixelHeight));
			worldRect = new Rect(bl.x, bl.y, tr.x-bl.x, tr.y - bl.y);
		}

		public Vector3 NormalizedPointToWorld(Vector2 point)
		{
			float x = worldRect.x + (worldRect.width * point.x);
			float y = worldRect.y + (worldRect.height * point.y);
			return new Vector3(x, y, 0f);
		}
	#endregion

	}
}