/**
 *	\brief This script changes the main material to be used when drawing on every new object creation.
 */

using UnityEngine;
using System.Collections;

public class EventListenerExample : MonoBehaviour
{
	public Material[] materialLibrary = new Material[0];
	int curMatIndex = 0;

	void Start ()
	{
		GetComponent<Draw>().Changed += new System.EventHandler(OnCreatedNewObject);

		CycleMaterial();
	}
	
	// On creating a new object, cycle through the materials.	
	void OnCreatedNewObject(object sender, System.EventArgs e)
	{
		if(materialLibrary == null || materialLibrary.Length < 1)
			return;
		
		CycleMaterial();
	}

	void Update()
	{
		if(Input.GetKeyUp(KeyCode.T))
			CycleMaterial();
	}

	void CycleMaterial()
	{
		curMatIndex++;
		if(curMatIndex >= materialLibrary.Length)
			curMatIndex = 0;

		GetComponent<Draw>().SetMaterial(materialLibrary[curMatIndex]);
	}
}
