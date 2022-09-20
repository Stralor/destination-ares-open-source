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
			"energyConsumed",
			"energyProduced",
			"energyWasted",
			"eventsSurvived",
			"foodGrown",
			"fuelSpent",
			"materialsSpent",
			"oxygenBreathed",
			"partsUsed",
			"totalAirPressureChange",
			"wasteCreated",
			"maxSpeed",
			"maxProgress",
			"score"
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

	private int _clicks, _clicks_total, _alertsUsed, _alertsUsed_total, _crewStressedOut, _crewStressOut_total, _crewInjured, _crewInjured_total,
		_crewKnockedUnconscious, _crewKnockedUnconscious_total, _crewGoneInsane, _crewGoneInsane_total, _crewRestrained, _crewRestrained_total,
		_crewDied, _crewDied_total, _energyConsumed, _energyConsumed_total, _energyProduced, _energyProduced_total, _energyWasted, _energyWasted_total,
		_eventsSurvived, _eventsSurvived_total, _foodGrown, _foodGrown_total, _fuelSpent, _fuelSpent_total, _materialsSpent, _materialsSpent_total,
		_oxygenBreathed, _oxygenBreathed_total, _partsUsed, _partsUsed_total, _totalAirPressureChange, _totalAirPressureChange_total,
		_wasteCreated, _wasteCreated_total, _maxSpeed, _maxSpeed_total, _maxProgress;



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
				int s = ((((ShipResources.res.startingDistance - ShipResources.res.distance) / 10) * (1 + GameReference.r.allCharacters.FindAll(t => t.status != Character.CharStatus.Dead).Count)
				        / (2 ^ GameReference.r.allCharacters.FindAll(t => t.status == Character.CharStatus.Dead).Count))
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

	public int clicks
	{
		get { return _clicks; }
		set
		{
			_clicks_total += value - wasteCreated;
			_clicks = value;
		}
	}

	public int alertsUsed
	{
		get { return _alertsUsed; }
		set
		{
			if (value - _alertsUsed > 0)
				_alertsUsed_total += value - _alertsUsed;
			_alertsUsed = value;
		}
	}

	public int crewStressedOut
	{
		get { return _crewStressedOut; }
		set
		{
			if (value - _crewStressedOut > 0)
				_crewStressOut_total += value - _crewStressedOut;
			_crewStressedOut = value;
		}
	}

	public int crewInjured
	{
		get { return _crewInjured; }
		set
		{
			if (value - _crewInjured > 0)
				_crewInjured_total += value - _crewInjured;
			_crewInjured = value;
		}
	}

	public int crewKnockedUnconscious
	{
		get { return _crewKnockedUnconscious; }
		set
		{
			if (value - _crewKnockedUnconscious > 0)
				_crewKnockedUnconscious_total += value - _crewKnockedUnconscious;
			_crewKnockedUnconscious = value;
		}
	}

	public int crewGoneInsane
	{
		get { return _crewGoneInsane; }
		set
		{
			if (value - _crewGoneInsane > 0)
				_crewGoneInsane_total += value - _crewGoneInsane;
			_crewGoneInsane = value;
		}
	}

	public int crewRestrained
	{
		get { return _crewRestrained; }
		set
		{
			if (value - _crewRestrained > 0)
				_crewRestrained_total += value - _crewRestrained;
			_crewRestrained = value;
		}
	}

	public int crewDied
	{
		get { return _crewDied; }
		set
		{
			if (value - _crewDied > 0)
				_crewDied_total += value - _crewDied;
			_crewDied = value;
		}
	}

	public int energyConsumed
	{
		get { return _energyConsumed; }
		set
		{
			if (value - _energyConsumed > 0)
				_energyConsumed_total += value - _energyConsumed;
			_energyConsumed = value;
		}
	}

	public int energyProduced
	{
		get { return _energyProduced; }
		set
		{
			if (value - _energyProduced > 0)
				_energyProduced_total += value - _energyProduced;
			_energyProduced = value;
		}
	}

	public int energyWasted
	{
		get { return _energyWasted; }
		set
		{
			if (value - _energyWasted > 0)
				_energyWasted_total += value - _energyWasted;
			_energyWasted = value;
		}
	}

	public int eventsSurvived
	{
		get { return _eventsSurvived; }
		set
		{
			if (value - _eventsSurvived > 0)
				_eventsSurvived_total += value - _eventsSurvived;
			_eventsSurvived = value;
		}
	}

	public int foodGrown
	{
		get { return _foodGrown; }
		set
		{
			if (value - _foodGrown > 0)
				_foodGrown_total += value - _foodGrown;
			_foodGrown = value;
		}
	}

	public int fuelSpent
	{
		get { return _fuelSpent; }
		set
		{
			if (value - _fuelSpent > 0)
				_fuelSpent_total += value - _fuelSpent;
			_fuelSpent = value;
		}
	}

	public int materialsSpent
	{
		get { return _materialsSpent; }
		set
		{
			if (value - _materialsSpent > 0)
				_materialsSpent_total += value - _materialsSpent;
			_materialsSpent = value;
		}
	}

	public int oxygenBreathed
	{
		get { return _oxygenBreathed; }
		set
		{
			if (value - _oxygenBreathed > 0)
				_oxygenBreathed_total += value - _oxygenBreathed;
			_oxygenBreathed = value;
		}
	}

	public int partsUsed
	{
		get { return _partsUsed; }
		set
		{
			if (value - _partsUsed > 0)
				_partsUsed_total += value - _partsUsed;
			_partsUsed = value;
		}
	}

	public int totalAirPressureChange
	{
		get { return _totalAirPressureChange; }
		set
		{
			_totalAirPressureChange_total += value - _totalAirPressureChange;
			_totalAirPressureChange = value;
		}
	}

	public int wasteCreated
	{
		get { return _wasteCreated; }
		set
		{
			if (value - _wasteCreated > 0)
				_wasteCreated_total += value - _wasteCreated;
			_wasteCreated = value;
		}
	}

	public int maxSpeed
	{
		get { return _maxSpeed; }
		set
		{
			_maxSpeed = value;
			if (_maxSpeed_total < value)
				_maxSpeed_total = value;
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


	/*
	 * TOTAL STATS
	 */

	public int highScore { get; set; }

	public int clicks_total { get { return _clicks_total; } set { _clicks_total = value; } }

	public int alertsUsed_total { get { return _alertsUsed_total; } set { _alertsUsed_total = value; } }

	public int crewStressedOut_total { get { return _crewStressOut_total; } set { _crewStressOut_total = value; } }

	public int crewInjured_total { get { return _crewInjured_total; } set { _crewInjured_total = value; } }

	public int crewKnockedUnconscious_total { get { return _crewKnockedUnconscious_total; } set { _crewKnockedUnconscious_total = value; } }

	public int crewGoneInsane_total { get { return _crewGoneInsane_total; } set { _crewGoneInsane_total = value; } }

	public int crewRestrained_total { get { return _crewRestrained_total; } set { _crewRestrained_total = value; } }

	public int crewDied_total { get { return _crewDied_total; } set { _crewDied_total = value; } }

	public int energyConsumed_total { get { return _energyConsumed_total; } set { _energyConsumed_total = value; } }

	public int energyProduced_total { get { return _energyProduced_total; } set { _energyProduced_total = value; } }

	public int energyWasted_total { get { return _energyWasted_total; } set { _energyWasted_total = value; } }

	public int eventsSurvived_total { get { return _eventsSurvived_total; } set { _eventsSurvived_total = value; } }

	public int foodGrown_total { get { return _foodGrown_total; } set { _foodGrown_total = value; } }

	public int fuelSpent_total { get { return _fuelSpent_total; } set { _fuelSpent_total = value; } }

	public int materialsSpent_total { get { return _materialsSpent_total; } set { _materialsSpent_total = value; } }

	public int oxygenBreathed_total { get { return _oxygenBreathed_total; } set { _oxygenBreathed_total = value; } }

	public int partsUsed_total { get { return _partsUsed_total; } set { _partsUsed_total = value; } }

	public int totalAirPressureChange_total { get { return _totalAirPressureChange_total; } set { _totalAirPressureChange_total = value; } }

	public int wasteCreated_total { get { return _wasteCreated_total; } set { _wasteCreated_total = value; } }

	public int maxSpeed_total { get { return _maxSpeed_total; } set { _maxSpeed_total = value; } }

	public int longestJourney { get; set; }
	//TODO Interface int with clock-managed dates



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

		//Go through the crew. Go GameRef version first
		if (!useCurrentData && GameReference.r != null)
		{
			foreach (var t in GameReference.r.allCharacters)
			{
				//Crew starts alive
				UpdateCrewInMemorial(t, vessel, survived, "Alive");
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

				crew.result = CalculateCrewResult(survived, ((Character.CharStatus)(int)dat ["status"]) == Character.CharStatus.Dead, dat ["result"].stringValue);
				
				vessel.crew.Add(crew);
			}
		}

		//Add it if it's unique (don't make excess copies!)
		if (!memorial.Exists(obj => obj.vesselName == vessel.vesselName))
			memorial.Add(vessel);
	}

	/// <summary>
	/// Updates the current vessel in memorial.
	/// </summary>
	/// <param name="survived">If set to <c>true</c>, ship survived (or is surviving).</param>
	/// <param name="time">Time so far.</param>
	/// <param name="result">Result of journey (so far, if current).</param>
	public void UpdateCurrentVesselInMemorial(bool survived, string time, string result, bool useCurrentData = false)
	{
		Vessel vessel = GetCurrentVesselFromMemorial();

		vessel.survived = survived;
		vessel.time = time;
		vessel.result = result;

		if (!useCurrentData && GameReference.r != null)
		{
			for (int i = 0; i < vessel.crew.Count; i++)
			{
				var ch = GameReference.r.allCharacters.Find(obj => (obj.lastName + ", " + obj.firstName) == vessel.crew [i].crewName);
				if (ch != null)
				{
					Crew crew = new Crew();

					crew.crewName = vessel.crew [i].crewName;
					crew.survived = survived && ch.status != Character.CharStatus.Dead;

					if (vessel.crew [i].survived != crew.survived)
						crew.result = CalculateCrewResult(survived, ch.status == Character.CharStatus.Dead, ch.result);

					vessel.crew [i] = crew;
				}
			}
		}
		else
		{
			for (int i = 0; i < vessel.crew.Count; i++)
			{
				for (int j = 0; j < SaveLoad.s.currentData ["characters_count"]; j++)
				{
					//Get the BSONObject
					Kernys.Bson.BSONObject dat = SaveLoad.s.currentData ["characters"] [j.ToString()] as Kernys.Bson.BSONObject;

					//Find the specific crew we want
					if ((dat ["lastName"] + ", " + dat ["firstName"]) == vessel.crew [i].crewName)
					{
						Crew crew = new Crew();

						crew.crewName = vessel.crew [i].crewName;
						crew.survived = survived && (Character.CharStatus)(int)dat ["status"] != Character.CharStatus.Dead;

						if (vessel.crew [i].survived != crew.survived)
							crew.result = CalculateCrewResult(survived, ((Character.CharStatus)(int)dat ["status"]) == Character.CharStatus.Dead, dat ["result"].stringValue);

						vessel.crew [i] = crew;

						//This was the one we wanted
						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets the current vessel from memorial.
	/// </summary>
	/// <returns>The current vessel from memorial.</returns>
	public Vessel GetCurrentVesselFromMemorial()
	{
		return memorial [memorial.Count - 1];
	}

	/// <summary>
	/// Adds the crew to memorial if not present. Otherwise updates the character data.
	/// </summary>
	/// <param name="character">Character to add.</param>
	/// <param name="memorialVessel">Memorial vessel to add to.</param>
	/// <param name="survived">If set to <c>true</c>, ship survived.</param>
	/// <param name="specificResult">Specific result for vessel.</param>
	public void UpdateCrewInMemorial(Character character, Vessel memorialVessel, bool survived, string specificResult = null)
	{
		Crew crew = new Crew();
		crew.crewName = character.lastName + ", " + character.firstName;
		//Crew 'survives' with the ship and if they aren't dead
		crew.survived = survived && character.status != Character.CharStatus.Dead;

		crew.result = CalculateCrewResult(survived, character.status == Character.CharStatus.Dead, character.result, specificResult);

		//Clear old one
		if (memorialVessel.crew.Exists(obj => obj.crewName == crew.crewName))
			memorialVessel.crew.Remove(memorialVessel.crew.Find(obj => obj.crewName == crew.crewName));

		//Add new one
		memorialVessel.crew.Add(crew);
	}

	/// <summary>
	/// Calculates the crew result.
	/// </summary>
	/// <returns>The crew result.</returns>
	/// <param name="shipSurvived">If set to <c>true</c> ship survived.</param>
	/// <param name="charIsDead">If set to <c>true</c> char is dead.</param>
	/// <param name="charDeathResultText">Char death result text.</param>
	/// <param name="specificResult">Specific result take priority.</param>
	string CalculateCrewResult(bool shipSurvived, bool charIsDead, string charDeathResultText, string specificResult = null)
	{
		var survived = shipSurvived && !charIsDead;

		if (specificResult != null)
			return specificResult;
		else if (survived)
			return "Survived";
		else if (charIsDead)
			return "Left for Dead";
		else if (charDeathResultText != null && charDeathResultText != "")
			return charDeathResultText;
		else
			return "Died";
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
