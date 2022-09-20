using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Environment_SplashScreen : Environment
{
	#pragma warning disable 0108
	
	public Animator loadingAnimator;

	public GameObject musicController;

	//Visible loading bar.
	//Slider progressBar;


	public override void PressedCancel()
	{
		//Peek at save data
		Kernys.Bson.BSONObject data = SaveLoad.s.Peek();

		//First run should go straight to tutorial!
//		if (data == null)
//			StartCoroutine(Level.MoveToScene("Loading"));
//		//Otherwise continue as normal
//		else
		StartCoroutine(Level.MoveToScene("Start Menu"));
	}

	void Start()
	{
		//In case of restart, gotta have time!
		Time.timeScale = 1;

		//Audio levels
		if (PlayerPrefs.HasKey("vol_AllSFX"))
			AudioController.aud.SetEffectsVolume(PlayerPrefs.GetFloat("vol_AllSFX"));
		if (PlayerPrefs.HasKey("vol_Music"))
			AudioController.aud.SetMusicVolume(PlayerPrefs.GetFloat("vol_Music"));

		//Other prefs
		if (!PlayerPrefs.HasKey("Tooltips"))
			PlayerPrefs.SetInt("Tooltips", 2);	//Default simple
		if (!PlayerPrefs.HasKey("EventTags"))
			PlayerPrefs.SetInt("EventTags", 1);	//Default on
		if (!PlayerPrefs.HasKey("ResultTags"))
			PlayerPrefs.SetInt("ResultTags", 1);	//Default on
		if (!PlayerPrefs.HasKey("MinigameDisabled"))
			PlayerPrefs.SetInt("MinigameDisabled", 1);
	}

	public void StartMusic()
	{
		//Set the song if nothing is playing
		if (MusicController.mc.musicPlayer == null || !MusicController.mc.musicPlayer.isPlaying)
			MusicController.mc.SetSong(MusicController.mc.opening);
	
		//Add theme to playlist anyway
		MusicController.mc.playlist.Add(MusicController.mc.theme);
	}

	public void StartLoadAnim()
	{
		loadingAnimator.SetBool("Loaded", true);
	}

	void Update()
	{

		base.Update();

		//Also check for input for loading the scene!
		if (Input.GetKeyDown("space") || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
			PressedCancel();
	}

}
