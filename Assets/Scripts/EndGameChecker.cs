using UnityEngine;
using System.Collections;

public class EndGameChecker : MonoBehaviour
{

	string result = "";
	bool end = false, survived = false;

	//	IEnumerator Win()
	//	{
	//		//Finish last story event
	//		yield return null;	//One frame +
	//		yield return new WaitUntil(() => !GameEventManager.gem.eventIsActive); //Wait
	//
	//		StatTrack.stats.AddCurrentVesselToMemorial(true, GameClock.clock.day.ToString(), result);
	//
	//		Level.AddScene("GameOver");
	//	}
	//
	//	IEnumerator Lose()
	//	{
	//		//Finish last story event
	//		yield return null;	//One frame +
	//		yield return new WaitUntil(() => !GameEventManager.gem.eventIsActive); //Wait
	//
	//		StatTrack.stats.AddCurrentVesselToMemorial(false, GameClock.clock.day.ToString(), result);
	//
	//		Level.AddScene("GameOver");
	//	}

	void Update()
	{
		//End conditions
		if (!end && !GameEventManager.gem.eventIsActive)
		{
			if (!GameReference.r.allCharacters.Exists(chara => chara.status != Character.CharStatus.Dead))
			{
				end = true;
				survived = false;
				result = "All Crew Deceased";

				//Achievement
				AchievementTracker.UnlockAchievement("DEATH");

				//Time to lose
				GameEventManager.gem.ForceEvent(EventCondition.Loss);
				AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);

				StartCoroutine(MemorialAndScene());
			}
			else if (ShipResources.res.progress < -10)
			{
				end = true;
				survived = false;
				result = "Lost in the Void";

				//Achievement
				AchievementTracker.UnlockAchievement("VOID");

				//Time to lose
				GameEventManager.gem.ForceEvent(EventCondition.Loss);
				AudioClipOrganizer.aco.PlayAudioClip("Event Start", null);

				StartCoroutine(MemorialAndScene());
			}
			else if (ShipResources.res.distance <= 0)
			{
				end = true;
				survived = true;

				if (result == "")
					result = "Reached Destination";

				//Achievements
				if (!GameReference.r.shipName.Contains("Tutorial"))
				{
					AchievementTracker.UnlockAchievement("ARES");

					if (StatTrack.stats.shipType == 0)
						AchievementTracker.UnlockAchievement("DEFAULT_WIN");

					if (StatTrack.stats.shipType == 1)
						AchievementTracker.UnlockAchievement("CUSTOM_WIN");
				}


				StartCoroutine(MemorialAndScene());
			}
		}
	}

	public void ChangeResultText(string newResult)
	{
		if (newResult != "")
			result = newResult;
	}

	IEnumerator MemorialAndScene()
	{
		//Delay long enough for last event to pop up
		yield return new WaitForSeconds(0.5f);
		//Then wait for that event to finish
		yield return new WaitUntil(() => !GameEventManager.gem.eventIsActive);

		StatTrack.stats.AddCurrentVesselToMemorial(survived, GameClock.clock.day.ToString(), result);

		//Log shortest journey (if it isn't the tut)
		if (GameClock.clock.day < StatTrack.stats.shortestJourney && !GameReference.r.shipName.Contains("Tutorial"))
		{
			if (survived)
				StatTrack.stats.shortestJourney = StatTrack.stats.shortestJourney > GameClock.clock.day ? GameClock.clock.day : StatTrack.stats.shortestJourney;

			if (StatTrack.stats.shortestJourney <= 30)
				AchievementTracker.UnlockAchievement("30_DAYS");
		}

		//Store stats!
		SteamStats.s.StoreStatsAndAchievements();

		Level.AddScene("GameOver");
	}
}
