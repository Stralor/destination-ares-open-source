using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDWarningManager : MonoBehaviour
{
	Dictionary<string, string> warningTypes = new Dictionary<string, string>()
	{
		{ "Low Air", "Low oxygen" },
		{ "No Air", "Out of oxygen" },
		{ "Low Energy", "Low energy" },
		{ "No Energy", "Out of energy" },
		{ "Off Course", "Losing progress" },
		{ "Speed", "Not gaining progress" },
		{ "Parts", "Out of parts" },
		{ "Comms", "Communications offline" },
		{ "Food", "Crew unable to eat" },
		{ "Toilet", "Crew unable to defecate" },
		{ "Bed", "Crew unable to sleep" },
		{ "Crew", "No available crew" },
		{ "Heading", "Cannot correct heading" },
		{ "Storage", "Too little storage — leaking" },
	};

	public List<GameObject> activeWarnings = new List<GameObject>();
	List<GameObject> pool = new List<GameObject>();
	Image border;

	//Debug management
	bool warnedAboutType = false;

	//Check statuses
	void Update()
	{
		//Don't give these hints in hard mode!
		if (PlayerPrefs.GetInt("HardMode") == 1)
			return;

		//Clean the pool, you sexy shirtless hunk
		pool.RemoveAll(obj => obj == null);


		/*
		 * WARNING TIME
		 */

		if (!CheckWarning("No Air", ShipResources.res.usableAir < 1))
			CheckWarning("Low Air", ShipResources.res.usableAir < 6);
		else
			RemoveWarning("Low Air");

		if (!CheckWarning("No Energy", ShipResources.res.energy < 1))
			CheckWarning("Low Energy", ShipResources.res.energy < 6);
		else
			RemoveWarning("Low Energy");

		if (!CheckWarning("Off Course", ShipMovement.sm.CalculateEffectiveSpeed(ShipResources.res.speed) < -10))
			CheckWarning("Speed", ShipMovement.sm.CalculateEffectiveSpeed(ShipResources.res.speed) < 5);
		else
			RemoveWarning("Speed");

		CheckWarning("Parts", ShipResources.res.parts < 1);

		CheckWarning("Comms", !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled));

		CheckWarning("Food", GameReference.r.allCharacters.Exists(obj => obj.hunger > obj.hungerResilience && obj.GetCurrentThought() != Character.Thought.Eating)
		&& (ShipResources.res.food < 1 || !GameReference.r.allSystemsLessIgnored.Exists(sys => sys.function == ShipSystem.SysFunction.Kitchen && sys.status != ShipSystem.SysStatus.Disabled && !sys.inUse)));

		CheckWarning("Toilet", GameReference.r.allCharacters.Exists(obj => obj.waste > obj.wasteResilience)
		&& !GameReference.r.allSystemsLessIgnored.Exists(sys => sys.function == ShipSystem.SysFunction.Toilet && sys.status != ShipSystem.SysStatus.Disabled));

		CheckWarning("Bed", GameReference.r.allCharacters.Exists(obj => obj.sleepiness > obj.sleepinessResilience && obj.GetCurrentThought() != Character.Thought.Sleeping)
		&& !GameReference.r.allSystemsLessIgnored.Exists(sys => sys.function == ShipSystem.SysFunction.Bed && sys.status != ShipSystem.SysStatus.Disabled && !sys.inUse));

		CheckWarning("Crew", !GameReference.r.allCharacters.Exists(obj => obj.isControllable && obj.canAct));

		CheckWarning("Heading", ShipMovement.sm.GetOffCourse() > 0.3f && !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Helm && sys.status != ShipSystem.SysStatus.Disabled));

		CheckWarning("Storage", ShipResources.res.storageRemaining < 0);


		//Have any (non-suppressed) warnings? Show the border!
		border.enabled = activeWarnings.Exists(obj => obj.activeSelf);
	}

	/**Returns the condition bool value, so you can do substates
	 */
	bool CheckWarning(string warningTypesKey, bool condition)
	{
		//Keycheck
		string warningText;
		if (!warningTypes.TryGetValue(warningTypesKey, out warningText))
		{
			if (!warnedAboutType)
			{
				Debug.LogWarning("No warning of that type in Dictionary: " + warningTypesKey);
				warnedAboutType = true;
			}
			return false;
		}

		bool warningIsActive = activeWarnings.Exists(obj => obj.name == warningText);

		//Add it when necessary
		if (!warningIsActive && condition)
			AddWarning(warningTypesKey);
		
		//Remove it when necessary
		else if (warningIsActive && !condition)
			RemoveWarning(warningTypesKey);

		//Let's us know what state we're in
		return condition;
	}

	void AddWarning(string warningTypesKey)
	{
		GameObject warning;

		//Get a warning object from the pool
		if (pool.Exists(obj => obj.activeSelf == false && !activeWarnings.Contains(obj)))
		{
			warning = pool.Find(obj => obj.activeSelf == false);
		}
		//Or create it
		else
		{
			warning = Instantiate(Resources.Load("HUD Warning")) as GameObject;
			warning.transform.SetParent(transform);
			warning.transform.localScale = Vector3.one;
		}

		//Set position
		warning.transform.SetAsLastSibling();
		warning.transform.localEulerAngles = Vector3.zero;

		//Set text
		warning.GetComponentInChildren<Text>().text = warning.gameObject.name = warningTypes [warningTypesKey];

		//Track it
		activeWarnings.Add(warning);

		//Leggo!
		warning.SetActive(true);

		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Beep", warning.transform);
	}

	void RemoveWarning(string warningTypesKey)
	{
		//Is this warning active?
		var warning = activeWarnings.Find(obj => obj.name == warningTypes [warningTypesKey]);

		//Safety / early exit
		if (warning == null)
			return;

		//No more tracking
		activeWarnings.Remove(warning);

		//Shut it off
		warning.SetActive(false);
	}

	void OnEnable()
	{
		border = GetComponent<Image>();
	}
}
