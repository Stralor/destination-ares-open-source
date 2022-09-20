using UnityEngine;
using System.Collections;

public class MatchTargetXY : MonoBehaviour
{

	public Transform target;

	float _z;

	void Start()
	{
		_z = transform.position.z;
	}

	void Update()
	{
		if (transform.position.x != target.position.x || transform.position.y != target.position.y)
		{
			transform.position = new Vector3(target.position.x, target.position.y, _z);
		}
	}
}
