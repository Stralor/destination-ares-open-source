using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Environment_SetupScreen : Environment
{
	#pragma warning disable 0108

	public Animator setupInterfaceAnim;

	[HideInInspector] public string shipName;

	public int storageMax = 420;
	private int storageRemaining = 420;
	public int sliderMaxWidth = 100;
	public float sliderMinWidthRatio;
	public int startingMaterials, startingAir, startingFood, startingFuel, startingParts, startingWaste;
	public Text storageText, materialsText, airText, foodText, fuelText, partsText, wasteText;
	public Slider storageSlider, materialsSlider, airSlider, foodSlider, fuelSlider, partsSlider, wasteSlider;

	const float STORAGE_MAX_POSITION = 1000;


	/*
	 * PROPERTIES
	 */

	private float materialsValue
	{
		get
		{
			return FromPositional(materialsSlider.value);
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
			return FromPositional(airSlider.value);
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
			return FromPositional(foodSlider.value);
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
			return FromPositional(fuelSlider.value);
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
			return FromPositional(partsSlider.value);
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
			return FromPositional(wasteSlider.value);
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
			return Mathf.Clamp((float)storageRemaining / (float)storageMax * STORAGE_MAX_POSITION, 0, STORAGE_MAX_POSITION);
		}
	}


	/*
	 * METHODS
	 */

	float FromPositional(float position)
	{
		float value = (Mathf.Pow(position / (float)STORAGE_MAX_POSITION, 2) * (float)storageMax);

		Mathf.Clamp(value, 0, storageRemaining);

		return value;
	}

	float ToPositional(float value)
	{
		float position = Mathf.Sqrt(value / (float)storageMax) * (float)STORAGE_MAX_POSITION;

		Mathf.Clamp(position, 0, STORAGE_MAX_POSITION);

		return position;
	}

	/**Ship name was esablished. Open the interface! */
	public void OpenInterface()
	{
		//Open the rest of the setup
		setupInterfaceAnim.SetTrigger("Open Setup");
		setupInterfaceAnim.SetBool("Ready", true);
	}

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

	public void LoadMain()
	{
		//Set Res
		StartingResources.sRes.shipName = shipName;
		StartingResources.sRes.air = Mathf.RoundToInt(airValue);
		StartingResources.sRes.food = Mathf.RoundToInt(foodValue);
		StartingResources.sRes.fuel = Mathf.RoundToInt(fuelValue);
		StartingResources.sRes.materials = Mathf.RoundToInt(materialsValue);
		StartingResources.sRes.parts = Mathf.RoundToInt(partsValue);
		StartingResources.sRes.waste = Mathf.RoundToInt(wasteValue);

		//Give random keywords (TEMP until customization)
		StartingResources.sRes.giveSystemKeywords = true;

		//Story
		StoryChooser.story.ChooseStory("Refugee Ship");

		//Ready
		StartingResources.sRes.isReady = true;

		//Go!
		StartCoroutine(Level.MoveToScene("Main"));
	}

	public override void PressedCancel()
	{
//		ReadyForNextScene();
	}

	void Update()
	{

		base.Update();

		//Update storage remaining!
		storageRemaining = storageMax - (Mathf.RoundToInt(materialsValue) + Mathf.RoundToInt(airValue) + Mathf.RoundToInt(foodValue) + Mathf.RoundToInt(fuelValue)
		+ (Mathf.RoundToInt(partsValue) * ShipResources.partsVolume) + Mathf.RoundToInt(wasteValue));

		//Update resource sliders
		materialsSlider.maxValue = ToPositional(materialsValue + storageRemaining);
		airSlider.maxValue = ToPositional(airValue + storageRemaining);
		foodSlider.maxValue = ToPositional(foodValue + storageRemaining);
		fuelSlider.maxValue = ToPositional(fuelValue + storageRemaining);
		partsSlider.maxValue = ToPositional(partsValue + storageRemaining / ShipResources.partsVolume);
		wasteSlider.maxValue = ToPositional(wasteValue + storageRemaining);

		//Update width of sliders to match percent of max resources available
		materialsSlider.GetComponent<LayoutElement>().preferredWidth 
			= (sliderMaxWidth * ToPositional(materialsValue + storageRemaining) / storageSlider.maxValue);
		airSlider.GetComponent<LayoutElement>().preferredWidth 	
			= (sliderMaxWidth * ToPositional(airValue + storageRemaining) / storageSlider.maxValue);
		foodSlider.GetComponent<LayoutElement>().preferredWidth 
			= (sliderMaxWidth * ToPositional(foodValue + storageRemaining) / storageSlider.maxValue);
		fuelSlider.GetComponent<LayoutElement>().preferredWidth 
			= (sliderMaxWidth * ToPositional(fuelValue + storageRemaining) / storageSlider.maxValue);
		partsSlider.GetComponent<LayoutElement>().preferredWidth 
			= (sliderMaxWidth * ToPositional(partsValue * ShipResources.partsVolume + storageRemaining) / storageSlider.maxValue);
		wasteSlider.GetComponent<LayoutElement>().preferredWidth 
			= (sliderMaxWidth * ToPositional(wasteValue + storageRemaining) / storageSlider.maxValue);

		/* OLD: adjust width of the object (aka, size/ range of slider movement) based on percent of total storage used, clamped to minimum width
		float x;

		materialsSlider.GetComponent<LayoutElement>().preferredWidth 
			= (x = sliderMaxWidth * (materialsSlider.value + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		airSlider.GetComponent<LayoutElement>().preferredWidth 	
			= (x = sliderMaxWidth * (airSlider.value + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		foodSlider.GetComponent<LayoutElement>().preferredWidth 
			= (x = sliderMaxWidth * (foodSlider.value + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		fuelSlider.GetComponent<LayoutElement>().preferredWidth 
			= (x = sliderMaxWidth * (fuelSlider.value + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		partsSlider.GetComponent<LayoutElement>().preferredWidth 
			= (x = sliderMaxWidth * (partsSlider.value * ShipResources.partsVolume + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		wasteSlider.GetComponent<LayoutElement>().preferredWidth 
			= (x = sliderMaxWidth * (wasteSlider.value + storageRemaining) / storageSlider.maxValue) > sliderMaxWidth * sliderMinWidthRatio ? x : sliderMaxWidth * sliderMinWidthRatio;
		*/
		
		storageSlider.value = storagePosition;

		//"Lock" animation for slider colors
		materialsSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		airSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		foodSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		fuelSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		partsSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		wasteSlider.GetComponentInChildren<Animator>().SetBool("Lock", storageRemaining == 0);
		
		//Update texts
		storageText.text = storageRemaining.ToString();
		materialsText.text = Mathf.RoundToInt(materialsValue).ToString();
		airText.text = Mathf.RoundToInt(airValue).ToString();
		foodText.text = Mathf.RoundToInt(foodValue).ToString();
		fuelText.text = Mathf.RoundToInt(fuelValue).ToString();
		partsText.text = Mathf.RoundToInt(partsValue).ToString();
		wasteText.text = Mathf.RoundToInt(wasteValue).ToString();
	}
}
