using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipResources : MonoBehaviour
{

	/**Singleton reference for this class (and thus all the ship's stored resources!)
	 */
	public static ShipResources res;

	[Tooltip("Display-only. Changing this doesn't adjust game value.")]
	public int speedDisplay, storTotalDisplay, storRemainDisplay;

	public static int partsVolume = 2;
	//How large are parts in the hold?

	//Declare the private storage fields!
	#pragma warning disable 0649
	public int startingDistance = 1;
	[SerializeField] private int dis, eCap;
	#pragma warning restore 0649



	void Update()
	{
		//Clean out excess resources
		if (!IsInvoking("TrimRes"))
			InvokeRepeating("TrimRes", 0, 5);

		//Show displays in inspector
		#if UNITY_EDITOR
		speedDisplay = speed;
		storTotalDisplay = storageTotal;
		storRemainDisplay = storageRemaining;
		#endif
	}

	/*
	 * PUBLIC RESOURCE PROPERTIES
	 */

	public int distance
	{
		get
		{
			return dis;
//			if (startingDistance > 0)
//				return dis;
//			else
//				return 1;
		}
		set
		{
			dis = value >= 0 ? value : 0;
		}
	}

	/**Actual ship speed.
	 */
	public int speed{ get; set; }

	/**Returns the percentage of the trip completed. Can be negative, as it's based off of the starting distance.
	 */
	public int progress
	{
		get
		{
			var prog = (int)(100 - ((float)distance / (float)startingDistance * 100));

			if (prog > StatTrack.stats.maxProgress)
				StatTrack.stats.maxProgress = prog;

			return prog;
		}
	}

	/**How much physical storage space is aboard the ship.
	 */
	public int storageTotal
	{ 
		get
		{
			//Get ready
			int value = 0;

			//Check storage
			foreach (var t in GameReference.r.allSystems)
			{
				if (t.function == ShipSystem.SysFunction.Storage)
					value += (int)(t.Use() * 16);
			}

			//Check modules
			foreach (var t in GameReference.r.allModules)
			{
				value += Module.ModuleStorageDictionary [t.size];
			}

			//Account for openings
			value -= 4 * GameReference.r.allOpenings.Count;

			//Return
			return value > 0 ? value : 0;
		}
	}

	/**How much physical storage space is unused.
	 */
	public int storageRemaining
	{
		get
		{
			//Evaluate
			int value = storageTotal - (totalAir + food + fuel + materials + waste + (parts * partsVolume));

			//Return!
			return value;
		}
	}

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
				//Batteries, when online
				if (sys.function == ShipSystem.SysFunction.Battery)
				{
					//Get output, scale capacity
					systemCapacity += (int)(10 * sys.Use());
				}
				//Fuel cells
				else if (sys.function == ShipSystem.SysFunction.FuelCell && !sys.isBroken)
				{
					//Latent capacity
					systemCapacity++;

					//More capacity scales on output
					systemCapacity += (int)(9 * sys.Use());
				}
				//Reactors
				else if (sys.function == ShipSystem.SysFunction.Reactor && !sys.isBroken)
				{
					systemCapacity += 8;
				}
				//Solar Panels, when online
				else if (sys.function == ShipSystem.SysFunction.Solar && sys.status != ShipSystem.SysStatus.Disabled)
				{
					systemCapacity += 2;
				}
			}

			//Trim excess energy, if necessary
			if (energy > eCap + systemCapacity)
				energy = eCap + systemCapacity;

			return eCap + systemCapacity;
		}
	}

	/**How much more energy the ship can potentially hold.
	 */
	public int capacityRemaining
	{
		get
		{
			return (capacityTotal - energy);
		}
	}

	public int usableAir { get; private set; }

	/**Note the name. This is usableAir + all the rest of the air (aka, the unusable shit).	 */
	public int totalAir{ get; private set; }

	public int energy{ get; private set; }

	public int food{ get; private set; }

	public int fuel{ get; private set; }

	public int materials{ get; private set; }

	public int parts{ get; private set; }

	public int waste{ get; private set; }


	/*
	 * DIRECT FUNCTIONS
	 */

	/**Set the starting distance! (Also resets distance to match)
	 */
	public void SetStartingDistance(int value)
	{
		if (value == 0)
		{
			Debug.Log("Zero is an invalid starting distance!");
			return;
		}

		startingDistance = value;
		distance = value;
	}

	/**Adjust how much usableAir there is. Doesn't affect totalAir, but is limited by it.
	 * Notably used by Scrubbers.
	 */
	public bool SetUsableAir(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;

		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -usableAir;
			usableAir = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("air", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
		//Not enough space to add? (Doesn't count for subtraction)
		else if (newValue > totalAir && usableAir < newValue)
		{
			amountChanged = totalAir - usableAir;
			usableAir += amountChanged;
		}
		//All good!
		else
		{
			amountChanged = newValue - usableAir;
			usableAir = newValue;
			fullSuccess = true;
		}

		if (amountChanged < 0 && updateStatTrack)
			StatTrack.stats.oxygenBreathed -= amountChanged;
		
		CallSpawnIcon("air", iconSpawnTarget, amountChanged);

		//Changing air!
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.airText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	/**Change the total amount of air on the ship. Cuts out totalAir first, but can remove usableAir if totalAir falls lower.
	 * Really just used by AddAir and RemoveAir.
	 */
	public bool SetTotalAir(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -totalAir;
			totalAir = 0;
			usableAir = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("air", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if (newValue - totalAir > storageRemaining && totalAir < newValue)
//		{
//			amountChanged = storageRemaining;
//			totalAir += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - totalAir;
			totalAir = newValue;
			//Rectify amount of usableAir if it's greater than totalAir
			if (usableAir > totalAir)
				SetUsableAir(totalAir);
			fullSuccess = true;
		}

		if (amountChanged != 0 && updateStatTrack)
			StatTrack.stats.totalAirPressureChange += amountChanged;
		
		//Icon spawn
		CallSpawnIcon("air", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.airText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	/**Set how much energy is on the ship.
	 */
	public bool SetEnergy(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged = newValue - energy;
		if (amountChanged < 0 && PlayerPrefs.GetInt("ResourceDrain") == 1 && Random.Range(0, 4) == 0)
		{
			var offset = amountChanged < -1 ? amountChanged / 2 : -1;
			newValue -= offset;
		}

		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -energy;
			energy = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("energy", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
		//Not enough space to add? (Doesn't count for subtraction)
		else if (newValue - energy > capacityRemaining && energy < newValue)
		{
			//Energy wasted
			if (updateStatTrack)
				StatTrack.stats.energyWasted += newValue - energy - capacityRemaining;
			
			amountChanged = capacityRemaining;
			energy += amountChanged;
		}
		//All good!
		else
		{
			amountChanged = newValue - energy;
			energy = newValue;
			fullSuccess = true;
		}

		if (amountChanged > 0 && updateStatTrack)
			StatTrack.stats.energyProduced += amountChanged;
		if (amountChanged < 0 && updateStatTrack)
			StatTrack.stats.energyConsumed -= amountChanged;

		//Icon spawn
		CallSpawnIcon("energy", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.energyText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	public bool SetFood(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -food;
			food = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("food", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if (newValue - food > storageRemaining && food < newValue)
//		{
//			amountChanged = storageRemaining;
//			food += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - food;
			food = newValue;
			fullSuccess = true;
		}

		if (amountChanged > 0 && updateStatTrack)
			StatTrack.stats.foodGrown += amountChanged;
		
		//Icon spawn
		CallSpawnIcon("food", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.foodText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	public bool SetFuel(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -fuel;
			fuel = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("fuel", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if (newValue - fuel > storageRemaining && fuel < newValue)
//		{
//			amountChanged = storageRemaining;
//			fuel += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - fuel;
			fuel = newValue;
			fullSuccess = true;
		}

		if (amountChanged < 0 && updateStatTrack)
			StatTrack.stats.fuelSpent -= amountChanged;
		
		//Icon spawn
		CallSpawnIcon("fuel", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.fuelText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	public bool SetMaterials(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -materials;
			materials = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("materials", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if (newValue - materials > storageRemaining && materials < newValue)
//		{
//			amountChanged = storageRemaining;
//			materials += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - materials;
			materials = newValue;
			fullSuccess = true;
		}

		if (amountChanged < 0 && updateStatTrack)
			StatTrack.stats.materialsSpent -= amountChanged;
		
		//Icon spawn
		CallSpawnIcon("materials", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.materialsText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	public bool SetParts(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -parts;
			parts = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("parts", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if ((newValue - parts) * partsVolume > storageRemaining && parts < newValue)
//		{
//			amountChanged = storageRemaining / partsVolume;
//			parts += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - parts;
			parts = newValue;
			fullSuccess = true;
		}

		if (amountChanged < 0 && updateStatTrack)
			StatTrack.stats.partsUsed -= amountChanged;
		
		//Icon spawn
		CallSpawnIcon("parts", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.partsText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	public bool SetWaste(int newValue, Transform iconSpawnTarget = null, bool updateStatTrack = true)
	{
		//How much the value changed, and whether positively or negatively
		int amountChanged;
		//Were we able to set the value without limitation?
		bool fullSuccess = false;
		
		//Not enough supplies?
		if (newValue < 0)
		{
			amountChanged = -waste;
			waste = 0;

			if (iconSpawnTarget != null)
				IconSpawner.ico.SpawnResourceIcon("waste", iconSpawnTarget, IconSpawner.Direction.NegativeDecrement);
		}
//		//Not enough space to add? (Doesn't count for subtraction)
//		else if (newValue - waste > storageRemaining && waste < newValue)
//		{
//			amountChanged = storageRemaining;
//			waste += amountChanged;
//		}
		//All good!
		else
		{
			amountChanged = newValue - waste;
			waste = newValue;
			fullSuccess = true;
		}

		if (amountChanged > 0 && updateStatTrack)
			StatTrack.stats.wasteCreated += amountChanged;

		//Icon spawn
		CallSpawnIcon("waste", iconSpawnTarget, amountChanged);

		//Anim
		if (HUDUpdater.hudu != null)
			HUDUpdater.hudu.AnimateResourceElements(HUDUpdater.hudu.wasteText.transform.parent.gameObject, amountChanged);

		//Return whether we were able to implement the full value
		return fullSuccess;
	}

	/**Add one new usableAir. Overload method.
	 * Expands totalAir to match.
	 * Do not use for removing air!
	 */
	public void AddAir(Transform iconSpawnTarget = null)
	{
		AddAir(1, iconSpawnTarget);
	}

	/**Add x new usableAir. Expands totalAir to match.
	 * Do not use for removing air!
	 */
	public void AddAir(int x, Transform iconSpawnTarget = null)
	{
		SetTotalAir(totalAir + x);
		SetUsableAir(usableAir + x, iconSpawnTarget);
	}

	/**Remove one usableAir (and therefore also totalAir). Overload method.
	 * See main version for other options.
	 */
	public int RemoveAir(Transform iconSpawnTarget = null)
	{
		return RemoveAir(1, true, iconSpawnTarget);
	}

	/**Remove x Air. Prioritizes usableAir. Returns how much Usable Air was removed.
	 * Furthermore, will ONLY work on Usable Air if usableOnly is true.
	 */
	public int RemoveAir(int x, bool usableOnly, Transform iconSpawnTarget = null)
	{
		int uAirRemoved;

		//Do we have enough usableAir to not worry about limiting our air removal? Or do we not care?
		if (x <= usableAir || !usableOnly)
		{
			//Pull as much of each as possible (up to x)
			uAirRemoved = x;
		}
		//If not, we need to remove just the right amount of total air.
		else
		{
			//Remove only as much air as there is usable
			uAirRemoved = usableAir;
		}

		//Remove the air
		SetUsableAir(usableAir - uAirRemoved);
		SetTotalAir(totalAir - uAirRemoved, iconSpawnTarget);

		return uAirRemoved;
	}

	/**Trim excess resources to fill storageRemaining.
	 */
	public void TrimRes()
	{
		if (storageRemaining < 0)
			print("Adjusting total res to match available storage. " + -storageRemaining + " resource units to trim.");

		//Trim excess TODO In-game Leakage HUD FX, maybe create other problems
		if (storageRemaining < 0 && GameReference.r != null && GameReference.r.isReady)
		{
			//Order of operations: least to most directly important for crew survival
			if (!SetWaste(waste - 1))
			if (!SetMaterials(materials - 1))
			if (!SetParts(parts - 1))
			if (!SetFuel(fuel - 1))
			if (!SetFood(food - 1))
				SetTotalAir(totalAir - 1);
		}
	}

	/*
	 * UTILITY AND MISCELLANEOUS METHODS
	 */

	/**Is this a consumable resource? In other words, is it part of the resource flow, and not a limiter resource or some other property?
	 */
	public bool IsConsumableResource(string name)
	{
		switch (name.ToLower())
		{
		case "usableair":
			return true;
		case "totalair":
			return true;
		case "energy":
			return true;
		case "food":
			return true;
		case "fuel":
			return true;
		case "materials":
			return true;
		case "parts":
			return true;
		case "waste":
			return true;
		default:
			return false;
		}
	}

	/**Decides if a call to IconSpawner is needed, and then handles it.
	 */
	private void CallSpawnIcon(string resource, Transform iconSpawnTarget, int changeInValue)
	{
		//Which direction are we gonna spawn?
		IconSpawner.Direction direction = IconSpawner.Direction.None;

		if (changeInValue < 0)
			direction = IconSpawner.Direction.Decrement;
		if (changeInValue > 0)
			direction = IconSpawner.Direction.Increment;

		//If we have a direction and target, Spawn!
		if (iconSpawnTarget != null && changeInValue != 0)
			IconSpawner.ico.SpawnResourceIcon(resource, iconSpawnTarget, direction);
		//TODO Default spawn location for nulls. HUD? Mouse?

	}

	void Awake()
	{
		if (res == null)
		{
			res = this;
		}
		else if (res != this)
		{
			Destroy(this);
		}
	}
}
