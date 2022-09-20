using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

[CreateAssetMenu()]
public class Achievement : ScriptableObject
{
	public string achievementID;
	public Sprite achievedArt;
	public string description;
	public int unlockPoints;

	public void Unlock(bool save = true)
	{
		//It's unlocked!
		if (!AchievementTracker.unlockedAchievements.Contains(this))
		{
			AchievementTracker.unlockedAchievements.Add(this);

			//Save this progress
			if (save)
			{
				//"Achievement Get!" find popup + add to anim queue
				var achPop = GameObject.FindObjectOfType<AchievementPopup>();
				if (achPop == null)
				{
					var go = GameObject.Instantiate((GameObject)Resources.Load("Achievement Popup"));
					achPop = go.GetComponent<AchievementPopup>();
					achPop.transform.position = new Vector3(0, -150, 0);
				}
				achPop.queue.Add(this);

				//Actual effects
				MetaGameManager.currentUnlockPoints += unlockPoints;
				SaveLoad.s.SaveMetaGame();
			}
		}

		//Steam Tracking
		if (SteamManager.Initialized && SteamStats.s != null)
		{
			SteamUserStats.SetAchievement(achievementID);

			SteamStats.s.StoreStatsAndAchievements();
		}
	}
}
