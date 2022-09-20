using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Steamworks;

// This is a port of StatsAndAchievements.cpp from SpaceWar, the official Steamworks Example.
class SteamStats : MonoBehaviour
{
	private static SteamStats _s;

	public static SteamStats s
	{
		get
		{
			if (_s == null)
			{
				GameObject go = new GameObject();
				_s = go.AddComponent<SteamStats>();
				DontDestroyOnLoad(_s);
			}

			return _s;
		}
	}

	// GameID
	private CGameID m_GameID;

	// Did we get the stats from Steam?
	private bool m_bRequestedStats;
	private bool m_bStatsValid;

	// Should we store stats this frame?
	protected bool m_bStoreStats;

	protected Callback<UserStatsReceived_t> m_UserStatsReceived;
	protected Callback<UserStatsStored_t> m_UserStatsStored;
	protected Callback<UserAchievementStored_t> m_UserAchievementStored;

	void OnEnable()
	{
		if (!SteamManager.Initialized)
			return;

		// Cache the GameID for use in the Callbacks
		m_GameID = new CGameID(SteamUtils.GetAppID());

		m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
		m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
		m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

		// These need to be reset to get the stats upon an Assembly reload in the Editor.
		m_bRequestedStats = false;
		m_bStatsValid = false;

		//Load in the achievements
//		var dir = new DirectoryInfo(Application.streamingAssetsPath + "/Achievements");
//		foreach (var t in dir.GetFiles("*.asset"))
//		{
//		}
	}

	private void Update()
	{
		if (!SteamManager.Initialized)
			return;

		if (!m_bRequestedStats)
		{
			// Is Steam Loaded? if no, can't get stats, done
			if (!SteamManager.Initialized)
			{
				m_bRequestedStats = true;
				return;
			}

			// If yes, request our stats
			bool bSuccess = SteamUserStats.RequestCurrentStats();

			// This function should only return false if we weren't logged in, and we already checked that.
			// But handle it being false again anyway, just ask again later.
			m_bRequestedStats = bSuccess;
		}

		if (!m_bStatsValid)
			return;

		//Store stats in the Steam database if necessary
		if (m_bStoreStats)
		{
			StoreStatsAndAchievements();
		}
	}

	public void StoreStatsAndAchievements()
	{
		if (!SteamManager.Initialized)
			return;

		if (StatTrack.stats != null)
		{
			//Set straight stats
			SteamUserStats.SetStat("days_trip", StatTrack.stats.longestJourney);
			SteamUserStats.SetStat("days_total", StatTrack.stats.daysInSpace_total);
			SteamUserStats.SetStat("days_fastest", StatTrack.stats.shortestJourney);
			SteamUserStats.SetStat("speed_fastest", StatTrack.stats.maxSpeed_total);
			SteamUserStats.SetStat("speed_effective", StatTrack.stats.maxEffectiveSpeed_total);
			SteamUserStats.SetStat("results_strong", StatTrack.stats.strongResults_total);
			SteamUserStats.SetStat("results_strong_hard", StatTrack.stats.strongHardResults_total);
			SteamUserStats.SetStat("results_fair", StatTrack.stats.fairResults_total);
			SteamUserStats.SetStat("results_weak", StatTrack.stats.weakResults_total);
			SteamUserStats.SetStat("results_fail", StatTrack.stats.eventsFailed_total);
			SteamUserStats.SetStat("deaths", StatTrack.stats.crewDied_total);
			SteamUserStats.SetStat("breaks", StatTrack.stats.systemsBroken_total);
			SteamUserStats.SetStat("destroys", StatTrack.stats.systemsDestroyed_total);
			SteamUserStats.SetStat("alerts", StatTrack.stats.alertsUsed_total);
			SteamUserStats.SetStat("crew_stressed", StatTrack.stats.crewStressedOut_total);
			SteamUserStats.SetStat("crew_injured", StatTrack.stats.crewInjured_total);
			SteamUserStats.SetStat("crew_unconscious", StatTrack.stats.crewKnockedUnconscious_total);
			SteamUserStats.SetStat("crew_psychotic", StatTrack.stats.crewGoneInsane_total);
			SteamUserStats.SetStat("crew_restrained", StatTrack.stats.crewRestrained_total);
			SteamUserStats.SetStat("energy_consumed", StatTrack.stats.energyConsumed_total);
			SteamUserStats.SetStat("energy_produced", StatTrack.stats.energyProduced_total);
			SteamUserStats.SetStat("energy_wasted", StatTrack.stats.energyWasted_total);
			SteamUserStats.SetStat("events_survived", StatTrack.stats.eventsSurvived_total);
			SteamUserStats.SetStat("food_grown", StatTrack.stats.foodGrown_total);
			SteamUserStats.SetStat("food_eaten", StatTrack.stats.foodEaten_total);
			SteamUserStats.SetStat("fuel_spent", StatTrack.stats.fuelSpent_total);
			SteamUserStats.SetStat("materials_spent", StatTrack.stats.materialsSpent_total);
			SteamUserStats.SetStat("oxygen_breathed", StatTrack.stats.oxygenBreathed_total);
			SteamUserStats.SetStat("parts_used", StatTrack.stats.partsUsed_total);
			SteamUserStats.SetStat("waste_created", StatTrack.stats.wasteCreated_total);
			SteamUserStats.SetStat("runs", StatTrack.stats.memorial.Count);
		}
				
		bool success = SteamUserStats.StoreStats();
		m_bStoreStats = !success;
	}

	public void SetStat(string statID, int value, bool storeAllStats = false)
	{
		SteamUserStats.SetStat(statID, value);

		if (storeAllStats)
			StoreStatsAndAchievements();
	}


	//-----------------------------------------------------------------------------
	// Purpose: We have stats data from Steam. It is authoritative, so update
	//			our data with those results now. Hah, jk idgaf xcept achievs
	//-----------------------------------------------------------------------------
	private void OnUserStatsReceived(UserStatsReceived_t pCallback)
	{
		if (!SteamManager.Initialized)
			return;

		// we may get callbacks for other games' stats arriving, ignore them
		if ((ulong)m_GameID == pCallback.m_nGameID)
		{
			if (EResult.k_EResultOK == pCallback.m_eResult)
			{
				Debug.Log("Received stats and achievements from Steam\n");

				m_bStatsValid = true;

				// Sync Achievements
				foreach (var ach in AchievementTracker.allAchievements)
				{
					//Skip nulls in our list
					if (ach == null)
						continue;

					//Update achieved status

					bool achieved = false;
					bool succ = SteamUserStats.GetAchievement(ach.achievementID, out achieved);

					//If updating failed, log it
					if (!succ)
						Debug.Log("Achievement not retrieved from Steam: " + ach.achievementID + " \"" + ach.name + "\"");
					//Otherwise, track achieved
					else if (achieved)
						AchievementTracker.UnlockAchievement(ach);
				}

//				SteamUserStats.GetStat("NumGames", out m_nTotalGamesPlayed);
//				SteamUserStats.GetStat("NumWins", out m_nTotalNumWins);
//				SteamUserStats.GetStat("NumLosses", out m_nTotalNumLosses);
//				SteamUserStats.GetStat("FeetTraveled", out m_flTotalFeetTraveled);
//				SteamUserStats.GetStat("MaxFeetTraveled", out m_flMaxFeetTraveled);
//				SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);
			}
			else
			{
				Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Our stats data was stored!
	//-----------------------------------------------------------------------------
	private void OnUserStatsStored(UserStatsStored_t pCallback)
	{
		// we may get callbacks for other games' stats arriving, ignore them
		if ((ulong)m_GameID == pCallback.m_nGameID)
		{
			if (EResult.k_EResultOK == pCallback.m_eResult)
			{
				Debug.Log("StoreStats - success");
			}
			else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
			{
				// One or more stats we set broke a constraint. They've been reverted,
				// and we should re-iterate the values now to keep in sync.
				Debug.Log("StoreStats - some failed to validate");
				// Fake up a callback here so that we re-load the values.
				UserStatsReceived_t callback = new UserStatsReceived_t();
				callback.m_eResult = EResult.k_EResultOK;
				callback.m_nGameID = (ulong)m_GameID;
				OnUserStatsReceived(callback);
			}
			else
			{
				Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: An achievement was stored
	//-----------------------------------------------------------------------------
	private void OnAchievementStored(UserAchievementStored_t pCallback)
	{
		// We may get callbacks for other games' stats arriving, ignore them
		if ((ulong)m_GameID == pCallback.m_nGameID)
		{
			if (0 == pCallback.m_nMaxProgress)
			{
				Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
			}
			else
			{
				Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
			}
		}
	}

	void Awake()
	{
		if (_s == null)
		{
			_s = this;
			DontDestroyOnLoad(this);
		}
		else
			Destroy(this);
	}
}