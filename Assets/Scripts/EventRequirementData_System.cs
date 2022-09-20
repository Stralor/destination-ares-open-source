using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Req Sys - ", menuName = "Events/Event Requirement Data (System)")]
public class EventRequirementData_System : EventRequirementData
{

	/* Just a bunch of possible requirements for an option to be available.
	 * Filled out as needed.
	 * 
	 * Each List only needs one of it's contents to be true, if there are any contents. Thus a list of all options is equivalent to an empty list.
	 */

	[Tooltip("How many qualifying systems must be found? Ignored if there are no system requirements.")]
	public int
		systemsRequired = 1;

	public List<ShipSystem.SysFunction> system = new List<ShipSystem.SysFunction>();
	public List<ShipSystem.SysCondition> systemCondition = new List<ShipSystem.SysCondition>();
	public List<ShipSystem.SysStatus> systemStatus = new List<ShipSystem.SysStatus>();
	public List<ShipSystem.SysQuality> systemQuality = new List<ShipSystem.SysQuality>();
	public List<ShipSystem.SysKeyword> systemKeyword = new List<ShipSystem.SysKeyword>();

	public override string RequirementText
	{ 
		get
		{
			var stringBuilder = new System.Text.StringBuilder();

			//System requirements
			if (system.Count > 0 || systemCondition.Count > 0 || systemStatus.Count > 0 || systemQuality.Count > 0 || systemKeyword.Count > 0)
			{
				//Inversion
				if (invertRequirements)
					stringBuilder.Append("No");

				//Function
				if (system.Count > 0)
				{
					//Add system types
					stringBuilder.Append(BuildStringSectionFromList(system));
					//Clear last char
					stringBuilder.Remove(stringBuilder.Length - 1, 1);
				}
				//Generic
				else
					stringBuilder.Append("System");

				//Number
				stringBuilder.Append(" (" + systemsRequired + ")");

				//Any other requirements related to this system? Let's label them
				if (systemCondition.Count > 0 || systemStatus.Count > 0 || systemQuality.Count > 0 || systemKeyword.Count > 0)
					stringBuilder.Append(":");

				//Status
				stringBuilder.Append(BuildStringSectionFromList(systemStatus));

				//Condition
				stringBuilder.Append(BuildStringSectionFromList(systemCondition));

				//Quality
				stringBuilder.Append(BuildStringSectionFromList(systemQuality));

				//Keyword
				stringBuilder.Append(BuildStringSectionFromList(systemKeyword));

				//Remove the last character, if it's a semicolon
				stringBuilder.Replace(";", "", stringBuilder.Length - 1, 1);
			}

			return stringBuilder.ToString();
		}
	}

	/**Finds all systems that match the requirements in a given list.
	 * Then checks each of those systems against the requirements in the other lists.
	 * If any system gets through all of this, returns true. Otherwise, returns false.
	 */
	public override bool CheckRequirements()
	{
		if (!isUsable)
			return false;

		//Process allSys based on first available criterion.
		if (system.Count > 0)
		{
			//Start at the top with all of the systems of these types
			return CheckSysFunction(GameReference.r.allSystems);
		}
		else if (systemCondition.Count > 0)
		{
			//We can start with the systems at these conditions
			return CheckSysCondition(GameReference.r.allSystems);
		}
		else if (systemStatus.Count > 0)
		{
			//We can start with the systems at these statuses
			return CheckSysStatus(GameReference.r.allSystems);
		}
		else if (systemQuality.Count > 0)
		{
			//We can start with systems of these qualities
			return CheckSysQuality(GameReference.r.allSystems);
		}
		else if (systemKeyword.Count > 0)
		{
			//We can start and end with the systems using these keywords
			return CheckSysKeyword(GameReference.r.allSystems);
		}
		else
		{
			//There are no system-based requirements.
			return true;
		}
	}


	/*
	 * CHECK SYSTEM CHAIN
	 */

	/**Searches given list of systems for any matches with any item on the function list.
	* SubMethod to CheckSystems. Called by CheckSystems or it's chain.
	*/
	private bool CheckSysFunction(List<ShipSystem> input)
	{

		//Let's skip this if there are no function requirements
		//NOTE, while necessary for the methods in the chain, this is redundant in this specific method because of context
		if (system.Count == 0)
			return CheckSysCondition(input);

		//Need a list to pass along to the next checker
		List<ShipSystem> output = new List<ShipSystem>();

		//Populate output for next checker
		foreach (ShipSystem.SysFunction req in system)
		{
			foreach (ShipSystem ss in input)
			{
				if (ss.function == req)
				{
					output.Add(ss);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < systemsRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckSysCondition(output);
	}

	/**Searches given list of systems for any matches with any item on the condition list.
	 * SubMethod to CheckSystems. Called by CheckSystems or it's chain.
	 */
	private bool CheckSysCondition(List<ShipSystem> input)
	{

		//Let's skip this if there are no condition requirements
		if (systemCondition.Count == 0)
			return CheckSysStatus(input);

		//Need a list to pass along to the next checker
		List<ShipSystem> output = new List<ShipSystem>();

		//Populate output for next checker
		foreach (ShipSystem.SysCondition req in systemCondition)
		{
			foreach (ShipSystem ss in input)
			{
				if (ss.condition == req)
				{
					output.Add(ss);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < systemsRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckSysStatus(output);
	}

	/**Searches given list of systems for any matches with any item on the status list.
	 * SubMethod to CheckSystems. Called by CheckSystems or it's chain.
	 */
	private bool CheckSysStatus(List<ShipSystem> input)
	{

		//Let's skip this if there are no status requirements
		if (systemStatus.Count == 0)
			return CheckSysQuality(input);

		//Need a list to pass along to the next checker
		List<ShipSystem> output = new List<ShipSystem>();

		//Populate output for next checker
		foreach (ShipSystem.SysStatus req in systemStatus)
		{
			foreach (ShipSystem ss in input)
			{
				if (ss.status == req)
				{
					output.Add(ss);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < systemsRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckSysQuality(output);
	}

	/**Searches given list of systems for any matches with any item on the quality list.
	 * SubMethod to CheckSystems. Called by CheckSystems or it's chain.
	 */
	private bool CheckSysQuality(List<ShipSystem> input)
	{

		//Let's skip this if there are no quality requirements
		if (systemQuality.Count == 0)
			return CheckSysKeyword(input);

		//Need a list to pass along to the next checker
		List<ShipSystem> output = new List<ShipSystem>();

		//Populate output for next checker
		foreach (ShipSystem.SysQuality req in systemQuality)
		{
			foreach (ShipSystem ss in input)
			{
				if (ss.quality == req)
				{
					output.Add(ss);
				}
			}
		}

		//We're done if we didn't find enough valid parts to pass along
		if (output.Count < systemsRequired)
			return invertRequirements;
		//Or keep the chain going!
		else
			return CheckSysKeyword(output);
	}

	/**Searches given list of systems for any matches with any item on the keyword list.
	 * SubMethod to CheckSystems. Called by CheckSystems or it's chain.
	 */
	private bool CheckSysKeyword(List<ShipSystem> input)
	{
		//Final method in chain! (Finally)

		//We're done if there are no keyword requirements
		if (systemKeyword.Count == 0)
			return !invertRequirements;

		//Need a list to count how many we found!
		List<ShipSystem> output = new List<ShipSystem>();

		//Or we should check for matches
		foreach (ShipSystem.SysKeyword req in systemKeyword)
		{
			foreach (ShipSystem ss in input)
			{
				if (ss.keyCheck(req))
				{
					output.Add(ss);
				}
			}
		}

		//Hopefully we found enough! We've come far enough.
		if (output.Count < systemsRequired)
			return invertRequirements;
		else
			return !invertRequirements;
	}
}