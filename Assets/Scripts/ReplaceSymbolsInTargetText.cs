using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class ReplaceSymbolsInTargetText
{

	static string targetNameSymbol = "*T*", shipNameSymbol = "*S*";
	//, wordPattern = @"\b\w+\b";


	/**Called to insert the target and ship's names into the text. Customizes the text, if you will.
	 * Returns the result.
	 * Target's name will be used on all instances of "*T*".
	 * Ship's name will be used on all instances of "*S*".
	 */
	public static string ReplaceSymbols(string textToEdit, string targetName = "Nothing", string shipName = "Ship", bool enforceValidity = true)
	{

		//Trim off white space
		if (targetName != null)
			targetName = targetName.Trim();
		if (shipName != null)
			shipName = shipName.Trim();

		//Ensure non-nulls
		if (targetName == null || targetName == "")
			targetName = "Nothing";
		if (shipName == null || shipName == "")
		{
			//Use last (non-tutorial) ship's name
			if (StatTrack.stats.memorial.Count > 0 && StatTrack.stats.memorial.Exists(vessel => !vessel.vesselName.ToLower().Contains("tutorial")))
				shipName = StatTrack.stats.memorial.FindLast(vessel => !vessel.vesselName.ToLower().Contains("tutorial")).vesselName;
			//No recent name
			else
				shipName = "Ship";
		}

		//Set up our returnable, without tampering with input.
		string editedText = textToEdit;

		//Change text
		if (enforceValidity)
		{
			//Target
			while (editedText.Contains(targetNameSymbol))
				editedText = editedText.Replace(targetNameSymbol, EnforceValidity(targetName, targetNameSymbol));
			//Ship
			while (editedText.Contains(shipNameSymbol))
				editedText = editedText.Replace(shipNameSymbol, EnforceValidity(shipName, shipNameSymbol));
		}
		else
		{
			editedText = ReplaceIterativelyWithSubstrings(editedText, targetNameSymbol, targetName);
			editedText = ReplaceIterativelyWithSubstrings(editedText, shipNameSymbol, shipName);
		}
		
		//Make sure first letter is capitalized (Mostly in case we inserted a word at the front)
		if (editedText.Length > 0)
			editedText = editedText.Substring(0, 1).ToUpper() + editedText.Substring(1);

		//Now let's dick around with color changes to dialogue
		editedText = Regex.Replace(editedText, @""".*""", new MatchEvaluator((Match m) => ColorPalette.ColorText(ColorPalette.cp.gry3, m.ToString())), RegexOptions.Singleline);

		//Return text
		return editedText;
	}

	/**Processes string and protects specific symbols by removing them from text.
	 */
	private static string EnforceValidity(string textToCheck, string symbolToSafeguard)
	{

		//Don't accept the symbol!
		if (textToCheck.Contains(symbolToSafeguard))
			return "<i>invalid</i>";

		//ONLY I GET TO USE MARKDOWN MWUAHAHAHA
		//(Check the valid text for those brackets)
		string checkedText = textToCheck;
		while (checkedText.Contains("<") && checkedText.Contains(">"))
			checkedText = checkedText.Replace("<", "").Replace(">", "");

		//Valid result!
		return checkedText;
	}

	private static string ReplaceIterativelyWithSubstrings(string textBody, string symbol, string newValue)
	{
		List<string> substrings = new List<string>();
		//Get substrings
		while (textBody.Contains(symbol))
		{
			//Find location
			int index = textBody.IndexOf(symbol);
			//Cut it out and add to substring
			substrings.Add(textBody.Substring(0, index + symbol.Length));
			//Shorten main text
			textBody = textBody.Substring(index + symbol.Length);
		}
		//Add remainder and clear editedText
		substrings.Add(textBody);
		textBody = "";

		//Replace and tack back on
		foreach (var t in substrings)
			textBody += t.Replace(symbol, newValue);

		return textBody;
	}
}
