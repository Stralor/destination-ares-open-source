using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Note: this has a dependency on ColorPalette's red4 and blue4

public class ToggleSetting : MonoBehaviour
{
	[Tooltip("String name of PlayerPref int. This component can cycle through ints of any size.")]
	public string settingToToggle;
	public bool toggleText = true, toggleImage;

	[Tooltip("Name of toggle, if you want a static label before the state name. Used only when toggleText is true.")]
	public string displayName;

	[Tooltip("States on first Text child when toggleText is true. If also using toggleImage, please make sure there are the same number of states.")]
	public List<string> textStates = new List<string>()
	{
		"Off",
		"On"
	};

	[Tooltip("States on first Image child when toggleImage is true. If also using toggleText, please make sure there are the same number of states.")]
	public List<Sprite> imageStates = new List<Sprite>();

	private int index = 0;

	/**Toggle the PlayerPref. Also calls a method to change the button text.
	 * Cycles through all the states in order
	 */
	public void Toggle()
	{
		//Current value
		index = PlayerPrefs.GetInt(settingToToggle);

		//Change (it cycles)
		if (toggleText)
			index = index >= textStates.Count - 1 ? 0 : index + 1;
		else if (toggleImage)
			index = index >= imageStates.Count - 1 ? 0 : index + 1;
		
		ForceSetting(index);
	}

	/**Jump to a setting.
	 */
	public void ForceSetting(int value)
	{
		//New setting
		PlayerPrefs.SetInt(settingToToggle, value);

		//Text
		SetState();
	}
	
	/**Set the button text and image state, as appropriate.
	 * If jumpToValue >= 0, will try to set to that state
	 */
	void SetState()
	{
		//Current value
		index = PlayerPrefs.GetInt(settingToToggle);

		//Safety
		if ((toggleText && index >= textStates.Count) || (toggleImage && index >= imageStates.Count))
			index = 0;

		//Color
		var color = ColorPalette.cp.wht;
		if (index == 0)
			color = ColorPalette.cp.red4;
		else if (index == 1)
			color = ColorPalette.cp.blue4;

		//Explicit text
		if (toggleText)
		{
			var comp = GetComponentInChildren<UnityEngine.UI.Text>();
			var staticText = displayName + ": ";
			comp.text = String.IsNullOrWhiteSpace(displayName) ? string.Empty : staticText;
			comp.text += ColorPalette.ColorText(color, textStates[index]);
		}

		//Explicit image
		if (toggleImage)
			GetComponentInChildren<UnityEngine.UI.Image>().sprite = imageStates [index];
	}

	void Update()
	{
		//Keep up to date, in case the setting changes elsewhere
		if (index != PlayerPrefs.GetInt(settingToToggle))
			SetState();
	}

	void Start()
	{
		//Make sure we're up to date
		SetState();
	}
}
