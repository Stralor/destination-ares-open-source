using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Customization_ResourcesMenu : MonoBehaviour
{


	public int sliderMaxWidth = 100;
	public float sliderMinWidthRatio;
	public int startingMaterials, startingAir, startingFood, startingFuel, startingParts, startingWaste;
	public Text totalCostText, storageText, materialsText, airText, foodText, fuelText, partsText, wasteText;
	public Slider storageSlider, materialsSlider, airSlider, foodSlider, fuelSlider, partsSlider, wasteSlider;

	public Button confirmButton;

	const float STORAGE_MAX_POSITION = 1000;

	float _lastValue = 0;

	/*
	 * PROPERTIES
	 */

	private float materialsValue
	{
		get
		{
			var value = FromPositional(materialsSlider.value);
			Environment_Customization.cust.materials = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			materialsSlider.value = ToPositional(value);
		}
	}

	private float airValue
	{ 
		get
		{
			var value = FromPositional(airSlider.value);
			Environment_Customization.cust.air = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			airSlider.value = ToPositional(value);
		}
	}

	private float foodValue
	{ 
		get
		{
			var value = FromPositional(foodSlider.value);
			Environment_Customization.cust.food = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			foodSlider.value = ToPositional(value);
		}
	}

	private float fuelValue
	{ 
		get
		{
			var value = FromPositional(fuelSlider.value);
			Environment_Customization.cust.fuel = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			fuelSlider.value = ToPositional(value);
		}
	}

	private float partsValue
	{ 
		get
		{
			var value = FromPositional(partsSlider.value);
			Environment_Customization.cust.parts = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			partsSlider.value = ToPositional(value);
		}
	}

	private float wasteValue
	{ 
		get
		{
			var value = FromPositional(wasteSlider.value);
			Environment_Customization.cust.waste = Mathf.RoundToInt(value);
			return value;
		}
		set
		{
			wasteSlider.value = ToPositional(value);
		}
	}

	/**The positional value of storageRemaining, in terms of total slider space rather than raw resource value
	 */
	private float storagePosition
	{
		get
		{
			return Mathf.Clamp((float)Environment_Customization.cust.storageRemaining / (float)Environment_Customization.cust.storageTotal * STORAGE_MAX_POSITION, 0, STORAGE_MAX_POSITION);
		}
	}



	/*
	 * PUBLIC METHODS
	 */

	public void SetResValue()
	{
		Customization_CurrencyController.c.resValue = Customization_CurrencyController.GetResCost(Mathf.Round(airValue), Mathf.Round(foodValue), Mathf.Round(fuelValue),
		                                                                                          Mathf.Round(materialsValue), Mathf.Round(partsValue), Mathf.Round(wasteValue));
		
	}

	public void SetSliders(int air, int food, int fuel, int materials, int parts, int waste)
	{
		StartCoroutine(WaitForSlidersThenSet(air, food, fuel, materials, parts, waste));
	}

	IEnumerator WaitForSlidersThenSet(int air, int food, int fuel, int materials, int parts, int waste)
	{
		yield return new WaitUntil(() => airSlider.isActiveAndEnabled && foodSlider.isActiveAndEnabled && fuelSlider.isActiveAndEnabled
		&& materialsSlider.isActiveAndEnabled && partsSlider.isActiveAndEnabled && wasteSlider.isActiveAndEnabled);

		airValue = air;
		foodValue = food;
		fuelValue = fuel;
		materialsValue = materials;
		partsValue = parts;
		wasteValue = waste;
	}

	public void PlaySteppedAudio(float value)
	{
		if (Mathf.Abs(FromPositional(value) - _lastValue) >= 1f)
		{
			_lastValue = FromPositional(value);
			AudioClipOrganizer.aco.PlayAudioClip("pop", null);
		}
	}




	/*
	 * UTILITY METHODS
	 */

	float FromPositional(float position)
	{
		float value = (Mathf.Pow(position / (float)STORAGE_MAX_POSITION, 2) * (float)Environment_Customization.cust.storageTotal);

		Mathf.Clamp(value, 0, Environment_Customization.cust.storageRemaining);

		return value;
	}

	float ToPositional(float value)
	{
		float position = Mathf.Sqrt(value / (float)Environment_Customization.cust.storageTotal) * (float)STORAGE_MAX_POSITION;

		Mathf.Clamp(position, 0, STORAGE_MAX_POSITION);

		return position;
	}

	//	/**Ship name was esablished. Open the interface! */
	//	public void OpenInterface()
	//	{
	//		//Open the rest of the setup
	////		setupInterfaceAnim.SetTrigger("Open Setup");
	////		setupInterfaceAnim.SetBool("Ready", true);
	//	}

	void Start()
	{
		//		airSlider.minValue = 28;

		ResetResourceSliders();

		storageSlider.maxValue = STORAGE_MAX_POSITION;

		//Set the song, add it to playlist
		MusicController.mc.SetSong(MusicController.mc.setup);
		MusicController.mc.playlist.Add(MusicController.mc.setup);

	}

	public void ResetResourceSliders()
	{
		//Reset sliders max
		materialsSlider.maxValue = storageSlider.maxValue;
		airSlider.maxValue = storageSlider.maxValue;
		foodSlider.maxValue = storageSlider.maxValue;
		fuelSlider.maxValue = storageSlider.maxValue;
		partsSlider.maxValue = storageSlider.maxValue;
		wasteSlider.maxValue = storageSlider.maxValue;

		//Reset current values to starting
		materialsValue = startingMaterials;
		airValue = startingAir;
		foodValue = startingFood;
		fuelValue = startingFuel;
		partsValue = startingParts;
		wasteValue = startingWaste;

		//Update will fix maxes if necessary
	}

	//	public void LoadMain()
	//	{
	//		//Set Res
	//		//StartingResources.sRes.shipName = shipName;
	//		StartingResources.sRes.air = Mathf.RoundToInt(airValue);
	//		StartingResources.sRes.food = Mathf.RoundToInt(foodValue);
	//		StartingResources.sRes.fuel = Mathf.RoundToInt(fuelValue);
	//		StartingResources.sRes.materials = Mathf.RoundToInt(materialsValue);
	//		StartingResources.sRes.parts = Mathf.RoundToInt(partsValue);
	//		StartingResources.sRes.waste = Mathf.RoundToInt(wasteValue);
	//
	//		//Give random keywords (TEMP until customization)
	//		StartingResources.sRes.giveSystemKeywords = true;
	//
	//		//Story
	//		StoryChooser.story.ChooseStory("Refugee Ship");
	//
	//		//Ready
	//		StartingResources.sRes.isReady = true;
	//
	//		//Go!
	//		StartCoroutine(Level.MoveToScene("Main"));
	//	}

	void Update()
	{
		//Update resource sliders
		materialsSlider.maxValue = ToPositional(materialsValue + Environment_Customization.cust.storageRemaining);
		airSlider.maxValue = ToPositional(airValue + Environment_Customization.cust.storageRemaining);
		foodSlider.maxValue = ToPositional(foodValue + Environment_Customization.cust.storageRemaining);
		fuelSlider.maxValue = ToPositional(fuelValue + Environment_Customization.cust.storageRemaining);
		partsSlider.maxValue = ToPositional(partsValue + Environment_Customization.cust.storageRemaining / ShipResources.partsVolume);
		wasteSlider.maxValue = ToPositional(wasteValue + Environment_Customization.cust.storageRemaining);

		//Update width of sliders to match percent of max resources available
		materialsSlider.GetComponent<LayoutElement>().preferredWidth 
		= (sliderMaxWidth * ToPositional(materialsValue + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);
		airSlider.GetComponent<LayoutElement>().preferredWidth 	
		= (sliderMaxWidth * ToPositional(airValue + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);
		foodSlider.GetComponent<LayoutElement>().preferredWidth 
		= (sliderMaxWidth * ToPositional(foodValue + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);
		fuelSlider.GetComponent<LayoutElement>().preferredWidth 
		= (sliderMaxWidth * ToPositional(fuelValue + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);
		partsSlider.GetComponent<LayoutElement>().preferredWidth 
		= (sliderMaxWidth * ToPositional(partsValue * ShipResources.partsVolume + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);
		wasteSlider.GetComponent<LayoutElement>().preferredWidth 
		= (sliderMaxWidth * ToPositional(wasteValue + Environment_Customization.cust.storageRemaining) / storageSlider.maxValue);


		storageSlider.value = storagePosition;

		//"Lock" animation for slider colors
		materialsSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);
		airSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);
		foodSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);
		fuelSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);
		partsSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);
		wasteSlider.GetComponentInChildren<Animator>().SetBool("Lock", Environment_Customization.cust.storageRemaining == 0);

		//Update texts
		storageText.text = Environment_Customization.cust.storageRemaining.ToString();
		materialsText.text = Mathf.RoundToInt(materialsValue).ToString();
		airText.text = Mathf.RoundToInt(airValue).ToString();
		foodText.text = Mathf.RoundToInt(foodValue).ToString();
		fuelText.text = Mathf.RoundToInt(fuelValue).ToString();
		partsText.text = Mathf.RoundToInt(partsValue).ToString();
		wasteText.text = Mathf.RoundToInt(wasteValue).ToString();

		//Update resValue
		var oldResValue = Customization_CurrencyController.c.resValue;
		SetResValue();

		//Update TotalCost
		var resValue = Customization_CurrencyController.c.resValue;
		string valColored = resValue - oldResValue > Customization_CurrencyController.c.effectiveCurrency
			? ColorPalette.ColorText(ColorPalette.cp.red4, resValue.ToString())
			: ColorPalette.ColorText(ColorPalette.cp.wht, resValue.ToString());
		totalCostText.text = "Total Cost: " + valColored;

		//Confirm button interactability NOW JUST ALWAYS ON - SAVE PREVENTS OVER COST
		confirmButton.interactable = true; //resValue - oldResValue <= Customization_CurrencyController.c.effectiveCurrency;
	}
}
