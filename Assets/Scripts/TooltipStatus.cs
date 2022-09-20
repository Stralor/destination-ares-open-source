using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TooltipStatus : MonoBehaviour {

	Character character;	//Identifier for the parent Character script, if applicable
	ShipSystem shipSys;		//Identifier for the parent ShipSystem script, if applicable

	void Start(){
		//Identify any applicable parent scripts
		character = gameObject.GetComponentInParent<Character> ();
		shipSys = gameObject.GetComponentInParent<ShipSystem> ();
		//Import the game's color palette
	}

	void Update () {
		//Establish the appropriate text
		if (character != null) {
			gameObject.GetComponent<Text>().text = character.status.ToString();
			switch (character.status) {
				case Character.CharStatus.Good :
					GetComponentInParent<Image>().color = ColorPalette.cp.blue3;
					break;
				case Character.CharStatus.Stressed :
					GetComponentInParent<Image>().color = ColorPalette.cp.blue1;
					break;
				case Character.CharStatus.Injured :
					GetComponentInParent<Image>().color = ColorPalette.cp.yellow1;
					break;
				case Character.CharStatus.Psychotic :
					GetComponentInParent<Image>().color = ColorPalette.cp.red3;
					break;
				case Character.CharStatus.Restrained :
					GetComponentInParent<Image>().color = ColorPalette.cp.red1;
					break;
				case Character.CharStatus.Unconscious :
					GetComponentInParent<Image>().color = ColorPalette.cp.yellow3;
					break;
				case Character.CharStatus.Dead :
					GetComponentInParent<Image>().color = ColorPalette.cp.gry1;
					break;
			}
		}
		if (shipSys != null) {
			gameObject.GetComponent<Text>().text = shipSys.status.ToString();
		}
	}
}
