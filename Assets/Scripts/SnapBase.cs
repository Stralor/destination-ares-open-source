using UnityEngine;
using System.Collections;

public abstract class SnapBase : MonoBehaviour
{

	void LateUpdate()
	{
		//Keep it in position
		//Snap();
	}

	public abstract void Snap();
}
