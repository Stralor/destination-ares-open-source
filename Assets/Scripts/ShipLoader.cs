using UnityEngine;
using System.Collections;

public class ShipLoader : MonoBehaviour
{

	void Start()
	{
		if (StartingResources.sRes != null && StartingResources.sRes.shipLoadFile != null && StartingResources.sRes.shipLoadFile != "")
		{
			//Default ship load
			SaveLoad.s.LoadShip(StartingResources.sRes.shipLoadFile);

			//Premades need some randomness in their lives. Also, they define shipType in StatTrack
			if (StartingResources.sRes.shipLoadFile.Contains("(default)") || StartingResources.sRes.shipLoadFile.Contains("(advanced)") || StartingResources.sRes.shipLoadFile.Contains("(beginner)"))
			{
				StartCoroutine(RandomizeAppearance());

				if (StartingResources.sRes.shipLoadFile.Contains("(default)"))
					StatTrack.stats.shipType = 0;
			}
			//Custom Ship
			else
				StatTrack.stats.shipType = 1;

			//Set camera bounds
			CameraBoundsSetter.cbs.SetCameraBounds();

			//Some systems start offline
			foreach (var t in GameReference.r.allSystems)
			{
				//Specifically, all large systems or powered systems		OLD: and any systems that aren't inert, manual, the comms, or a fuel cell
				if (t.isLarge || t.usesEnergy)//|| !(t.isPassive || t.isManualProduction || t.function == ShipSystem.SysFunction.Communications || t.function == ShipSystem.SysFunction.FuelCell))
				{
					t.status = ShipSystem.SysStatus.Disabled;
				}
			}

			//Loaded. Better rescan next frame.
			StartCoroutine(Rescan());
		}
	}

	IEnumerator Rescan()
	{
		yield return null;

		AstarPath.active.Scan();
	}

	IEnumerator RandomizeAppearance()
	{
		yield return null;

		yield return new WaitUntil(() => GameReference.r != null);

		//Module art
		foreach (var t in GameReference.r.allModules)
		{
			foreach (var u in t.GetComponentsInChildren<ModuleArt>())
			{
				u.ClearArt();
				u.ChooseArt();
			}
		}

		//Character randomness
		foreach (var t in GameReference.r.allCharacters)
		{
			t.GetComponent<CharacterColors>().Randomize(true);
			CharacterNames.AssignRandomName(out t.firstName, out t.lastName);
			t.Rename();
		}
	}
}
