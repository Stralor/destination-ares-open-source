using UnityEngine;
using System.Collections;

public class LinkedOption : MonoBehaviour
{
	//The whole point of this script is to store and retrieve this value. Used by GameEventManager.
	public EventOptionData linkedOption { get; set; }

	public void OptionPressed()
	{
		GameEventManager.gem.OptionPressed(linkedOption);
	}
}
