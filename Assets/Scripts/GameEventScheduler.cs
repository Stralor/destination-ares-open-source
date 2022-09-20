using UnityEngine;
using System.Collections;
using System.Linq;

public class GameEventScheduler : MonoBehaviour
{
	
	//Declarations
	//Random number limit for days to add
	public int dice = 6;
	//Max amount of chances (less one) to NOT get helpful event
	public int noHelpBaseChance = 6;

	/**How close the the end is the player? Make events more common late game.
	 */
	float progressMultiplier
	{
		get
		{
			int gameProgress = ShipResources.res.progress;
			if (gameProgress <= 30)
				return 1f;
			else if (gameProgress <= 60)
				return 0.8f;
			else
				return 0.5f;
		}
	}



	/**This is used to create and populate events at a future time.
	 * Arguments are for calculation baseline (usually current time, though could create an event for different times).
	 * RandomHelpful might create a helpful event in place of a standard (and then will still call for a standard to be created)
	 */
	public void CreateEventByTime(int d, int h, int m, EventCondition condition = EventCondition.Standard, bool randomHelpful = true)
	{
		//Get the values to add to current time
		int min = (int)(Random.value * 60);
		int hr = (int)(Random.value * 24);
		int day = (int)((Random.Range(2, dice) + Offset()) * progressMultiplier);

		//Add arguments to generated values
		min += m;
		hr += h;
		day += d;

		//Set the actual date and time
		if (hr < h + 12 && day == d)
		{
			//Always a minimum adjustment of 12 hours
			hr += 12;
		}
		if (min >= 60)
		{
			min -= 60;
			hr++;
		}
		if (hr >= 24)
		{
			hr -= 24;
			day++;
		}

		//Now create the event

		//Random chance to create a Helpful event when trying for a Standard
		var chance = noHelpBaseChance - Offset() - PlayerPrefs.GetInt("Luck");
		chance = chance > 0 ? chance : 0;
		if (randomHelpful && condition == EventCondition.Standard && Random.Range(0, chance) == 0)
		{
			//The next will be standard, for sure. Just schedule it now, only make it sooner (even more dramatic at end game!)
			CreateEventByTime(day - (Offset() / 2), hr, min, EventCondition.Standard, false);
			//This one will be helpful
			condition = EventCondition.Helpful;
			//Keep it a tight
			day += 2;
		}

		//Access GameEvent script. Build Event.
		GameEventManager.gem.ScheduleTimedEvent(day, hr, min, condition: condition);

		Debug.Log("Scheduling timed event. Type: " + condition.ToString() + " (Offset is: " + Offset() + ")");
	}

	/**This is used to create and populate events at a rough progress point. For exact progress trigger, skip straight to GEM's ScheduleProgressEvent.
	 * Progress events can be scheduled backward (at a lower level than current progress) without instantly being triggered; particularly useful for story or catch-up ("helpful") events.
	 */
	public void CreateEventByProgress(int progress, EventCondition condition, EventStoreData specificEvent = null, string specificEventName = "", int range = 5)
	{
		int fudge = Random.Range(-range, range + 1);

		//Make sure it's bounded
		progress = Mathf.Clamp(progress + fudge, -9, 100);

		//Create the event
		GameEventManager.gem.ScheduleProgressEvent(progress, specificEvent, specificEventName, condition: condition);

		Debug.Log("Scheduling progress event. Type: " + condition.ToString());
	}

	/**Give the first few events some space for players to orient themselves. Also give berth when things are rough.
	 * Since helpful events are more likely with increased offset, it's also more likely that a helpful event will be followed by a long wait.
	 */
	int Offset()
	{
		//Return value
		int totalOffset = -GameClock.clock.day / 10;		//Reduced by longer trips
		totalOffset -= (StatTrack.stats.eventsSurvived - StatTrack.stats.eventsFailed) / 3;	//Reduced by successful events
		totalOffset -= GameReference.r.allCharacters.Where(ch => ch.status != Character.CharStatus.Dead).ToList().Count;	//Reduced by living crew
		totalOffset += 5 * (ShipResources.res.storageTotal - ShipResources.res.storageRemaining) / ShipResources.res.storageTotal; //Increased by low resources, in fifths
		totalOffset += StatTrack.stats.systemsBroken / 3;	//Increased by broken systems
		totalOffset += StatTrack.stats.systemsDestroyed;	//Increased by destroyed systems
		totalOffset += StatTrack.stats.crewDied;			//Increased by dead crew
		totalOffset += (StatTrack.stats.maxProgress - ShipResources.res.progress) / 5;	//Increased by progress loss

		//Return
		return totalOffset > 0 ? totalOffset : 0;
	}
}
