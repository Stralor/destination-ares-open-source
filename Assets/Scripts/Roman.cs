using System.Text;
using System.Collections;
using System.Collections.Generic;

public static class Roman
{
	public static readonly Dictionary<char, int> RomanNumberDictionary;
	public static readonly Dictionary<int, string> NumberRomanDictionary;

	static Roman()
	{
		RomanNumberDictionary = new Dictionary<char, int>
		{
			{ 'I', 1 },
			{ 'V', 5 },
			{ 'X', 10 },
			{ 'L', 50 },
			{ 'C', 100 },
			{ 'D', 500 },
			{ 'M', 1000 },
		};
		
		NumberRomanDictionary = new Dictionary<int, string>
		{
			{ 1000, "M" },
			{ 900, "CM" },
			{ 500, "D" },
			{ 400, "CD" },
			{ 100, "C" },
			{ 90, "XC" },
			{ 50, "L" },
			{ 40, "XL" },
			{ 10, "X" },
			{ 9, "IX" },
			{ 5, "V" },
			{ 4, "IV" },
			{ 1, "I" },
		};
	}

	public static string To(int number)
	{
		var roman = new StringBuilder();
		
		foreach (var item in NumberRomanDictionary)
		{
			while (number >= item.Key)
			{
				roman.Append(item.Value);
				number -= item.Key;
			}
		}
		
		return roman.ToString();
	}

	public static int From(string roman)
	{
		int total = 0;
		
		int current, previous = 0;
		char currentRoman, previousRoman = '\0';
		
		for (int i = 0; i < roman.Length; i++)
		{
			currentRoman = roman [i];
			
			previous = previousRoman != '\0' ? RomanNumberDictionary [previousRoman] : '\0';
			current = RomanNumberDictionary [currentRoman];
			
			if (previous != 0 && current > previous)
			{
				total = total - (2 * previous) + current;
			}
			else
			{
				total += current;
			}
			
			previousRoman = currentRoman;
		}
		
		return total;
	}

	public static bool IsRoman(string stringToCheck)
	{
		for (int i = 0; i < stringToCheck.Length; i++)
		{
			if (!RomanNumberDictionary.ContainsKey(stringToCheck [i]))
				return false;
		}
		return true;
	}

	public static bool IsRoman(char charToCheck)
	{
		return IsRoman(charToCheck.ToString());
	}

	/// <summary>
	/// Finds any Roman Numerals at the end of a string, and increments them (if they need to be incremented).
	/// </summary>
	/// <returns>The original string with incremented end romans.</returns>
	/// <param name="input">Input string (with or without roman numerals on end).</param>
	/// <param name="conditionMethod">Condition method. Needs to return true while the string needs to be incremented.</param>
	public static string IncrementEndRomans(string input, System.Func<string, bool> conditionMethod = null)
	{
		//First, Trim()
		string trimmed = input.Trim();
		
		//Adjust Romans if necessary
		if (conditionMethod == null || conditionMethod(trimmed))
		{
			//Find any romans on the end
			var roman = new System.Text.StringBuilder();		//The string builder for the roman numerals we're finding
			for (int i = trimmed.Length - 1; i >= 0; i--)
			{
				//Is Roman?
				if (IsRoman(trimmed [i]))
					//Put it in our StringBuilder (we're working backwards)
					roman.Insert(0, trimmed [i]);
				//No more Romans
				else
					break;
			}

			//Get rid of the romans for now. We'll add them back in a bit.
			trimmed = TrimEndRomans(trimmed);
			
			//Increment our Romans (min "II", since the first might not have been numbered and we start at 1 not 0)
			int newRomanValue = From(roman.ToString()) + 1;
			newRomanValue = newRomanValue >= 2 ? newRomanValue : 2;
			
			do
			{
				//Change the string!
				input = trimmed + " " + To(newRomanValue);
				//Be ready to try the next value up, if we still are in the memorial!
				newRomanValue++;
			}
			//Only do it once if we're null, otherwise keep going until condition is met
			while (conditionMethod != null && conditionMethod(input));
		}

		return input;
	}

	/**Cut off any Roman Numerals at the end of a string. Also calls Trim() on the result.
	 */
	public static string TrimEndRomans(string input)
	{
		//Safety
		if (input == null || input.Length <= 0)
			return null;

		//Find out where the roman bits begin
		int romanIndex = -1;
		for (int i = input.Length - 1; i >= 0; i--)
		{
			if (IsRoman(input [i]))
				romanIndex = i;
			else
				break;
		}

		//Cut them out if they're there! Then Trim again, of course.
		if (romanIndex >= 0)
			input = input.Remove(romanIndex).Trim();	

		return input;
	}
}
