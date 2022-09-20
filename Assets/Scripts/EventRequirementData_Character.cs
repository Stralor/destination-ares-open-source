using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Req Char - ", menuName = "Events/Event Requirement Data (Character)")]
public class EventRequirementData_Character : EventRequirementData
{

	/* Just a bunch of possible requirements for an option to be available.
	 * Filled out as needed.
	 * 
	 * Each List only needs one of it's contents to be true, if there are any contents. Thus a list of all options is equivalent to an empty list.
	 */

	[Tooltip("How many qualifying characters must be found? Ignored if there are no character requirements.")]
	public int
		charactersRequired = 1;

	public List<Character.CharStatus> characterStatus = new List<Character.CharStatus>();
	public List<Character.Team> characterTeam = new List<Character.Team>();
	public List<Character.CharRoles> characterRole = new List<Character.CharRoles>();
	public List<Character.CharSkill> characterSkill = new List<Character.CharSkill>();
	public List<Character.Task> characterTask = new List<Character.Task>();


	public override string RequirementText
	{ 
		get
		{
			var stringBuilder = new System.Text.StringBuilder();

			//Character requirements
			if (characterStatus.Count > 0 || characterRole.Count > 0 || characterSkill.Count > 0 || characterTask.Count > 0)
			{
				//Inversion
				if (invertRequirements)
					stringBuilder.Append("No ");

				//Count
				stringBuilder.Append("Crew (" + charactersRequired + "):");

				//Status
				stringBuilder.Append(BuildStringSectionFromList(characterStatus));

				//Team
				stringBuilder.Append(BuildStringSectionFromList(characterTeam));

				//Role
				stringBuilder.Append(BuildStringSectionFromList(characterRole));

				//Skill
				if (characterSkill.Count > 0)
					stringBuilder.Append(" with skill in");
				stringBuilder.Append(BuildStringSectionFromList(characterSkill));

				//Task
				if (characterSkill.Count > 0)
					stringBuilder.Append(" with task");
				stringBuilder.Append(BuildStringSectionFromList(characterTask));

				//Remove the last character, if it's a semicolon
				stringBuilder.Replace(";", "", stringBuilder.Length - 1, 1);
			}

			return stringBuilder.ToString();
		}
	}

	/**Finds all characters that match the requirements in a given list.
	 * Then checks each of those systems against the requirements in the other lists.
	 * If any character gets through all of this, returns true. Otherwise, returns false.
	 */
	public override bool CheckRequirements()
	{
		if (!isUsable)
			return false;

		//Process allChar based on first available criterion.
		if (characterStatus.Count > 0)
		{
			//Start at the top with all of the characters in these statuses
			return CheckCharStatus(GameReference.r.allCharacters);
		}
		else if (characterTeam.Count > 0)
		{
			//We can start with the characters with these roles
			return CheckCharTeam(GameReference.r.allCharacters);
		}
		else if (characterRole.Count > 0)
		{
			//We can start with the characters with these roles
			return CheckCharRole(GameReference.r.allCharacters);
		}
		else if (characterSkill.Count > 0)
		{
			//We can start with the characters with these skills
			return CheckCharSkill(GameReference.r.allCharacters);
		}
		else if (characterTask.Count > 0)
		{
			//We can start and end with characters doing these tasks
			return CheckCharTask(GameReference.r.allCharacters);
		}
		else
		{
			//There are no character-based requirements.
			return true;
		}
	}


	/*
	 * CHECK CHARACTERS CHAIN
	 */

	/**Searches given list of characters for any matches with any item on the status list.
	* SubMethod to CheckCharacters. Called by CheckCharacters or it's chain.
	*/
	private bool CheckCharStatus(List<Character> input)
	{

		//Let's skip this if there are no required statuses
		//NOTE, while necessary for the methods in the chain, this is redundant in this specific method because of context
		if (characterStatus.Count == 0)
			return CheckCharTeam(input);

		//Need a list to pass along to the next checker
		List<Character> output = new List<Character>();

		//Populate output for next checker
		foreach (Character.CharStatus req in characterStatus)
		{
			foreach (Character ch in input)
			{
				if (ch.status == req)
				{
					output.Add(ch);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < charactersRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckCharTeam(output);
	}

	/**Searches given list of characters for any matches with any item on the role list.
	 * SubMethod to CheckCharacters. Called by CheckCharacters or it's chain.
	 */
	private bool CheckCharTeam(List<Character> input)
	{

		//Let's skip this if there are no required roles
		if (characterTeam.Count == 0)
			return CheckCharSkill(input);

		//Need a list to pass along to the next checker
		List<Character> output = new List<Character>();

		//Populate output for next checker
		foreach (Character.Team req in characterTeam)
		{
			foreach (Character ch in input)
			{
				if (ch.team == req)
				{
					output.Add(ch);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < charactersRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckCharRole(output);
	}

	/**Searches given list of characters for any matches with any item on the role list.
	 * SubMethod to CheckCharacters. Called by CheckCharacters or it's chain.
	 */
	private bool CheckCharRole(List<Character> input)
	{

		//Let's skip this if there are no required roles
		if (characterRole.Count == 0)
			return CheckCharSkill(input);

		//Need a list to pass along to the next checker
		List<Character> output = new List<Character>();

		//Populate output for next checker
		foreach (Character.CharRoles req in characterRole)
		{
			foreach (Character ch in input)
			{
				if (ch.roles.Contains(req))
				{
					output.Add(ch);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < charactersRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckCharSkill(output);
	}

	/**Searches given list of characters for any matches with any item on the skill list.
	 * SubMethod to CheckCharacters. Called by CheckCharacters or it's chain.
	 */
	private bool CheckCharSkill(List<Character> input)
	{

		//Let's skip this if there are no required skills
		if (characterSkill.Count == 0)
			return CheckCharTask(input);

		//Need a list to pass along to the next checker
		List<Character> output = new List<Character>();

		//Populate output for next checker
		foreach (Character.CharSkill req in characterSkill)
		{
			foreach (Character ch in input)
			{
				if (ch.skills.Contains(req))
				{
					output.Add(ch);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < charactersRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckCharTask(output);
	}

	/**Searches given list of characters for any matches with any item on the task list.
	 * SubMethod to CheckCharacters. Called by CheckCharacters or it's chain.
	 */
	private bool CheckCharTask(List<Character> input)
	{
		//The final checker of them all

		//We're done if there are no required tasks
		if (characterTask.Count == 0)
			return !invertRequirements;

		//Need a list to count how many we found!
		List<Character> output = new List<Character>();

		//Return true if any character in the input has this task
		foreach (Character.Task req in characterTask)
		{
			foreach (Character ch in input)
			{
				if (ch.task == req)
				{
					output.Add(ch);
				}
			}
		}

		//The bitter end! Did we find enough?
		if (output.Count < charactersRequired)
			return invertRequirements;
		else
			return !invertRequirements;
	}
}