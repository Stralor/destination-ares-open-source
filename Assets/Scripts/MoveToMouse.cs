using UnityEngine;
using System.Collections;

public class MoveToMouse : MonoBehaviour
{

	//Camera container (or just camera)
	public Transform cameraReferenceObject;
	//Offset from camera to container
	public float zPos;

	void Update()
	{
		//Screen pos
		var mousePos = Input.mousePosition;

		//zPos
		mousePos.z = -(cameraReferenceObject.position.z + zPos);

		//Convert to world
		var worldSpacePos = Camera.main.ScreenToWorldPoint(mousePos);

		//Set pos
		if (transform.position != worldSpacePos)
			transform.position = worldSpacePos;
	}
}
