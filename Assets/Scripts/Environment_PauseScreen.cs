using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Environment_PauseScreen : Environment
{

	public Canvas menu;
	public GameObject primary, confirm;
	public Slider music, sfx;

	bool confirmDialogueOpen;
	Intent intent;

	enum Intent
	{
		NONE,
		Quit,
		Menu
	}


	public override void PressedCancel()
	{
		base.PressedCancel();

		if (confirmDialogueOpen)
			LoadButtons(true);
		else
		{
			if (GameClock.clock)
				GameClock.clock.Unpause();

			//Allow cam movement
			if (CameraEffectsController.cec != null)
				CameraEffectsController.cec.canMove = true;

			if (GameEventManager.gem != null)
			{
				//Update Event
				if (GameEventManager.gem.eventIsActive)
					GameEventManager.gem.UpdateEventData();

				//Or remove overlay
				else if (GameReference.r != null)
					GameReference.r.overlayActive = false;
			}

			//Close Pause
			var fade = GetComponent<FadeChildren>();
			fade.onFadeOutFinish.AddListener(() => Level.CloseScene("Pause Menu"));
			fade.FadeOut();
		}
	}

	/*
	 * BUTTONS
	 */

	public void Resume()
	{
		PressedCancel();
	}

	public void Exit()
	{
		intent = Intent.Quit;
		LoadButtons(false);
	}

	public void Mute()
	{
		AudioListener.pause = !AudioListener.pause;
	}

	public void Menu()
	{
		intent = Intent.Menu;
		LoadButtons(false);
	}

	public void Confirm()
	{
		switch (intent)
		{
		case Intent.Quit:
			StartCoroutine(QuitAfterWaitUntil(SaveLoad.s.SaveGame()));
			break;
		case Intent.Menu:
			StartCoroutine(MoveToSceneAfterWaitUntil(SaveLoad.s.SaveGame(), "Splash"));
			break;
		default :
			PressedCancel();
			break;
		}
	}

	public void SetSFXVol(float vol)
	{
		AudioController.aud.SetEffectsVolume(vol);
	}

	public void SetMusicVol(float vol)
	{
		AudioController.aud.SetMusicVolume(vol);
	}

	/*
	 * UTILITY
	 */

	IEnumerator QuitAfterWaitUntil(bool waitUntil)
	{
		yield return new WaitUntil(() => waitUntil);
		Application.Quit();
	}

	IEnumerator MoveToSceneAfterWaitUntil(bool waitUntil, string scene)
	{
		yield return new WaitUntil(() => waitUntil);
		StartCoroutine(Level.MoveToScene(scene));
	}

	/**If main, loads default buttons. Else, loads confirm dialogue.
	 */
	public void LoadButtons(bool main)
	{
		if (main)
		{
			confirmDialogueOpen = false;
			primary.SetActive(true);
			confirm.SetActive(false);
			primary.GetComponentInChildren<Selectable>().Select();
		}
		else
		{
			confirmDialogueOpen = true;
			primary.SetActive(false);
			confirm.SetActive(true);
			confirm.GetComponentInChildren<Selectable>().Select();
		}
	}

	protected override void Start()
	{
		base.Start();

		//Set starting slider values
		if (PlayerPrefs.HasKey("vol_AllSFX"))
			sfx.value = PlayerPrefs.GetFloat("vol_AllSFX");
		if (PlayerPrefs.HasKey("vol_Music"))
			music.value = PlayerPrefs.GetFloat("vol_Music");

		//Start selection
		primary.GetComponentInChildren<Selectable>().Select();

		//Also prevent cam movement
		if (CameraEffectsController.cec != null)
			CameraEffectsController.cec.canMove = false;

		//And set overlay
		if (GameReference.r != null)
			GameReference.r.overlayActive = true;
		
		SceneManager.SetActiveScene(SceneManager.GetSceneByName("Pause Menu"));
	}

	void Awake()
	{
		menu.worldCamera = Camera.main;

		doPings = false;
	}
}
