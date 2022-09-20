using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class InputHandler : Environment
{
	#pragma warning disable 0108
	
	/* Handles calls from general input. Currently useful for global UI.
	 */

	[Header("Disabled Button Help Texts")]
	[TextArea]
	public string
		isInert;
	[TextArea]
	public string
		needsComms, disabled, notManual, broken, dead, destroyed;

	private bool buttonsOpen = true;
	private bool selectionChange = true;
	public static List<PlayerInteraction> allSelectables = new List<PlayerInteraction>();
	private PlayerInteraction currentlySelected, currentlyHighlighted, currentTarget;

	//Character fading shit
	private List<UnityEngine.Rendering.SortingGroup> characterSorting = new List<UnityEngine.Rendering.SortingGroup>();
	private int systemsSortID, characterSortID;
	public bool layeringToggle;

	//Buttons names by category!
	public List<string> alarmButtons
	{
		get
		{
			return new List<string>()
			{
				"Warning",
				"Alert",
				"Emergency",
				"Danger",
				"Ignore",
			};
		}
	}

	public List<string> characterButtons
	{
		get
		{
			return new List<string>()
			{
				
			};
		}
	}

	public List<string> systemButtons
	{
		get
		{
			return new List<string>()
			{
				"Toggle Power",
				"Overdrive",
				"Use",
				"Salvage",
			};
		}
	}

	const float BASE_COMMS_TIME = 2;

	//In-game construction variables
	public GameObject selectionMenu;
	public GameObject underlay;
	private string _lastSelectionPopulation;
	private bool _repeatInstance = false;

	//Cache
	private Alert al;
	//currentlySelected's Alert component
	private ShipSystem sh;
	//currentlySelected's ShipSystem component
	private Character ch;
	//currentlySelected's Character component

	private Animator controlsBarAnim;
	//The animator for the bar full of buttons
	private List<Button> buttons = new List<Button>();
	//UI buttons
	private Text targetName, targetStatus;
	//UI objects that update to reflect current highlight/ selection


	/*
	 * ONSCREEN BUTTON CONTROLS
	 */

	/**Basic button logic. Called by InputHandler.Update() when the key is pressed.
	 * Can also be called directly to trigger button logic via script (Ex.: mouseclicks on HUD buttons).
	 */
	public void DoButton(string name)
	{

		//SFX! Here since it can be called by script (via hotkeypress) rather than just click
		AudioClipOrganizer.aco.PlayAudioClip("press", null);

		//Update buttons after this
		selectionChange = true;

		//Actual game effects
		//Alarms
		if (alarmButtons.Contains(name))
		{
			if (al == null)
				return;
			//Use the comms to set this alarm!
			ShipSystem comms = GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled);
			//Toggle the alarm if we can use the comms
			if (comms != null)
			{
				float output = comms.Use();
				comms.Invoke("EndUse", comms.GetTickTime());
				al.ToggleAlert(name, BASE_COMMS_TIME / output);
				StatTrack.stats.alertsUsed++;
			}
			if (currentTarget)
				currentTarget.Deselect();
		}
		//Custom
		else
		{
			//Safety
			if (currentTarget == null)
				return;

			//Buttons
			switch (name.ToUpper())
			{
				//Characters
					
				//ShipSystems
				case "TOGGLE POWER":
					if (sh != null)
					{
						sh.TogglePower();
						currentTarget.Deselect();
					}
					break;
				case "OVERDRIVE":
					if (sh != null)
					{
						if (sh.status == ShipSystem.SysStatus.Disabled)
							sh.OverdriveOn();
						else
							sh.OverdriveToggle();
						currentTarget.Deselect();
					}
					break;
				case "USE":
					if (al != null)
					{
						//Use the comms to set this alert!
						ShipSystem comms = GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled);
						//Toggle the alarm if we can use the comms
						if (comms != null)
						{
							float output = comms.Use();
							comms.Invoke("EndUse", comms.GetTickTime());
							al.ToggleAlert(name, BASE_COMMS_TIME / output);
							StatTrack.stats.alertsUsed++;

							//If system is also off, turn it on
							if (sh.status == ShipSystem.SysStatus.Disabled)
								sh.status = ShipSystem.SysStatus.Inactive;
						}
						currentTarget.Deselect();
					}
					break;
				case "SALVAGE":
					if (al != null)
					{
						//Use the comms to set this alert!
						ShipSystem comms = GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled);
						//Toggle the alarm if we can use the comms
						if (comms != null)
						{
							float output = comms.Use();
							comms.Invoke("EndUse", comms.GetTickTime());
							al.ToggleAlert(name, BASE_COMMS_TIME / output);
							StatTrack.stats.alertsUsed++;
							
							sh.Disable();
						}
						currentTarget.Deselect();
					}
					break;
					
				//Unrecognized
				default :
					Debug.Log("No button found called \"" + name + "\".");
					break;
			}
		}
	}

	/**Turn off the UI button to prevent use.
	 */
	void CloseButton(Button button)
	{
		//Not interactable
		button.interactable = false;

		//Invisible
		var anim = button.GetComponent<Animator>();
		anim.SetBool("Hide", true);
		anim.SetBool("Active", false);
	}

	/**Turn on the UI button for use.
	 */
	void OpenButton(Button button)
	{
		//Interactable
		button.interactable = true;

		//Set the anim's active condition
		var anim = button.GetComponent<Animator>();
		anim.SetBool("Hide", false);
		anim.SetBool("Active", IsButtonAlreadyActive(button));
	}

	/**Check if the button's condition is active.
	 * (Used to do the pulsing text effect)
	 */
	bool IsButtonAlreadyActive(Button button)
	{
		//Alerts checks
		if (al != null && al.GetActivatedAlerts().Exists(obj => obj.ToString() == button.name))
			return true;

		//Character checks

		//Sys checks
		if (sh != null)
		{
			switch (button.name)
			{
			case "Toggle Power":
				return sh.status != ShipSystem.SysStatus.Disabled && !sh.isPassive;
			case "Overdrive":
				return sh.overdriven;
			}
		}

		//Otherwise false
		return false;
	}

	/**When mouse is over the button
	 */
	public void HoverButton(Button button)
	{
		if (button.interactable)
			//SFX
			AudioClipOrganizer.aco.PlayAudioClip("hover", null);
	}

	/**When mouse has clicked the button (or its hotkey has been pressed)
	 */
	public void ClickButton(Button button)
	{
		//Is it open and available?
		if (button.interactable)
		{
			DoButton(button.name);
		}
		else
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
	}

	public void ToggleLayering()
	{
		//Flip it
		layeringToggle = !layeringToggle;
		//Save it
		PlayerPrefs.SetInt("Layering Toggle", layeringToggle ? 1 : 0);
	}

	
	
	#region In-Game Construction Menu

	public void OpenSelectionMenu()
	{
		selectionMenu.SetActive(true);
		selectionMenu.GetComponent<FadeChildren>().FadeIn();
		
		underlay.SetActive(true);
	}

	public void ToggleSelectionMenu(string population)
	{
		if (!selectionMenu.activeSelf || population != _lastSelectionPopulation)
			OpenSelectionMenu();
		else
			CloseSelectionMenu();

		_lastSelectionPopulation = population;
	}

	public void CloseSelectionMenu()
	{
		//Automatically calls SetActive(false)
		if (selectionMenu.activeSelf)
			selectionMenu.GetComponent<FadeChildren>().FadeOut();
		
		underlay.SetActive(false);
	}
	
	public void NewSystem(string function)
	{
		//System Type
		ShipSystem.SysFunction functionEnum = (ShipSystem.SysFunction)System.Enum.Parse(typeof(ShipSystem.SysFunction), function, true);

		var partsCost = Customization_CurrencyController.GetPartsCost(functionEnum,
			ShipSystem.SysQuality.UnderConstruction, new List<ShipSystem.SysKeyword>());

		//Can afford initial cost?
		if (partsCost <= ShipResources.res.parts)
		{
			var go = Instantiate<GameObject>(Resources.Load("ShipSystem") as GameObject);

			//Initial system setup
			var sys = go.GetComponent<ShipSystem>();
			sys.function = functionEnum;
			sys.quality = ShipSystem.SysQuality.UnderConstruction;
			if (PlayerPrefs.GetInt("RandomKeywords") == 1 && sys.function != ShipSystem.SysFunction.Storage)
				sys.keywords.Add(ShipSystem.SysKeyword.Random);

			//Let's create, prep, and buy it
			ProcessSystem(go);
		
			//Audio
			AudioClipOrganizer.aco.PlayAudioClip("Press", null);
		}
		//Can't afford, first try to purchase
		else if (!_repeatInstance)
		{
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
		}
		//Can't afford, clear repeat
		else
		{
			_repeatInstance = false;
		}
	}
	
	/**System-specific actions when creating a system.
	 * Calls ProcessPlacement
	 */
	void ProcessSystem(GameObject go)
	{
		//Add it to our shit
		NewCurrentPickup(go);

		//Housekeeping
		go.GetComponent<SnapBase>().enabled = true;

		//Do more of these!
		_repeatInstance = true;

		//Customization Components
		var placement = go.GetComponentInChildren<Placement>();
		placement.enabled = true;
		go.GetComponent<ShipSystemArtSpawner>().enabled = true;
		go.GetComponent<PlayerInteraction>().enabled = false;
		go.GetComponent<ShipSystemAnim>().enabled = false;
		go.GetComponent<ShipSystemAudio>().enabled = false;
		go.GetComponent<BoxCollider2D>().enabled = false;
		
		//Add it to our shit
		go.transform.SetParent(GameObject.Find("ShipSystems").transform);

		//It's ready, set it up
		var sys = go.GetComponent<ShipSystem>();
		sys.GetComponent<ShipSystemArtSpawner>().UpdateValues();
		
		//Prevent crew interaction until it's placed
		sys.enabled = false;
	}

	void NewCurrentPickup(GameObject go)
	{
		//Clear old
		DestroyCurrentPickup();

		if (PlacementManager.currentPlacement != null)
			PlacementManager.currentPlacement.ClearOldFlags();
		
		//Set new
		PlacementManager.currentPlacement = go.GetComponentInChildren<Placement>();
		PlacementManager.currentPlacement.isSelectable = true;
		PlacementManager.currentPlacement.PickUp();
	}
	
	public void OnShipSystemPlaced(Placement placement)
	{
		var sys = placement.GetComponentInParent<ShipSystem>(); 

		Assert.IsNotNull(sys);
		
		var partsCost = Customization_CurrencyController.GetPartsCost(sys);
		
		//Buy it
		if (!ShipResources.res.SetParts(ShipResources.res.parts - partsCost, sys.transform))
		{
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
			_repeatInstance = false;
			Destroy(placement.mainObject);
			
			return;
		}
		
		//Flip switches to lock it into the game
		sys.enabled = true;
		placement.enabled = false;
		sys.GetComponent<ShipSystemArtSpawner>().enabled = false;
		sys.GetComponent<PlayerInteraction>().enabled = true;
		sys.GetComponent<ShipSystemAnim>().enabled = true;
		sys.GetComponent<ShipSystemAudio>().enabled = true;
		sys.GetComponent<BoxCollider2D>().enabled = true;

		UIGenerator.gen.AttachFollowCanvas(sys.gameObject);
	}

	public void DestroyCurrentPickup()
	{
		DestroyPickup(PlacementManager.currentPlacement);
	}

	public void DestroyPickup(Placement pickup)
	{
		if (pickup && !pickup.isPlaced)
		{
			//No more copies pls!
			_repeatInstance = false;

			Destroy(pickup.mainObject);
			
			AudioClipOrganizer.aco.PlayAudioClip("Hurt", null);

			CloseSelectionMenu();
		}
	}
	
	/**Spawn another new instance of what just got placed... if it wasn't just picked up */
	public void Recreate(Placement placement)
	{
		if (_repeatInstance)
		{
			var sys = placement.GetComponentInParent<ShipSystem>();

			bool recreated = false;

			//Do it
			if (sys != null)
			{
				NewSystem(sys.function.ToString());
				recreated = true;
			}

			//Did it?
			if (recreated)
			{
				//Keep rot
				PlacementManager.currentPlacement.mainObject.transform.localRotation = placement.mainObject.transform.localRotation;
			}
		}
	}

	#endregion
	
	
	
	/*
	 * UTILITY METHODS
	 */

	void Update()
	{

		base.Update();

		//Clear old selection on any non-PlayerInteraction click
		if (currentlySelected != null && Input.GetMouseButtonUp(1) && !allSelectables.Exists(obj => obj.highlighted))
		{
			currentlySelected.Deselect();
			currentlySelected = null;
		}

		//Check for selection/highlight updates
		bool foundSel = false;
		bool foundHigh = false;
		foreach (PlayerInteraction pI in allSelectables)
		{
			if (pI.selected)
			{
				//Update buttons when target changed
				if (currentlySelected != pI)
					selectionChange = true;

				currentlySelected = pI;
				foundSel = true;
			}
			if (pI.highlighted)
			{
				//Update buttons when target changed
				if (currentlyHighlighted != pI)
					selectionChange = true;

				currentlyHighlighted = pI;
				foundHigh = true;
			}
		}

		//Clear anything not found
		if (!foundSel)
			currentlySelected = null;
		if (!foundHigh)
		{
			currentlyHighlighted = null;
			//Make sure we know to reset back to currentlySelected for our target, where applicable
			if (currentTarget != currentlySelected)
				selectionChange = true;
		}

		//Found something? Update the target texts
		if (foundSel || foundHigh)
		{
			//Set currentTarget
			currentTarget = currentlyHighlighted;
			if (currentTarget == null)
				currentTarget = currentlySelected;
			
			UpdateTargetTexts();
		}
		//Nothing Highlighted/Selected. Close the text.
		else if (targetName && targetStatus)
		{
			targetName.enabled = targetStatus.enabled = false;
			currentTarget = null;
		}

		//Check for all input commands
		foreach (Button b in buttons)
		{
			//Has the button been pressed?
			if (Input.GetButtonDown(b.name))
			{
				ClickButton(b);
			}
		}

		//Tab toggling
		if (Input.GetKeyUp(KeyCode.Tab))
			ToggleLayering();
		//Or look up current state (in case it changed from a UI button)
		else
			layeringToggle = PlayerPrefs.GetInt("Layering Toggle") == 1;
		
		//Delete placeables, if the save menu isn't open
		if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
		{
			DestroyCurrentPickup();
		}

		//Are we fading?
		var fade = Input.GetButton("Fade Crew") ^ layeringToggle;

		//Keep SortingGroups up to date (Only do this when we have to! It's expensive mid-frame)
		if (GameReference.r.allCharacters.Count != characterSorting.Count)
		{
			//Clear nulls and inactives
			characterSorting.RemoveAll(obj => obj == null || !obj.isActiveAndEnabled);

			characterSorting.Sort();

			//Add new ones
			foreach (var t in GameReference.r.allCharacters)
			{
				var sort = t.GetComponentInChildren<UnityEngine.Rendering.SortingGroup>();
				if (!characterSorting.Contains(sort))
					characterSorting.Add(sort);
			}
		}

		//Fade out crew when appropriate
		foreach (var t in characterSorting)
		{
			if (t == null)
				continue;

			//Be sure to move the character zpos to match fade
			t.transform.root.position = fade ? new Vector3(0, 0, 0.002f) : Vector3.zero;

			//Fade the correct direction
			t.sortingLayerID = fade ? systemsSortID : characterSortID;
		}

		//If nothing is currentlySelected or currentlyHighlighted, we should close the buttons. We also don't need to continue this method.
		if (currentlySelected == null && currentlyHighlighted == null)
		{
			if (buttonsOpen)
			{
				//Turn off menu
				controlsBarAnim.SetBool("Active", false);
				//Disable buttons
				foreach (Button b in buttons)
					CloseButton(b);
				//Signal that buttons are closed
				buttonsOpen = false;
			}
		}
		//Don't continue if the buttons are open but we don't need to update them
		else if (buttonsOpen && !selectionChange)
			return;
		//Buttons need to be open or we need to change them. Flip a couple switches on the way in.
		else
		{
			buttonsOpen = true;
			selectionChange = false;
			UpdateButtons();
		}
	}

	/**Sets selectionChange to true.
	 * Call this to force a button update, or Invoke this for timed updates.
	 */
	public void SelectionUpdate()
	{
		selectionChange = true;
	}

	public void AbandonShip()
	{
		//Abandon
		SaveLoad.s.WipeRun();

		AchievementTracker.UnlockAchievement("ABANDON");

		//Exit
		StartCoroutine(Level.MoveToScene("Start Menu"));
	}

	/**Something is selected/ highlighted. Update the texts.
	 */
	void UpdateTargetTexts()
	{
		//Update UI for currentlySelected/ currentlyHighlighted. Safety check.
		if (currentlyHighlighted != null || currentlySelected != null)
		{
			
			//Let's set the UI text. Prioritize Highlight over Selection.
			if (currentlyHighlighted != null && currentlyHighlighted != currentlySelected)
			{
				
				//Color
				targetName.color = targetStatus.color = ColorPalette.cp.blue4;
				//Set text, from whatever component we find
				if (!TargetTextUpdate(currentlyHighlighted.GetComponent<Character>()))
					TargetTextUpdate(currentlyHighlighted.GetComponent<ShipSystem>());
			}
			
			//Selection
			else
			{
				
				//Color
				targetName.color = targetStatus.color = ColorPalette.cp.yellow4;
				//Set text, from whatever component we find
				if (!TargetTextUpdate(currentlySelected.GetComponent<Character>()))
					TargetTextUpdate(currentlySelected.GetComponent<ShipSystem>());
			}
		}
	}

	/**We've had a change of target or status. Bring on the full updates!
	 */
	void UpdateButtons()
	{
		//Set up the necessary components
		al = currentTarget.GetComponentInChildren<Alert>();
		sh = currentTarget.GetComponentInChildren<ShipSystem>();
		ch = currentTarget.GetComponentInChildren<Character>();

		//Bring menu up
		controlsBarAnim.SetBool("Active", true);
		
		//BUTTON CHECKS
		
		//Alert component is present
		if (al != null)
		{
			//Open buttons
			foreach (Button b in buttons)
			{
				if (alarmButtons.Contains(b.name))
				{
					OpenButton(b);
					
					//Set not interactable if the comms are offline!
					if (!GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled))
					{
						b.interactable = false;
						b.GetComponent<ControlsReference>().SetNeedsText(needsComms);
					}
				}
			}
		}
		//Has no Alert
		else
		{
			//Be sure these buttons are closed
			foreach (Button b in buttons)
			{
				if (alarmButtons.Contains(b.name))
					CloseButton(b);
			}
		}
		
		//Character component is present
		if (ch != null)
		{
			//Open buttons
			foreach (Button b in buttons)
			{
				if (characterButtons.Contains(b.name))
					OpenButton(b);
			}
		}
		//Not a Character
		else
		{
			//Be sure these buttons are closed
			foreach (Button b in buttons)
			{
				if (characterButtons.Contains(b.name))
					CloseButton(b);
			}
		}
		
		//ShipSystem component is present
		if (sh != null)
		{
			//Open buttons
			foreach (Button b in buttons)
			{
				if (systemButtons.Contains(b.name))
				{
					OpenButton(b);
					
					//Special Cases!

					//"Salvage" disabled when no usable comms
					if (b.name == "Salvage")
					{
						b.interactable = GameReference.r.allSystems.Exists(sys =>
							sys.function == ShipSystem.SysFunction.Communications &&
							sys.status != ShipSystem.SysStatus.Disabled);
						
						if (b.interactable == false)
							b.GetComponent<ControlsReference>().SetNeedsText(needsComms);
					}
					
					//Inert Systems - All other system buttons do nothing
					else if (sh.isPassive)
					{
						b.interactable = false;
						b.GetComponent<ControlsReference>().SetNeedsText(isInert);
					}
					
					//"Use" when not manual prod or no usable comms
					else if (b.name == "Use" &&
					         ((sh.condition != ShipSystem.SysCondition.Functional && sh.condition != ShipSystem.SysCondition.Strained)
					         || !sh.isManualProduction
					         || !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled)))
					{
						b.interactable = false;
						if (!sh.isManualProduction)
							b.GetComponent<ControlsReference>().SetNeedsText(notManual);
						else if (sh.isBroken)
							b.GetComponent<ControlsReference>().SetNeedsText(broken);
						else
							b.GetComponent<ControlsReference>().SetNeedsText(needsComms);
					}
					
					//"Overdrive" when broken
					else if (b.name == "Overdrive" && sh.isBroken)
					{
						b.interactable = false;
						b.GetComponent<ControlsReference>().SetNeedsText(broken);
					}

					//"Toggle" when broken
					else if (b.name == "Toggle Power" && sh.condition == ShipSystem.SysCondition.Broken)
					{
						b.interactable = false;
						b.GetComponent<ControlsReference>().SetNeedsText(broken);
					}
				}
			}
		}
		//Not a ShipSystem
		else
		{
			//Be sure these buttons are closed
			foreach (Button b in buttons)
			{
				if (systemButtons.Contains(b.name))
					CloseButton(b);
			}
		}
		
		//Disable buttons if character is dead or system is destroyed
		if ((ch != null && ch.status == Character.CharStatus.Dead) || (sh != null && sh.condition == ShipSystem.SysCondition.Destroyed))
		{
			foreach (Button b in buttons)
			{
				if (b.name != "Salvage")
				{
					b.interactable = false;
				}
				
				if (ch != null)
					b.GetComponent<ControlsReference>().SetNeedsText(dead);
				if (sh != null)
					b.GetComponent<ControlsReference>().SetNeedsText(destroyed);	
			}
		}
	}

	/**Update the UI text based on the selected/ highlighted target's values.
	 * Accepts MonoBehaviours (doors, maybe?), but supplies status/condition info with Characters and ShipSystems.
	 * Returns true only if a valid, non-null MonoBehavior was given.
	 */
	bool TargetTextUpdate(MonoBehaviour target)
	{

		if (target == null)
			return false;

		//Enable the name and status
		targetName.text = target.name;
		targetName.enabled = targetStatus.enabled = true;

		//Identify type, and set the status
		if (target is ShipSystem)
		{

			ShipSystem targ = (ShipSystem)target;
			targetStatus.text = targ.condition.ToString();
			//Instead, overdriven?
			if (targ.overdriven)
				targetStatus.text = "Overdriven";
			//Non-destroyed systems can add more information
			if (targ.condition != ShipSystem.SysCondition.Destroyed)
			{
				//Disabled?
				if (targ.status == ShipSystem.SysStatus.Disabled && targ.condition != ShipSystem.SysCondition.Broken)
					targetStatus.text += " (Disabled)";
				//Quality!
				targetStatus.text += ", " + targ.quality.ToString();
			}
		}
		else if (target is Character)
		{

			Character targ = (Character)target;
			targetStatus.text = targ.status.ToString();
			//Non-dead characters can add more information
			if (targ.status != Character.CharStatus.Dead)
			{
				if (targ.hunger > targ.hungerResilience)
					targetStatus.text += ", Hungry";
				else if (targ.waste > targ.wasteResilience)
					targetStatus.text += ", Needs Toilet";
				else if (targ.sleepiness > targ.sleepinessResilience)
					targetStatus.text += ", Tired";
			}
		}

		//Or, actually, don't show status, if not applicable
		else
			targetStatus.enabled = false;

		return true;
	}

	public override void PressedCancel()
	{
		if (PlacementManager.currentPlacement && !PlacementManager.currentPlacement.isPlaced)
		{
			AudioClipOrganizer.aco.PlayAudioClip("Hurt", null);
			DestroyCurrentPickup();
		}
		else
		{
			base.PressedCancel();
		}
	}

	void Start()
	{
		//Get objects
		foreach (var t in FindObjectsOfType<PlayerInteraction>())
			if (!allSelectables.Contains(t))
				allSelectables.Add(t);

		controlsBarAnim = GameObject.Find("LowerHUD").GetComponent<Animator>();
		buttons.AddRange(GameObject.Find("Controls").GetComponentsInChildren<Button>());

		systemsSortID = SortingLayer.NameToID("Systems");
		characterSortID = SortingLayer.NameToID("Characters");

		layeringToggle = PlayerPrefs.GetInt("Layering Toggle") == 1;

//		foreach (var t in buttons){
//			Text text = t.GetComponentInChildren<Text>();
//			text.text += " [" + ((KeyCode)System.Enum.Parse(typeof(KeyCode), "Emergency")).ToString() + "]";
//		}


		targetName = GameObject.Find("TargetName").GetComponent<Text>();
		targetStatus = GameObject.Find("TargetStatus").GetComponent<Text>();

		//Set the song
		SetGameProgressSongs(false);
	}

	/// <summary>
	/// Sets the song.
	/// </summary>
	/// <param name="now">If set to <c>true</c>, change the song now. If <c>false</c>, queue the song.</param>
	public void SetGameProgressSongs(bool now)
	{
		//The song we've chosen
		AudioClip song;

		//Various transition points
		if (ShipResources.res.progress < 20)
		{
			//Song set
			song = MusicController.mc.journey0;

			//Set playlist
			MusicController.mc.playlist = new List<AudioClip>()
			{
				MusicController.mc.journey0,
				MusicController.mc.journey0amb,
				MusicController.mc.extras [Random.Range(0, MusicController.mc.extras.Count)]
			};

			//Get ready for next change
			StartCoroutine(ChangeSongAtProgress(20));
		}
		else if (ShipResources.res.progress < 45)
		{
			//Song set
			song = MusicController.mc.journey1;

			//Set playlist
			MusicController.mc.playlist = new List<AudioClip>()
			{
				MusicController.mc.journey1,
				MusicController.mc.journey1amb,
//				MusicController.mc.extras [Random.Range(0, MusicController.mc.extras.Count)]
			};

			//Get ready for next change
			StartCoroutine(ChangeSongAtProgress(45));
		}
		else if (ShipResources.res.progress < 70)
		{
			//Song set
			song = MusicController.mc.journey2;

			//Set playlist
			MusicController.mc.playlist = new List<AudioClip>()
			{
				MusicController.mc.journey2,
				MusicController.mc.journey2amb,
//				MusicController.mc.extras [Random.Range(0, MusicController.mc.extras.Count)]
			};

			//Get ready for next change
			StartCoroutine(ChangeSongAtProgress(70));
		}
		else
		{
			//Final song set
			song = MusicController.mc.journey3;

			//Set playlist
			MusicController.mc.playlist = new List<AudioClip>()
			{
				MusicController.mc.journey3,
				MusicController.mc.journey3amb,
//				MusicController.mc.extras [Random.Range(0, MusicController.mc.extras.Count)]
			};
		}


		//When to do this change?
		if (now)
			MusicController.mc.SetSong(song);
		else
			MusicController.mc.QueueSong(song);
	}

	IEnumerator ChangeSongAtProgress(int value)
	{
		//Wait until progress is where we want it
		yield return new WaitUntil(() => ShipResources.res.progress >= value);

		//Queue a new song!
		SetGameProgressSongs(false);
	}

	private void OnEnable()
	{
		PlacementManager.onPlace += OnShipSystemPlaced; 
		PlacementManager.onClear += DestroyPickup;
		PlacementManager.onAfterPlace += Recreate;
	}

	private void OnDisable()
	{
		PlacementManager.onPlace -= OnShipSystemPlaced;
		PlacementManager.onClear -= DestroyPickup;
		PlacementManager.onAfterPlace -= Recreate;
	}
}

