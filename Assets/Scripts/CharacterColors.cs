using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class CharacterColors : MonoBehaviour
{
	public List<SpriteRenderer> clothes = new List<SpriteRenderer>(), eyes = new List<SpriteRenderer>(), hair = new List<SpriteRenderer>(), skin = new List<SpriteRenderer>();

	public Color eyeColor, hairColor, skinColor;

	public Character.Team team = Character.Team.None;

	//Color Sets
	public static List<string> basicEyeColors = new List<string>()
	{
		"blk",
		"blue2"
	};
	public static List<string> basicHairColors = new List<string>()
	{
		"blk",
		"wht",
		"yellow0",
		"yellow2",
		"yellow4",
		"red3"
	};
	public static List<string> basicSkinColors = new List<string>()
	{
		"yellow0",
		"yellow1",
		"yellow2",
		"yellow3",
		"yellow4",
	};

	/**Colors universally available, usually from achievements
	 */
	public static List<string> unlockedColors = new List<string>()
	{
//		"red4",
//		"red3",
//		"red2",
//		"red1",
//		"red0",
//		"blue4",
//		"blue3",
//		"blue2",
//		"blue1",
//		"blue0",
//		"yellow0",
//		"yellow1",
//		"yellow2",
//		"yellow3",
//		"yellow4",
//		"blk",
//		"wht",
//		"gry3",
//		"gry2",
//		"gry1"
	};

	/**Set the sprite colors (incl. job team clothes).
	 */
	public void UpdateColors()
	{
		var me = GetComponent<Character>();
		if (me != null)
			team = me.team;

		//Job Team
		if (team == Character.Team.Science)
		{
			foreach (SpriteRenderer sr in clothes)
				sr.color = ColorPalette.cp.red2;
		}
		else if (team == Character.Team.Engineering)
		{
			foreach (SpriteRenderer sr in clothes)
				sr.color = ColorPalette.cp.yellow2;
		}
		else if (team == Character.Team.Medical)
		{
			foreach (SpriteRenderer sr in clothes)
				sr.color = ColorPalette.cp.blue2;
		}
		else
		{
			foreach (SpriteRenderer sr in clothes)
				sr.color = ColorPalette.cp.gry2;
		}

		//Eyes, Hair, and Skin
		foreach (var t in eyes)
			t.color = eyeColor;
		foreach (var t in hair)
			t.color = hairColor;
		foreach (var t in skin)
			t.color = skinColor;
	}

	/**Choose valid random colors.
	 * ignoreUnlocked will limit to the basic pools.
	 */
	public void Randomize(bool ignoreUnlocked = false)
	{
		eyeColor = ChooseRandomColor(basicEyeColors, ignoreUnlocked);
		hairColor = ChooseRandomColor(basicHairColors, ignoreUnlocked);
		skinColor = ChooseRandomColor(basicSkinColors, ignoreUnlocked);
		UpdateColors();
	}

	/**Parses string lists for a valid random color (from ColorPalette.cp)
	 */
	public static Color ChooseRandomColor(List<string> specificSet = null, bool ignoreUnlocked = false)
	{
		var colors = GetAllValidColors(specificSet, ignoreUnlocked);

		//Get one
		return colors [Random.Range(0, colors.Count)];
	}

	public static List<Color> GetAllValidEyeColors()
	{
		return GetAllValidColors(basicEyeColors);
	}

	public static List<Color> GetAllValidHairColors()
	{
		return GetAllValidColors(basicHairColors);
	}

	public static List<Color> GetAllValidSkinColors()
	{
		return GetAllValidColors(basicSkinColors);
	}

	/**Does the actual parsing of string list to get all the valid colors
	 */
	static List<Color> GetAllValidColors(List<string> specificSet, bool ignoreUnlocked = false)
	{
		//What we can choose from
		var fullRange = new List<string>();

		//Unlocked
		if (!ignoreUnlocked)
			fullRange.AddRange(unlockedColors);

		//Specific Set
		if (specificSet != null)
			fullRange.AddRange(specificSet.Where(obj => !fullRange.Contains(obj)));

		//More Lists
		var validColors = new List<Color>();
		var colorPaletteFields = typeof(ColorPalette).GetFields().ToList();	//A lil reflection

		//Translate fullRange to validColors
		foreach (var t in fullRange)
		{
			validColors.Add((Color)colorPaletteFields.Find(obj => obj.Name == t).GetValue(ColorPalette.cp));
		}

		//Return the found colors
		return validColors;
	}

	void Start()
	{
		UpdateColors();
	}
}
