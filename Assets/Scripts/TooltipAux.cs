using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TooltipAux : MonoBehaviour {

	Character character;	//Identifier for the parent Character script, if applicable
	ShipSystem shipSys;		//Identifier for the parent ShipSystem script, if applicable

	void Start(){
		//Identify any applicable parent scripts
		character = gameObject.GetComponentInParent<Character> ();
		shipSys = gameObject.GetComponentInParent<ShipSystem> ();
		//Import the game's color palette
	}

	void Update () {
		//Establish the appropriate texts
		if (character != null) {
			string text = "";
			bool afterFirst = false;
			foreach (Character.CharSkill sk in character.skills){
				if (afterFirst){
					text += " ";
				}
				text += sk.ToString();
				afterFirst = true;
			}
			gameObject.GetComponent<Text>().text = text;
		}
		if (shipSys != null) {
			gameObject.GetComponent<Text>().text = shipSys.condition.ToString() + " " + shipSys.quality.ToString();
			switch (shipSys.condition) {
				case ShipSystem.SysCondition.Functional :
					GetComponentInParent<Image>().color = ColorPalette.cp.blue3;
					break;
				case ShipSystem.SysCondition.Strained :
					GetComponentInParent<Image>().color = ColorPalette.cp.yellow3;
					break;
				case ShipSystem.SysCondition.Broken :
					GetComponentInParent<Image>().color = ColorPalette.cp.red3;
					break;
			}
		}
	}
}
