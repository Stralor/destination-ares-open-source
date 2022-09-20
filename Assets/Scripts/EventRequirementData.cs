using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class EventRequirementData : EventData
{
	/**SHH IT'S SECRET*/
	public bool hiddenRequirement = false;

	[Tooltip("Instead of trying to find positive matches, return true so long as no full matches are found.")]
	public bool	invertRequirements;

	/**The text displayed in-game about the requirement, if not hidden.*/
	public abstract string RequirementText { get; }

	/**Check the requirements for the given condition(s).*/
	public abstract bool CheckRequirements();

	/**Utility method that iterates through a given list of valid conditions, creating a string for display. Used in some complicated EventRequirement's requirementText property.*/
	public string BuildStringSectionFromList<T>(List<T> list)
	{
		var stringBuilder = new System.Text.StringBuilder();

		for (int i = 0; i < list.Count; i++)
		{
			//This valid value
			stringBuilder.Append(" " + list [i].ToString());

			//Penultimate?
			if (i + 2 == list.Count)
			{
				//Not just 2?
				if (list.Count != 2)
					stringBuilder.Append(",");
				//The "or"
				stringBuilder.Append(" or");
			}
			//More?
			else if (i + 1 < list.Count)
				stringBuilder.Append(",");
			//Last
			else
				stringBuilder.Append(";");
		}

		return stringBuilder.ToString();
	}
}
