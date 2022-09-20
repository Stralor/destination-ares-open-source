using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameEventSeriesData : ScriptableObject
{
	public string storyName;

	[SerializeField] List<EventStoreData> eventsInOrder = new List<EventStoreData>();
	[SerializeField] EventControlData eventController;

	/// <summary>
	/// Sets the series, splayed out across the game. First will go immediately, last will go at win condition. Everything else spaced out across the middle.
	/// </summary>
	public void ChooseMe()
	{
		StoryChooser.story.StartCoroutine(ChooseMeWhenReady());
	}

	private IEnumerator ChooseMeWhenReady()
	{
		yield return new WaitUntil(() => GameReference.r != null && ShipResources.res != null);

		Debug.Log("Choose me! " + storyName);

		//Get the scheduler
		var ges = FindObjectOfType<GameEventScheduler>();

		//Set valid loss events
		eventController.OverrideLossEvents(eventController.primaryCustomLossEvents);

		//Set events
		for (int i = 0; i < eventsInOrder.Count; i++)
		{
			//Initial
			if (i == 0)
				GameEventManager.gem.ScheduleTimedEvent(0, 0, 0, eventsInOrder [i]);
			//Final
			else if (i == eventsInOrder.Count - 1)
				GameEventManager.gem.ScheduleProgressEvent(100, eventsInOrder [i]);
			//Middles
			else
				ges.CreateEventByProgress((100 - ShipResources.res.progress) / (eventsInOrder.Count - 1) * i, EventCondition.Story, eventsInOrder [i], eventsInOrder [i].name);
		}

		//Don't need StoryChooser anymore now that the coroutine is done
		Destroy(StoryChooser.story);
	}
}
