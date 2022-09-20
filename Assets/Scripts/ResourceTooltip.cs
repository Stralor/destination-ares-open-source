using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ResourceTooltip : MonoBehaviour
{

	public enum ResourceType
	{
		Air,
		Distance,
		Energy,
		Food,
		Fuel,
		Materials,
		Parts,
		Speed,
		Waste
	}

	//Declarations
	private string text = "";
	//The raw input text
	public float fadeTime;
	//How long it takes for tooltip to fade in/ out
	public float waitTime;
	//How long to wait before activation/ deactivation
	private bool beginFade = false;
	//Trigger a fade change
	private bool draw = false;
	//Currently faded/fading in?
	private float waited = 0;
	//How long has already been waited
	private Transform target;
	//Location of HUD resource
	private ResourceType currentResource;
	//Type of HUD resource

	//The raw tip texts
	[TextArea()] public string airTip, distanceTip, energyTip, foodTip, fuelTip, materialsTip, partsTip, speedTip, wasteTip;

	//Cache
	private Text textArea;
	private Image background;


	/*
	 * PUBLIC (TRIGGER) METHODS
	 */

	/**Set the target to move to, and setup tooltip.
	 */
	public void SetTarget(RectTransform targ)
	{
		//Set target
		target = targ;

		//Set text and resource
		if (target != null)
		{
			switch (target.name.ToLower())
			{
			case "air":
				text = airTip;
				currentResource = ResourceType.Air;
				break;
			case "energy":
				text = energyTip;
				currentResource = ResourceType.Energy;
				break;
			case "food":
				text = foodTip;
				currentResource = ResourceType.Food;
				break;
			case "fuel":
				text = fuelTip;
				currentResource = ResourceType.Fuel;
				break;
			case "materials":
				text = materialsTip;
				currentResource = ResourceType.Materials;
				break;
			case "parts":
				text = partsTip;
				currentResource = ResourceType.Parts;
				break;
			case "waste":
				text = wasteTip;
				currentResource = ResourceType.Waste;
				break;
			case "distancetext":
				text = distanceTip;
				currentResource = ResourceType.Distance;
				break;
			case "shipspeedtext":
				text = speedTip;
				currentResource = ResourceType.Speed;
				break;
			default :
				Debug.Log("Couldn't find a resource called " + target.name + " on the HUD.");
				break;
			}
		}
	}

	/**Start/stop counting for delay!
	 */
	public void ToggleTooltip(bool drawing)
	{
		beginFade = true;
		waited = 0;
		draw = drawing;
	}


	/*
	 * SETUP AND UPDATE METHODS
	 */

	/**Turn on/ off the tooltip.
	 * Repositions to beneath the target, invokes actual activation.
	 */
	private void ActivateDeactivate(bool activate)
	{

		//Fade in!
		if (activate && target != null)
		{
			//transform.position = new Vector2(target.position.x, target.position.y - 0.1f);
			transform.position = new Vector2(target.position.x, transform.position.y);
			
			if (!isActiveAndEnabled)
				gameObject.SetActive(true);

			textArea.CrossFadeAlpha(1, fadeTime, true);
			background.CrossFadeAlpha(1, fadeTime, true);
		}
		//Fade out!
		else
		{
			textArea.CrossFadeAlpha(0, fadeTime, true);
			background.CrossFadeAlpha(0, fadeTime, true);
		}
	}

	/**Adjust values in the text.
	 */
	private string UpdateValues(string input)
	{
		//Text to edit
		string edittedText = input;

		//Find the capacity/ storage (or distance left/ current speed)
		int value;
		if (currentResource == ResourceType.Energy)
			value = ShipResources.res.capacityRemaining;
		else if (currentResource == ResourceType.Distance)
			value = ShipResources.res.distance;
		else if (currentResource == ResourceType.Speed)
			value = (int)(ShipResources.res.speed * 60 / ShipMovement.sm.tickDelay / GameClock.clock.clockSpeed);
		else
			value = ShipResources.res.storageRemaining;

		//Check if there's something to change
		while (edittedText.Contains("*R*"))
		{
			//Change the first instance
			edittedText = edittedText.Replace("*R*", value.ToString());
		}
		
		//Return text
		return edittedText;
	}

	void Update()
	{
		//Adjust size
		RectTransform t = (RectTransform)transform;
		t.sizeDelta = new Vector2(t.rect.width, (textArea.preferredHeight) + 12);

		//Set the text values
		if (textArea != null && draw)
			textArea.text = UpdateValues(text);

		//Draw the tooltip
		//Fade
		if (beginFade)
		{
			//Do the fade
			if (waited >= waitTime)
			{
				ActivateDeactivate(draw);
				beginFade = false;
			}
			//Or wait to fade it
			else
				waited += Time.unscaledDeltaTime;
		}
	}

	void Start()
	{
		textArea = GetComponentInChildren<Text>();
		background = GetComponent<Image>();

		//Start faded out
		textArea.CrossFadeAlpha(0, 0, true);
		background.CrossFadeAlpha(0, 0, true);
	}
}
