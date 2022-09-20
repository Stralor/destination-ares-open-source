using UnityEngine;
using System.Collections;

public class LinkButtonToPlayerInteraction : MonoBehaviour {

	PlayerInteraction pI;

	// Use this for initialization
	void Start () {
		pI = GetComponentInParent<PlayerInteraction>();
	}
	
	public void UnlockOthers(){
		pI.Deselect ();
	}
}
