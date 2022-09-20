using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HUDUpdater : MonoBehaviour
{

	/**Static ref
	 */
	public static HUDUpdater hudu;

	/**How far to shift resource texts when inducing movement.*/
	public float shiftAmount;
	/**How long to shift resource texts.*/
	public float timeToShift;

	[SerializeField]
	internal Text
		airText, distanceText, energyText, foodText, fuelText, materialsText, partsText, shipSpeedText, wasteText, clockText, gameSpeedText;

	public Button pauseButton, playButton, fastForwardButton;

	[SerializeField]
	internal GameObject gameSpeedLayout = null;



	void Start()
	{
		List<Text> upperHUD = new List<Text>();
		upperHUD.AddRange(GameObject.Find("UpperHUD").GetComponentsInChildren<Text>());
		foreach (Text txt in upperHUD)
		{
			switch (txt.name.ToLower())
			{
			case "airtext":
				airText = txt;
				break;
			case "distancetext":
				distanceText = txt;
				break;
			case "energytext":
				energyText = txt;
				break;
			case "foodtext":
				foodText = txt;
				break;
			case "fueltext":
				fuelText = txt;
				break;
			case "materialstext":
				materialsText = txt;
				break;
			case "partstext":
				partsText = txt;
				break;
			case "speedtext":
				shipSpeedText = txt;
				break;
			case "wastetext":
				wasteText = txt;
				break;
			case "clock":
				clockText = txt;
				break;
			case "gamespeedtext":
				gameSpeedText = txt;
				break;
			}
		}
	}

	void Update()
	{

		//Set Texts and colors
		if (!(airText && distanceText && energyText && foodText && fuelText && materialsText
		    && partsText && wasteText && clockText && shipSpeedText && gameSpeedText))
			return;

		//Air
		airText.text = ": " + ShipResources.res.usableAir + "/" + ShipResources.res.totalAir;
		airText.color = ColorText(ShipResources.res.usableAir, ShipResources.res.totalAir - ShipResources.res.usableAir);
		
		//Distance
		distanceText.text = "Progress: " + ShipResources.res.progress + "%";

		//Energy
		energyText.text = ": " + ShipResources.res.energy + "/" + ShipResources.res.capacityTotal;
		energyText.color = ColorText(ShipResources.res.energy, ShipResources.res.capacityRemaining);
		
		//Food
		foodText.text = ": " + ShipResources.res.food;
		foodText.color = ColorText(ShipResources.res.food, ShipResources.res.storageRemaining);
		
		//Fuel
		fuelText.text = ": " + ShipResources.res.fuel;
		fuelText.color = ColorText(ShipResources.res.fuel, ShipResources.res.storageRemaining);
		
		//Materials
		materialsText.text = ": " + ShipResources.res.materials;
		materialsText.color = ColorText(ShipResources.res.materials, ShipResources.res.storageRemaining);
		
		//Parts
		partsText.text = ": " + ShipResources.res.parts;
		partsText.color = ColorText(ShipResources.res.parts, ShipResources.res.storageRemaining);
		
		//Waste
		wasteText.text = ": " + ShipResources.res.waste;
		wasteText.color = ColorText(ShipResources.res.waste, ShipResources.res.storageRemaining);
		
		//Ship Speed
		SetShipSpeedText();

		//Time Controls
		pauseButton.gameObject.SetActive(!GameClock.clock.isPaused);
		playButton.gameObject.SetActive(Time.timeScale != 1);
		fastForwardButton.gameObject.SetActive(!GameClock.clock.isSpedUp);
		pauseButton.interactable = playButton.interactable = fastForwardButton.interactable = !GameClock.clock.pauseControlsLocked;

		//Clock
		clockText.text = GameClock.clock.clockText;

		//Game Speed
		if (Time.timeScale != 1)
		{
			gameSpeedLayout.SetActive(true);
			if (Time.timeScale <= GameClock.PAUSE_SPEED)
				gameSpeedText.text = "Paused";
			else
				gameSpeedText.text = Time.timeScale.ToString() + "x Speed";
		}
		else
			gameSpeedLayout.SetActive(false);

		//Missing Needs TODO Maybe add? Kinda against philosophy of AI awareness and micromanagement, but players would really benefit
//		if (missingNeedsImage != null)
//		{
//			//Food
//			bool needFood = ShipResources.res.food == 0;
//			bool needKitchen = !GameReference.r.allSystems.Exists(obj => obj.function == ShipSystem.SysFunction.Kitchen && obj.status != ShipSystem.SysStatus.Disabled)
//			                   && GameReference.r.allCharacters.Exists(obj => obj.hunger > obj.hungerResilience);
//			//Air
//			bool needAir = ShipResources.res.usableAir == 0;
//			//Sleep
//			bool needBed = !GameReference.r.allSystems.Exists(obj => obj.function == ShipSystem.SysFunction.Bed && obj.status != ShipSystem.SysStatus.Disabled)
//			               && GameReference.r.allCharacters.Exists(obj => obj.sleepiness > obj.sleepinessResilience);
//			//Toilet
//			bool needToilet = !GameReference.r.allSystems.Exists(obj => obj.function == ShipSystem.SysFunction.Toilet && obj.status != ShipSystem.SysStatus.Disabled)
//			                  && GameReference.r.allCharacters.Exists(obj => obj.waste > obj.wasteResilience);
//		}
	}

	/**Move the chosen text briefly to indicate change
	 */
	public void AnimateResourceElements(GameObject target, int colorBurst)
	{

		if (target != null)
		{
			var text = target.GetComponentInChildren<Text>();
			var symbol = target.GetComponentInChildren<Image>();

			if (text != null && text.isActiveAndEnabled)
				text.GetComponent<Animator>().SetTrigger("PopOut");

			if (symbol != null && symbol.isActiveAndEnabled && colorBurst != 0)
			{
				bool dir = colorBurst > 0;

				StartCoroutine(symbol.GetComponent<ResourceColor>().BurstColor(dir));
			}
		}
	}

	/**Custom code for the ship speed text.
	 */
	void SetShipSpeedText()
	{
		//Get offCourse value
		float courseDeviation = ShipMovement.sm.GetOffCourse();

		//Set course portion of shipSpeedText
		if (courseDeviation < 0.05f)
		{
			shipSpeedText.text = "Perfect Course";
			shipSpeedText.color = ColorPalette.cp.blue4;
		}
		else if (courseDeviation < 0.15f)
		{
			shipSpeedText.text = "Good Course";
			shipSpeedText.color = ColorPalette.cp.blue4;
		}
		else if (courseDeviation < 0.3f)
		{
			shipSpeedText.text = "On Course";
			shipSpeedText.color = ColorPalette.cp.blue4;
		}
		else if (courseDeviation < 0.5f)
		{
			shipSpeedText.text = "Near Course";
			shipSpeedText.color = ColorPalette.cp.wht;
		}
		else if (courseDeviation < 0.75f)
		{
			shipSpeedText.text = "Poor Course";
			shipSpeedText.color = ColorPalette.cp.wht;
		}
		else if (courseDeviation < 1.05f)
		{
			shipSpeedText.text = "Off Course";
			shipSpeedText.color = ColorPalette.cp.yellow4;
		}
		else if (courseDeviation < 1.4f)
		{
			shipSpeedText.text = "Losing Progress";
			shipSpeedText.color = ColorPalette.cp.yellow4;
		}
		else
		{
			shipSpeedText.text = "Wrong Way";
			shipSpeedText.color = ColorPalette.cp.red4;
		}

		//Special case:
		if (ShipResources.res.speed < 0)
		{
			shipSpeedText.text = "Negative Momentum";
			shipSpeedText.color = ColorPalette.cp.red4;
		}

		//Set speed portion of shipSpeedText
		if (ShipResources.res.speed == 0)
		{
			shipSpeedText.text = "Stopped";
			shipSpeedText.color = ColorPalette.cp.yellow4;
		}
		else if (Mathf.Abs(ShipResources.res.speed) < 30)
			shipSpeedText.text += ", Slow";
		else if (Mathf.Abs(ShipResources.res.speed) < 60)
			shipSpeedText.text += ", Fair";
		else if (Mathf.Abs(ShipResources.res.speed) < 110)
			//Nothing at this speed
			shipSpeedText.text += "";
		else if (Mathf.Abs(ShipResources.res.speed) < 150)
			shipSpeedText.text += ", Brisk";
		else if (Mathf.Abs(ShipResources.res.speed) < 180)
			shipSpeedText.text += ", Fast";
		else
		{
			shipSpeedText.text += ", Blazing";

			//Achievement
			if (courseDeviation >= 1.4f)
				AchievementTracker.UnlockAchievement("WRONG_WAY");

		}
	}

	/**Returns color to use for numbers based on how much of the resource is there.
	 * storageLeft checks if the resource has maxed its space.
	 */
	Color ColorText(int value, int storageLeft)
	{
		if (value <= 0)
			return ColorPalette.cp.red4;
		else if (value < 6)
			return ColorPalette.cp.yellow4;
		else
			return ColorPalette.cp.wht;
	}

	/**Helper method for values with no limit.
	 * Change the color of numbers based on how much is left
	 */
	Color ColorText(int value)
	{
		return ColorText(value, -1);
	}

	void Awake()
	{
		if (hudu == null)
		{
			hudu = this;
		}
		else if (hudu != this)
			Destroy(this);
	}
}
