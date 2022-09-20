using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GetTime : MonoBehaviour
{

	[Tooltip("Use EventGameParameter's clock instead of the main game's GameClock.")]
	public bool
		useEventClockRatherThanGameClock;
	[SerializeField]
	Text
		clockUI;

	void Update()
	{
		if (!useEventClockRatherThanGameClock)
			clockUI.text = GameClock.clock.clockText;
		else
		{
			//Play audio when it changes and when not paused (aka, not at load)
			if (Time.timeScale > GameClock.PAUSE_SPEED && !clockUI.text.Contains(Mathf.CeilToInt(EventGameParameters.s.timeRemaining).ToString()))
				AudioClipOrganizer.aco.PlayAudioClip("Beep", null);

			//Text color
			if (EventGameParameters.s.timeRemaining == 0)	//Dead, prolly
				clockUI.color = ColorPalette.cp.wht;
			else if (EventGameParameters.s.timeRemaining < EventStoreData.TierResultDictionary [EventStoreData.MinigameResult.Fair])	//Danger
				clockUI.color = ColorPalette.cp.red4;
			else if (EventGameParameters.s.timeRemaining < EventStoreData.TierResultDictionary [EventStoreData.MinigameResult.Strong])	//Weakening
				clockUI.color = ColorPalette.cp.yellow4;

			//And set the time
			clockUI.text = Mathf.CeilToInt(EventGameParameters.s.timeRemaining).ToString();//ColorPalette.ColorText(textColor, Mathf.CeilToInt(EventGameParameters.s.timeRemaining).ToString());
		}
	}

	void Start()
	{
		if (useEventClockRatherThanGameClock)
			EventGameParameters.s.SetCacheAndDifficultyText(this);
	}
}
