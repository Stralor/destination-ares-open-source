using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AchievementTracker
{
	public static List<Achievement> allAchievements = new List<Achievement>();
	public static List<Achievement> unlockedAchievements = new List<Achievement>();

	public static void UnlockAchievement(string achID, bool save = true)
	{
		var ach = AchievementTracker.allAchievements.Find(obj => obj != null && obj.achievementID.ToUpper() == achID.ToUpper());

		if (ach != null)
		{
			UnlockAchievement(ach, save: save);
		}
		else
		{
			Debug.Log("Couldn't find achievementID: " + achID);
		}
	}

	public static void UnlockAchievement(Achievement ach, bool save = true)
	{
		if (ach != null)
		{
			ach.Unlock(save: save);
		}
		else
		{
			Debug.Log("Achievement invalid: " + ach);
		}
	}
}
