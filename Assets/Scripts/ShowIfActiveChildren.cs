using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowIfActiveChildren : MonoBehaviour
{

	void Update()
	{
		bool activeChildFound = false;

		foreach (Transform child in transform)
		{
			if (child.gameObject.activeSelf)
			{
				activeChildFound = true;
				break;
			}
		}

		gameObject.SetActive(activeChildFound);
		return;
	}
}
