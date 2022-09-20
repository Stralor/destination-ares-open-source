using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class SetupShipNameControls : MonoBehaviour
{
	public Text targetText;

	public string nameToInsert { get; set; }

	private string referenceTextBody;
	[TextArea] public string initialTextBody, shipNameEnteredTextBody;
	bool finishedInitialTyping = false;
	bool readyToSkip = false;

	public Button beginJourney;

	Coroutine typewriter;

	public void InsertName()
	{

		//Only do this if we're done with the initial typing.
		if (finishedInitialTyping)
			//Set the changes
			targetText.text = ReplaceSymbolsInTargetText.ReplaceSymbols(referenceTextBody, shipName: Roman.IncrementEndRomans(nameToInsert, StatTrack.stats.IsVesselNameInMemorial));
	}

	private IEnumerator TypeWithPreInsertedName(float delay = 0)
	{
		var inputs = GetComponentsInChildren<Graphic>().Where(obj => obj.name != "Welcome").ToList();
		inputs.ForEach(obj => obj.CrossFadeAlpha(0, 0, true));

		yield return new WaitForSecondsRealtime(0.5f + delay);

		readyToSkip = true;

//		yield return new WaitForSeconds(0.5f);

		//Replace symbols for typing
		string preinserted = ReplaceSymbolsInTargetText.ReplaceSymbols(referenceTextBody, shipName: "AI");

		//Type
		typewriter = StartCoroutine(TypewriterText.TypeText(targetText, preinserted, typingSpeed: .15f, ignoreTimeScale: true));
		yield return typewriter;

		//Unlock new InsertName calls
		finishedInitialTyping = true;
		GetComponent<InputField>().Select();

//		GetComponentInParent<Animator>().SetTrigger("Fade in Name");

		inputs.ForEach(obj => obj.CrossFadeAlpha(1, 1, true));
	}

	public void NameEntered()
	{
		if (!readyToSkip)
		{
			GetComponent<InputField>().Select();
			return;
		}

		//Select the Begin Journey button
		beginJourney.Select();
		beginJourney.image.CrossFadeAlpha(1, 1, true);
		beginJourney.GetComponentInChildren<Text>().CrossFadeAlpha(1, 1, true);

		//Stop the inital coroutines. We don't need them anymore.
		StopCoroutine(TypeWithPreInsertedName());
		StopCoroutine(typewriter);

		//We've finished any initial typing.
		finishedInitialTyping = true;

		//Get the current input name (or default), increment the end romans. That's our ship name.
		nameToInsert = Roman.IncrementEndRomans(ReplaceSymbolsInTargetText.ReplaceSymbols("*S*", shipName: nameToInsert), StatTrack.stats.IsVesselNameInMemorial);

		//Set our new text.
		referenceTextBody = shipNameEnteredTextBody;
		InsertName();

		//Set the ship name by passing the ship symbol to a ReplaceSymbols call. It's just easier than rewriting the same logic.
		StartingResources.sRes.shipName = nameToInsert;

		//First time we call this:
//		if (!nameAlreadyEntered)
//		{
//			//Open the next interface
//			environment.OpenInterface();
//			nameAlreadyEntered = true;
//		}
	}

	void PlayTypewriterAudio()
	{
		AudioClipOrganizer.aco.PlayAudioClip("Talk", null);
	}

	void Start()
	{
		//Set the initial referenceTextBody!
		referenceTextBody = initialTextBody;

		//Begin Button isn't visible
		beginJourney.image.CrossFadeAlpha(0, 0, true);
		beginJourney.GetComponentInChildren<Text>().CrossFadeAlpha(0, 0, true);

		//Type the referenceTextBody into the targetText!
		StartCoroutine(TypeWithPreInsertedName(1));
	}
}
