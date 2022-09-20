using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FetchLastStatTrackValue : MonoBehaviour
{
	//I should have done these as a struct with a CustomPropertyDrawer per stat, but I have a week to finish this game
	public List<string> stats = new List<string>();
	public List<bool> showStatSign = new List<bool>();
	public List<string> personalPrefixPerStat = new List<string>();
	public List<string> personalSuffixPerStat = new List<string>();
	public List<string> nextLinePerStat = new List<string>();

	public int statTypefaceSize;

	void Start()
	{
		var text = GetComponent<Text>();
		text.text = "";

		//Lets build a string one stat at a time
		foreach (var t in stats)
		{
			string prefix = "", suffix = "", nextLine = "", valueString = "";

			//Prefix get
			if (personalPrefixPerStat.Count > stats.IndexOf(t) && personalPrefixPerStat [stats.IndexOf(t)] != null)
				prefix = personalPrefixPerStat [stats.IndexOf(t)];

			//Suffix get
			if (personalSuffixPerStat.Count > stats.IndexOf(t) && personalSuffixPerStat [stats.IndexOf(t)] != null)
				suffix = personalSuffixPerStat [stats.IndexOf(t)];

			//Next line get
			if (nextLinePerStat.Count > stats.IndexOf(t) && nextLinePerStat [stats.IndexOf(t)] != null)
			{
				string nextLineContent = nextLinePerStat [stats.IndexOf(t)];
				if (nextLineContent != "")
					nextLine = "\n" + nextLineContent;
			}

			//Stat value get
			int value = (int)StatTrack.endGameStats.Find(obj => obj.Name.ToLower() == t.ToLower()).GetValue(StatTrack.stats, null);

			//Sign get
			if (showStatSign.Count > stats.IndexOf(t) && showStatSign [stats.IndexOf(t)])
			{
				if (value > 0)
					valueString = "+";
			}

			//Typeface size
			if (statTypefaceSize != 0)
				valueString += "<size=" + statTypefaceSize + ">" + prefix + value + suffix + "</size>";
			else
				valueString += prefix + value + suffix;

			//This stat can now be added to the string
			text.text += valueString + nextLine;
		}
	}
}
