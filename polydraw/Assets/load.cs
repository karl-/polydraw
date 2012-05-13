using UnityEngine;
using System.Collections;

public class load : MonoBehaviour {
	public string lvl = "Load Mesh Gen";
	public int lvlnum = 1;
	void OnGUI()
	{
		if(GUILayout.Button(lvl, GUILayout.MinHeight(48)))
			Application.LoadLevel(lvlnum);
		
	}
}
