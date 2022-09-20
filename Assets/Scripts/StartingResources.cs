using UnityEngine;
using System.Collections;

#pragma warning disable

public class StartingResources : MonoBehaviour
{
	private static StartingResources _sRes;
	public static StartingResources sRes;

	//Toggle this when you're ready for StartingResources to do it's thing, like after you set the values (it will still wait on ShipRes and GameRef if necessary)
	public bool isReady;

	public bool	maxStartingEnergy = true;
	public bool	setAir = true;
	public bool giveSystemKeywords = false;

	public int distance, energy, air, food, fuel, materials, parts, waste, shipSpeed = 0;
	public string shipName, shipLoadFile;

	void Start()
	{
		StartCoroutine(SetResources());
	}

	IEnumerator SetResources()
	{
		//Wait until the scene is set to go
		yield return new WaitUntil(() => ShipResources.res != null && GameReference.r.isReady && sRes.isReady);

		//Begin setting
		GameReference.r.shipName = shipName;

		//System keywords
		if (giveSystemKeywords)
			foreach (var t in GameReference.r.allSystems)
				t.SetKeywords(true);

		//Distance
		ShipResources.res.SetStartingDistance(distance);

		//Energy
		if (maxStartingEnergy)
			ShipResources.res.SetEnergy(ShipResources.res.capacityTotal, updateStatTrack: false);
		else
			ShipResources.res.SetEnergy(energy, updateStatTrack: false);

		//Air
		if (setAir)
		{
			ShipResources.res.SetTotalAir(air, updateStatTrack: false);
			ShipResources.res.SetUsableAir(air, updateStatTrack: false);
		}

		//All other resources
		ShipResources.res.SetFood(food, updateStatTrack: false);
		ShipResources.res.SetFuel(fuel, updateStatTrack: false);
		ShipResources.res.SetParts(parts, updateStatTrack: false);
		ShipResources.res.SetMaterials(materials, updateStatTrack: false);
		ShipResources.res.SetWaste(waste, updateStatTrack: false);
		ShipResources.res.speed = shipSpeed;

		//It's a new scene, so make sure StatTrack is loaded
		SaveData.UnloadStatTrack(SaveLoad.s.Peek());
		//This run's stats should be cleared!
		StatTrack.stats.ResetRunStats();
		EventSpecialConditions.c.ResetFields();

		//As should the HelperAI tips!
		HelperAI.ResetUsedTips();

		//Start new vessel/ memorial entry
		//StatTrack.stats.AddCurrentVesselToMemorial(true, "0", "En Route");

		//Starting event(s) after StatTrack is reset
		var ges = FindObjectOfType<GameEventScheduler>();
		//Guaranteed Standard
		ges.CreateEventByTime(0, 0, 1, EventCondition.Standard, false);

		//Add music
		FindObjectOfType<InputHandler>().SetGameProgressSongs(true);

		//Set initial blackouts
		FindObjectOfType<BlackoutController>().SetAllBlackouts(true);

		//Done!
		Destroy(gameObject);
	}

	void Awake()
	{
		if (sRes == null)
		{
			sRes = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (sRes != this)
			Destroy(this);
	}
}