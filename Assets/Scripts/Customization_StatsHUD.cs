using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Customization_StatsHUD : MonoBehaviour
{

	public static Customization_StatsHUD s;

	public Text mass, storage, capacity, thrust, heading, mech, science, command, energyProd, matProd, oxyProd, fuelProd, foodProd, wasteProd, wasteCycle;

	public Toggle randomKeyword;


	/**How much energy the ship can store.
	 */
	public int capacityTotal
	{
		get
		{
			//Check system capacity
			int systemCapacity = 0;
			//Search
			foreach (ShipSystem sys in GameReference.r.allSystems)
			{
				//Batteries
				if (sys.function == ShipSystem.SysFunction.Battery)
				{
					//Get output, scale storage
					systemCapacity += (int)(10 * sys.Use());
				}
				//Fuel cells
				else if (sys.function == ShipSystem.SysFunction.FuelCell)
				{
					//Storage scales on output
					systemCapacity += (int)(10 * sys.Use());
				}
				//Reactors, always
				else if (sys.function == ShipSystem.SysFunction.Reactor)
				{
					systemCapacity += 8;
				}
				//Solar Panels, when online
				else if (sys.function == ShipSystem.SysFunction.Solar && sys.status != ShipSystem.SysStatus.Disabled)
				{
					systemCapacity += 2;
				}
			}

			return systemCapacity;
		}
	}

	public int thrustScore
	{
		get
		{
			float thrust = 0;

			foreach (var sys in GameReference.r.allSystems)
			{
				if (sys.thrusts)
				{
					if (sys.function == ShipSystem.SysFunction.Engine)
					{
						thrust += 25 * sys.Use();
					}
					else if (sys.function == ShipSystem.SysFunction.Sail)
					{
						thrust += 5 * sys.Use();
					}
					else if (sys.function == ShipSystem.SysFunction.WasteCannon)
					{
						thrust += 50 * sys.Use();
					}
				}
			}

			return (int)thrust;
		}
	}

	public int navigationScore
	{
		get
		{
			float navi = 0;
			float multiplier = 1;

			//Raw stats
			foreach (var sys in GameReference.r.allSystems)
			{
				if (sys.function == ShipSystem.SysFunction.Helm)
				{
					navi += sys.Use() * 10;
				}
			}

			//Multipliers
			foreach (var sys in GameReference.r.allSystems)
			{
				if (sys.function == ShipSystem.SysFunction.Radar)
				{
					//Diminishing returns
					multiplier += sys.Use() / multiplier;
				}
			}

			return (int)(navi * multiplier);
		}
	}

	public int mechScore
	{
		get
		{
			int score = 0;

			foreach (var t in GameReference.r.allCharacters)
			{
				score += t.skills.FindAll(obj => obj == Character.CharSkill.Mechanical).Count * 10;

				if (t.roles.Contains(Character.CharRoles.Electrician))
					score += 10;
				if (t.roles.Contains(Character.CharRoles.Pilot))
					score += 5;
			}

			return score;
		}
	}

	public int sciScore
	{
		get
		{
			int score = 0;

			foreach (var t in GameReference.r.allCharacters)
			{
				score += t.skills.FindAll(obj => obj == Character.CharSkill.Science).Count * 10;

				if (t.roles.Contains(Character.CharRoles.Doctor))
					score += 10;
				if (t.roles.Contains(Character.CharRoles.Pilot))
					score += 5;
			}

			return score;
		}
	}

	public int commandScore
	{
		get
		{
			int score = 0;

			foreach (var t in GameReference.r.allCharacters)
			{
				score += t.skills.FindAll(obj => obj == Character.CharSkill.Command).Count * 10;
			}

			return score;
		}
	}

	public int energyScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.resourcesCreated.Contains("energy"))
				{
					switch (t.function)
					{
					case ShipSystem.SysFunction.Solar:
						score += t.Use();
						break;
					case ShipSystem.SysFunction.Engine:
						score += t.Use() * 5;
						break;
					case ShipSystem.SysFunction.FuelCell:
						score += 5 / t.Use();
						break;
					case ShipSystem.SysFunction.Generator:
						score += t.Use() * 20;
						break;
					case ShipSystem.SysFunction.Reactor:
						score += t.Use() * 30;
						break;
					}
				}
			}

			return (int)score;
		}
	}

	public int matScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.resourcesCreated.Contains("materials"))
				{
					switch (t.function)
					{
					case ShipSystem.SysFunction.Processor:
						score += t.Use() * 15;
						break;
					case ShipSystem.SysFunction.FuelCell:
						score += 5 / t.Use();
						break;
					}
				}
			}

			return (int)score;
		}
	}

	public string oxyString
	{
		get
		{
			float cycle = 0, prod = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				switch (t.function)
				{
				case ShipSystem.SysFunction.Scrubber:
					cycle += t.Use() * 30;
					break;
				case ShipSystem.SysFunction.Electrolyser:
					prod += t.Use();
					break;
				case ShipSystem.SysFunction.Injector:
					prod -= t.Use();
					break;
				}
			}

			return cycle.ToString() + " (" + prod.ToString() + ")";
		}
	}

	public int fuelScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.resourcesCreated.Contains("fuel"))
				{
					switch (t.function)
					{
					case ShipSystem.SysFunction.Electrolyser:
						score += t.Use() * 10;
						break;
					case ShipSystem.SysFunction.Still:
						score += t.Use() * 5;
						break;
					}
				}
			}

			return (int)score;
		}
	}

	public int foodScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.function == ShipSystem.SysFunction.Hydroponics)
				{
					score += t.Use() * 10;
				}
			}

			return (int)score;
		}
	}

	public int wasteScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.function == ShipSystem.SysFunction.Reactor)
				{
					score += t.Use() * 5;
				}
			}

			//Give shitting points if there's a toilet
			if (GameReference.r.allSystems.Exists(obj => obj.function == ShipSystem.SysFunction.Toilet))
			{
				score += GameReference.r.allCharacters.Count;
			}

			return (int)score;
		}
	}

	public int cycleScore
	{
		get
		{
			float score = 0;

			foreach (var t in GameReference.r.allSystems)
			{
				if (t.resourcesConsumed.Contains("waste"))
				{
					switch (t.function)
					{
					case ShipSystem.SysFunction.Hydroponics:
						score += t.Use() * 5;
						break;
					case ShipSystem.SysFunction.Processor:
						score += t.Use() * 15;
						break;
					case ShipSystem.SysFunction.WasteCannon:
						score += t.Use() * 10;
						break;
					}
				}
			}

			return (int)score;
		}
	}

	void Update()
	{
		//Let's set all those texts with some arbitrary scores!

		mass.text = GameReference.r.totalShipMass.ToString();

		storage.text = Environment_Customization.cust.storageTotal.ToString();

		capacity.text = capacityTotal.ToString();

		thrust.text = thrustScore.ToString();

		heading.text = navigationScore.ToString();

		mech.text = mechScore.ToString();

		science.text = sciScore.ToString();

		command.text = commandScore.ToString();

		energyProd.text = energyScore.ToString();

		matProd.text = matScore.ToString();

		oxyProd.text = oxyString;

		fuelProd.text = fuelScore.ToString();

		foodProd.text = foodScore.ToString();

		wasteProd.text = wasteScore.ToString();

		wasteCycle.text = cycleScore.ToString();
	}



	void Start()
	{
		//Set initial Random Keyword toggle state
		randomKeyword.isOn = PlayerPrefs.GetInt("RandomKeywords") == 1;
	}

	void Awake()
	{
		if (s == null)
			s = this;
		else if (s != this)
			Destroy(s);
	}
}
