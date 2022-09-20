using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {

	public int rate;
	Rigidbody2D rigid;

	void Start(){
		rigid = GetComponent<Rigidbody2D> ();
		if (rigid)
			rigid.angularVelocity = rate;
	}

	public void StartSpin(){
		if (rigid)
			rigid.angularVelocity = rate;
	}

	public void StopSpin(){
		if (rigid)
			rigid.angularVelocity = 0;
	}
}
