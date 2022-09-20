using UnityEngine;
using System.Collections;

public class MiniGameBridge : MonoBehaviour
{

	private static MiniGameBridge _s;

	/**Object passed between scenes to set parameters like difficulty.
	 */
	public static MiniGameBridge b
	{
		get
		{
			if (_s == null)
			{
				_s = FindObjectOfType<MiniGameBridge>();

				if (_s == null)
				{
					GameObject gO = new GameObject();
					gO.name = "SceneBridge";
					
					_s = gO.AddComponent<MiniGameBridge>();

					DontDestroyOnLoad(gO);
				}
			}
			return _s;
		}
	}


	public EventOptionData callingEO;

	public void StartMinigame(EventOptionData initiator, int commandValue)
	{
		SaveLoad.s.SaveGame();

		//Get some data points
		callingEO = initiator;
		var difficulty = initiator.minigameDifficulty;

		//Let's start the minigame, if there's a minigame to start and there's more than one possible result!
		if (difficulty == EventOptionData.Minigame.NONE 
		    || PlayerPrefs.GetInt("MinigameDisabled") == 0
		    || initiator.nextEventChances.FindAll(obj => obj != null && obj.AreRequirementsMet()).Count < 2)
			StartCoroutine(ReturnToMainScene(-2));
		else
		{
			//Load minigame
			StartCoroutine(Level.MoveToScene("Event Game"));

			//Establish game parameters
			switch (difficulty)
			{
			case EventOptionData.Minigame.EASY:
				EventGameParameters.s.SetDifficulty(EventGameParameters.s.EASY, commandValue);
				break;
			case EventOptionData.Minigame.MEDIUM:
				EventGameParameters.s.SetDifficulty(EventGameParameters.s.MEDIUM, commandValue);
				break;
			case EventOptionData.Minigame.HARD:
				EventGameParameters.s.SetDifficulty(EventGameParameters.s.HARD, commandValue);
				break;
			}
		}
	}

	/**Be sure to call as coroutine.
	 */
	public IEnumerator ReturnToMainScene(int score)
	{

		//Wait for scene to load
		print("Waiting on GEM...");
		yield return new WaitUntil(() => GameEventManager.gem != null);
		print("...Returning to Main Scene.");

		//Set last action text
		if (GameEventManager.gem.eventLastAction != null)
			GameEventManager.gem.eventLastAction.text = callingEO.optionText;

		//Get result
		var next = callingEO.ChooseNextEvent(score);

		//If result wasn't fated, resolve minigame result
		if (callingEO.nextEventChances.FindAll(obj => obj != null && obj.isUsable && obj.AreRequirementsMet()).Count > 1)
		{
			//Result tag
			if (PlayerPrefs.GetInt("ResultTags") == 1)
			{
				GameEventManager.gem.eventLastAction.text += " [";
				if (next.minimumResult == EventStoreData.MinigameResult.Fail)
					GameEventManager.gem.eventLastAction.text += "failure";
				else
					GameEventManager.gem.eventLastAction.text += next.minimumResult.ToString().ToLower() + " success";
			
				GameEventManager.gem.eventLastAction.text += "]";
			}

			//Stats
			switch (next.minimumResult)
			{
			case EventStoreData.MinigameResult.Strong:
				StatTrack.stats.strongResults++;

				if (StatTrack.stats.strongResults_total > 41)
					AchievementTracker.UnlockAchievement("42_STRENGTH");

				if (callingEO.minigameDifficulty == EventOptionData.Minigame.HARD)
				{
					StatTrack.stats.strongHardResults++;

					if (StatTrack.stats.strongHardResults_total > 99)
						AchievementTracker.UnlockAchievement("HARD_STRENGTH");
				}

				break;
			case EventStoreData.MinigameResult.Fair:
				StatTrack.stats.fairResults++;
				break;
			case EventStoreData.MinigameResult.Weak:
				StatTrack.stats.weakResults++;
				break;
			case EventStoreData.MinigameResult.Fail:
				StatTrack.stats.eventsFailed++;
				break;
			}
		}

		//Return to the game!
		GameEventManager.gem.ResumeEvent(next);

		//We don't need these anymore
		Destroy(EventGameParameters.s.gameObject);
		Destroy(gameObject);
	}
}
