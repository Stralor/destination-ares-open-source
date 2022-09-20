using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

[Serializable]
public class StatTrack
{

	private static StatTrack _stats;

	public static StatTrack stats
	{
		get
		{
			if (_stats == null)
			{
				_stats = new StatTrack();
			}
			return _stats;
		}
		set
		{
			_stats = value;
		}
	}

	/**The given Ship/AI for a single playthrough. Contains the associated Crew.
	 */
	public struct Vessel
	{

		public string vesselName;
		public bool survived;
		public string time;
		public string result;
		public List<Crew> crew;
	}

	/**A crew member on a vessel, as identified by name and if they survived.
	 * TODO Sprite/ pic for memorial?
	 */
	public struct Crew
	{

		public string crewName;
		public bool survived;
		public string result;
	}



	/*
	 * END GAME AND MEMORIAL
	 */

	public List<Vessel> memorial = new List<Vessel>();
	// List of Ships/AIs that attempted or made the trek to Ares
	public List<Vessel> memorialReversed
	{
		get
		{
			List<Vessel> temp = new List<Vessel>();
			memorial.ForEach(obj => temp.Add(obj));
			temp.Reverse();
			return temp;
		}
	}

	private static string[] endGameStatNames =
		{
			"alertsUsed",
			"crewStressedOut",
			"crewInjured",
			"crewKnockedUnconscious",
			"crewGoneInsane",
			"crewRestrained",
			"crewDied",
			"systemsBroken",
			"systemsDestroyed",
			"energyConsumed",
			"energyProduced",
			"energyWasted",
			"eventsSurvived",
			"foodGrown",
			"foodEaten",
			"fuelSpent",
			"materialsSpent",
			"oxygenBreathed",
			"partsUsed",
			"totalAirPressureChange",
			"wasteCreated",
			"maxSpeed",
			"maxEffectiveSpeed",
			"maxProgress",
			"score",
			"strongResults",
			"strongHardResults",
			"fairResults",
			"weakResults",
			"eventsFailed"
		};

	public static List<PropertyInfo> endGameStats
	{
		get
		{
			List<PropertyInfo> list = new List<PropertyInfo>();

			foreach (var t in typeof(StatTrack).GetProperties())
			{
				foreach (var n in endGameStatNames)
				{
					if (t.Name == n)
						list.Add(t);
				}
			}

			return list;
		}
	}



	/*
	 * PILE OF PRIVATE FIELDS
	 */

	private int _clicks, _alertsUsed, _crewStressedOut, _crewInjured, _crewKnockedUnconscious, _crewGoneInsane, _crewRestrained, _crewDied, _crewFunerals, 
		_systemsBroken, _systemsDestroyed, _energyConsumed, _energyProduced, _energyWasted, _eventsSurvived, _foodGrown, _foodEaten, _fuelSpent, _materialsSpent, 
		_oxygenBreathed, _partsUsed, _totalAirPressureChange, _wasteCreated, _maxSpeed, _maxEffectiveSpeed, _maxProgress,
		_strongResults, _strongHardResults, _fairResults, _weakResults, _failResults;



	/*
	 * SESSION STATS
	 */

	public int score
	{ 
		get
		{
			try
			{
				//Tenth of travelled distance * 1 per alive char + 1, divided by 2 per dead char; +100 per mat, food, and part; all times progress multiplier (1 + 1 per 50%)
				int s = (int)((((ShipResources.res.startingDistance - ShipResources.res.distance) / 10) * (1 + GameReference.r.allCharacters.FindAll(t => t.status != Character.CharStatus.Dead).Count)
				        / (int)(Mathf.Pow(2, GameReference.r.allCharacters.FindAll(t => t.status == Character.CharStatus.Dead).Count)))
				        + (100 * (ShipResources.res.materials + ShipResources.res.food + ShipResources.res.parts)))
				        * ((ShipResources.res.progress / 50) + 1);

				//Maybe update high score
				if (s > highScore)
					highScore = s;

				return s;
			}
			//I don't give a fuck that I didn't use 'ex'. That's not the point; this is a safety for if those refs above are null. Turn off the warning.
			#pragma warning disable 0168
			catch (Exception ex)
			{
				if (SaveLoad.s.currentData.ContainsKey("score"))
					return SaveLoad.s.currentData ["score"];
				else
					return 0;
			}
			#pragma warning restore 0168
		}
	}

	/**	0 = default, 1 = custom, 2 = advanced */
	public int shipType { get; set; }

	public int clicks
	{
		get { return _clicks; }
		set
		{
			clicks_total += value - _clicks;
			_clicks = value;
		}
	}

	public int alertsUsed
	{
		get { return _alertsUsed; }
		set
		{
			if (value - _alertsUsed > 0)
				alertsUsed_total += value - _alertsUsed;
			_alertsUsed = value;
		}
	}

	public int crewStressedOut
	{
		get { return _crewStressedOut; }
		set
		{
			if (value - _crewStressedOut > 0)
				crewStressedOut_total += value - _crewStressedOut;
			_crewStressedOut = value;
		}
	}

	public int crewInjured
	{
		get { return _crewInjured; }
		set
		{
			if (value - _crewInjured > 0)
				crewInjured_total += value - _crewInjured;
			_crewInjured = value;
		}
	}

	public int crewKnockedUnconscious
	{
		get { return _crewKnockedUnconscious; }
		set
		{
			if (value - _crewKnockedUnconscious > 0)
				crewKnockedUnconscious_total += value - _crewKnockedUnconscious;
			_crewKnockedUnconscious = value;
		}
	}

	public int crewGoneInsane
	{
		get { return _crewGoneInsane; }
		set
		{
			if (value - _crewGoneInsane > 0)
				crewGoneInsane_total += value - _crewGoneInsane;
			_crewGoneInsane = value;
		}
	}

	public int crewRestrained
	{
		get { return _crewRestrained; }
		set
		{
			if (value - _crewRestrained > 0)
				crewRestrained_total += value - _crewRestrained;
			_crewRestrained = value;
		}
	}

	public int crewDied
	{
		get { return _crewDied; }
		set
		{
			if (value - _crewDied > 0)
				crewDied_total += value - _crewDied;

			_crewDied = value;
		}
	}

	/**Currently never incremented
	 */
	public int crewGivenFunerals
	{
		get { return _crewFunerals; }
		set
		{
			if (value - _crewFunerals > 0)
				crewGivenFunerals_total += value - _crewFunerals;

			_crewFunerals = value;
		}
	}

	public int systemsBroken
	{
		get { return _systemsBroken; }
		set
		{
			if (value - _systemsBroken > 0)
				systemsBroken_total += value - _systemsBroken;
			_systemsBroken = value;
		}
	}

	public int systemsDestroyed
	{
		get { return _systemsDestroyed; }
		set
		{
			if (value - _systemsDestroyed > 0)
				systemsDestroyed_total += value - _systemsDestroyed;

			_systemsDestroyed = value;
		}
	}

	public int energyConsumed
	{
		get { return _energyConsumed; }
		set
		{
			if (value - _energyConsumed > 0)
				energyConsumed_total += value - _energyConsumed;
			_energyConsumed = value;
		}
	}

	public int energyProduced
	{
		get { return _energyProduced; }
		set
		{
			if (value - _energyProduced > 0)
				energyProduced_total += value - _energyProduced;
			_energyProduced = value;
		}
	}

	public int energyWasted
	{
		get { return _energyWasted; }
		set
		{
			if (value - _energyWasted > 0)
				energyWasted_total += value - _energyWasted;
			_energyWasted = value;
		}
	}

	public int eventsSurvived
	{
		get { return _eventsSurvived; }
		set
		{
			if (value - _eventsSurvived > 0)
				eventsSurvived_total += value - _eventsSurvived;
			_eventsSurvived = value;
		}
	}

	public int foodGrown
	{
		get { return _foodGrown; }
		set
		{
			if (value - _foodGrown > 0)
				foodGrown_total += value - _foodGrown;
			_foodGrown = value;
		}
	}

	public int foodEaten
	{
		get { return _foodEaten; }
		set
		{
			if (value - _foodEaten > 0)
				foodEaten_total += value - _foodEaten;
			_foodEaten = value;
		}
	}

	public int fuelSpent
	{
		get { return _fuelSpent; }
		set
		{
			if (value - _fuelSpent > 0)
				fuelSpent_total += value - _fuelSpent;
			_fuelSpent = value;
		}
	}

	public int materialsSpent
	{
		get { return _materialsSpent; }
		set
		{
			if (value - _materialsSpent > 0)
				materialsSpent_total += value - _materialsSpent;
			_materialsSpent = value;
		}
	}

	public int oxygenBreathed
	{
		get { return _oxygenBreathed; }
		set
		{
			if (value - _oxygenBreathed > 0)
				oxygenBreathed_total += value - _oxygenBreathed;
			_oxygenBreathed = value;
		}
	}

	public int partsUsed
	{
		get { return _partsUsed; }
		set
		{
			if (value - _partsUsed > 0)
				partsUsed_total += value - _partsUsed;
			_partsUsed = value;
		}
	}

	public int totalAirPressureChange
	{
		get { return _totalAirPressureChange; }
		set
		{
			totalAirPressureChange_total += value - _totalAirPressureChange;
			_totalAirPressureChange = value;
		}
	}

	public int wasteCreated
	{
		get { return _wasteCreated; }
		set
		{
			if (value - _wasteCreated > 0)
				wasteCreated_total += value - _wasteCreated;
			_wasteCreated = value;
		}
	}

	public int maxSpeed
	{
		get { return _maxSpeed; }
		set
		{
			_maxSpeed = value;
			if (maxSpeed_total < value)
				maxSpeed_total = value;
		}
	}

	public int maxEffectiveSpeed
	{
		get { return _maxEffectiveSpeed; }
		set
		{
			_maxEffectiveSpeed = value;
			if (maxEffectiveSpeed_total < value)
				maxEffectiveSpeed_total = value;
		}
	}

	public int maxProgress
	{
		get { return _maxProgress; }
		set
		{
			_maxProgress = value;
		}
	}

	public int strongResults
	{
		get { return _strongResults; }
		set
		{
			if (value - _strongResults > 0)
				strongResults_total += value - _strongResults;

			_strongResults = value;
		}
	}

	public int strongHardResults
	{
		get { return _strongHardResults; }
		set
		{
			if (value - _strongHardResults > 0)
				strongHardResults_total += value - _strongHardResults;

			_strongHardResults = value;
		}
	}

	public int fairResults
	{
		get { return _fairResults; }
		set
		{
			if (value - _fairResults > 0)
				fairResults_total += value - _fairResults;
			_fairResults = value;
		}
	}

	public int weakResults
	{
		get { return _weakResults; }
		set
		{
			if (value - _weakResults > 0)
				weakResults_total += value - _weakResults;
			_weakResults = value;
		}
	}

	public int eventsFailed
	{
		get { return _failResults; }
		set
		{
			if (value - _failResults > 0)
				eventsFailed_total += value - _failResults;

			_failResults = value;
		}
	}

	public List<Crew> lostCrew = new List<Crew>();


	/*
	 * TOTAL STATS
	 */

	public int highScore { get; set; }

	public int clicks_total { get; set; }

	public int alertsUsed_total { get; set; }

	public int crewStressedOut_total  { get; set; }

	public int crewInjured_total { get; set; }

	public int crewKnockedUnconscious_total { get; set; }

	public int crewGoneInsane_total { get; set; }

	public int crewRestrained_total { get; set; }

	public int crewDied_total{ get; set; }

	public int crewGivenFunerals_total{ get; set; }

	public int systemsBroken_total { get; set; }

	public int systemsDestroyed_total{ get; set; }

	public int energyConsumed_total { get; set; }

	public int energyProduced_total { get; set; }

	public int energyWasted_total { get; set; }

	public int eventsSurvived_total { get; set; }

	public int foodGrown_total{ get; set; }

	public int foodEaten_total{ get; set; }

	public int fuelSpent_total{ get; set; }

	public int materialsSpent_total { get; set; }

	public int oxygenBreathed_total { get; set; }

	public int partsUsed_total{ get; set; }

	public int totalAirPressureChange_total{ get; set; }

	public int wasteCreated_total{ get; set; }

	public int maxSpeed_total { get; set; }

	public int maxEffectiveSpeed_total{ get; set; }

	public int strongResults_total { get; set; }

	public int strongHardResults_total { get; set; }

	public int fairResults_total{ get; set; }

	public int weakResults_total { get; set; }

	public int eventsFailed_total { get; set; }

	internal int daysInSpace_total { get; set; }

	internal int shortestJourney = 10000;

	internal int longestJourney { get; set; }


	/*
	 * METHODS
	 */

	/// <summary>
	/// Resets the run stats. Allows for new, clean run.
	/// </summary>
	public void ResetRunStats()
	{
		clicks = 0;
		foreach (var t in endGameStats)
		{
			if (t.Name != "score")
				t.SetValue(stats, 0, null);
		}

		//Other stuff that should be reset but isn't in endGameStats
		lostCrew.Clear();
	}

	/// <summary>
	/// Adds the current vessel to memorial.
	/// </summary>
	/// <param name="survived">If set to <c>true</c> survived.</param>
	/// <param name="result">Result of the journey for the vessel.</param>
	/// <param name="useCurrentData">Use SaveLoad.s.currentData rather than GameReference for vessel and crew info.</param> 
	public void AddCurrentVesselToMemorial(bool survived, string time, string result, bool useCurrentData = false)
	{
		//Create and populate the vessel object
		Vessel vessel;
		if (!useCurrentData && GameReference.r != null)
			vessel.vesselName = GameReference.r.shipName;
		else
			vessel.vesselName = SaveLoad.s.currentData ["shipName"];
			
		vessel.survived = survived;
		vessel.time = time;
		vessel.result = result;

		//Crew
		vessel.crew = new List<Crew>();

		//Go through the crew still on ship. Go GameRef version first
		if (!useCurrentData && GameReference.r != null)
		{
			foreach (var t in GameReference.r.allCharacters)
			{
				vessel.crew.Add(CreateCrewMemorialFromCharacter(t, survived));
			}
		}
		//CurrentData version of this
		else
		{
			for (int i = 0; i < SaveLoad.s.currentData ["characters_count"]; i++)
			{
				//Get the BSONObject
				Kernys.Bson.BSONObject dat = SaveLoad.s.currentData ["characters"] [i.ToString()] as Kernys.Bson.BSONObject;
				Crew crew;
				crew.crewName = dat ["lastName"] + ", " + dat ["firstName"];
				//Crew 'survives' with the ship and if they aren't dead
				crew.survived = survived && (Character.CharStatus)(int)dat ["status"] != Character.CharStatus.Dead;

				if (crew.survived)
					crew.result = "Survived";
				else if ((Character.CharStatus)(int)dat ["status"] != Character.CharStatus.Dead)
					crew.result = "Left for Dead";
				else if (dat ["result"] != null && dat ["result"] != "")
				{
					crew.result = dat ["result"];
				}
				else
					crew.result = "Died";
				vessel.crew.Add(crew);
			}
		}

		//Characters no longer on ship
		foreach (var t in lostCrew)
		{
			vessel.crew.Add(t);
		}

		//Add it
		memorial.Add(vessel);
	}

	/**Creates a StatTrack.Crew struct memorial-object from a Character object */
	public static Crew CreateCrewMemorialFromCharacter(Character character, bool shipSurvived)
	{
		Crew crew;
		crew.crewName = character.lastName + ", " + character.firstName;

		//Crew 'survives' with the ship and if they aren't dead
		crew.survived = shipSurvived && character.status != Character.CharStatus.Dead;

		//Result
		if (crew.survived)
			crew.result = "Survived";
		else if (character.status != Character.CharStatus.Dead)
			crew.result = "Left for Dead";
		else if (character.result != "")
		{
			crew.result = character.result;
		}
		else
			crew.result = "Died";

		//Return it
		return crew;
	}

	/// <summary>
	/// Determines whether the given vessel name is in the memorial already.
	/// </summary>
	/// <returns><c>true</c> if the name is in the memorial; otherwise, <c>false</c>.</returns>
	/// <param name="name">Name to check.</param>
	public bool IsVesselNameInMemorial(string name)
	{
		if (memorial.Exists(obj => obj.vesselName == name))
			return true;
		else
			return false;
	}
}
