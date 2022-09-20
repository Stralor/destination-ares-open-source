using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Store - ", menuName = "Events/Event Store Data")]
public class EventStoreData : EventData
{

	/* This class stores an item from EventList, as parsed by EventListParser.
	 * It can also be used directly in Unity to create events without parsing.
	 * Represents a single Event "page," complete with settings, options, effects, and potential follow-up events.
	*/

	//ENUMS
	//List of possible event types

	public enum MinigameResult
	{
		Fail,
		Weak,
		Fair,
		Strong
	}

	//Minigame Result Tier Dictionaries
	public static readonly Dictionary<int, MinigameResult> ResultTierDictionary = new Dictionary<int, MinigameResult>
	{
		{ -1, MinigameResult.Fail },
		{ 0, MinigameResult.Weak },
		{ 15, MinigameResult.Fair },
		{ 30, MinigameResult.Strong }
	};
	public static readonly Dictionary<MinigameResult, int> TierResultDictionary = new Dictionary<MinigameResult, int>
	{
		{ MinigameResult.Fail, -1 },
		{ MinigameResult.Weak, 0 },
		{ MinigameResult.Fair, 15 },
		{ MinigameResult.Strong, 30 }
	};



	//Declarations
	//Is this event unique?
	public bool unique = false;
	[Tooltip("Condition(s) for the event's trigger. For the love of sanity, put the primary condition first.")]
	public List<EventCondition> conditions = new List<EventCondition>();
	//Requirement(s) to choose this event. For particular use in top level events.
	public List<EventRequirementData> requirements = new List<EventRequirementData>();

	[Tooltip("If a possible minigame result, what score is needed to choose this option.")]
	public MinigameResult minimumResult;

	[Tooltip("Number of chances to be drawn. Total chances from all included events create the odds. Default 1.")]
	public int chances = 1;

	[TextArea(1, 12)] public string eventText;
	//Text of the event
	public List<EventEffectData> effects = new List<EventEffectData>();
	public UnityEngine.Events.UnityEvent onActivate;
	public List<EventOptionData> options = new List<EventOptionData>();
	public List<Achievement> unlockAchievements = new List<Achievement>();


	/**Check to make sure that this Event's requirements are met, if they exists.
	 * ONLY checks the EventStore's requirements, not any options.
	 */
	public bool AreRequirementsMet()
	{
		if (!isUsable)
			return false;

		//Check requirements
		foreach (var req in requirements)
		{
			//Ignore nulls
			if (req == null)
				continue;
			//False if any fail
			if (!req.CheckRequirements())
				return false;
		}

		//If there aren't any requirements, or none turn up false, we're good!
		return true;
	}
}
