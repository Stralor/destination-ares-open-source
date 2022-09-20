using UnityEngine;
using System.Collections.Generic;

public class EventControlData : EventData
{
	public EventDataList primaryCustomLossEvents;


	public void FadeBlackout(int blackoutLayer)
	{
		var bc = FindObjectOfType<BlackoutController>();

		bc.FadeBlackout(blackoutLayer);
	}

	public void OverrideLossEvents(EventDataList validLossEvents)
	{
		//Ignore this is we have no events to replace the current list with
		if (validLossEvents == null || validLossEvents.list.Count == 0)
			return;

		//Clear out old ones
		GameEventManager.gem.lossEvents.Clear();

		//Toggle new ones
		validLossEvents.list.ForEach(loss => GameEventManager.gem.lossEvents.Add((EventStoreData)loss));
	}

	public void ChangeGameResultText(string text)
	{
		FindObjectOfType<EndGameChecker>().ChangeResultText(text);
	}

	public void PlayVictoryMusic()
	{
		MusicController.mc.SetSong(MusicController.mc.victory);
		MusicController.mc.playlist.Clear();
		MusicController.mc.playlist.Add(MusicController.mc.victory);
	}
}
