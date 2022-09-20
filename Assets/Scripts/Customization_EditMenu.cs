using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Customization_EditMenu : MonoBehaviour
{

	public Text targetName;
	public Image portrait, outline;
	public GameObject characterImages;
	public Image portraitHair, portraitPupil0, portraitPupil1, portraitMouth;

	public GameObject bodyParent;

	//System Options
	public GameObject systemParent;
	public Dropdown sysQuality;
	public List<Dropdown> sysKeys = new List<Dropdown>();
	public Text functionMultiplier;

	//Crew Options
	public GameObject crewParent;
	public InputField chFirstName, chLastName;
	public Dropdown chTeam;
	public List<Dropdown> chRoles = new List<Dropdown>();
	public List<Dropdown> chSkills = new List<Dropdown>();
	public string originalFirstName, originalLastName;
	private bool nameChanged = false;

	//Color Options
	public GameObject colorParent;
	public GameObject colorButtonPrefab;
	ColorButton currentHair, currentEye, currentSkin;
	public Transform hairColorParent, eyeColorParent, skinColorParent;
	public List<ColorButton> hairColors = new List<ColorButton>(), 
		eyeColors = new List<ColorButton>(), 
		skinColors = new List<ColorButton>();
	public Dropdown chPersonality;

	public Button doneButton;
	public Text totalCost;

	MonoBehaviour target;
	int currentCost, originalCost;

	bool isInitialSetupDone = false;


	/*
	 * MENU AND TARGET MANIPULATION
	 */

	public bool IsValidTarget(Placement placement)
	{
		//System or Character?
		if (placement.GetComponentInParent<ShipSystem>() != null || placement.GetComponentInParent<Character>() != null)
		{	
			//and is placed?
			if (placement.isPlaced)
				return true;
		}

		return false;
	}

	/**New Target! Update the menu.
	 * Returns if we have a valid (aka handleable) target type
	 */
	public bool SetNewTarget(Placement placement)
	{
		InitialMenuSetup();

		//Only on placeds
		if (!placement.isPlaced)
			return false;

		//Ship Sys setup
		if ((target = placement.GetComponentInParent<ShipSystem>()) != null)
		{
			systemParent.SetActive(true);
			crewParent.SetActive(false);
			colorParent.SetActive(false);

			UpdateMenuBySystemTarget();

			//Achievement
			AchievementTracker.UnlockAchievement("CUSTOM_SYSTEMS");

			return true;
		}

		//Crew setup
		if ((target = placement.GetComponentInParent<Character>()) != null)
		{
			systemParent.SetActive(false);
			crewParent.SetActive(true);
			colorParent.SetActive(false);

			UpdateMenuByCrewTarget();

			//Achievement
			AchievementTracker.UnlockAchievement("CUSTOM_CREW");

			return true;
		}


		return false;
	}

	/**Set the target's values, based on the menu values */
	public void SetValues()
	{
		//Sys
		if (target is ShipSystem)
		{
			//Typing
			var sys = target as ShipSystem;

			//Sys Quality
			sys.quality = (ShipSystem.SysQuality)sysQuality.value;

			//Clear keywords, we're replacing them all
			sys.keywords.Clear();
			//Add whatever we have
			foreach (var t in sysKeys)
			{
				//Not going to add "NOTDEFINED"s (t.value == 0) to the list at all
				if (t.value == 1)
					sys.keywords.Add(ShipSystem.SysKeyword.Random);
				else if (t.value > 1)
					sys.keywords.Add(ShipSystem.unlockedKeywords [t.value - 2]);
			}

			//Rename to match
			sys.Rename();
		}
		if (target is Character)
		{
			//Typing
			var ch = target as Character;

			//Team and Appearance
			ch.team = (Character.Team)chTeam.value;
			var colors = ch.GetComponent<CharacterColors>();
			colors.team = ch.team;
			colors.hairColor = currentHair.color;
			colors.eyeColor = currentEye.color;
			colors.skinColor = currentSkin.color;
			colors.UpdateColors();
			ch.GetComponent<CharacterSpeech>().personality = CharacterSpeech.unlockedPersonalities [chPersonality.value];

			//Clear Roles and Skills, then replace
			ch.roles.Clear();
			ch.skills.Clear();

			foreach (var t in chRoles)
			{
				if (t.value != 0)
					ch.roles.Add(Character.unlockedRoles [t.value - 1]);
			}
			foreach (var t in chSkills)
			{
				if (t.value != 0)
					ch.skills.Add((Character.CharSkill)(t.value - 1));
			}

			//Name
			ch.firstName = chFirstName.text;
			ch.lastName = chLastName.text;
			ch.Rename();

			//Name check
			if (nameChanged && (originalFirstName != ch.firstName || originalLastName != ch.lastName))
			{
				//Achievement
				AchievementTracker.UnlockAchievement("CUSTOM_CREW_NAME");
			}
		}

		//Pay for it
		Customization_CurrencyController.c.preResCurrency -= currentCost - originalCost;
	}

	/**Update the texts and costs on the menu, based on menu values */
	public void OnValueChange()
	{
		if (target is ShipSystem)
		{
			//Get the keywords as shown by the menu values
			List<ShipSystem.SysKeyword> tempKeys = new List<ShipSystem.SysKeyword>();
			foreach (var t in sysKeys)
			{
				if (t.value == 0)
					tempKeys.Add(ShipSystem.SysKeyword.NOTDEFINED);
				else if (t.value == 1)
					tempKeys.Add(ShipSystem.SysKeyword.Random);
				else
					tempKeys.Add(ShipSystem.unlockedKeywords [t.value - 2]);
			}

			//Get the current cost, from menu values
			currentCost = Customization_CurrencyController.GetAssetsCost((target as ShipSystem).function, (ShipSystem.SysQuality)sysQuality.value, tempKeys);

			//Set our name with those keys
			targetName.text = (target as ShipSystem).Rename(tempKeys);

			//Update Done Button
			SetDoneButtonStatus();
		}
		if (target is Character)
		{
			var ch = target as Character;

			//Get list values
			var tempRoles = new List<Character.CharRoles>();
			foreach (var t in chRoles)
			{
				if (t.value != 0)
					tempRoles.Add(Character.unlockedRoles [t.value - 1]);
			}
			var tempSkills = new List<Character.CharSkill>();
			foreach (var t in chSkills)
			{
				if (t.value != 0)
					tempSkills.Add((Character.CharSkill)(t.value - 1));
			}

			//Get current cost, from menu values
			currentCost = Customization_CurrencyController.GetAssetsCost((target as Character).isRandomCrew, tempRoles, tempSkills);

			//Check for name changes
			if (ch.firstName != chFirstName.text || ch.lastName != chLastName.text)
				nameChanged = true;

			//Set name with those roles
			ch.firstName = chFirstName.text;
			ch.lastName = chLastName.text;
			targetName.text = ch.Rename(tempRoles);

			//Colorsss
			UpdateMenuColors();

			//Update Done Button
			SetDoneButtonStatus();
		}
	}

	/**Sets the menu values to whatever the target's values are (if it's a system)
	 */
	public void UpdateMenuBySystemTarget()
	{
		if (target is ShipSystem)
		{
			var sys = target as ShipSystem;

			originalCost = 0;

			//Header
			targetName.text = target.name;
			portrait.sprite = target.GetComponentInChildren<SpriteRenderer>().sprite;
			portrait.color = Color.white;
			outline.enabled = true;
			outline.sprite = target.GetComponentsInChildren<SpriteRenderer>().First(obj => obj.name == "Outline").sprite;
			characterImages.SetActive(false);

			//Quality
			sysQuality.value = (int)sys.quality;
			sysQuality.captionText.text = sysQuality.options [sysQuality.value].text;

			//Keywords
			for (int i = 0; i < sysKeys.Count; i++)
			{
				//There's a keyword there!
				if (i < sys.keywords.Count)
				{
					//It's Notdefined
					if (sys.keywords [i] == ShipSystem.SysKeyword.NOTDEFINED)
					{
						sysKeys [i].value = 0;
					}
					//It's random
					else if (sys.keywords [i] == ShipSystem.SysKeyword.Random)
					{
						sysKeys [i].value = 1;
					}
					//It's the usual
					else
					{
						//Set option
						if (ShipSystem.unlockedKeywords.Contains(sys.keywords [i]))
							sysKeys [i].value = ShipSystem.unlockedKeywords.IndexOf(sys.keywords [i]) + 2;
						//We don't have that one unlocked to play with
						else
							sysKeys [i].value = 0;
					}
				}
				//No keyword there, means we should use "None"
				else
				{
					sysKeys [i].value = 0;
				}

				//Update caption text to match internally selected option
				sysKeys [i].captionText.text = sysKeys [i].options [sysKeys [i].value].text;

			}

			//Set current and original cost
			currentCost = originalCost = Customization_CurrencyController.GetAssetsCost(sys);

			//Function multiplier text
			functionMultiplier.text = "x " + ColorPalette.ColorText(ColorPalette.cp.wht, Customization_CurrencyController.SysFunctionMultiplierDictionary [(int)sys.function].ToString())
			+ "  <size=12>(" + sys.function.ToString() + ")</size>";

			SetDoneButtonStatus();
		}
		else
		{
			Debug.LogError("Target is not a system. INVALID.");
		}
	}

	public void UpdateMenuByCrewTarget()
	{
		if (target is Character)
		{
			var ch = target as Character;

			originalCost = 0;

			//Names (we don't need the old naming inputs now)
//			ch.firstNameSet = ch.lastNameSet = true;
//			ch.TryCloseNames();

			// !!! Have to stagger like this, since editing chFirstName or chLastName calls OnValueChange, which then resets the other if we haven't cached yet !!!
			originalFirstName = ch.firstName;
			originalLastName = ch.lastName;
			chFirstName.text = originalFirstName;
			chLastName.text = originalLastName;
			nameChanged = false;	//This got tripped to true when we set the names. Immediately toggle it back to false before we unlock an achievement

			//Colors
			var colors = ch.GetComponent<CharacterColors>();
			currentHair = hairColors.Find(obj => obj.color == colors.hairColor);
			currentEye = eyeColors.Find(obj => obj.color == colors.eyeColor);
			currentSkin = skinColors.Find(obj => obj.color == colors.skinColor);
			portraitHair.color = colors.hairColor;
			portraitPupil0.color = portraitPupil1.color = colors.eyeColor;
			portrait.color = colors.skinColor;
			UpdateMenuColors();

			//Header
			targetName.text = target.name;
			portrait.sprite = target.GetComponentInChildren<SpriteRenderer>().sprite;
			characterImages.SetActive(true);
			outline.enabled = false;
			portraitMouth.enabled = Random.Range(0, 2) == 0;

			//Team
			chTeam.value = (int)ch.team;
			chTeam.captionText.text = chTeam.options [chTeam.value].text;

			//Personality
			chPersonality.value = (int)CharacterSpeech.unlockedPersonalities.IndexOf(ch.GetComponent<CharacterSpeech>().personality);
			chPersonality.captionText.text = chPersonality.options [chPersonality.value].text;

			//Have to do this by hand each time, since we can't constrain to List<Enum>
			//Roles
			for (int i = 0; i < chRoles.Count; i++)
			{
				//One there! (Is it also in our wheelhouse?
				if (i < ch.roles.Count && Character.unlockedRoles.Contains(ch.roles [i]))
				{
					chRoles [i].value = Character.unlockedRoles.IndexOf(ch.roles [i]) + 1;
				}
				//None there, use "None"!
				else
				{
					chRoles [i].value = 0;
				}

				//This is only interactable if not random crew
				chRoles [i].interactable = !ch.isRandomCrew;

				//Update caption text to match internally selected option
				if (!ch.isRandomCrew)
					chRoles [i].captionText.text = chRoles [i].options [chRoles [i].value].text;
				//Or indicate random crew
				else
				{
					chRoles [i].captionText.text = "Random Crew";
				}
			}

			//Skills
			for (int i = 0; i < chSkills.Count; i++)
			{
				//One there!
				if (i < ch.skills.Count)
				{
					chSkills [i].value = (int)ch.skills [i] + 1;
				}
				//None there, use "None"!
				else
				{
					chSkills [i].value = 0;
				}

				//This is only interactable if not random crew
				chSkills [i].interactable = !ch.isRandomCrew;

				//Update caption text to match internally selected option
				if (!ch.isRandomCrew)
					chSkills [i].captionText.text = chSkills [i].options [chSkills [i].value].text;
				//Or indicate random crew
				else
				{
					chSkills [i].captionText.text = "Random Crew";
				}
			}

			//Set current and original cost
			currentCost = originalCost = Customization_CurrencyController.GetAssetsCost(ch);

			//Done Button Update
			SetDoneButtonStatus();
		}
		else
		{
			Debug.LogError("Target is not a character. INVALID.");
		}
	}


	public void ResetNames()
	{
		//Do a final name reset (since the other name changes are permanent otherwise)
		if (target != null && target is Character)
		{
			var ch = target as Character;
			ch.firstName = originalFirstName;
			ch.lastName = originalLastName;
			ch.Rename();
		}
	}

	public void MoveToColorMenu()
	{
		crewParent.SetActive(false);
		colorParent.SetActive(true);
	}

	public void MoveToCrewMenu()
	{
		crewParent.SetActive(true);
		colorParent.SetActive(false);
	}

	public void NewColorChosen(ColorButton cb)
	{
		if (hairColors.Contains(cb))
		{
			currentHair.selected = false;
			cb.selected = true;
			currentHair = cb;
			portraitHair.color = currentHair.color;
		}
		if (eyeColors.Contains(cb))
		{
			currentEye.selected = false;
			cb.selected = true;
			currentEye = cb;
			portraitPupil0.color = portraitPupil1.color = currentEye.color;
		}
		if (skinColors.Contains(cb))
		{
			currentSkin.selected = false;
			cb.selected = true;
			currentSkin = cb;
			portrait.color = currentSkin.color;
		}
	}



	/*
	 * UTILITY AND PRIVATE METHODS
	 */

	void InitialMenuSetup()
	{
		if (!isInitialSetupDone)
		{
			isInitialSetupDone = true;

			//Dropdowns setup

			//SysQuality
			sysQuality.ClearOptions();
			var qualityNames = System.Enum.GetNames(typeof(ShipSystem.SysQuality)).ToList();
			qualityNames.Remove("UnderConstruction");
			sysQuality.AddOptions(qualityNames);

			//Add values
			AddValues(sysQuality.options, Customization_CurrencyController.SysQualityValueDictionary);

			//SysKeys
			foreach (var t in sysKeys)
			{
				t.ClearOptions();
				t.AddOptions(new List<string>(){ "None", "Random (50%)" });
				t.AddOptions(Utility.ListOfItemNames(ShipSystem.unlockedKeywords));

				//Add values to demarcate cost
				for (int i = 0; i < t.options.Count; i++)
				{
					//None will be marked as 0
					int val = 0;

					//Random is at index 1
					if (i == 1)
						Customization_CurrencyController.SysKeywordValueDictionary.TryGetValue(i, out val);
					//Then we have to sort by the actual keyword, based on those we have unlocked
					else if (i > 1)
						Customization_CurrencyController.SysKeywordValueDictionary.TryGetValue((int)ShipSystem.unlockedKeywords [i - 2], out val);

					t.options [i].text += ": " + ColorPalette.ColorText(ColorPalette.cp.wht, val.ToString());
				}
			}

			//Teams
			chTeam.ClearOptions();
			chTeam.AddOptions(System.Enum.GetNames(typeof(Character.Team)).ToList());

			//Character Rolers
			foreach (var t in chRoles)
			{
				t.ClearOptions();
				t.AddOptions(Utility.ListOfItemNames(Character.unlockedRoles));

				//Add Values to demarcate cost
				for (int i = 0; i < t.options.Count; i++)
				{
					int val = 0;

					Customization_CurrencyController.CharRolesValueDictionary.TryGetValue((int)Character.unlockedRoles [i], out val);

					t.options [i].text += ": " + ColorPalette.ColorText(ColorPalette.cp.wht, val.ToString());
				}

				//Add a none option, move it to front
				t.AddOptions(new List<string>(){ "None: " + ColorPalette.ColorText(ColorPalette.cp.wht, "0") });
				t.options.Insert(0, t.options [t.options.Count - 1]);
				t.options.RemoveAt(t.options.Count - 1);
			}

			//Character Skills
			foreach (var t in chSkills)
			{
				t.ClearOptions();
				t.AddOptions(new List<string>(){ "None" });
				t.AddOptions(System.Enum.GetNames(typeof(Character.CharSkill)).ToList());

				for (int i = 0; i < t.options.Count; i++)
				{
					if (i == 0)
						t.options [i].text += ": " + ColorPalette.ColorText(ColorPalette.cp.wht, "0");
					else
						//New straaaang
						t.options [i].text += ": " + ColorPalette.ColorText(ColorPalette.cp.wht, "10");
				}
			}

			//Colors
			//Hair
			foreach (var t in CharacterColors.GetAllValidHairColors())
			{
				var go = Instantiate<GameObject>(colorButtonPrefab);
				go.transform.SetParent(hairColorParent);
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;

				var cb = go.GetComponent<ColorButton>();
				cb.SetColor(t);
				hairColors.Add(cb);
			}
			//Eyes
			foreach (var t in CharacterColors.GetAllValidEyeColors())
			{
				var go = Instantiate<GameObject>(colorButtonPrefab);
				go.transform.SetParent(eyeColorParent);
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;

				var cb = go.GetComponent<ColorButton>();
				cb.SetColor(t);
				eyeColors.Add(cb);
			}
			//Skin
			foreach (var t in CharacterColors.GetAllValidSkinColors())
			{
				var go = Instantiate<GameObject>(colorButtonPrefab);
				go.transform.SetParent(skinColorParent);
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;

				var cb = go.GetComponent<ColorButton>();
				cb.SetColor(t);
				skinColors.Add(cb);
			}

			//Personalities
			chPersonality.ClearOptions();
			chPersonality.AddOptions(Utility.ListOfItemNames(CharacterSpeech.unlockedPersonalities));
		}
	}

	/**For each item in the list, add its index's corresponding value from the dictionary. It's a generic use one for when you don't need to sort over a list shorter than ALL the enums
	 */
	void AddValues(List<Dropdown.OptionData> list, System.Collections.Generic.Dictionary<int, int> valueDictionary)
	{
		for (int i = 0; i < list.Count; i++)
		{
			//Value to add to string
			int val = 0;

			//Actual value
			valueDictionary.TryGetValue(i, out val);

			//New straaaang
			list [i].text += ": " + ColorPalette.ColorText(ColorPalette.cp.wht, val.ToString());
		}
	}

	void SetDoneButtonStatus()
	{
		//Can we buy it?
		doneButton.interactable = Customization_CurrencyController.c.effectiveCurrency >= currentCost - originalCost;

		//Set text
		var sb = new System.Text.StringBuilder();
		if (doneButton.IsInteractable())
		{
			sb.Append("Total Cost: " + ColorPalette.ColorText(ColorPalette.cp.wht, currentCost.ToString()));
		}
		else
		{
			sb.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "Can't Afford: " + currentCost));
		}

		//Cost change tag
		string signAndColor = currentCost - originalCost < 0 ? "<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.blue4) + ">" : "<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.wht) + ">+";
		sb.Append(" (" + signAndColor + (currentCost - originalCost) + "</color>)");

//		doneButton.GetComponentInChildren<Text>().text = sb.ToString();
		totalCost.text = sb.ToString();
	}

	void UpdateMenuColors()
	{
		//Hair
		foreach (var t in hairColors)
		{
			t.selected = t == currentHair;
		}

		//Eyes
		foreach (var t in eyeColors)
		{
			t.selected = t == currentEye;
		}

		//Skin
		foreach (var t in skinColors)
		{
			t.selected = t == currentSkin;
		}
	}

	void Start()
	{
		InitialMenuSetup();

		CameraEffectsController.cec.canMove = false;
	}

	void OnDisable()
	{
		CameraEffectsController.cec.canMove = true;
	}
}
