using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public class Environment_Customization : Environment
{
	//public LayerMask validRaycastLayers;

	public static Environment_Customization cust;

	public Transform modules, systems, crew;

	public CenterGroup centerAdjuster;

	public GameObject controls_Modules, controls_Systems, controls_Crew, selectionMenu, saveMenu, loadMenu, tooltipWarning;

	public Customization_ResourcesMenu resourcesMenu;

	public Customization_EditMenu editMenu;

	public Button buyDoorButton, cancelSceneButton, resourcesButton;

	List<Placement> _allPickUps = new List<Placement>();
	List<Placement> _allModules = new List<Placement>();
	List<Placement> _allOpenings = new List<Placement>();
	List<Placement> _allSystems = new List<Placement>();
	List<Placement> _allCrew = new List<Placement>();

	[HideInInspector] public bool tab_Module, tab_System, tab_Crew;

	string _lastSelectionPopulation;

	bool _repeatInstance = false;

	public int air = 0, food = 0, fuel = 0, materials = 0, parts = 0, waste = 0;

	public string shipSaveName { get; set; }



	/**How much physical storage space is aboard the ship.
	 */
	public int storageTotal
	{ 
		get
		{
			//Get ready
			int value = 0;

			//Check storage
			foreach (var t in GameReference.r.allSystems)
			{
				if (t.function == ShipSystem.SysFunction.Storage)
					value += (int)(t.Use() * 16);
			}

			//Check modules
			foreach (var t in _allModules)
			{
				value += Module.ModuleStorageDictionary [t.GetComponentInParent<Module>().size];
			}

			//Account for openings
			value -= 4 * _allOpenings.Count;

			//Return
			return value > 0 ? value : 0;
		}
	}

	public int storageRemaining
	{
		get
		{
			int value =	storageTotal - (air + food + fuel + materials + (parts * ShipResources.partsVolume) + waste);

			return value;
		}
	}

	/*
	 * TAB TRACKING
	 */

	public void ChangeToModuleTab()
	{
		tab_Module = true;
		tab_System = false;
		tab_Crew = false;

		controls_Modules.SetActive(true);
		controls_Systems.SetActive(false);
		controls_Crew.SetActive(false);

		FindObjectOfType<CustomizationTips>().ChangeTip();

		CloseMenus();

		DestroyCurrentPickup();

		UpdateModuleSelectability();
	}

	public void ChangeToSystemTab()
	{
		tab_Module = false;
		tab_System = true;
		tab_Crew = false;

		controls_Modules.SetActive(false);
		controls_Systems.SetActive(true);
		controls_Crew.SetActive(false);
	
		FindObjectOfType<CustomizationTips>().ChangeTip();

		CloseMenus();

		DestroyCurrentPickup();
	}

	public void ChangeToCrewTab()
	{
		tab_Module = false;
		tab_System = false;
		tab_Crew = true;
	
		controls_Modules.SetActive(false);
		controls_Systems.SetActive(false);
		controls_Crew.SetActive(true);

		FindObjectOfType<CustomizationTips>().ChangeTip();

		CloseMenus();

		DestroyCurrentPickup();
	}

	public void UpdateModuleSelectability(Placement ignored = null)
	{
		//Update our modules selectability based on contents
		foreach (var t in _allModules)
		{
			t.isSelectable = !t.OthersContainsTag("Char&Sys");
		}
	}



	/*
	 * PLACEABLES CONTROLS
	 */

	public override void PressedCancel()
	{
		//Close Menus
		if (editMenu.isActiveAndEnabled)
		{
			//Special case
			CloseEditMenu();
			editMenu.ResetNames();
		}
		else if (resourcesMenu.isActiveAndEnabled || saveMenu.activeSelf || selectionMenu.activeSelf)
		{
			//Everything else
			CloseMenus();
		}

		//Delete current placement
		else if (PlacementManager.currentPlacement && !PlacementManager.currentPlacement.isPlaced)
		{
			AudioClipOrganizer.aco.PlayAudioClip("Hurt", null);
			DestroyCurrentPickup();
		}
		//Or Move to Leave Scene
		else
		{
			cancelSceneButton.Select();
		}
		//Once selected, cancelSceneButton will leave scene if Cancel key is pressed again (this is handled on cancelSceneButton's event triggers)
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

			_allPickUps.Remove(pickup);

			if (_allModules.Remove(pickup))
				Customization_CurrencyController.c.Rebate(pickup.GetComponentInParent<Module>());

			_allOpenings.Remove(pickup);

			if (_allSystems.Remove(pickup))
				Customization_CurrencyController.c.Rebate(pickup.GetComponentInParent<ShipSystem>());

			if (_allCrew.Remove(pickup))
				Customization_CurrencyController.c.Rebate(pickup.GetComponentInParent<Character>());

			Destroy(pickup.mainObject);

			CloseMenus();
		}
	}

	public void DestroyAllPickups()
	{
		DestroyCurrentPickup();

		for (int i = _allPickUps.Count; i > 0; i--)
		{
			Destroy(_allPickUps [i - 1].mainObject);
		}
		
		_allModules.Clear();
		_allOpenings.Clear();
		_allSystems.Clear();
		_allCrew.Clear();
		_allPickUps.Clear();
	}

	/**Spawn another new instance of what just got placed... if it wasn't just picked up */
	public void Recreate(Placement placement)
	{
		if (_repeatInstance)
		{
			//Get repeatable type
			var module = placement.GetComponentInParent<Module>();
			var opening = placement.GetComponentInParent<Opening>();
			var sys = placement.GetComponentInParent<ShipSystem>();
			var ch = placement.GetComponentInParent<Character>();

			bool recreated = false;

			//Do it
			if (module != null)
			{
				NewModule((int)module.size);
				recreated = true;
			}
			else if (opening != null)
			{
				NewDoor();
				recreated = true;
			}
			else if (sys != null)
			{
				NewSystem(sys.function.ToString());
				recreated = true;
			}
			else if (ch != null)
			{
				NewCrew(!ch.isRandomCrew);
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

	public void NewModule(int size)
	{
		//Can afford?
		if (Customization_CurrencyController.c.effectiveCurrency >= Customization_CurrencyController.ModuleValueDictionary [size])
		{
			//Instantiate module based on size
			GameObject go = null;
			switch ((Module.Size)size)
			{
			case Module.Size.Large:
					//Create
				go = Instantiate<GameObject>(Resources.Load("Module_Lar") as GameObject);
				break;
			case Module.Size.Medium:
					//Create
				go = Instantiate<GameObject>(Resources.Load("Module_Med") as GameObject);
				break;
			case Module.Size.Small:
					//Create
				go = Instantiate<GameObject>(Resources.Load("Module_Sma") as GameObject);
				break;
			default :
					//UNDEFINED. Just break, don't need it.
				break;
			}
			
			//If we have a module, let's set dat bitch up
			if (go != null)
			{
				//Prep and buy it
				ProcessModule(go);

				AudioClipOrganizer.aco.PlayAudioClip("Press", null);
			}
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

	public void NewDoor()
	{
		//Create a door
		ProcessDoor(Instantiate<GameObject>(Resources.Load("Door") as GameObject));
	}
	
	public void NewSystem(string function)
	{
		//System Type
		ShipSystem.SysFunction functionEnum = (ShipSystem.SysFunction)System.Enum.Parse(typeof(ShipSystem.SysFunction), function, true);

		//Can afford?
		if (Customization_CurrencyController.c.effectiveCurrency >= Customization_CurrencyController.GetAssetsCostOfBase(functionEnum))
		{
			var go = Instantiate<GameObject>(Resources.Load("ShipSystem") as GameObject);

			//Initial system setup
			var sys = go.GetComponent<ShipSystem>();
			sys.function = functionEnum;
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

	public void NewCrew(bool premium)
	{
		int baseCrewCost = 5;

		if (premium)
			baseCrewCost += 15;

		//Can afford
		if (Customization_CurrencyController.c.effectiveCurrency >= baseCrewCost)
		{
			//Create, prep, and buy it
			var go = Instantiate<GameObject>(Resources.Load("Char") as GameObject);

			//Crew setup
			var ch = go.GetComponentInChildren<Character>();
			ch.isRandomCrew = !premium;
			CharacterNames.AssignRandomName(out ch.firstName, out ch.lastName);
			ch.Rename();

			//Premos have something nice
			if (premium)
			{	
				//Add Value
				ch.GiveRandomRoleOrSkill();
			}

			//Colors setup
			CharacterColors colors = go.GetComponent<CharacterColors>();
			colors.Randomize(!premium);	//Don't always avoid unlocked, but at least make filler crew basic af

			ProcessCrew(go);

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

	public void ResetResources()
	{
		air = food = fuel = materials = parts = waste = 0;
	}




	/*
	 * SCENE CONTROL
	 */

	public void SaveShip()
	{
		//Achievements
		AchievementTracker.UnlockAchievement("CUSTOM_SHIP");

		//This achievement has to go before the SaveShip call
		if (Environment_Customization.cust.shipSaveName != null && SaveLoad.DoesShipNameAlreadyExist(Environment_Customization.cust.shipSaveName))
			AchievementTracker.UnlockAchievement("UPGRADE");

		SaveLoad.s.SaveShip(shipSaveName,
		                    new int[6]
		{
			air,
			food,
			fuel,
			materials,
			parts,
			waste
		});
	}

	public void LoadShip()
	{
		var shipLoadName = FindObjectOfType<LoadingMenu>().currentShipLoadName;

		if (shipLoadName != null && shipLoadName.Trim() != "")
		{
			try
			{
				DestroyAllPickups();
				Customization_CurrencyController.c.ResetCurrency();
				
				SaveLoad.s.LoadShip(shipLoadName, ProcessUnknownItem, SetResources, willCatchError: true);

				loadMenu.GetComponent<LoadingMenu>().failText.gameObject.SetActive(false);

				//Snap it all
				UpdateCenterPosition();

				//Stop recreating
				_repeatInstance = false;

				//Get that art back
				//		_allSystems.ForEach(obj => obj.GetComponentInParent<ShipSystemArtSpawner>().UpdateHullArt());

				//Close menus here, so it doesn't close if fail
				CloseMenus();
			}
			//Failed
			catch (System.Exception e)
			{
				Debug.LogWarning("Exception: " + e.Source + "\n\n" + e.StackTrace);

				//Which?
				var loadCust = loadMenu.GetComponent<LoadingMenu>();
				var offendingButton = loadCust.contentObject.ActiveToggles().ToList() [0];

				//Disable
				offendingButton.interactable = false;
				offendingButton.isOn = false;
				offendingButton.onValueChanged.Invoke(false);
				loadCust.failText.gameObject.SetActive(true);
			}
		}
		else
		{
			print("Cannot load ship: no valid file name!");
		}
	}

	public void LoadStart()
	{
		StartCoroutine(Level.MoveToScene("Start Menu"));
	}

	public void OpenSelectionMenu()
	{
		selectionMenu.SetActive(true);
		selectionMenu.GetComponent<FadeChildren>().FadeIn();
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
	}

	public void OpenEditMenu(Placement placement)
	{
		AudioClipOrganizer.aco.PlayAudioClip("Succeed", null);

		CameraEffectsController.cec.canMove = false;

		DestroyCurrentPickup();

		_repeatInstance = false;

		editMenu.gameObject.SetActive(true);
		editMenu.GetComponent<FadeChildren>().FadeIn();
	}

	public void CloseEditMenu()
	{
		CameraEffectsController.cec.canMove = true;
	
		//Automatically calls SetActive(false)
		if (editMenu.isActiveAndEnabled)
			editMenu.GetComponent<FadeChildren>().FadeOut();
	}

	public void OpenSaveMenu()
	{
		DestroyCurrentPickup();
		saveMenu.SetActive(true);
		saveMenu.GetComponent<FadeChildren>().FadeIn();
	}

	public void CloseSaveMenu()
	{
		//Automatically calls SetActive(false)
		if (saveMenu.activeSelf)
			saveMenu.GetComponent<FadeChildren>().FadeOut();
	}

	public void OpenLoadMenu()
	{
		DestroyCurrentPickup();
		loadMenu.SetActive(true);
		loadMenu.GetComponent<FadeChildren>().FadeIn();
	}

	public void CloseLoadMenu()
	{
		//Automatically calls SetActive(false)
		if (loadMenu.activeSelf)
			loadMenu.GetComponent<FadeChildren>().FadeOut();
	}

	public void OpenResourcesMenu()
	{
		DestroyCurrentPickup();

		resourcesMenu.gameObject.SetActive(true);
		resourcesMenu.SetSliders(air, food, fuel, materials, parts, waste);
		resourcesMenu.GetComponent<FadeChildren>().FadeIn();

		CameraEffectsController.cec.canMove = false;
	}

	public void CloseResourcesMenu()
	{
		//Automatically calls SetActive(false)
		if (resourcesMenu.isActiveAndEnabled)
			resourcesMenu.GetComponent<FadeChildren>().FadeOut();

		CameraEffectsController.cec.canMove = true;
	}

	public void CloseMenus()
	{
		CloseSelectionMenu();
		CloseEditMenu();
		CloseSaveMenu();
		CloseLoadMenu();
		CloseResourcesMenu();
	}




	/*
	 * PUBLIC GET FUNCTIONS
	 */

	/** Are all modules connected together? */
	public bool CheckModules()
	{
		//Easy answer: none exist, or one isn't connected
		if (_allModules.Count == 0 || (_allModules.Exists(obj => !obj.isConnected) && _allModules.Count > 1))
		{
			return false;
		}

		//Hard answer: loop through all connections and see if a complete chain can be made from a given start module
		var starter = _allModules [0].GetComponentInParent<Module>();

		//Add to this as connections are checked. We'll compare the resulting list to the full expected list. If it matches, we've got a good build.
		List<Module> checkedModuleList = new List<Module>();

		//Seed and iterate
		checkedModuleList.Add(starter);
		CheckModuleConnections(starter, ref checkedModuleList);

		//Compare results
		if (checkedModuleList.Count == _allModules.Count)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/** Are all the modules accessible?
	 */
	public bool CheckDoors()
	{
		//Easy answer: not enough doors
		if (_allModules.Count == 0 || _allOpenings.Count + 1 < _allModules.Count)
		{
			return false;
		}

		//Hard answer: loop through all connections, find connected openings, and see if a complete chain can be made from a given start module
		var starter = _allModules [0].GetComponentInParent<Module>();

		//Add to this as openings are checked. We'll compare the resulting list to the full expected list. If it matches, we've got a good build.
		List<Module> checkedModuleList = new List<Module>();

		//Seed and iterate
		checkedModuleList.Add(starter);
		CheckModuleOpenings(starter, ref checkedModuleList);

		//Compare results
		if (checkedModuleList.Count == _allModules.Count)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public int GetCrewCount()
	{
		return _allCrew.Count;
	}




	/*
	 * UTILITY AND MISCELLANEOUS METHODS
	 */


	public void TurnOnTooltips()
	{
		PlayerPrefs.SetInt("Tooltips", 1);	
	}


	/** Recursive scan through chained connections to find all modules, and add them to the checkedModules list */
	void CheckModuleConnections(Module source, ref List<Module> checkedModules)
	{
		//Loop through all the connections
		foreach (var t in source.GetComponentsInChildren<Connection>())
		{
			//Check connections colliding with this one
			var others = t.GetOthers();

			foreach (var u in others)
			{
				//Found  one
				var module = u.GetComponentInParent<Module>();

				//Only check those we haven't checked
				if (!checkedModules.Contains(module))
				{
					//Add it to the checked list, since we don't want to keep looping back to check it again infinitely
					checkedModules.Add(module);
					//Check this one's connections
					CheckModuleConnections(module, ref checkedModules);
				}
			}
		}
	}

	/**Recursive scan through modules connected to/by openings, looking for all the modules in a chain. Quite a bit more complicated and painful (to code) than just finding connected modules. */
	void CheckModuleOpenings(Module source, ref List<Module> checkedModules)
	{
		//Loop through all the connections
		foreach (var t in source.GetComponentsInChildren<Connection>())
		{
			//Check this connection, and connections colliding with this one
			var connectionsToCheck = new List<Connection>();
			connectionsToCheck.Add(t);
			connectionsToCheck.AddRange(t.GetOthers());

			//If we find an opening on any one of these, we'll want to include all of them in our module search
			bool foundAnOpening = false;

			foreach (var u in connectionsToCheck)
			{
				//Backdoor to finding connections with doors: SnapToNodes' currentNode
				if (_allOpenings.Exists(obj => obj.GetComponent<SnapToNodes>().currentNode == u.transform))
				{
					//Got one!
					foundAnOpening = true;
					break;
				}
			}

			//If there's an opening here (on any of these overlapping connections), we can find the related modules
			if (foundAnOpening)
			{
				//Look at all of the connections to find the modules sharing this opening
				foreach (var u in connectionsToCheck)
				{
					//The associated modules
					var module = u.GetComponentInParent<Module>();

					//Have we already checked it?
					if (!checkedModules.Contains(module))
					{
						//Add it to the checked list, since we don't want to keep looping back to check it again infinitely
						checkedModules.Add(module);
						//Check this one's openings
						CheckModuleOpenings(module, ref checkedModules);
					}
				}
			}
		}
	}

	public void SetResources(int[] res)
	{
		UnityEngine.Assertions.Assert.IsTrue(res.Length == 6);

		air = res [0];
		food = res [1];
		fuel = res [2];
		materials = res [3];
		parts = res [4];
		waste = res [5];

		Customization_CurrencyController.c.resValue = Customization_CurrencyController.GetResCost(air, food, fuel, materials, parts, waste);
	}

	public void SetRandomKeywordPref(bool pref)
	{
		var intPref = 0;
		if (pref)
			intPref = 1;

		PlayerPrefs.SetInt("RandomKeywords", intPref);

		foreach (var t in FindObjectsOfType<Customization_SystemMenuTooltip>())
		{
			t.UpdateTooltip();
		}
	}

	/**Determines what specific processor to call and calls it.
	 * Supports Modules, Doors, ShipSystems, Characters, and Hull Art.
	 */
	public void ProcessUnknownItem(GameObject item)
	{
		bool kept = false;

		if (item.GetComponentInChildren<Module>() != null)
		{
			ProcessModule(item);
			kept = true;
		}
		else if (item.GetComponent<Opening>() != null)
		{
			ProcessDoor(item);
			kept = true;
		}
		else if (item.GetComponent<ShipSystem>() != null)
		{
			ProcessSystem(item);
			kept = true;
		}
		else if (item.GetComponent<Character>() != null)
		{
			ProcessCrew(item);
			kept = true;
		}
		else if (item.GetComponent<HullExtension>() != null)
		{
			Destroy(item);
		}
		else
		{
			Debug.LogError(item + " is not supported by this processor.");
		}

		//Don't make more
		_repeatInstance = false;

		//These start placed.
		if (kept)
		{
			//Place
			var place = item.GetComponentInChildren<Placement>();
			place.Place();
		}
	}

	/**Basic creation actions all placements undergo.
	 * Called by the specific processors
	 */
	void ProcessPlacement(GameObject go)
	{
		//Add it to our shit
		NewCurrentPickup(go);

		//Housekeeping
		go.GetComponent<SnapBase>().enabled = true;

		//Do more of these!
		_repeatInstance = true;
	}

	/**Module-specific actions when creating a module.
	 * Calls ProcessPlacement
	*/
	void ProcessModule(GameObject go)
	{
		ProcessPlacement(go);

		_allModules.Add(PlacementManager.currentPlacement);

		go.GetComponentInChildren<Placement>().isSelectable = true;
		
		//Pos
		go.transform.SetParent(modules);

		//Only module? It's 'connected' already
		if (_allModules.Count == 1)
		{
			PlacementManager.currentPlacement.isConnected = true;
		}

		//Buy it
		Customization_CurrencyController.c.Buy(go.GetComponent<Module>());
	}

	/**Door-specific actions when creating a door.
	 * Calls ProcessPlacement
	 */
	void ProcessDoor(GameObject go)
	{
		ProcessPlacement(go);

		go.GetComponent<SnapBase>().Snap();

		//Add it
		_allOpenings.Add(PlacementManager.currentPlacement);
		go.transform.SetParent(modules);

	}

	/**System-specific actions when creating a system.
	 * Calls ProcessPlacement
	 */
	void ProcessSystem(GameObject go)
	{
		ProcessPlacement(go);

		//Customization Components
		go.GetComponentInChildren<Placement>().enabled = true;
		go.GetComponent<ShipSystemArtSpawner>().enabled = true;
		go.GetComponent<PlayerInteraction>().enabled = false;
		go.GetComponent<ShipSystemAnim>().enabled = false;
		go.GetComponent<ShipSystemAudio>().enabled = false;
		go.GetComponent<BoxCollider2D>().enabled = false;

		//Add it to our shit
		_allSystems.Add(PlacementManager.currentPlacement);
		go.transform.SetParent(systems);

		//Tooltip prep
		var tt = go.GetComponent<ShipSystemTooltip>();
		tt.costToShow = ShipSystemCost.Assets;
		tt.showRightClick = true;
		tt.showCondition = false;

		//It's ready, set it up
		var sys = go.GetComponent<ShipSystem>();
		sys.GetComponent<ShipSystemArtSpawner>().UpdateValues();

		//Buy it
		Customization_CurrencyController.c.Buy(sys);
	}

	/**Crew-specific actions when creating a character.
	 * Calls ProcessPlacement
	 */
	void ProcessCrew(GameObject go)
	{
		ProcessPlacement(go);
		
		//Enable the customization components and disable that glitchy old PlayerInteraction for the meantime
		go.GetComponent<Placement>().enabled = true;
		go.GetComponent<PlayerInteraction>().enabled = false;

		//Add it
		_allCrew.Add(PlacementManager.currentPlacement);
		go.transform.SetParent(crew);

		//Tooltip Prep
		var tt = go.GetComponent<CharacterTooltip>();
		tt.showCost = tt.showRightClick = true;
		tt.showNeeds = tt.showStatus = false;

		//Buy it
		Customization_CurrencyController.c.Buy(go.GetComponent<Character>());
	}


	void NewCurrentPickup(GameObject go)
	{
		//Clear old
		DestroyCurrentPickup();

		//Set new
		PlacementManager.currentPlacement = go.GetComponentInChildren<Placement>();
		PlacementManager.currentPlacement.isSelectable = true;
		PlacementManager.currentPlacement.PickUp();

		//Add
		if (!_allPickUps.Contains(PlacementManager.currentPlacement))
			_allPickUps.Add(PlacementManager.currentPlacement);

		//Safety
		while (_allPickUps.Contains(null))
		{
			_allPickUps.Remove(null);
		}
	}

	void ClearOldPlacementFlags(Placement placement)
	{
		foreach (var t in _allPickUps)
		{
			if (t != placement)
			{
				t.ClearOldFlags();
			}
		}
	}

	bool StateCheck(Placement placement)
	{
		//Null check
		if (placement == null)
			return false;

		return true;

		//We don't need to differentiate by
//		//Module layer
//		if (tab_Module && (_allModules.Contains(placement) || _allOpenings.Contains(placement)))
//			return true;
//
//		//System layer
//		if (tab_System && (_allSystems.Contains(placement) || _allOpenings.Contains(placement)))
//			return true;
//
//		//Crew layer
//		if (tab_Crew && _allCrew.Contains(placement))
//			return true;
//
//		//All other cases
//		return false;
	}

	new void Update()
	{
		base.Update();

		/* MANAGEMENT */

		//Buy button availability
		if (tab_Module)
		{
			if (_allModules.Count > 1 && !buyDoorButton.interactable)
			{
				buyDoorButton.interactable = true;
			}
			else if (_allModules.Count < 2 && buyDoorButton.interactable)
			{
				buyDoorButton.interactable = false;
			}
		}

		//Resources button availability
		resourcesButton.interactable = storageTotal > 0;

		/* INPUT */

		//Delete, if the save menu isn't open
		if (!saveMenu.activeSelf && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)))
		{
			AudioClipOrganizer.aco.PlayAudioClip("Hurt", null);
			DestroyCurrentPickup();
		}
	}

	protected override void Start()
	{
		base.Start();

		ChangeToModuleTab();

		MusicController.mc.playlist = new List<AudioClip>(){ MusicController.mc.setup };

		MusicController.mc.QueueSong(MusicController.mc.setup);

		if (PlayerPrefs.GetInt("Tooltips") == 0)
		{
			tooltipWarning.SetActive(true);
		}

		//Fade Out listeners
		var fade = editMenu.GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => editMenu.gameObject.SetActive(false));

		fade = saveMenu.GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => saveMenu.gameObject.SetActive(false));

		fade = loadMenu.GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => loadMenu.gameObject.SetActive(false));

		fade = selectionMenu.GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => selectionMenu.gameObject.SetActive(false));

		fade = resourcesMenu.GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => resourcesMenu.gameObject.SetActive(false));
	}

	void Awake()
	{
		if (cust == null)
			cust = this;
		else if (cust != this)
			Destroy(this);
	}

	void UpdateCenterPosition(Placement ignore = null)
	{
		if (centerAdjuster)
		{
			//Adjust the modules and get the new position
			var vector = centerAdjuster.UpdateGroupPosition();

			//Update everything else to the new position
			CenterGroup.MoveChildrenRigidbody2Ds(systems, vector);
			CenterGroup.MoveChildrenRigidbody2Ds(crew, vector);
		}
	}

	/**Re-Snaps all openings (doors), so they rebind their currentNode.
	 * Necessary so we can have updated opening info for finding modules connected by openings (in CheckModuleOpenings).
	 */
	void UpdateOpeningsSnap(Placement ifModule)
	{
		if (_allModules.Contains(ifModule))
		{
			_allOpenings.ForEach(obj => obj.GetComponent<SnapBase>().Snap());
		}
	}

	void ModuleCheck(Placement placement)
	{	
		//Module check
		if (_allModules.Contains(placement))
		{
			if (_allModules.Count == 1)
				placement.doesNotNeedConnections = true;
			else
				foreach (var t in _allModules.FindAll(obj => obj.doesNotNeedConnections))
				{
					t.doesNotNeedConnections = false;
				}
		}
	}

	void StopRecreating(Placement ignore = null)
	{
		_repeatInstance = false;
	}

	void OnEnable()
	{
		//Set
		PlacementManager.stateCheck = StateCheck;
		PlacementManager.longPressPrep = editMenu.SetNewTarget;
		PlacementManager.canLongPress = editMenu.IsValidTarget;
		PlacementManager.rightClickPrep = editMenu.SetNewTarget;
		//Add
		PlacementManager.onPlace += ClearOldPlacementFlags;
		PlacementManager.onPlace += UpdateCenterPosition;
		PlacementManager.onPlace += UpdateModuleSelectability;
		PlacementManager.onPlace += UpdateOpeningsSnap;
		PlacementManager.onClear += DestroyPickup;
		PlacementManager.onPickUp += ModuleCheck;
		PlacementManager.onPickUp += UpdateModuleSelectability;
		PlacementManager.onPickUp += StopRecreating;
		PlacementManager.onLongPress += OpenEditMenu;
		PlacementManager.onRightClick += OpenEditMenu;
		PlacementManager.onAfterPlace += Recreate;
	}

	void OnDisable()
	{
		//Remove
		PlacementManager.stateCheck -= StateCheck;
		PlacementManager.longPressPrep -= editMenu.SetNewTarget;
		PlacementManager.canLongPress -= editMenu.IsValidTarget;
		PlacementManager.rightClickPrep -= editMenu.SetNewTarget;
		PlacementManager.onPlace -= ClearOldPlacementFlags;
		PlacementManager.onPlace -= UpdateCenterPosition;
		PlacementManager.onPlace -= UpdateModuleSelectability;
		PlacementManager.onPlace -= UpdateOpeningsSnap;
		PlacementManager.onClear -= DestroyPickup;
		PlacementManager.onPickUp -= ModuleCheck;
		PlacementManager.onPickUp -= UpdateModuleSelectability;
		PlacementManager.onPickUp -= StopRecreating;
		PlacementManager.onLongPress -= OpenEditMenu;
		PlacementManager.onRightClick -= OpenEditMenu;
		PlacementManager.onAfterPlace -= Recreate;
	}
}
