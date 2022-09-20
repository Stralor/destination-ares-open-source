using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CharacterSpeech : MonoBehaviour
{

	/* This class controls when and what a character says (in speech bubbles).
	 * It is attached to each character.
	 */


	//Enums
	public enum Personality
	{
		ANY,
		Chatty,
		Cheerful,
		Curious,
		Grumpy,
		Lazy,
		Quiet,
		Serious
	}

	/* after feedback in playtests, this was designed to not overwhelm new players with lots of excess text that's hard to understand
	 * but for the re-release I've decided that forcing only "Quiet" on new and brief players:
	 * 1 - reduces important feedback on game state (even if it's too much or essentially unintelligible)
	 * 2 - deprives them of the subtle, but primary, way worldbuilding and flavor is conveyed
	 * 3 - really just pisses me off considering the love and craft that went into designing the linguistics and dialogue
	 * of course, this also means that some players may be put off by early cussing or crew belligerence
	 * I suggest that this game, between the pervasive darkness, demented optimism, and poop jokes, is probably not for them
	 */
	public static List<Personality> unlockedPersonalities = new List<Personality>()
	{
		Personality.ANY,
		Personality.Chatty,
		Personality.Cheerful,
		Personality.Curious,
		Personality.Grumpy,
		Personality.Lazy,
		Personality.Quiet,
		Personality.Serious,
	};


	//Declarations
	public Personality personality;
	[SerializeField] float bubbleAlpha = 0.4f, bubbleHoverAlpha = 0.9f, baseFadeTime = 0.5f;
	[SerializeField] private string inputText;
	public float chunkPause;
	public AudioClip voice;
	private bool talking = false;

	public List<SpeechData> allSpeeches = new List<SpeechData>();
	private List<SpeechData> recentSpeeches = new List<SpeechData>();

	//Cache
	private Character me;
	//The character this is attached to
	private BehaviorHandler bHand;
	//The character's BehaviorHandler
	private Text speechText;
	//The text UI used for displaying the speech
	private Canvas bubbleCanvas;
	//The actual canvas all of this is displayed on
	private Image bubble, arrow;
	//The Arrow and Bubble images
	//private BoxCollider2D bubbleCollider;
	private PlayerInteraction pI;
	//Character's PlayerInteraction, to check for selected status


	/*
	 * BUBBLE CONTROLS
	 */

	/**Time to show the bubble!
	 */
	public void OpenBubble()
	{

		if (IsInvoking("CloseBubble"))
			CancelInvoke("CloseBubble");

		talking = true;

		speechText.CrossFadeAlpha(1, baseFadeTime, true);
		
		if (!pI.selected)
		{
			arrow.CrossFadeAlpha(bubbleAlpha, baseFadeTime, true);
			bubble.CrossFadeAlpha(bubbleAlpha, baseFadeTime, true);
		}
		else
		{
			OnMouseEnter();
		}
	}

	/**Make the bubble invisible.
	 * Call FinishTalking() unless you need something instant.
	 */
	public void CloseBubble()
	{
		talking = false;

		arrow.CrossFadeAlpha(0, baseFadeTime, true);
		bubble.CrossFadeAlpha(0, baseFadeTime, true);
		speechText.CrossFadeAlpha(0, baseFadeTime, true);
	}


	/*
	 * SPEECH SELECTION AND SET UP
	 */

	/**Find a valid speech. Populate the text.
	 * Input the trigger for the speech.
	 */
	public void ChooseSpeech(Character.Thought trigger)
	{
		//Populate a list of valid speeches
		List<SpeechData> targetSpeeches = new List<SpeechData>();
		foreach (SpeechData sp in allSpeeches)
		{
			if (sp != null && CheckPersonality(sp) && CheckTrigger(sp, trigger))
			{
				targetSpeeches.Add(sp);
			}
		}

		//If none, get out
		if (targetSpeeches.Count <= 0)
			return;

		//Choose one
		int chosenIndex = Random.Range(0, targetSpeeches.Count);

		//Reduce odds of duplicates - by rerolling
		if (recentSpeeches.Contains(targetSpeeches [chosenIndex]))
		{
			recentSpeeches.Remove(targetSpeeches [chosenIndex]);
			ChooseSpeech(trigger);
			return;
		}
		//Track what we're running with
		else
		{
			recentSpeeches.Add(targetSpeeches [chosenIndex]);
		}

		StopTalkingCoroutines();
		//Set it
		speechText.text = "";
		//Replace target with current target or last target, as necessary.
		inputText = ReplaceTargetInSpeechText(targetSpeeches [chosenIndex].text, thoughtIsAboutLastTarget(trigger));

		//Start it, if there's text
		if (inputText.Trim().Length > 0)
		{
			//Unavailable comms: write over with babble
			if (!GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled))
			{
				Invoke("CloseBubble", (inputText.Length * chunkPause) + 2);
				speechText.text = "<i>( inaudible )</i>";
			}
			//Do the talky talk
			else
				StartCoroutine(Talk());

			//Either way, show it
			OpenBubble();
		}
	}

	/**Does this speech match our personality?
	 */
	bool CheckPersonality(SpeechData sp)
	{

		//Are we ANY personality?
		//if (personality == Personality.ANY)
		//	return true;

		//Are we a less talkative personality? Chance to say nothing.
//		if (personality == Personality.Quiet)
//			if (Random.Range(0, 2) == 0)
//				return false;

		//Check the list of valid personalities for the speech.
		foreach (Personality p in sp.personalities)
		{
			//If it accepts ANY personality, we're likewise done
			if (p == Personality.ANY)
				return true;
			//If our personality matches one on the list, we're done
			if (p == personality)
				return true;
		}

		//If we get here, it doesn't match
		return false;
	}

	/**Does this speech have the right trigger condition?
	 * 
	 */
	bool CheckTrigger(SpeechData sp, Character.Thought tr)
	{

		//Just compare what it has with what we want
		foreach (Character.Thought t in sp.triggers)
		{
			if (t == tr)
				return true;
		}

		//Didn't find it
		return false;
	}

	/**Called to insert the target's name into the speech's text. Customizes the text, if you will.
	 * Pass true to 'lastTarget' if you want the replaced text to refer to the last target, and not the current one.
	 */
	private string ReplaceTargetInSpeechText(string txt, bool lastTarget)
	{
		
		//Set up our returnable, without tampering with input. Default to input, in case no changes.
		string editedText = txt;

		//Check if there's something to change
		//Target TODO What to replace *T* with when its an invalid/ empty target?
		while (editedText.Contains("*T*"))
		{
			//Don't bother if it would throw a null pointer
			if (me.currentThoughtTargetName == null)
				break;
			//Change the first instance
//			if (target.GetComponent<ShipSystem>() != null)
//				editedText = editedText.Replace("*T*", "the " + target.name);
//			else
			editedText = editedText.Replace("*T*", me.currentThoughtTargetName);
		}
		//Ship
		while (editedText.Contains("*S*"))
		{
			editedText = ReplaceSymbolsInTargetText.ReplaceSymbols(editedText, shipName: Roman.TrimEndRomans(GameReference.r.shipName));	//Temporary
		}

		//Make sure first letter is capitalized (Mostly in case we inserted a word at the front)
		if (editedText.Length > 0)
			editedText = editedText.Substring(0, 1).ToUpper() + editedText.Substring(1);

		//Return text
		return editedText;
	}

	/**Check if the thought needs to reference bHand.lastTarget rather than the current target.
	 */
	private bool thoughtIsAboutLastTarget(Character.Thought thought)
	{
		switch (thought)
		{
		case Character.Thought.BrokeSomething:
			return true;
		case Character.Thought.Failure:
			return true;
		case Character.Thought.HurtSomeone:
			return true;
		case Character.Thought.Success:
			return true;
		default :
			return false;
		}
	}


	/*
	 * CHANGE IN STATE
	 */

	/**Search through possible triggers, choose an active one, start the speech process.
	 * Prioritizes interrupt speech (injuries, etc.), but otherwise lets the character finish talking.
	 */
	void CheckForPrompt()
	{

		if (me.hasNewThought)
		{
			Character.Thought thought = me.currentThought;

			//Psychotics only say psycho things.
			if (me.status == Character.CharStatus.Psychotic && thought != Character.Thought.Psychotic)
			{
				//Instead, replace the thought!
				thought = Character.Thought.Psychotic;
			}

			//Interrupts
			if (thought == Character.Thought.Unconscious || thought == Character.Thought.Dead
			    || thought == Character.Thought.Injured || thought == Character.Thought.Tasking
			    || thought == Character.Thought.BrokeSomething || thought == Character.Thought.HurtSomeone
			    || thought == Character.Thought.Clicked)
				ChooseSpeech(thought);

			//Use the generic trigger if not saying something else (Also, cut wandering clutter in third)
			if (!talking && me.canAct && (thought != Character.Thought.Wandering || Random.Range(0, 3) == 0))
				ChooseSpeech(thought);
		}
	}

	/**Calls TypewriterText.TypeText, then invokes CloseBubble with a delay to wrap up the talking.
	 */
	public IEnumerator Talk()
	{
		yield return StartCoroutine(TypewriterText.TypeText(speechText, inputText, chunkPause));
		Invoke("CloseBubble", (inputText.Length * chunkPause) + 2);
	}

	/**CALL THIS BEFORE CHANGING speechText, inputText, OR letterPause!
	 */
	public void StopTalkingCoroutines()
	{
		StopAllCoroutines();
	}

	/*
	 * ENGINE EVENTS
	 */

	void OnMouseEnter()
	{

		if (talking)
		{
			//Focus alpha
			arrow.CrossFadeAlpha(bubbleHoverAlpha, baseFadeTime, true);
			bubble.CrossFadeAlpha(bubbleHoverAlpha, baseFadeTime, true);
			//Scale!
			bubbleCanvas.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
			//Pop to front
			if (!GameEventManager.gem.eventIsActive)
			{
				bubbleCanvas.sortingOrder = 4;
				GameClock.clock.onPause += OnMouseExit;
			}
		}
	}

	void OnMouseExit()
	{

		if (talking && (!pI.selected || GameEventManager.gem.eventIsActive))
		{
			//Unfocused
			arrow.CrossFadeAlpha(bubbleAlpha, baseFadeTime * 2, true);
			bubble.CrossFadeAlpha(bubbleAlpha, baseFadeTime * 2, true);
			//Reset scale and sorting order
			bubbleCanvas.transform.localScale = Vector3.one;
			bubbleCanvas.sortingOrder = 0;

			GameClock.clock.onPause -= OnMouseExit;
		}
	}

	void PlayTypewriterAudio()
	{
		AudioSource src;

		//Go for audio
		if (voice)
			src = AudioClipOrganizer.aco.GetSourceForCustomPlay(clip: voice, randomPitch: true, parent: transform);
		else
			src = AudioClipOrganizer.aco.GetSourceForCustomPlay(clipName: "talk", randomPitch: true, parent: transform);
		
		//Channel (volume attenuation)
		if (!pI.highlighted && !pI.selected)
			src.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Unfocused") [0];
		else
			src.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Important") [0];

		//TODO Status effects/ clips
		if (me.status == Character.CharStatus.Psychotic)
			src.pitch = Random.Range(1.0f, 1.2f);
		if (me.status == Character.CharStatus.Injured || me.status == Character.CharStatus.Restrained || me.status == Character.CharStatus.Stressed)
			src.pitch = Random.Range(0.8f, 1.0f);

		//Play
		StartCoroutine(AudioClipOrganizer.aco.PlayWhenActiveThenReturnToPool(src));
	}

	void Update()
	{

		//Turn off any speech, if the comms are offline TODO a static-y effect on shutdown / strained comms?
		if (talking && !IsInvoking("CloseBubble")
		    && !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled))
		{
			StopTalkingCoroutines();
			Invoke("CloseBubble", (inputText.Length * chunkPause) + 2);
			speechText.text = "<i>( inaudible )</i>";
		}

		//Check for state changes
		CheckForPrompt();

		//Change bubble (and bubbleCollider {NOT IN USE]) size
		if (bubble != null)
			//bubbleCollider.size =
			bubble.rectTransform.sizeDelta = new Vector2(bubble.rectTransform.rect.width, (speechText.preferredHeight / 100) + 0.3f);
	}

	static T GetRandomEnum<T>()
	{
		System.Array A = System.Enum.GetValues(typeof(T));
		T V = (T)A.GetValue(UnityEngine.Random.Range(0, A.Length));
		return V;
	}

	void Start()
	{

		me = GetComponent<Character>();
		pI = GetComponent<PlayerInteraction>();

		//Choose a random personality, if listed as ANY and we're in the main game
		while (personality == Personality.ANY && pI.enabled)
		{
			personality = unlockedPersonalities [Random.Range(1, unlockedPersonalities.Count)];
		}

		//Find the correct Canvas object
		List<Canvas> allChildCanvases = new List<Canvas>();
		allChildCanvases.AddRange(GetComponentsInChildren<Canvas>());
		foreach (Canvas c in allChildCanvases)
		{
			if (c.name == "Speech Bubble")
			{
				bubbleCanvas = c;
				break;
			}
		}

		//Find the correct Text object
		List<Text> allChildTexts = new List<Text>();
		allChildTexts.AddRange(GetComponentsInChildren<Text>());
		foreach (Text t in allChildTexts)
		{
			if (t.name == "Speech Text")
			{
				speechText = t;
				break;
			}
		}

		//And find the correct Image objects
		List<Image> allChildImages = new List<Image>();
		allChildImages.AddRange(GetComponentsInChildren<Image>());
		foreach (Image i in allChildImages)
		{
			if (i.name == "Bubble")
				bubble = i;
			if (i.name == "Arrow")
				arrow = i;
			if (arrow != null && bubble != null)
				break;
		}

		//Start faded out
		arrow.CrossFadeAlpha(0, 0, true);
		bubble.CrossFadeAlpha(0, 0, true);
		speechText.CrossFadeAlpha(0, 0, true);

		//bubbleCollider = bubble.GetComponent<BoxCollider2D>();
	}

}
