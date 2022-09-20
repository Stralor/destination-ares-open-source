using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Environment_GameOver : Environment
{

	public Canvas menu;
	public GameObject memorial;
	public ScrollRect scrollRect;


	public override void PressedCancel()
	{
		var fade = GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => StartCoroutine(Level.MoveToScene("Start Menu")));
		fade.FadeOut();
	}


	protected override void Start()
	{
		base.Start();

		//Add metagame points
		int unlockPoints = 0;
		bool survived = StatTrack.stats.memorial [StatTrack.stats.memorial.Count - 1].survived;

		//Strong results are good. Strong hard results are even better. Good job, get your points
		unlockPoints += StatTrack.stats.strongResults * 10 + StatTrack.stats.strongHardResults * 10;

		//Failed results
		unlockPoints += StatTrack.stats.eventsFailed * 3;

		//The bad things
		unlockPoints += StatTrack.stats.systemsBroken / 4 + StatTrack.stats.systemsDestroyed * 2 + StatTrack.stats.crewDied;

		//Distance boost
		unlockPoints += StatTrack.stats.maxProgress / 2;

		//Winning bonus
		if (survived)
		{
			unlockPoints += 50;
		}

		//Round it down to nearest ten
		unlockPoints = (unlockPoints / 10) * 10;

		MetaGameManager.currentUnlockPoints += unlockPoints;

		SaveLoad.s.SaveGame(false);
		GameClock.clock.Pause();

		//Load in this run's memorial listing! (Should be the last one)
		GameObject go = Environment_Memorial.DisplayMemorialListing(StatTrack.stats.memorial.Count - 1);
		go.transform.SetParent(memorial.transform);

		//Populate end game stats
//		foreach (var t in StatTrack.endGameStats)
//		{
//			//Get a text
//			go = Instantiate(Resources.Load("Text") as GameObject);
//			go.transform.SetParent(scrollRect.content.transform);
//
//			Text text = go.GetComponent<Text>();
//
//			//Set the name! Regex is weird, I've already forgotten it. Basically, this populates alphabetically, I think.
//			string statName = System.Text.RegularExpressions.Regex.Replace(t.Name, "([a-z]?)_?([A-Z])", "$1 $2");
//			statName = char.ToUpper(statName [0]) + statName.Substring(1);
//
//			//Set the text
//			text.text = statName + ": " + t.GetValue(StatTrack.stats, null);
//		}

		//Select Button
		GetComponentInChildren<Button>(includeInactive: true).Select();

		//Also prevent cam movement
		if (CameraEffectsController.cec != null)
			CameraEffectsController.cec.canMove = false;

		//And set overlay
		if (GameReference.r != null)
			GameReference.r.overlayActive = true;

		//Set the song
		//Clear the playlist
		MusicController.mc.playlist.Clear();

		//Survived
		if (StatTrack.stats.memorial [StatTrack.stats.memorial.Count - 1].survived)
		{
			MusicController.mc.SetSong(MusicController.mc.victory);
			MusicController.mc.playlist.Add(MusicController.mc.victory);
			Resources.Load<Unlockable>("Unlockables/Victory").Unleash();
		}
		//Didn't survive
		else
		{
			MusicController.mc.SetSong(MusicController.mc.defeat);
			MusicController.mc.playlist.Add(MusicController.mc.defeat);
		}

		SceneManager.SetActiveScene(SceneManager.GetSceneByName("GameOver"));
	}

	void Awake()
	{
		menu.worldCamera = Camera.main;
	}
}
