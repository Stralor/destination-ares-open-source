using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TooltipName : MonoBehaviour {

	Character character;	//Identifier for the parent Character script, if applicable
	ShipSystem shipSys;		//Identifier for the parent ShipSystem script, if applicable

	void Start(){
		//Identify any applicable parent scripts
		character = gameObject.GetComponentInParent<Character> ();
		shipSys = gameObject.GetComponentInParent<ShipSystem> ();
	}

	void Update () {
		//Establish the appropriate names
		if (character != null) {
			gameObject.GetComponent<Text>().text = character.title + " " + character.firstName + " " + character.lastName;
		}
		if (shipSys != null) {
			gameObject.GetComponent<Text>().text = shipSys.sysName;
		}
	}
}
