using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventCreationTool : Environment
{
	public Dropdown eventTypeDropdown;
	public List<Dropdown> effectTypeDropdowns;
	public Toggle miscToggle;
	public Text miscToggleLabel;
	public Slider oddsSlider;
	public InputField editableText;
	public Button newEventButton, newRequirementButton, newEffectButton, newOptionButton, doneButton;

	int currentLayer = 0;

	EventCondition eventType;
	EventStoreData.MinigameResult expectedMinigameResult;
	List<EventRequirementData> requirements = new List<EventRequirementData>();
	List<EventEffectData> effects = new List<EventEffectData>();
	List<EventOptionData> options = new List<EventOptionData>();
	List<EventStoreData> nextEvents = new List<EventStoreData>();


	public void OpenEventCreation()
	{
		if (currentLayer == 0)
		{
			eventTypeDropdown.gameObject.SetActive(true);
		}

		effectTypeDropdowns[0].gameObject.SetActive(false);
		
		//This will be our "increasedOdds" toggle
		miscToggle.isOn = false;
		miscToggleLabel.text = "Increased Odds";
		miscToggle.gameObject.SetActive(true);

		oddsSlider.gameObject.SetActive(false);

		editableText.text = "";

		newEventButton.gameObject.SetActive(false);
		newRequirementButton.gameObject.SetActive(true);
		newEffectButton.gameObject.SetActive(true);
		newOptionButton.gameObject.SetActive(true);
	}

	public void OpenRequirementCreation()
	{

	}

	public void OpenEffectCreation()
	{
		eventTypeDropdown.gameObject.SetActive(false);
		effectTypeDropdowns [0].gameObject.SetActive(true);
	}

	public void OpenOptionCreation()
	{
		eventTypeDropdown.gameObject.SetActive(false);
		effectTypeDropdowns [0].gameObject.SetActive(false);

		//This will be our "Hide if Unavailable" toggle
		miscToggle.isOn = false;
		miscToggleLabel.text = "Hide if Unavailable";
		miscToggle.gameObject.SetActive(true);


		oddsSlider.gameObject.SetActive(false);

		editableText.text = "";

		newEventButton.gameObject.SetActive(true);
		newRequirementButton.gameObject.SetActive(true);
		newEffectButton.gameObject.SetActive(true);
		newOptionButton.gameObject.SetActive(false);
	}

	void LoadAsset<T>() where T : EventData
	{
		BuildCurrentObject();

	}

	void BuildCurrentObject()
	{
		eventTypeDropdown.gameObject.SetActive(false);


		
	}

	void BuildEventStore(EventStoreData data)
	{
		data.conditions.Clear();

		if (currentLayer > 0)
			data.conditions.Add(EventCondition.SUBEVENT);

		data.conditions.Add(eventType);

		data.requirements = requirements;

		data.minimumResult = expectedMinigameResult;

		data.chances = miscToggle.isOn ? (int)oddsSlider.value : 1;

		data.eventText = editableText.text;

		data.effects = effects;

		data.options = options;

		//Save it
	}


	void BuildBranch()
	{

	}

}
