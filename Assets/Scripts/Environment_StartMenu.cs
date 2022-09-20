using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class Environment_StartMenu : Environment
{

	public Text text_CurrentMission;
	public Button button_Continue, button_Start, button_Abandon;
	//For slow-motion effect
	public float timeScale;
	public Slider music, sfx;
	public LoadingMenu loadingMenu;
	public Image shipFader;


	public override void PressedCancel()
	{
		//We're overriding the call to open Pause. There's no pause on this menu.
	}

	public void Abandon()
	{
		//Wipe the run
		SaveLoad.s.WipeRun();

		//Achievement
		AchievementTracker.UnlockAchievement("ABANDON");


		//Refresh page
		CheckForSaveFile();
		//Clear the BG
		FindObjectsOfType<ClearContents>().ToList().ForEach(obj => obj.ClearAll());
	}

	public void Continue()
	{
		StartCoroutine(Level.MoveToScene("Main"));
	}

	public void NewGame()
	{
		//Reset events
		if (EventTree.s != null)
			Destroy(EventTree.s.gameObject);

		loadingMenu.gameObject.SetActive(true);

		//OLD Go to Setup
		//StartCoroutine(Level.MoveToScene("Setup"));
	}

	public void ShipNaming()
	{
		GetComponent<Animator>().SetTrigger("FadeMenu");
	}

	public void BeginGame()
	{
		GetComponent<FadeChildren>()?.FadeOut();
		
		//Reset events
		if (EventTree.s != null)
			Destroy(EventTree.s.gameObject);

		//Set the ship to load
		StartingResources.sRes.shipLoadFile = loadingMenu.currentShipLoadName;
		StartingResources.sRes.isReady = true;

		//Story
		StoryChooser.story.ChooseStory("Refugee Ship");
	}

	public void TransitionToGame()
	{
		StartCoroutine(Level.MoveToScene("Main"));
	}

	public void PermanentlyUnlockUnlocks()
	{
		MetaGameManager.AddKey("Unlock Menu");
	}

	public void TestLoadShip(GameObject toggle)
	{
		StartCoroutine(CallLoadShipInBG(toggle));
	}

	IEnumerator CallLoadShipInBG(GameObject toggle)
	{
		var toggleName = toggle.GetComponentInChildren<Text>().text;
		var shipLoadName = loadingMenu.currentShipLoadName;

		if (shipLoadName != null && shipLoadName.Trim() != "" && toggleName == shipLoadName)
		{
			StartCoroutine(CoroutineUtil.DoAfter(() => LoadShipInBG(shipLoadName), 0.5f));
		}
		else if (toggleName == shipLoadName)
		{
			print("Cannot load ship: no valid file name!");
			shipFader.CrossFadeAlpha(1, 0.5f, true);
		}
		else
		{
			shipFader.CrossFadeAlpha(1, 0.5f, true);
			StartCoroutine(CoroutineUtil.DoAfter(() => FindObjectsOfType<ClearContents>().ToList().ForEach(obj => obj.ClearAll()), 0.5f));
		}

		yield return null;
	}

	void LoadShipInBG(string shipLoadName)
	{
		try
		{
			SaveLoad.s.LoadShip(shipLoadName, ProcessUnknownItem, SetResources, willCatchError: true, spritesOnly: true);
			loadingMenu.failText.gameObject.SetActive(false);
			shipFader.CrossFadeAlpha(0, 0.5f, true);
			//Get that art back
			//		_allSystems.ForEach(obj => obj.GetComponentInParent<ShipSystemArtSpawner>().UpdateHullArt());
		}
		//Failed
		catch (System.Exception e)
		{
			Debug.LogWarning("Load exception: " + e.Source + "\n\n" + e.StackTrace);

			//Which?
			var offendingButton = loadingMenu.contentObject.ActiveToggles().ToList() [0];

			//Disable
			offendingButton.interactable = false;
			offendingButton.isOn = false;
			offendingButton.onValueChanged.Invoke(false);
			loadingMenu.failText.gameObject.SetActive(true);

			shipFader.CrossFadeAlpha(1, 0.5f, true);
		}
	}

	void ProcessUnknownItem(GameObject item)
	{
		//Turn off the Generic Tooltip
		var tt = item.GetComponentInChildren<GenericTooltip>();
		if (tt != null)
			tt.lockedFromOpenClose = true;
	}

	void SetResources(int[] res)
	{
		UnityEngine.Assertions.Assert.IsTrue(res.Length == 6);

		StartingResources.sRes.air = res [0];
		StartingResources.sRes.food = res [1];
		StartingResources.sRes.fuel = res [2];
		StartingResources.sRes.materials = res [3];
		StartingResources.sRes.parts = res [4];
		StartingResources.sRes.waste = res [5];
	}

	public void ShipBuilding()
	{
		StartCoroutine(Level.MoveToScene("Customization"));
	}

	public void Memorial()
	{
		Level.AddScene("Memorial");
	}

	public void Unlocks()
	{
		StartCoroutine(Level.MoveToScene("Unlocks"));
	}

	public void Quit()
	{
		Application.Quit();
	}

	public void SetSFXVol(float vol)
	{
		AudioController.aud.SetEffectsVolume(vol);
	}

	public void SetMusicVol(float vol)
	{
		AudioController.aud.SetMusicVolume(vol);
	}

	/**Is there an active run that should be loaded?
	 * Establishes context menu.
	 */
	public void CheckForSaveFile()
	{
		//Peek
		Kernys.Bson.BSONObject data = SaveLoad.s.Peek();

		//If a run exists, we can load in data we need from it!
		if (data != null && data ["runExists"])
		{
			//Valid version!
			if (data ["SaveVersion"] >= SaveLoad.SAVE_VERSION)
			{
				text_CurrentMission.text = data ["shipName"] + "\n" + "Progress: " + data ["progress"] + "%";
				button_Continue.gameObject.SetActive(true);
				button_Abandon.gameObject.SetActive(true);
				button_Start.gameObject.SetActive(false);
				button_Continue.Select();
				//Load up the sprites!
				SaveLoad.s.LoadGame(spritesOnly: true);
				shipFader.CrossFadeAlpha(0, 0.5f, true);
			}

			//Invalid version, bummer
			else
			{
				text_CurrentMission.text = "Comms frequencies changed.\n" +
				data ["shipName"] + " can't be contacted.\n" +
				"Start a new trip to Ares.";
				button_Continue.gameObject.SetActive(false);
				button_Abandon.gameObject.SetActive(false);
				button_Start.gameObject.SetActive(true);
				button_Start.Select();
				shipFader.CrossFadeAlpha(0, 0.5f, true);
				//At least fill in the StatTrack data
				SaveData.UnloadStatTrack(data);
				StatTrack.stats.AddCurrentVesselToMemorial(false, data ["day"], "Obsolesced", true);
			}
		}
		//Or start a new run
		else
		{
			text_CurrentMission.text = "Start a trip to Ares";
			button_Continue.gameObject.SetActive(false);
			button_Abandon.gameObject.SetActive(false);
			button_Start.gameObject.SetActive(true);
			button_Start.Select();
			shipFader.CrossFadeAlpha(0, 0.5f, true);
			//At least fill in the StatTrack data
			SaveData.UnloadStatTrack(data);
		}
	}

	
	protected override void Start()
	{
		base.Start();

		CheckForSaveFile();

		//Slow motion effect for the lulz
		Time.timeScale = timeScale;

		//Set song
		if (MusicController.mc.musicPlayer == null || !MusicController.mc.musicPlayer.isPlaying)
			MusicController.mc.SetSong(MusicController.mc.opening);

		//Set starting slider values
		if (PlayerPrefs.HasKey("vol_AllSFX"))
			sfx.value = PlayerPrefs.GetFloat("vol_AllSFX");
		if (PlayerPrefs.HasKey("vol_Music"))
			music.value = PlayerPrefs.GetFloat("vol_Music");

		//And remove overlay
		if (GameReference.r != null)
			GameReference.r.overlayActive = false;

		//Give TestLoadShip to the load menu toggles
		loadingMenu.actionsOnValueChange.Add(TestLoadShip);

		//Initial unlocks key
//		if (MetaGameManager.currentUnlockPoints > 99 && !MetaGameManager.keys.Exists(obj => obj.name == "Unlock Menu"))
//		{
//			MetaGameManager.AddKey("Unlock Menu");
//		}

		//How many unlock points do we have?
		print("Current unlock points: " + MetaGameManager.currentUnlockPoints);
	}

	void OnDisable()
	{
		Time.timeScale = 1;
	}
}
