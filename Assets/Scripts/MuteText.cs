using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MuteText : MonoBehaviour {

	public Text textToChange;


	public void SetMute(){
		if (!AudioListener.pause)
			Unmute();
		else
			Mute();
	}

	public void Mute(){
		if (textToChange != null)
			textToChange.text = "Unmute";
	}

	public void Unmute(){
		if (textToChange != null)
			textToChange.text = "Mute";
	}

	void Start(){
		SetMute();
	}
}
