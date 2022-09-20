using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameReference : MonoBehaviour
{

	/**Singleton ref */
	public static GameReference r;

	public string shipName { get; set; }

	public bool isReady { get; private set; }

	public List<ShipSystem> allSystems = new List<ShipSystem>();
	public List<Character> allCharacters = new List<Character>();
	public List<Module> allModules = new List<Module>();
	public List<Opening> allOpenings = new List<Opening>();
	public List<HullExtension> allHullExtensions = new List<HullExtension>();

	public bool overlayActive;


	public int commandValue
	{
		get
		{
			int value = 0;

			foreach (var t in allCharacters)
			{
				if (t.isControllable)
					value += t.skills.FindAll(sk => sk == Character.CharSkill.Command).Count;
			}

			return value;
		}
	}

	public int moduleMass
	{
		get
		{
			int value = 0;

			foreach (var t in allModules)
			{
				value += Module.ModuleMassDictionary [t.size];
			}

			return value;
		}
	}

	public int resourceMass
	{
		get
		{
			int rawValue = 0;

			if (ShipResources.res != null)
				rawValue = (ShipResources.res.storageTotal - ShipResources.res.storageRemaining);
			else if (Environment_Customization.cust != null)
				rawValue = (Environment_Customization.cust.air + Environment_Customization.cust.food + Environment_Customization.cust.fuel
				+ Environment_Customization.cust.materials + (Environment_Customization.cust.parts * ShipResources.partsVolume) + Environment_Customization.cust.waste);

			return rawValue / 4;
		}
	}

	public int systemMass
	{
		get
		{
			int value = 0;

			foreach (var t in allSystems)
			{
				value += t.mass;
			}

			return value;
		}
	}

	public int totalShipMass
	{
		get
		{
			return moduleMass + allCharacters.Count + systemMass + resourceMass;
		}
	}

	/**Get the allSystems list, with any marked "ignored" left out. */
	public List<ShipSystem> allSystemsLessIgnored
	{
		get
		{
			List<ShipSystem> temp = new List<ShipSystem>();
			foreach (var t in allSystems)
			{
				if (!t.GetComponentInChildren<Alert>().GetActivatedAlerts().Contains(AlertType.Ignore))
					temp.Add(t);
			}
			return temp;
		} 
	}

	/**Get the allCharacters list, with any marked "ignored" left out. */
	public List<Character> allCharactersLessIgnored
	{
		get
		{
			List<Character> temp = new List<Character>();
			foreach (var t in allCharacters)
			{
				if (!t.GetComponentInChildren<Alert>().GetActivatedAlerts().Contains(AlertType.Ignore))
					temp.Add(t);
			}
			return temp;
		} 
	}

	/**Used by ShipSystemAnim to determine lerp between status light colors*/
	public float systemColorFade { get; private set; }

	private bool fadingUp = false;
	//Determines direction of systemColorFade change


	public bool communicationsAvailable
	{
		get
		{
			return (GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && sys.status != ShipSystem.SysStatus.Disabled));
		}
	}




	/**Set overlayActive. For Inspector use.
	 */
	public void SetOverlayActive(bool value)
	{
		overlayActive = value;
	}

	void Update()
	{
		//Set systemColorFace
		if (fadingUp)
			systemColorFade += Time.deltaTime;
		else
			systemColorFade -= Time.deltaTime;
		//Set direction
		if (systemColorFade >= 1)
		{
			systemColorFade = 1;
			fadingUp = false;
		}
		else if (systemColorFade <= 0)
		{
			systemColorFade = 0;
			fadingUp = true;
		}
	}

	void Start()
	{
		//Double check that we've started the scene with all items accounted for

		//Systems
		foreach (ShipSystem sys in GameObject.FindObjectsOfType<ShipSystem>())
		{
			if (!allSystems.Contains(sys))
				allSystems.Add(sys);
		}

		//Characters
		foreach (Character ch in GameObject.FindObjectsOfType<Character>())
		{
			if (!allCharacters.Contains(ch))
				allCharacters.Add(ch);
		}

		//Modules
		foreach (var t in GameObject.FindObjectsOfType<Module>())
		{
			if (!allModules.Contains(t))
				allModules.Add(t);
		}

		//Openings
		foreach (var t in GameObject.FindObjectsOfType<Opening>())
		{
			if (!allOpenings.Contains(t))
				allOpenings.Add(t);
		}

		//Openings
		foreach (var t in GameObject.FindObjectsOfType<HullExtension>())
		{
			if (!allHullExtensions.Contains(t))
				allHullExtensions.Add(t);
		}

		isReady = true;
	}


	void Awake()
	{
		if (r == null)
		{
			r = this;
		}
		else if (r != this)
			Destroy(this);

		isReady = false;
	}
}

