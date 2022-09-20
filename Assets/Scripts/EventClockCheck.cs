using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventClockCheck : MonoBehaviour
{

	/* This method checks the GameEventManager's scheduled events' trigger times against the current GameClock time.
	 * If the time to trigger an event has come (or passed) this will send the activate signal. */

	[TextArea(1, 1)]
	public string nextEvent;

	/**Minimum time when we'll allow another event. An event will pop if it past its scheduled time AND past this target time. */
	public int minimumDay = 0, minimumHour = 0, minimumMinute = 0;

	/**Minimum time between event activations */
	const int HOURS_BETWEEN_POPS = 12;

	int lastProgress = 0;

	void Update()
	{
		//Check when an event isn't already running, and nothing has popped too recently
		if (!GameEventManager.gem.eventIsActive)
		{
			//Go through the scheduled events
			foreach (var t in GameEventManager.gem.scheduledEvents)
			{
				//Progress Event?
				if (t.progressType)
				{
					//Is it the right progress? (Or did we somehow skip the correct amount of progress)
					if (ShipResources.res.progress == t.progress
					    || (ShipResources.res.progress > t.progress && t.progress > lastProgress)
					    || (ShipResources.res.progress < t.progress && t.progress < lastProgress))
					{
						//Cool, act.
						GameEventManager.gem.ActivateEvent(t.eventStoreData);
						AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);
						//Prevent stacked event
						SetMinTimeForNextPop();
						break;
					}
				}

				//Is it the right day?
				else if (GameClock.clock.day == t.day && GameClock.clock.day >= minimumDay)
				{
					//Is it the right hour?
					if (GameClock.clock.hour == t.hour && GameClock.clock.hour >= minimumHour)
					{
						//Is it the right minute?
						if (GameClock.clock.minute >= t.minute && GameClock.clock.minute >= minimumMinute)
						{
							//Cool, act.
							GameEventManager.gem.ActivateEvent(t.eventStoreData);
							AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);
							//Prevent stacked event
							SetMinTimeForNextPop();
							break;
						}
					} 
					//Did the correct hour already pass?
					else if (GameClock.clock.hour > t.hour && GameClock.clock.hour > minimumHour)
					{
						//Yes! Act!
						GameEventManager.gem.ActivateEvent(t.eventStoreData);
						AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);
						//Prevent stacked event
						SetMinTimeForNextPop();
						break;
					}
				}
				//Did the correct day already pass?
				else if (GameClock.clock.day > t.day && GameClock.clock.day > minimumDay)
				{
					//Yes! Act!
					GameEventManager.gem.ActivateEvent(t.eventStoreData);
					AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);
					//Prevent stacked event
					SetMinTimeForNextPop();
					break;
				}
			}

			//Keep lastProgress updated so we know if we jumped progress a bunch
			lastProgress = ShipResources.res.progress;

			#if UNITY_EDITOR
			//Update our little "next event" string for debugging
			int d = -1, h = 0, m = 0, p = 0;
			bool progressType = false;
			string condition = "";

			foreach (var t in GameEventManager.gem.scheduledEvents)
			{
				//find one with shortest timer

				//beat out other progressTypes using progress
				if (t.progressType && progressType && t.progress > p)
					continue;
				//don't use progresstypes when we have others
				if (t.progressType && !progressType)
					continue;
				//Initial clear for timed
				if (!t.progressType && t.day > d && d != -1)
					continue;
				//tie breakers
				if (!t.progressType && t.day == d && (t.hour > h || (t.hour == h && t.minute > m)))
					continue;

				//Set values
				d = t.day;
				h = t.hour;
				m = t.minute;
				p = t.progress;
				progressType = t.progressType;
				condition = t.eventStoreData.conditions [0].ToString();
			}

			//Use the values to set the string for inspector debugging
			if (d != -1)
			{	
				if (progressType)
					nextEvent = string.Format("{0}: Progress {1}. {2} more scheduled.", condition, p, GameEventManager.gem.scheduledEvents.Count - 1);
				else
					nextEvent = string.Format("{0}: Day {1}, {2:00}:{3:00}. {4} more scheduled.", condition, d, h, m, GameEventManager.gem.scheduledEvents.Count - 1);
			}
			else
				nextEvent = "No events currently scheduled.";

			#endif
		}
	}


	public void SetMinTimeForNextPop()
	{
		//Base
		minimumMinute = Random.Range(0, 60);
		minimumHour = GameClock.clock.hour + HOURS_BETWEEN_POPS;
		minimumDay = GameClock.clock.day + 1; //+1 because events all add a day to the clock anyway

		//Adjusted
		while (minimumHour > 23)
		{
			minimumHour -= 24;
			minimumDay++;
		}
	}
}
