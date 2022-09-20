using UnityEngine;
using System.Collections;

public class PoolReturner : MonoBehaviour {

	/**Calls ICO's ReturnToPool with this as the argument.*/
	public void ReturnToPool(){
		IconSpawner.ico.ReturnToPool(transform.parent.gameObject);
	}

	/**Set's the parent's name. */
	public void SetParentName(string name){
		transform.parent.name = name;
	}
}