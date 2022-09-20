using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameEventManager : MonoBehaviour
{

	/**Singleton-like reference to the GameEventManager component.
	 */
	public static GameEventManager gem;

	//Declarations
	public bool eventIsActive { get; private set; }
	//Is the event active?
	private bool ready = false;

	//A list of all the events coded into the game.
	public List<EventStoreData> allEvents = new List<EventStoreData>();
	public EventDataList defaultLossList;
	//List of valid loss events
	public List<EventStoreData> lossEvents = new List<EventStoreData>();
	//A list of all eventControllers in the scene
	public List<EventControlData> customEventControllers = new List<EventControlData>();

	//Current Event
	public EventStoreData currentEvent { get; private set; }
	//Last Event
	private EventStoreData _lastEvent;

	//Next events
	public List<ScheduledEvent> scheduledEvents = new List<ScheduledEvent>();
	public List<EventEffectData> preppedEffects = new List<EventEffectData>();


	//Cache the UI elements
	[SerializeField]
	private GameObject eventPopUp;
	public Image eventBorder, eventHeaderBorder;
	public Text eventHeader;
	public Text eventLastAction;
	public Text eventBody;

	public GameObject optionPrefab;
	public Transform optionGroup;
	[SerializeField]
	private List<GameObject> optionPool = new List<GameObject>();


	public struct ScheduledEvent
	{
		public EventStoreData eventStoreData;
		public int day, hour, minute, progress;
		public bool progressType;
	}


	#region EVENT CONTROL

	/**Create and define the next event in line to trigger at a precise clock time.
	 * Condition is ignored if specificEventName is provided.
	 * Note: starts a coroutine that waits for the GameEventManager and ShipResources objects to both be ready
	 */
	public void ScheduleTimedEvent(int d, int h, int m, EventStoreData specificEvent = null, string specificEventName = "", EventCondition condition = EventCondition.Standard, int attempt = 0)
	{
		StartCoroutine(ScheduleTimedEventWhenReady(d, h, m, specificEvent, specificEventName, condition, attempt));
	}

	/**Create and define the next event in line to trigger once a given amount of progress has been made.
	 * Condition is ignored if specificEventName is provided.
	 * Note: starts a coroutine that waits for the GameEventManager and ShipResources objects to both be ready
	 */
	public void ScheduleProgressEvent(int progress, EventStoreData specificEvent = null, string specificEventName = "", EventCondition condition = EventCondition.Helpful, int attempt = 0)
	{
		StartCoroutine(ScheduleProgressEventWhenReady(progress, specificEvent, specificEventName, condition, attempt));
	}

	/**Do whatever happens when that option is chosen.
	 */
	public void OptionPressed(EventOptionData option)
	{
		//Do the currentEvents effects whenever we leave it.
		DoEffects();

		//EndEvent if there was no listed option there
		if (option == null)
		{
			EndEvent();
			return;
		}

		//Start the minigame!
		MiniGameBridge.b.StartMinigame(option, GameReference.r.commandValue);
	}

	/**This is called when we're back in the scene after a minigame (or when there wasn't a minigame).
	 * Pass the result of ChooseEvent as ev.
	 */
	public void ResumeEvent(EventStoreData ev)
	{
		//We've chosen this one! Let's go to the next event!
		currentEvent = ev;

		//We're done if there is not a next event
		if (currentEvent == null)
			EndEvent();

		//Or we need to change text and process event
		else if (eventIsActive)
			UpdateEventData();	//Already active? Just change texts
		else
			ActivateEvent(currentEvent);	//Otherwise, activate the event!
	}

	/**String overload, so SaveLoad can resume a current event.
	 */
	public void ResumeEvent(string evName, Color borderOverride)
	{
		ResumeEvent(allEvents.FindLast(obj => obj.name == evName));
		eventBorder.color = borderOverride;
	}

	/**Change the text, (de)activate options, prepare effects etc.
	 */
	public void UpdateEventData()
	{
		if (currentEvent == null)
		{
			Debug.LogWarning("Improperly called UpdateEventData. Don't do that.");
			return;
		}

		//Border color && Header text
		if (currentEvent.conditions.Contains(EventCondition.Loss))
		{
			eventBorder.color = ColorPalette.cp.red0;
			eventHeader.text = "Game Over";
			eventHeaderBorder.color = ColorPalette.cp.red0;

			eventHeader.GetComponent<GenericTooltip>().tooltipText = "You have lost. Your fate is sealed. You failed them.";
		}
		else if (currentEvent.conditions.Contains(EventCondition.Story))
		{
			eventBorder.color = ColorPalette.cp.red1;
			eventHeader.text = "Story";
			eventHeaderBorder.color = ColorPalette.cp.red1;

			eventHeader.GetComponent<GenericTooltip>().tooltipText = "Story events advance as you make progress. \n\nWhat happens is often determined by past decisions and results.";
		}
		else if (currentEvent.conditions.Contains(EventCondition.Helpful))
		{
			eventBorder.color = ColorPalette.cp.blue1;
			eventHeader.text = "Helpful";
			eventHeaderBorder.color = ColorPalette.cp.blue1;

			eventHeader.GetComponent<GenericTooltip>().tooltipText = "Helpful events provide relief for the weary traveler. \n\nThey replace standard events more frequently when you're struggling.";

		}
		else if (currentEvent.conditions.Contains(EventCondition.Standard))
		{
			eventBorder.color = ColorPalette.cp.yellow1;
			eventHeader.text = "Standard";
			eventHeaderBorder.color = ColorPalette.cp.yellow1;

			eventHeader.GetComponent<GenericTooltip>().tooltipText = "Standard events are usually bad news. \n\nThey appear more often when you're doing well.";
		}
		else
		{
			eventBorder.color = ColorPalette.cp.gry1;
			eventHeader.text = "Unknown";
			eventHeaderBorder.color = ColorPalette.cp.gry1;

			eventHeader.GetComponent<GenericTooltip>().tooltipText = "??!!\n\n^JU!NG*unI3-lBH_QbAR#K@()";
		}

		//Assign event text (remember to adjust ship name even in events without prepped effects! [this will be called again with more if there are prepped effects])
		//This doesn't affect target text, so it can still properly assign.
		eventBody.text = ReplaceSymbolsInTargetText.ReplaceSymbols(currentEvent.eventText, "*T*", GameReference.r.shipName, false);

		//Show last action?
		if (eventLastAction)
			eventLastAction.gameObject.SetActive(currentEvent.conditions.Contains(EventCondition.SUBEVENT));

		//Prepare effects! TODO Make sure this doesn't break stuff, or move it back down and put only currentEvent.onActivate.Invoke() here.
		PrepEffects();

		//Close any old buttons
		foreach (var t in optionPool)
			if (t.activeSelf)
				t.SetActive(false);

		//Set premade buttons
		int usableButtonsSet = 0;
		foreach (EventOptionData eo in currentEvent.options)
		{
			//Skip it if it's null or disabled
			if (eo == null || !eo.isUsable)
				continue;

			//Make this button if the requirement to use it was met
			if (eo.AreRequirementsMet())
			{
				//Get an option to use
				var opt = GetAnOptionInstance();
				var optText = opt.GetComponentInChildren<Text>();

				//This option is available
				opt.GetComponent<Button>().interactable = true;

				//Set the option's text
				if (eo.optionText != "")
					optText.text = eo.optionText;
				else
					optText.text = "Continue.";
				
				//Tags
				if (PlayerPrefs.GetInt("EventTags") == 1)
				{
					//Multiple possible results
					if (eo.nextEventChances.FindAll(obj => obj != null && obj.AreRequirementsMet()).Count > 1)
					{
						//Minigame difficulty
						if (eo.minigameDifficulty == EventOptionData.Minigame.NONE || PlayerPrefs.GetInt("MinigameDisabled") == 0)
						{
							optText.text += " [random]";
						}
						else
						{
							optText.text += " [" + eo.minigameDifficulty.ToString().ToLower() + "]";
						}
					}
					//Only one chance available
					else if (eo.nextEventChances.FindAll(obj => obj != null && obj.AreRequirementsMet()).Count == 1)
						optText.text += " [fated]";
				}

				//Tag secret options in-line
				if (eo.hideIfUnavailable)
					optText.text += " [secret]";

				//Tag the button with its option
				opt.GetComponent<LinkedOption>().linkedOption = eo;

				//Put it in order
				opt.transform.SetAsLastSibling();

				//Tooltip
				SetOptionTooltip(eo, opt);

				usableButtonsSet++;
			}
			//Or we aren't going to use it
			//Visible, but unavailable
			else if (!eo.hideIfUnavailable)
			{
				//Get an option to use
				var opt = GetAnOptionInstance();
				//This option isn't available, but should show information about itself
				opt.GetComponent<Button>().interactable = false;
				//Text
				opt.GetComponentInChildren<Text>().text = eo.optionText + " [unavailable]";

				//Tooltip
				SetOptionTooltip(eo, opt);
			}
		}

		//Use a default if no buttons listed
		if (usableButtonsSet == 0)
		{
			var opt = GetAnOptionInstance();
			opt.GetComponentInChildren<Text>().text = "Continue.";
			opt.GetComponent<LinkedOption>().linkedOption = null;

			//This option is available
			opt.GetComponent<Button>().interactable = true;

			SetOptionTooltip(null, opt);
		}
		//Otherwise sort them
		else
		{
			foreach (var t in optionPool.FindAll(obj => obj.activeSelf))
			{
				if (!t.GetComponent<Button>().interactable)
					t.transform.SetAsLastSibling();
			}
		}

		//Preselect the top one
		GameObject preselected = null;
		foreach (var t in optionPool.FindAll(obj => obj.activeSelf))
		{
			//Top one
			if (t.transform.GetSiblingIndex() == 0)
			{
				preselected = t;
				break;
			}
			//Not top, but closest we've found yet
			else if (preselected == null || preselected.transform.GetSiblingIndex() > t.transform.GetSiblingIndex())
			{
				preselected = t;
			}
		}
		if (preselected != null)
			StartCoroutine(DelaySelectByAFrame(preselected.GetComponent<Button>()));
	}


	/**Do a random event NOW, cancelling any current event. Note, this can force nulls (if everything goes wrong), and thus get some debug calls. Meh, such is life.
	 */
	public void ForceEvent(EventCondition type)
	{
		//Shut down any old event
		if (eventIsActive)
			EndEvent();

		//Set new minimum time (so we don't stack a bunch)
		var ecc = FindObjectOfType<EventClockCheck>();
		if (ecc)
			ecc.SetMinTimeForNextPop();

		//Add the new event
		ActivateEvent(ChooseRandomEvent(type));
	}


	/**Called to trigger the event. 'Opens' the window, pauses game, presents the story, etc.
	 * Will only be prompted once per event.
	 */
	public void ActivateEvent(EventStoreData ev)
	{
		//Set currentEvent for our purposes!
		currentEvent = ev;

		//Make sure there is an event to activate
		if (currentEvent == null)
		{
			//If there isn't a standard event in the queue, make one
			if (!scheduledEvents.Exists(obj => obj.eventStoreData.conditions.Contains(EventCondition.Standard)))
				ScheduleTimedEvent(0, 0, 0);
			print("ActivateEvent failed due to null event.");
			return;
		}

		//If the event isn't already activated, we can activate
		if (!eventIsActive)
		{
			//Remove the current event from the schedule! Do this before checking requirements to avoid massive scheduling loops. (It's still stored as currentEvent)
			scheduledEvents.Remove(scheduledEvents.Find(obj => obj.eventStoreData == currentEvent));

			//Double check that this event can still be done
			if (!currentEvent.AreRequirementsMet())
			{
				//Try to reschedule Story events (maybe they'll work later... #wishfulthinking)
				if (currentEvent.conditions.Contains(EventCondition.Story))
					ScheduleProgressEvent(ShipResources.res.progress + 5, currentEvent, condition: EventCondition.Story);

				//Build a new Standard one for right now!
				ScheduleTimedEvent(0, 0, 0, condition: EventCondition.Standard);

				print("ActivateEvent failed due to unmet requirements.");
				return;
			}

			//Audio
			AudioClip clip = null;
			if (currentEvent.conditions.Contains(EventCondition.Story))
				clip = MusicController.mc.eventStory;
			else if (currentEvent.conditions.Contains(EventCondition.Helpful))
				clip = MusicController.mc.eventHelpful;
			else if (currentEvent.conditions.Contains(EventCondition.Standard))
				clip = MusicController.mc.eventStandard;

			if (clip)
			{
				//Event Start
				AudioClipOrganizer.aco.PlayAudioClip("EventStart", null);
				//Music
				MusicController.mc.InterruptCurrentSong(clip);
			}

			//Pause the game and lock it
			GameClock.clock.Pause();
			GameClock.clock.pauseControlsLocked = true;

			//Also prevent cam movement
			if (CameraEffectsController.cec != null)
				CameraEffectsController.cec.canMove = false;

			//And set overlay
			if (GameReference.r != null)
				GameReference.r.overlayActive = true;

			//Do final event setup
			UpdateEventData();

			//Show the window. Populate.
			eventIsActive = true;
		}
	}

	private void EndEvent()
	{
		StatTrack.stats.eventsSurvived++;

		//Clear current event
		_lastEvent = currentEvent;
		currentEvent = null;

		//Add time to the world!
		GameClock.clock.AddTime(0, Random.Range(16, 32), Random.Range(0, 60));

		//Return to your regular scheduled music
		MusicController.mc.ResumeInterruptedSong();

		//Remove window
		eventIsActive = false;

		//Allow cam movement
		if (CameraEffectsController.cec != null)
			CameraEffectsController.cec.canMove = true;

		//And remove overlay
		if (GameReference.r != null)
			GameReference.r.overlayActive = false;

		//Set the next standard event (only if no standards or helpfuls are scheduled, so that we can have more buffer after helpfuls)
		if (!scheduledEvents.Exists(obj => obj.eventStoreData.conditions.Contains(EventCondition.Standard) || obj.eventStoreData.conditions.Contains(EventCondition.Helpful)))
			GetComponent<GameEventScheduler>().CreateEventByTime(GameClock.clock.day, GameClock.clock.hour, GameClock.clock.minute);

		//Save
		SaveLoad.s.SaveGame();

		//Unpause
		GameClock.clock.pauseControlsLocked = false;
		GameClock.clock.Unpause();
	}

#endregion


	#region UTILITY

	/**Prepare the current event's effects!
	 */
	private void PrepEffects()
	{
		//Don't need old effects
		preppedEffects.Clear();

		foreach (var ee in currentEvent.effects)
		{
			if (ee != null)
			{
				//Prep the effect
				var eff = ee.PrepEffect();
				//Add it if it's valid
				if (eff != null)
					preppedEffects.Add(eff);
			}
		}

		//Invoke the onActivate UnityEvent for the currentEvent (which may have other callbacks more specific than currentEvent.effects)
		if (currentEvent.onActivate != null)
			currentEvent.onActivate.Invoke();

		//Update text for any target changes
		if (preppedEffects.Count > 0)
			eventBody.text = ReplaceSymbolsInTargetText.ReplaceSymbols(eventBody.text, preppedEffects [0].targetName, GameReference.r.shipName);
	}


	/**Do the prepped effects, or the currentEvent's effects if nothing was prepared
	 */
	private void DoEffects()
	{
		if (preppedEffects.Count > 0)
		{
			foreach (EventEffectData ee in preppedEffects)
				if (ee != null)
					ee.DoEffect();
		}
		else
		{
			if (currentEvent != null)
			{
				foreach (var ee in currentEvent.effects)
					if (ee != null)
						ee.DoEffect();
			}
			else
			{
				print("currentEvent is null and no effects are prepped. currentEvent.effects skipped.");
			}
		}

		//Achievement on this event?
		if (currentEvent != null)
			currentEvent.unlockAchievements.FindAll(obj => obj != null).ForEach(obj => AchievementTracker.UnlockAchievement(obj));

		//Update text for any target changes
		if (preppedEffects.Count > 0)
			eventBody.text = ReplaceSymbolsInTargetText.ReplaceSymbols(eventBody.text, preppedEffects [0].targetName, GameReference.r.shipName);
	}

	/**Call this from ScheduleTimedEvent.
	 */
	private IEnumerator ScheduleTimedEventWhenReady(int d, int h, int m, EventStoreData specificEvent = null, string specificEventName = "", EventCondition condition = EventCondition.Standard, int attempt = 0)
	{
		//Ready condition
		yield return new WaitUntil(() => ready && ShipResources.res != null);

		//The new event for the scheduler
		var sched = new ScheduledEvent();

		//Hardwire the schedule for the trigger
		sched.day = d;
		sched.hour = h;
		sched.minute = m;

		//Do it
		if (!ChooseEvent(sched, specificEvent, specificEventName, condition) && attempt < 10)
			//Didn't work, try again, loose like
			ScheduleTimedEvent(d, h, m, condition: condition, attempt: ++attempt);
	}

	/**Call this from ProgressEvent.
	 */
	private IEnumerator ScheduleProgressEventWhenReady(int p, EventStoreData specificEvent = null, string specificEventName = "", EventCondition condition = EventCondition.Helpful, int attempt = 0)
	{
		//Ready condition
		yield return new WaitUntil(() => ready && ShipResources.res != null);

		//The new event for the scheduler
		var sched = new ScheduledEvent();

		//Hardwire the schedule for the trigger
		sched.progress = p;
		sched.progressType = true;

		//Do it
		if (!ChooseEvent(sched, specificEvent, specificEventName, condition) && attempt < 10)
			//Didn't work, try again, loose like
			ScheduleProgressEvent(p, condition: condition, attempt: ++attempt);
	}

	/**Choose the actual event to schedule.
	 */
	bool ChooseEvent(ScheduledEvent sched, EventStoreData specificEvent = null, string specificEventName = "", EventCondition condition = EventCondition.Standard)
	{
		EventStoreData chosenEvent = specificEvent;

		//Prefer specificEvent
		if (specificEvent == null)
		{
			//Find a specific event
			if (specificEventName != "")
			{
				chosenEvent = specificEvent = allEvents.FindLast(obj => obj.name == specificEventName);
				//Using FindLast in case of override from a rename (mods?) - LAZY FUTURE-PROOFING TODO identify by something other than string
			}
			//Or choose a random one
			else
			{
				chosenEvent = ChooseRandomEvent(condition, ignoreRequirements: true);
			}
		}

		//Be sure this is a valid choice
		if (chosenEvent != null && chosenEvent.AreRequirementsMet() && (chosenEvent != _lastEvent || chosenEvent == specificEvent))
		{
			//Set the event!
			sched.eventStoreData = chosenEvent;

			//Only use this at most once if it's unique TODO proper unique culling between loads. Use a list of specificEventName (or whatever ID system replaces it) to sort from save.
//			if (chosenEvent.unique)
//				chosenEvent.gameObject.SetActive(false);

			//Add to the list!
			scheduledEvents.Add(sched);

			//Be sure SaveLoad knows the event exists!
			//SaveLoad.s.Save();

			return true;
		}
		//Otherwise fail
		return false;
	}


	EventStoreData ChooseRandomEvent(EventCondition condition, bool ignoreRequirements = false)
	{
		//Set up the event by condition
		List<EventStoreData> targetedEvents = new List<EventStoreData>();

		//Pick our list to search
		List<EventStoreData> listToSearch = allEvents;

		if (condition == EventCondition.Loss)
		{
			listToSearch = lossEvents;
		}

		//Get the proper list
		foreach (var t in listToSearch)
			if (t.conditions.Contains(condition) && (condition == EventCondition.SUBEVENT || !t.conditions.Contains(EventCondition.SUBEVENT)))
			{	
				if ((ignoreRequirements && t.isUsable) || t.AreRequirementsMet())
				{
					//Add it as many times as we need to
					for (int i = 0; i < t.chances; i++)
					{
						targetedEvents.Add(t);
					}
				}
			}

		if (targetedEvents.Count == 0)
		{
			Debug.LogError("No events found. Bailing from event creation.");
			return null;
		}

		return targetedEvents [Random.Range(0, targetedEvents.Count)];
	}

	IEnumerator DelaySelectByAFrame(Button b)
	{
		yield return null;

		//Don't let a random ugly tooltip open in the middle when we pre-select
		var tt = b.GetComponent<GenericTooltip>();
		//Do this by locking the tooltip
		tt.lockedFromOpenClose = true;

		//Now select
		b.Select();

		//Cool, we can unlock now
		tt.lockedFromOpenClose = false;
	}

	public void AddToLossEventsByName(string name)
	{
		EventStoreData ev = Resources.Load<EventStoreData>("EventStoreData/" + name);

		if (!lossEvents.Contains(ev))
			lossEvents.Add(ev);
	}

#endregion


	#region OPTION POOLING

	GameObject GetAnOptionInstance()
	{
		//Is there an option instance not in use?
		if (optionPool.Exists(obj => !obj.activeSelf))
		{
			//Find it
			var opt = optionPool.Find(obj => !obj.activeSelf);

			//Ready it
			opt.SetActive(true);

			//Use it
			return opt;
		}
		//Make one
		else
		{
			//Instantiate
			var opt = (GameObject)Instantiate(optionPrefab);
			//Set Parent
			opt.transform.SetParent(optionGroup);

			//Reset values
			opt.transform.localScale = Vector3.one;
			opt.transform.localRotation = Quaternion.identity;
			opt.transform.localPosition = Vector3.zero;

			//Add to pool
			optionPool.Add(opt);

			//Give it over
			return opt;
		}
	}

	void SetOptionTooltip(EventOptionData eo, GameObject instance)
	{
		var tip = instance.GetComponent<GenericTooltip>();

		//Blank options
		if (eo == null)
		{
			tip.tooltipTitle = "";
			tip.tooltipText = "No Requirements";
			return;
		}

		//Title for all other cases
		tip.tooltipTitle = "Requirements";

		//Set text
		var stringBuilder = new System.Text.StringBuilder();
		stringBuilder.AppendLine();

		//For options hidden when unavailable, hide requirements
		if (eo.hideIfUnavailable)
		{
			//Color
			if (eo.AreRequirementsMet())
				stringBuilder.Append("<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.wht) + ">");
			else
				stringBuilder.Append("<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.red4) + ">");

			//Alternate text
			stringBuilder.Append("Secret Option\n(Requirements Hidden)");

			//End color
			stringBuilder.Append("</color>");

			//Done
			tip.tooltipText = stringBuilder.ToString();
			return;
		}

		//Iterate through requirements
		var c = 0;
		foreach (var t in eo.requirements)
		{
			//Skip blanks
			if (t != null && !t.hiddenRequirement)
			{
				//Between entries
				if (c > 0)
					stringBuilder.AppendLine();
				//Color
				if (t.CheckRequirements())
					stringBuilder.Append("<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.wht) + ">");
				else
					stringBuilder.Append("<color=#" + ColorPalette.ColorToHex(ColorPalette.cp.red4) + ">");
				//Requirement text
				stringBuilder.Append(t.RequirementText + "</color>");
				//Increment
				c++;
			}
		}

		//Are all events invalid in NextEventChances?
		if (eo.nextEventChances.Exists(obj => obj != null)//There is a nextEventChance
		    && !eo.nextEventChances.Exists(obj => obj != null && obj.AreRequirementsMet()))	//But no nextEventChance is valid
		{
			//Between entries
			if (c > 0)
				stringBuilder.AppendLine();

			stringBuilder.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "Outcome Event's Requirements"));

			//Increment
			c++;
		}

		//Would totally work except it does not match hidden requirements?
		if (!eo.AreRequirementsMet()//Requirements aren't met
		    && !eo.requirements.Exists(obj => obj != null && !obj.hiddenRequirement && !obj.CheckRequirements())//No nonhidden requirement wasn't also failed
		    && (!eo.nextEventChances.Exists(obj => obj != null) || eo.nextEventChances.Exists(obj => obj != null && obj.AreRequirementsMet())))	//No nonnull nextEventChances exist, or there's a nextEventChance whose requirements are met (either means there's a valid outcome)
		{
			//Between entries
			if (c > 0)
				stringBuilder.AppendLine();

			stringBuilder.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "Secret Requirement"));

			//Increment
			c++;
		}

		//No requirements at all?
		if (c == 0)
		{
			tip.tooltipTitle = "";
			tip.tooltipText = "No Requirements";
			return;
		}

		//Output if requirements
		tip.tooltipText = stringBuilder.ToString();
	}

	#endregion

	
	#region LIFECYCLE

	void Update()
	{
		if (!ready)
			ready = true;

		if (eventPopUp)
		{
			if (eventIsActive && !eventPopUp.activeSelf)
				eventPopUp.SetActive(true);
			else if (!eventIsActive && eventPopUp.activeSelf)
			{
				var fade = eventPopUp.GetComponent<FadeChildren>();
				if (fade.onFadeOutFinish.GetPersistentEventCount() == 0)
					fade.onFadeOutFinish.AddListener(() => eventPopUp.SetActive(false));
				fade.FadeOut();
			}
		}
	}


	void Start()
	{
		if (eventBorder == null)
			eventBorder = eventPopUp.transform.GetChild(0).GetChild(0).GetComponent<Image>();

		//Populate list of events
		foreach (EventStoreData es in Resources.LoadAll<EventStoreData>("EventStoreData"))
		{
			if (!es.conditions.Contains(EventCondition.NEVER) && !allEvents.Contains(es) && es.isUsable)
				allEvents.Add(es);
		}

		//Populate loss events
		if (lossEvents.Count == 0)
		{
			defaultLossList.list.ForEach(ev => lossEvents.Add((EventStoreData)ev));
		}

		//Populate story controllers
		foreach (var t in Resources.LoadAll<EventControlData>("EventControlData"))
		{
			if (!customEventControllers.Contains(t) && t.isUsable)
				customEventControllers.Add(t);
		}

		eventIsActive = false; //Events come when triggered. (... dirty)
	}

	void Awake()
	{
		//Set up the singleton
		if (gem == null)
		{
			gem = this;
		}
		else if (gem != this)
		{
			Destroy(this);
		}
	}

	#endregion
}
