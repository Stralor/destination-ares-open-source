using UnityEngine;
using System.Collections;

public class ClearContents : MonoBehaviour
{

	public void ClearAll()
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			Destroy(transform.GetChild(i).gameObject);
		}

		transform.DetachChildren();
	}
}
