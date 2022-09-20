using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SnapToNodes : SnapBase
{
	public Placement placement;

	public Transform currentNode;

	public override void Snap()
	{
		Vector3 newPos = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

		foreach (var t in SnapNodeList.connectionNodes)
		{
			var newDist = Vector3.Distance(transform.localPosition, t.position);
			var oldDist = Vector3.Distance(transform.localPosition, newPos);
			if (newDist < oldDist && newDist < 2)
			{
				newPos = t.position;
				currentNode = t;
			}
		}

		//Set it
		if (newPos.x != Mathf.Infinity && newPos.y != Mathf.Infinity && newPos.z != Mathf.Infinity)
		{
			transform.localPosition = new Vector3(newPos.x, newPos.y, transform.localPosition.z);

			//Placement connection
			if (!placement.isConnected)
				placement.Connect();
		}
		//Placement disconnection
		else if (placement.isConnected)
		{
			placement.Disconnect();
			currentNode = null;
		}
	}

	void Awake()
	{
		if (placement == null)
			placement = GetComponent<Placement>();
	}
}

public static class SnapNodeList
{
	public static List<Transform> connectionNodes = new List<Transform>();
}
