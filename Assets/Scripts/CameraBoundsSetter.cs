using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraBoundsSetter : MonoBehaviour
{
	public static CameraBoundsSetter cbs;

	public CenterGroup mainBody;
	public List<Transform> subBodies = new List<Transform>();


	public void SetCameraBounds()
	{
		//Safety
		if (mainBody == null)
		{
			Debug.LogWarning("No CenterGroup defined. Camera Bounds not settable.");
			return;
		}

		//Center the main group. This also notifies CEC to change bounds.
		var vector = mainBody.UpdateGroupPosition();

		//match the sub groups
		foreach (var t in subBodies)
		{
			CenterGroup.MoveChildrenRigidbody2Ds(t, vector);
		}
	}

	void Awake()
	{
		if (cbs == null)
		{
			cbs = this;
		}
		else if (cbs != this)
		{
			Destroy(this);
		}
	}
}
