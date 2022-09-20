using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Opening : MonoBehaviour
{

	public enum Type
	{
		Door,
		Pin
	}

	public Type type;

	void OnEnable()
	{
		if (GameReference.r != null && !GameReference.r.allOpenings.Contains(this))	//Safety in case of initialization order
			GameReference.r.allOpenings.Add(this);
	}

	void OnDisable()
	{
		if (GameReference.r != null)
			GameReference.r.allOpenings.Remove(this);
	}
}
