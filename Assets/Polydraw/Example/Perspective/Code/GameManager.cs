using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using Polydraw;

namespace PolyDraw.Demo
{

	public class GameManager : MonoBehaviour {

		public static bool contextMenu = false;
		static Menu menu;
		static Draw draw;

		public void Start()
		{
			menu = (Menu)FindObjectsOfType(typeof(Menu))[0];
			draw = (Draw)FindObjectsOfType(typeof(Draw))[0];
		}

		public void Update()
		{
	#if !UNITY_WEBPLAYER
			if(Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Space))
			{
				if(contextMenu)
					CloseContextMenu();
				else
					OpenContextMenu();
			}
	#endif
		}

		public static void ExportAll()
		{
			// Saves all current meshes
		#if UNITY_EDITOR
				draw.ExportOBJ("Assets/Mesh.obj");
				AssetDatabase.Refresh();
		#elif UNITY_STANDALONE_WIN
				draw.ExportOBJ(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\Mesh.obj");
		#elif UNITY_STANDALONE_OSX	// Please note that this is not tested.  I'm assuming that relative paths will translate though.  If this doesn't work, you might also try "$HOME/Desktop/".
				draw.ExportOBJ("~/Desktop/" + "Mesh.obj");
		#endif
		}

		public static void OpenContextMenu()
		{
			contextMenu = true;
	#if UNITY_4
			draw.SetActive(false);
	#else
			draw.enabled = false;
	#endif
			menu.OpenMenu();
		}

		public static void CloseContextMenu()
		{
			contextMenu = false;
	#if UNITY_4
			draw.SetActive(true);
	#else
			draw.enabled = true;
	#endif
			menu.CloseMenu();
		}
	}
}
