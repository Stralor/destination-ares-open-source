using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Option - ", menuName = "Events/Event Option Data")]
public class EventOptionData : EventData
{

	/*
	This class is the container for the choices in a given EventStore
	*/

	//Minigame Difficulty enum
	public enum Minigame
	{
		NONE,
		EASY,
		MEDIUM,
		HARD
	}


	//Declarations
	//The text of the choice
	public string optionText;
	//Difficulty
	public Minigame minigameDifficulty;
	//Will this be hidden when unavailable?
	public bool hideIfUnavailable;
	//Requirement(s) for this option to be available
	public List<EventRequirementData> requirements = new List<EventRequirementData>();
	//Effects that go off when this is chosen.
	public List<EventEffectData> effects = new List<EventEffectData>();
	//The possible next events
	public List<EventStoreData> nextEventChances = new List<EventStoreData>();


	/**Checks if the requirements of both this option and at least one of it's nextEventChances is met.
	 * Returns false when the option has a requirement and it isn't met or all nextEventChances return false.
	 * If there are no nextEventChances, it only checks the option's requirement.
	 */
	public bool AreRequirementsMet()
	{
		if (!isUsable)
			return false;

		//First, check the option requirements
		foreach (var req in requirements)
		{
			//Ignore nulls
			if (req == null)
				continue;
			//Have to meet all the option requirements!
			if (!req.CheckRequirements())
				return false;
		}

		//Then check the requirements in each nextEventChance
		bool anyFalses = false;
		foreach (var nec in nextEventChances)
		{
			//Ignore nulls and disableds
			if (nec == null || !nec.isUsable)
				continue;

			if (nec.AreRequirementsMet())
				return true;	//We've already checked the option's requirements, so we can exit now!
			else
				anyFalses = true;
		}

		//If we get here, there may have been requirements in the nextEventChances, but none came up true.
		if (anyFalses)
			return false;

		//If we got here, there were no requirements at all
		return true;
	}

	/**Draw a next event from the available chances. Random within tier, tier by event result.
	 * (Regarding score: -2 is used to ignore score sorting [i.e., no minigame played], -1 is used for failures, and everything greater assumes success)
	 */
	public EventStoreData ChooseNextEvent(int score)
	{

		//Do any effects
		foreach (EventEffectData ee in effects)
			if (ee != null)
				ee.DoEffect();

		//We're done if there aren't any enabled EventChances
		if (!nextEventChances.Exists(ech => ech != null && ech.isUsable))
			return null;

		//Or we're still here. Let's math it out!

		//Create a pool of appropriate events
		List<EventStoreData> appropriateEvents = new List<EventStoreData>();

		//Use all valid events if requested in the call
		if (score == -2)
		{
			foreach (var t in nextEventChances)
				if (EventStoreIsValid(t))
					appropriateEvents.Add(t);
		}

		//Otherwise sort out appropriate values (Make sure we have a populated list!)
		else
		{
			while (appropriateEvents.Count == 0)
			{
				//Get the score's result level
				int resultValue = -100;	//Something far from actual values

				//On the money
				if (EventStoreData.ResultTierDictionary.ContainsKey(score))
					resultValue = score;

				//Or find an appropriate tier
				else
				{
					foreach (int t in EventStoreData.ResultTierDictionary.Keys)
					{
						//We only care about result tiers that are lower than score, but we want the highest ones
						if (score > t && t > resultValue)
							//Update resultValue to match any valid ResultTierDictionary keys
							resultValue = t;
					}
				}
				
				var result = EventStoreData.ResultTierDictionary [resultValue];

				//Populate list
				foreach (var es in nextEventChances)
				{
					if (EventStoreIsValid(es) && es.minimumResult == result)
						appropriateEvents.Add(es);
				}

				//We're not gonna use score again, so let's butcher it in case of another loop
				score = resultValue - 1;

				//Safety if there were no events at or below our desired level that also have requirements met
				if (score < -1 && appropriateEvents.Count == 0)
				{
					Debug.Log("No appropriate results available. Choosing randomly from whole pool.");
					appropriateEvents = nextEventChances;
				}
			}
		}

		var chosenEvent = ChooseNextEvent(appropriateEvents);

		if (PlayerPrefs.GetInt("Luck") == 1)
		{
			var reroll = Random.Range(0, 4) > (int) chosenEvent.minimumResult;
			if (reroll)
				chosenEvent = ChooseNextEvent(appropriateEvents);
		}

		return chosenEvent;
	}

	private EventStoreData ChooseNextEvent(List<EventStoreData> appropriateEvents)
	{
		//Choose at random from appropriateEvents. Can't just use count. Have to add the chance values to go through the pool, first.
		int totalChances = 0;
		foreach (var es in appropriateEvents)
		{
			//This 'if' is redundant. But who knows when I'll change something and fuck everything up without it.
			if (EventStoreIsValid(es))
			{
				totalChances += es.chances;
			}
		}

		//Finally, pick one of them!
		int chosenChance = Random.Range(0, totalChances);
		int processedChances = 0;
		foreach (var es in appropriateEvents)
		{
			//This 'if' is a necessary counterpart to that last redundant 'if'
			if (EventStoreIsValid(es))
				//Add this event's chance to see if we've made it
				processedChances += es.chances;

			if (processedChances > chosenChance)
			{
				//This is it, return it
				return es;
			}
		}
		
		//We might get here if there were non-null events that otherwise weren't suitable. It's unlikely with all of the safeties, though.
		return null;
	}
	
	/**Do the damn checks for me.*/
	bool EventStoreIsValid(EventStoreData es)
	{
		return es != null && es.isUsable && es.AreRequirementsMet();
	}
}
