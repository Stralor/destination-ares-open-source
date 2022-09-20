using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipSystem : MonoBehaviour
{

	//Define enums
	//Indicates how/if a system is currently working
	public enum SysStatus
	{
		Active,
		Inactive,
		Intermittent,
		Disabled
	}
	//Indicates what a system does
	public enum SysFunction
	{
		Engine,
		Scrubber,
		Radar,
		Bed,
		Gym,
		Kitchen,
		Helm,
		Toilet,
		Hydroponics,
		Reactor,
		Battery,
		Still,
		Processor,
		Electrolyser,
		Fabricator,
		Injector,
		Storage,
		Communications,
		Generator,
		FuelCell,
		Solar,
		WasteCannon,
		Sail,
		GuideBot
	}
	//Indicates how well a system can work
	public enum SysQuality
	{
		Exceptional,
		Average,
		Shoddy,
		Makeshift,
		UnderConstruction
	}
	//Indicates if the system is working optimally
	public enum SysCondition
	{
		Functional,
		Strained,
		Broken,
		Destroyed
	}
	//Indicates special feature(s) of the system
	public enum SysKeyword
	{
		NOTDEFINED,
		Random,
		//+1 repair skill
		Simple,
		//+1 output
		Efficient,
		//+1 dura
		Durable,
		//+2 output, -1 dura, -1 repair skill
		Prototype,
		//Repair cost is 1 material, -1 repair skill
		Nonstandard,
		//Special Engine type, no fuel TODO More Ion systems?
		//Ion,
		//+2 dura when strained
		Resilient,
		//Full output when strained
		Reliable,
		//+2 repair skill, -1 output
		Basic,
		//+2 dura in overdrive
		Hardened,
		//+2 output in overdrive
		Performant
	}

	public static List<SysKeyword> unlockedKeywords = new List<SysKeyword>()
	{
		SysKeyword.Simple,	
	};

	//Declare variables
	/**Used for in-game system display names. Seeds from function, keywords, and condition.*/
	public string sysName = "SYSTEM";
	public SysStatus status;
	//Changes based on context. Shorthand indicator for possible problems.
	public SysFunction function;
	//What kind of system is this?
	public SysQuality quality;
	//How good is this system?
	public SysCondition condition;
	//Is this system in good shape?
	private SysCondition lastCondition;
	//The last condition the system was in
	public List<SysKeyword> keywords = new List<SysKeyword>();
	//What is special about this system? keyword[0] should be type, if applicable
	private float outputModifier = 0.3f;
	//The fraction by which output is modified up or down (multiplicatively)

	private bool ticking = false;
	//Is this system currently doing its automated process?
	[SerializeField] private float tickTime = 10;
	//Base time between automated system activations. Often affect by output.
	[HideInInspector] public float timeLeftInTick = 0;
	//How long is left in this tick cycle
	private int scrubberCycles = 10;
	//How often scrubbers activate

	//Callbacks
	public System.Action onDamage;

	//Properties
	public bool toggleAudio { get; set; }

	//Only drop condition when it's been hit more than once
	public int conditionHit { get; set; }

	//Is this system currently overdriven?
	public bool overdriven { get; private set; }

	/**Can this system afford at least some repair costs? Takes into effect resource levels and resources needed for special cases.
	 * Also returns true when there's a condition hit, but no full repairs possible, since conditionHit is free to repair.
	 */
	public bool canAffordRepair
	{
		get
		{
			//False if ShipRes is null
			if (ShipResources.res == null)
				return false;

			//Nonstandards require materials
			if (keyCheck(SysKeyword.Nonstandard) && ShipResources.res.materials > 0)
				return true;

			//All others require parts
			if (ShipResources.res.parts > 0)
				return true;

			//We can at least get rid of conditionHit, if present
			return conditionHit > 0;
		}
	}

	public bool canAffordConstruction
	{
		get
		{
			//False if ShipRes is null
			if (ShipResources.res == null)
				return false;

			var reqMaterials = Customization_CurrencyController.GetMaterialsCost(this);
			var enoughMaterials = ShipResources.res.materials >= reqMaterials;

			return enoughMaterials;
		}
	}

	/**Value that represent the strength of the system against damage.
	 */
	public int durability
	{
		get
		{
			int dura;
			var assistBonus = PlayerPrefs.GetInt("ResourceDrain");
			
			//Base durability values
			switch (function)
			{
			case SysFunction.Battery:
			case SysFunction.FuelCell:
			case SysFunction.Reactor:
			case SysFunction.Storage:
				dura = 9 + (assistBonus * 3);
				break;
			case SysFunction.Communications:
			case SysFunction.Generator:
			case SysFunction.Hydroponics:
			case SysFunction.Injector:
			case SysFunction.Sail:
				dura = 3 + (assistBonus * 1);
				break;
			default:
				dura = 6 + (assistBonus * 2);
				break;
			}

			//Overdrive costs extra durability (beyond just Strained!)
			if (overdriven)
				dura--;

			//The condition affects durability
			if (condition == SysCondition.Strained)
				dura--;

			//Adjust dura by quality
			if (quality != SysQuality.Average && quality != SysQuality.Exceptional)
				dura--;
			
			//Keyword Bonuses and Penalties
			if (keyCheck(SysKeyword.Durable))
				dura++;
			if (keyCheck(SysKeyword.Prototype))
				dura--;
			if (keyCheck(SysKeyword.Resilient) && condition == SysCondition.Strained)
				dura += 2;
			if (keyCheck(SysKeyword.Hardened) && overdriven)
				dura += 2;
			
			//Return dura. Minimum usable value.
			return dura > 2 ? dura : 2;
		}
	}

	/**If true, greater output means faster (shorter) ticking and processing times.
	 * Often the only way systems increase effectiveness.
	 * Defaults to true.
	 */
	public bool outputIncreasesSpeed
	{
		get
		{
			switch (function)
			{
			case SysFunction.FuelCell:
				return false;
			default:
				return true;
			}
		}
	}

	/**Is the system currently in use?
	 */
	public bool inUse
	{	
		get
		{
			if (status == SysStatus.Active || status == SysStatus.Intermittent)
				return true;
			else
				return false;
		}
	}

	/**Does this system act automatically, if enabled?
	 */
	public bool isAutomated
	{
		get
		{
			switch (function)
			{
			case SysFunction.Electrolyser:
			case SysFunction.Engine:
			case SysFunction.FuelCell:
			case SysFunction.Generator:
			case SysFunction.Helm:
			case SysFunction.Hydroponics:
			case SysFunction.Radar:
			case SysFunction.Reactor:
			case SysFunction.Sail:
			case SysFunction.Scrubber:
			case SysFunction.Solar:
			case SysFunction.WasteCannon:
				return true;
			default :
				return false;
			}
		}
	}

	/**Broken or Destroyed (Or Under Construction)?
	 */
	public bool isBroken
	{
		get
		{
			return condition == SysCondition.Broken || condition == SysCondition.Destroyed || quality == SysQuality.UnderConstruction;
		}
	}

	/**Is this system a flight component (as used by pilots)?
	 */
	public bool isFlightComponent
	{
		get
		{
			switch (function)
			{
			case SysFunction.Communications:
			case SysFunction.Helm:
			case SysFunction.Radar:
				return true;
			default:
				return false;
			}
		}
	}

	public bool isLarge
	{
		get
		{
			switch (function)
			{
			case SysFunction.Electrolyser:
			case SysFunction.Engine:
			case SysFunction.Processor:
			case SysFunction.Reactor:
			case SysFunction.WasteCannon:
				return true;
			default:
				return false;
			}
		}
	}

	/**Systems that require crew interaction to function, and aren't a part of needs behavior.
	 */
	public bool isManualProduction
	{
		get
		{
			switch (function)
			{
			case SysFunction.Fabricator:
			case SysFunction.Helm:
			case SysFunction.Injector:
			case SysFunction.Processor:
			case SysFunction.Still:
				return true;
			default :
				return false;
			}
		}
	}


	/**Does this system just sit there, only providing passive abilities by existing (Not Use-able, Cannot be disabled)?
	 */
	public bool isPassive
	{
		get
		{
			switch (function)
			{
			case SysFunction.Battery:
			case SysFunction.Bed:
			case SysFunction.Gym:
			case SysFunction.Sail:
			case SysFunction.Solar:
			case SysFunction.Storage:
			case SysFunction.Toilet:
				return true;
			default :
				return false;
			}
		}
	}


	/**Does this system have damage that can be repaired?
	 * Just conditionHit doesn't count.
	 */
	public bool isRepairable
	{
		get
		{
			return (condition != ShipSystem.SysCondition.Destroyed && condition != ShipSystem.SysCondition.Functional && quality != SysQuality.UnderConstruction);
		}
	}

	public int mass
	{
		get
		{
			int value = 2;

			if (isLarge)
				value *= 8;

			return value;
		}
	}

	/**Does this system produce thrust for the ship?
	 */
	public bool thrusts
	{
		get
		{
			return resourcesCreated.Contains("thrust");
		}
	}

	/**Does this system produce energy for the ship?
	 */
	public bool createsEnergy => resourcesCreated.Contains("energy");

	/**Does this system require energy for use?
	 */
	public bool usesEnergy
	{
		get
		{
			return resourcesConsumed.Contains("energy");
		}
	}

	/**What resources are consumed by this system? */
	public List<string> resourcesConsumed
	{
		get
		{
			var list = new List<string>();

			switch (function)
			{
			case SysFunction.Battery:
				break;
			case SysFunction.Bed:
				break;
			case SysFunction.Communications:
				break;
			case SysFunction.Electrolyser:
				list.Add("energy");
				list.Add("materials");
				break;
			case SysFunction.Engine:
				list.Add("fuel");
				break;
			case SysFunction.Fabricator:
				list.Add("energy");
				list.Add("materials");
				break;
			case SysFunction.FuelCell:
				list.Add("fuel");
				break;
			case SysFunction.Generator:
				list.Add("fuel");
				list.Add("air");
				break;
			case SysFunction.Gym:
				break;
			case SysFunction.Helm:
				break;
			case SysFunction.GuideBot:
				break;
			case SysFunction.Hydroponics:
				list.Add("energy");
				list.Add("waste");
				break;
			case SysFunction.Injector:
				list.Add("air");
				break;
			case SysFunction.Kitchen:
				list.Add("energy");
				list.Add("food");
				break;
			case SysFunction.Processor:
				list.Add("energy");
				list.Add("waste");
				break;
			case SysFunction.Radar:
				list.Add("energy");
				break;
			case SysFunction.Reactor:
				list.Add("materials");
				break;
			case SysFunction.Sail:
				break;
			case SysFunction.Scrubber:
				list.Add("energy");
				break;
			case SysFunction.Solar:
				break;
			case SysFunction.Still:
				list.Add("materials");
				break;
			case SysFunction.Storage:
				break;
			case SysFunction.Toilet:
				break;
			case SysFunction.WasteCannon:
				list.Add("energy");
				list.Add("waste");
				break;
			default:
				print(function + " not listed in resource consumption, even as a blank.");
				break;
			}

			return list;
		}
	}

	/**What resources are created by this system? */
	public List<string> resourcesCreated
	{
		get
		{
			var list = new List<string>();

			switch (function)
			{
			case SysFunction.Battery:
				break;
			case SysFunction.Bed:
				break;
			case SysFunction.Communications:
				break;
			case SysFunction.Electrolyser:
				list.Add("air");
				list.Add("fuel");
				break;
			case SysFunction.Engine:
				list.Add("energy");
				list.Add("thrust");
				break;
			case SysFunction.Fabricator:
				list.Add("parts");
				break;
			case SysFunction.FuelCell:
				list.Add("energy");
				list.Add("materials");
				break;
			case SysFunction.Generator:
				list.Add("energy");
				break;
			case SysFunction.Gym:
				break;
			case SysFunction.Helm:
				list.Add("heading");
				break;
			case SysFunction.GuideBot:
				break;
			case SysFunction.Hydroponics:
				list.Add("food");
				break;
			case SysFunction.Injector:
				break;
			case SysFunction.Kitchen:
				break;
			case SysFunction.Processor:
				list.Add("materials");
				break;
			case SysFunction.Radar:
				list.Add("heading");
				break;
			case SysFunction.Reactor:
				list.Add("energy");
				list.Add("waste");
				break;
			case SysFunction.Sail:
				list.Add("thrust");
				break;
			case SysFunction.Scrubber:
				list.Add("air");
				break;
			case SysFunction.Solar:
				list.Add("energy");
				break;
			case SysFunction.Still:
				list.Add("fuel");
				break;
			case SysFunction.Storage:
				break;
			case SysFunction.Toilet:
				list.Add("waste");
				break;
			case SysFunction.WasteCannon:
				list.Add("thrust");
				break;
			default:
				print(function + " not listed in resource production, even as a blank.");
				break;
			}

			return list;
		}
	}

	public bool storesEnergy
	{
		get
		{
			switch (function)
			{
			case SysFunction.Battery:
			case SysFunction.FuelCell:
			case SysFunction.Reactor:
			case SysFunction.Solar:
				return true;
			default:
				return false;
			}
		}
	}


	/**Did this system get used since the last dura check? aka: Will there be another dura check soon?
	 */
	public bool wasUsed { get; private set; }



	
	
	/*
	 * AFFECT THE SYSTEM
	 */

	/**Tries to degrade system by running a check against durability
	 * Force = true to force a dura check, even if not used
	 */
	public void DurabilityCheck(bool force)
	{
		//Don't do anything if it wasn't used since the last dura check
		if (wasUsed || force)
		{
			//Debug.Log("DuraChecking " + name);
			//Math out the chances of shit going poorly
			int triggerOnZero = Random.Range(0, durability);
			//Let's do any damage
			if (triggerOnZero == 0)
			{
				Damage();
			}
			//Reset wasUsed, if we weren't forced to check
			if (!force)
			{
				wasUsed = false;
			}
		}
	}

	/**Break the system. Crit = true if it also needs to degrade quality. */
	public void Break(bool crit)
	{
		if (condition == SysCondition.Destroyed)
			return;
		
		StatTrack.stats.systemsBroken++;

		//Break it
		condition = SysCondition.Broken;
		EndUse();
		//Remove any hits
		conditionHit = 0;

		//Big pings
		PingPool.PingHere(transform, seconds: 2, growthRate: 0.02f, delay: 0.25f);
		PingPool.PingHere(transform, seconds: 1, growthRate: 0.03f, delay: 0.5f);

		//If it was a crit fail, also hurt the quality by a step
		if (crit)
		{
			switch (quality)
			{
			case SysQuality.Exceptional:
				quality = SysQuality.Average;
				break;
			case SysQuality.Average:
				quality = SysQuality.Shoddy;
				break;
			case SysQuality.Shoddy:
				quality = SysQuality.Makeshift;
				break;
			//If it's already at minimum, WRECK IT
			case SysQuality.Makeshift:
			case SysQuality.UnderConstruction:
				DestroySystem();
				break;
			}

			//Super ping
			PingPool.PingHere(transform, seconds: 2, growthRate: 0.03f);

			//If we broke, do some epic audio
			AudioClipOrganizer.aco.PlayAudioClip("QualityDown", transform);
		}
		else
		{
			AudioClipOrganizer.aco.PlayAudioClip("SystemBreak", transform);
		}
	}

	/**Damage the system. 
	 * Will guarantee damage if ignoreConditionHit is true.
	 */
	public void Damage(bool ignoreConditionHit = false)
	{
		if (onDamage != null)
			onDamage.Invoke();

		//Don't do damage if it hasn't been hit enough
		if (!ignoreConditionHit && Random.Range(1, 4) > conditionHit)	//was 2, 5
		{
			conditionHit += 1;
			return;
		}

		//Remove the hits now that it's being damaged
		conditionHit = 0;

		//Initial ping
		PingPool.PingHere(transform);

		//Hurt condition
		switch (condition)
		{
		case SysCondition.Functional:
			condition = SysCondition.Strained;
			//Secondary ping
			PingPool.PingHere(transform, seconds: 2, growthRate: 0.02f);
			break;
		case SysCondition.Strained:
			Break(false);
			break;
		default :
			break;
		}
	}

	/**Repair the system. Improve = true if it also needs to improve quality. */
	public void Repair(bool improve, bool evenIfDestroyed = false)
	{
		//Remove any hit against the system
		conditionHit = 0;
		//Repair condition
		switch (condition)
		{
		case SysCondition.Broken:
			condition = SysCondition.Strained;
			break;
		case SysCondition.Strained:
			condition = SysCondition.Functional;
			break;
		case SysCondition.Destroyed:
			if (evenIfDestroyed)
				condition = SysCondition.Broken;
			//Don't continue onto improve if we did this. This counts as the improve
			return;
		default :
			break;
		}
		//If improving, also up quality by a step
		if (improve)
			Improve(true);
	}

	/**Remove any hit to condition!
	 */
	public void ClearHits()
	{
		conditionHit = 0;
	}

	/**System is outright gone. Leave some materials? */
	public void DestroySystem()
	{
		StatTrack.stats.systemsDestroyed++;

		AchievementTracker.UnlockAchievement("1_DESTROY");
		if (StatTrack.stats.systemsDestroyed_total > 50)
			AchievementTracker.UnlockAchievement("50_DESTROYS");

		//No hits, no auto maintenance!
		ClearHits();
		condition = SysCondition.Destroyed;
	}

	public void Improve(bool audio)
	{
		switch (quality)
		{
			case SysQuality.Makeshift:
				quality = SysQuality.Shoddy;
				break;
			case SysQuality.Shoddy:
				quality = SysQuality.Average;
				break;
			case SysQuality.Average:
				quality = SysQuality.Exceptional;
				break;
			default :
				return;
		}

		//If we improved, do some epic audio
		if (audio)
			AudioClipOrganizer.aco.PlayAudioClip("QualityUp", transform);
	}

	public void Construct()
	{
		quality = SysQuality.Makeshift;
		condition = SysCondition.Functional;
		status = SysStatus.Inactive;
		ClearHits();
		SetKeywords(true);

		AudioClipOrganizer.aco.PlayAudioClip("QualityUp", transform);
	}

	public void Salvage()
	{
		Destroy(gameObject);

		AudioClipOrganizer.aco.PlayAudioClip("succeed", transform.position);
	}
	
	
	/*
	 * USE THE SYSTEM
	 */

	/**System is being used.
	 * Calculate and return a float that represents the system's effectiveness (output).
	 */
	public float Use()
	{
		//Don't do it if it's disabled
		if (status == SysStatus.Disabled)
		{
			return 0;
		}

		//Stop any EndUse invokes
		if (IsInvoking("EndUse"))
			CancelInvoke("EndUse");

		//Used. Mark it.
		wasUsed = true;
		//Audio for non-automated
		if (!isAutomated)
			toggleAudio = true;

		//Let's calculate our return value.
		float output;

		//Change the current status and assign a basic return value:
		if (condition == SysCondition.Strained && !keyCheck(SysKeyword.Reliable))
		{
			//This system is Intermittent!
			status = SysStatus.Intermittent;
			output = 0.5f;
		}
		else
		{
			status = SysStatus.Active;
			output = 1;
		}

		//Let's modify our value. Modifications with same bonus separated because more than one may be applicable.
		float modifier = 0;
		
		//Overdriven
		if (overdriven)
			modifier = AddModifier(modifier, outputModifier);

		//System Quality
		if (quality == SysQuality.Exceptional)
			modifier = AddModifier(modifier, outputModifier);

		if (quality == SysQuality.Makeshift)
			modifier = AddModifier(modifier, -outputModifier);	//NEGATIVE!

		//Keywords
		if (keyCheck(SysKeyword.Efficient))
			modifier = AddModifier(modifier, outputModifier);

		if (keyCheck(SysKeyword.Basic))
			modifier = AddModifier(modifier, -outputModifier);	//NEGATIVE

		if (keyCheck(SysKeyword.Prototype))
			modifier = AddModifier(AddModifier(modifier, outputModifier), outputModifier);	//Do it twice

		if (keyCheck(SysKeyword.Performant) && overdriven)
			modifier = AddModifier(AddModifier(modifier, outputModifier), outputModifier);	//Do it twice

		//Calculate final output. Output affects the significance of the modifier.
		float x = output + (output * modifier);
		//Minimum 0.
		output = x > 0 ? x : 0; 

		return output;
	}

	/**No longer being used.
	 */
	public void EndUse()
	{
		ticking = false;
//		if (IsInvoking("Tick"))
//			CancelInvoke("Tick");
		StopCoroutine("Tick");
		if (status != SysStatus.Disabled)
			status = SysStatus.Inactive;
	}

	/**Turn on this system's overdrive, if it can be.
	 */
	public void OverdriveOn()
	{
		if (condition == SysCondition.Functional || condition == SysCondition.Strained)
		{
			overdriven = true;
			//Turn on the system with it
			status = SysStatus.Inactive;
		}
	}

	/**Turn off this system's overdrive. Returns a system to Functional if bool 'damaged' is false.
	 * Use 'damaged' as true whenever the overdrive shuts off due to a status change.
	 */
	public void OverdriveOff()
	{
		overdriven = false;
	}

	/**Toggle the system's overdrive. Figures out whether to call OverdriveOn or OverdriveOff and does so.
	 * Used for input events. Use direct calls to OverdriveOff for shutdown events.
	 */
	public void OverdriveToggle()
	{
		if (overdriven)
			OverdriveOff();
		else
			OverdriveOn();
	}

	/**Turn the system on/ off. Changes status between Disabled and Inactive.
	 * 
	 */
	public void TogglePower()
	{
		//Safety
		if (GameReference.r == null)
			return;

		if (!isPassive)
		{
			//Disable it
			if (status != SysStatus.Disabled)
			{
				status = SysStatus.Disabled;
				EndUse();
			}
			
			//Enable it
			else if (!isBroken)
			{
				if (!usesEnergy || (ShipResources.res != null && ShipResources.res.energy > 0))
					status = SysStatus.Inactive;
				else
				{
					AudioClipOrganizer.aco.PlayAudioClip("Invalid", transform);

					IconSpawner.ico.SpawnResourceIcon("energy", transform, IconSpawner.Direction.NegativeDecrement);

					Debug.Log("No energy! " + name + " cannot turn on.");
				}
			}
			//Broken
			else
			{
				AudioClipOrganizer.aco.PlayAudioClip("Invalid", transform);
			
				IconSpawner.ico.SpawnResourceIcon("invalid", transform, IconSpawner.Direction.NegativeDecrement);
			}
		}
		//Passive
		else
		{
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", transform);

			IconSpawner.ico.SpawnResourceIcon("invalid", transform, IconSpawner.Direction.NegativeDecrement);
		}
	}

	/**Disable the system. Use this when resources are not sufficient.
	 */
	public void DisableFromLackOfResources()
	{
		if (Disable())
		{
			Debug.Log(name + " was forced to shut down from a lack of resources.");
		}
	}

	public bool Disable()
	{
		if (!isPassive)
		{
			status = SysStatus.Disabled;
			EndUse();

			return true;
		}

		return false;
	}

	/*
	 * UTILITY METHODS
	 */

	/**Timed, automated system behavior.
	 */
	IEnumerator Tick(bool free = false)
	{
		if (ShipResources.res == null)
		{
			Debug.LogWarning(name + " is trying to tick (do automated behavior) while ShipResources.res is null.");
			yield break;
		}

		//Wait function
		while (timeLeftInTick > 0)
		{
			//Cut time out of what's left based on output
			if (outputIncreasesSpeed)
			{
				timeLeftInTick -= Time.deltaTime * Use();
			}
			//Special Case of inverted effect (some systems may have detrimental effects when used)
			else
			{
				timeLeftInTick -= Time.deltaTime / Use();
			}

			//Clamp
			timeLeftInTick = timeLeftInTick < 0 ? 0 : timeLeftInTick;
			//Iterate
			yield return null;
		}

		//Why are we here if we aren't ticking?
		if (!ticking)
		{
			yield break;
		}

		if (status != SysStatus.Disabled)
		{
			//No longer ticking
			ticking = false;

			//Audio for automated
			toggleAudio = true;

			int chance = 0;	//For randoms
			bool activate = false; //Also for randoms

			switch (function)
			{

			//Electrolysers consume energy and materials to create new air and some fuel. Smart system.
			case SysFunction.Electrolyser:

				//Smart system. Requires materials
				if (!free && ShipResources.res.materials < 3)
				{
					IconSpawner.ico.SpawnResourceIcon("materials", transform, IconSpawner.Direction.NegativeDecrement);

					//May still shut down
					if (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform))
					{
						DisableFromLackOfResources();
					}
				}
				else if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) && ShipResources.res.SetMaterials(ShipResources.res.materials - 3, transform)))
				{
					ShipResources.res.SetFuel(ShipResources.res.fuel + 2, transform);
					ShipResources.res.AddAir(transform);
				}
				else
					DisableFromLackOfResources();
				break;



			//Engines spend fuel to create energy and produce thrust (aka, increase ship speed)
			case SysFunction.Engine:

					//First deal with special case: Ion Engines
//				if (keyCheck(SysKeyword.Ion))
//				{
//
//					//Try a couple ways to activate
//					chance = Random.Range(0, ionCycles);
//					if (chance > 0) //Free
//							activate = true;
//					else if (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform))
//						activate = true;
//
//					//Do it!
//					if (activate)
//					{
//						ShipMovement.sm.AddThrust(0.2f, transform);
//					}
//				}
					//Conventional Engines
				if (free || ShipResources.res.SetFuel(ShipResources.res.fuel - 1, transform))
				{
					ShipResources.res.SetEnergy(ShipResources.res.energy + 1, transform);
					ShipMovement.sm.AddThrust(transform);
				}
				else
					DisableFromLackOfResources();
				break;



			//Fuel cells revert fuel to materials, but get a little energy out of it
			case SysFunction.FuelCell:
				if (free || ShipResources.res.SetFuel(ShipResources.res.fuel - 1, transform))
				{
					ShipResources.res.SetMaterials(ShipResources.res.materials + 1, transform);
					ShipResources.res.SetEnergy(ShipResources.res.energy + 1, transform);
				}
				else
				{
					DisableFromLackOfResources();
				}
				break;



			//Generators burn fuel to create energy. As a side effect, they consume a usable air (not req., only if it's present)
			case SysFunction.Generator:
				if (free || ShipResources.res.SetFuel(ShipResources.res.fuel - 1, transform))
				{
					ShipResources.res.SetEnergy(ShipResources.res.energy + 4, transform);
					ShipResources.res.SetUsableAir(ShipResources.res.usableAir - 1, transform);
				}
				else
				{
					DisableFromLackOfResources();
				}
				break;



			//Helms do course corrections
			case SysFunction.Helm:
				ShipMovement.sm.ReduceOffCourse(iconSpawnTarget: transform);
				break;



			//Hydroponics make food out of waste, with some energy in the form of light
			case SysFunction.Hydroponics:

				//Smart system. Requires waste
				if (!free && ShipResources.res.waste < 1)
				{
					IconSpawner.ico.SpawnResourceIcon("waste", transform, IconSpawner.Direction.NegativeDecrement);

					//Still might shut off
					if (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform))
					{
						DisableFromLackOfResources();
					}
				}
				else if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) && ShipResources.res.SetWaste(ShipResources.res.waste - 1, transform)))
				{
					//First
					ShipResources.res.SetFood(ShipResources.res.food + 1, transform);

					//No more symbols
					if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1) && ShipResources.res.SetWaste(ShipResources.res.waste - 1)))
					{
						//Second
						ShipResources.res.SetFood(ShipResources.res.food + 1);

						//Third is energy freee
						if (free || ShipResources.res.SetWaste(ShipResources.res.waste - 1))
						{
							ShipResources.res.SetFood(ShipResources.res.food + 1);
						}
					}
				}
				else
					DisableFromLackOfResources();
				break;



			//Radars reduce course drift
			case SysFunction.Radar:

				chance = (int)Random.Range(0, 2);
				if (free || chance > 0)	//Free
						activate = true;
				else if (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform))
					activate = true;

				//Do it
				if (activate)
					ShipMovement.sm.AddToOffCourseDivisor(iconSpawnTarget: transform);
				else
					DisableFromLackOfResources();
				break;



			//Reactors create energy from materials. Waste is a byproduct.
			case SysFunction.Reactor:

				//Reactors only produce energy when necessary. They're smart like that.
				if (free || ShipResources.res.capacityRemaining >= 6 || (ShipResources.res.capacityTotal < 6 && ShipResources.res.capacityRemaining > 0))
				{
					if (free || ShipResources.res.SetMaterials(ShipResources.res.materials - 1))
					{
						//Initial energy gain
						ShipResources.res.SetEnergy(ShipResources.res.energy + 3, transform);

						if (free || ShipResources.res.SetMaterials(ShipResources.res.materials - 1, transform))
						{
							//Secondary energy gain
							ShipResources.res.SetEnergy(ShipResources.res.energy + 3);

							//Waste production
							ShipResources.res.SetWaste(ShipResources.res.waste + 1, transform);
						}
					}
					else
						DisableFromLackOfResources();
				}
				//Reduce (some) reactor damage when they didn't do their job
				else
				{
					wasUsed = false;

					//Smart, shorter tick
					StartTick(tickTime / 4);
				}
				break;



			//Solar sails slowly provide thrust
			case SysFunction.Sail:

				ShipMovement.sm.AddThrustTimesModifier(0.5f, transform);
				break;



			//Scrubbers clean the air that already exists. Periodically consumes energy.
			case SysFunction.Scrubber:

				chance = Random.Range(0, scrubberCycles);
				//Try to activate
				if (free || chance > 0)	//Free
					activate = true;
				else if (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform))
					activate = true;

				//Do it
				if (activate)
					ShipResources.res.SetUsableAir(ShipResources.res.usableAir + 1, transform);
				else
					DisableFromLackOfResources();
				break;



			//Solar panels slowly provide power, speed based on distance from Terra
			case SysFunction.Solar:

				ShipResources.res.SetEnergy(ShipResources.res.energy + 1, transform);
				break;



			//Waste Cannons shoot shit out the ship for propulsion
			case SysFunction.WasteCannon:

				//First shot costs energy
				if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) && ShipResources.res.SetWaste(ShipResources.res.waste - 1, transform)))
				{
					//Pow, thrust
					ShipMovement.sm.AddThrust(transform);

					//Do it again, no symbols. Waste cannons are beasts
					if (free || ShipResources.res.SetWaste(ShipResources.res.waste - 1))
						ShipMovement.sm.AddThrust();
				}
				else
				{
					DisableFromLackOfResources();
				}
				break;

			}
		}
		//Stop the system if it's disabled
		else
			EndUse();
	}

	/**Begin the countdown to Tick (invokes it).
	 * If provided custom time, will start a tick with that specific time
	 */
	public void StartTick(float customTime = -1)
	{
		//Double check safety
		if (status == SysStatus.Disabled || !isAutomated || ticking || ShipResources.res == null)
			return;

		//We're ticking!
		ticking = true;
		//Find out how long it will take to act
		//Default set
		if (customTime == -1)
		{
			timeLeftInTick = tickTime;

		
			//Scrubbers are faster
			if (function == SysFunction.Scrubber)
				timeLeftInTick /= scrubberCycles;
			//Electrolysers are slow
			else if (function == SysFunction.Electrolyser)
				timeLeftInTick *= 2;
			//Hydroponics are slower
			else if (function == SysFunction.Hydroponics)
				timeLeftInTick *= 3;
			//Solar is slow based on distance (err, progress)
			else if (function == SysFunction.Solar || function == SysFunction.Sail)
				timeLeftInTick *= 2 / (1f - (ShipResources.res.progress / 200f));
			//Ion Engines repeat rapidly, for smaller amounts and no energy production
//			else if (keyCheck(SysKeyword.Ion))
//				timeLeftInTick /= ionCycles;
		}
		else
			timeLeftInTick = customTime;

		//Everything else works at the standard rate. Invoke it all
		StartCoroutine(Tick());
	}

	/**Manual, character instantiated behavior.
	 */
	public void Process(bool free = false)
	{
		if (ShipResources.res == null)
		{
			Debug.LogError(name + " cannot take the process action since ShipResources.res is null.");
			return;
		}

		if (status != SysStatus.Disabled)
		{
			//In case audio is not already called (i.e., automated systems that can also be manually used)
			if (!toggleAudio)
				toggleAudio = true;

			switch (function)
			{

			//Fabricators create parts from materials, using energy
			case SysFunction.Fabricator:
				if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) && ShipResources.res.SetMaterials(ShipResources.res.materials - ShipResources.partsVolume, transform)))
					ShipResources.res.SetParts(ShipResources.res.parts + 1, transform);
				else
					DisableFromLackOfResources();
				break;

			//Helms do course corrections
			case SysFunction.Helm:
				//Manual adjustments: reduce by 0.3 or half, whichever is greater
				float oc = ShipMovement.sm.GetOffCourse();
				float adjustment = oc / 2 > 0.3f ? oc / 2 : 0.3f;

				//Do the correction
				ShipMovement.sm.ReduceOffCourse(adjustment, transform);
				break;

			//Injectors cosume air to boost the engine output
			case SysFunction.Injector:
				if (free || ShipResources.res.RemoveAir(transform) == 1)
				{
					ShipMovement.sm.Inject(transform);
				}
				else
					DisableFromLackOfResources();
				break;

			//Processors create materials from waste and energy
			case SysFunction.Processor:
				if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) && ShipResources.res.SetWaste(ShipResources.res.waste - 1, transform)))
				{
					ShipResources.res.SetMaterials(ShipResources.res.materials + 1, transform);
					//Get up to three, but only call icons on first
					if (free || (ShipResources.res.SetEnergy(ShipResources.res.energy - 1) && ShipResources.res.SetWaste(ShipResources.res.waste - 1)))
					{
						ShipResources.res.SetMaterials(ShipResources.res.materials + 1);
						//THREE IS FREE (at least as far as energy is required)
						if (free || (ShipResources.res.SetWaste(ShipResources.res.waste - 1)))
						{
							ShipResources.res.SetMaterials(ShipResources.res.materials + 1);
						}
					}
				}
				else
					DisableFromLackOfResources();
				break;

			//Stills create fuel using materials and energy
			case SysFunction.Still:
				if (free || ShipResources.res.SetMaterials(ShipResources.res.materials - 1, transform))
					ShipResources.res.SetFuel(ShipResources.res.fuel + 1, transform);
				else
					DisableFromLackOfResources();
				break;

			default :
				Debug.Log(name + " cannot be \"used\" like that.");
				break;
			}
		}
	}

	public void SetKeywords(bool forceFirst)
	{
		//Non-storage forceFirsts get a roll
		if (forceFirst && keywords.Count > 0 && keywords [0] == SysKeyword.NOTDEFINED && function != SysFunction.Storage && Random.Range(0, 2) == 0)
			keywords.Add(unlockedKeywords [Random.Range(0, unlockedKeywords.Count)]);

		//All Randoms get a roll
		for (int i = 0; i < keywords.Count; i++)
		{
			if (keywords [i] == SysKeyword.Random)
			{
				keywords.RemoveAt(i);
				if (Random.Range(0, 2) == 0)
					keywords.Add(unlockedKeywords [Random.Range(0, unlockedKeywords.Count)]);
			}
		}

		//Clear Ion keywords if not on engine
//		if (keyCheck(SysKeyword.Ion) && function != SysFunction.Engine)
//		{
//			for (int i = keywords.Count - 1; i >= 0; i--)
//			{
//				if (keywords [i] == SysKeyword.Ion)
//				{
//					keywords [i] = SysKeyword.NOTDEFINED;
//				}
//			}
//		}

		Rename();
	}

	public void GiveKeyword()
	{
		keywords.Add(unlockedKeywords [Random.Range(0, unlockedKeywords.Count)]);
		Rename();
	}

	void ConditionChange()
	{
		
		//Broken or Destroyed? Disable it!
		if (isBroken && status != SysStatus.Disabled)
		{
			status = SysStatus.Disabled;
			//Rename();
		}
		//No longer broken?
		if (!isBroken && lastCondition == SysCondition.Broken)
		{
			//Rename();
			status = SysStatus.Inactive;
		}
		//Accidentally disabled passives (safety, since it came up with repaired destroys and construction)
		if (!isBroken && isPassive && status == SysStatus.Disabled)
		{
			status = SysStatus.Inactive;
		}
		
		//Disabled
		if (status == SysStatus.Disabled)
		{
			EndUse();
			var al = GetComponentInChildren<Alert>();
			if (al != null && al.GetActivatedAlerts().Contains(AlertType.Use))
				al.EndAlert("Use");
		}

		//Auto Overdrive Shutoff
//		if (condition != SysCondition.Strained && condition != SysCondition.Functional)
		if (status == SysStatus.Disabled)
			OverdriveOff();

		lastCondition = condition;
	}

	void Start()
	{
		//Broken? Make sure it starts disabled.
		if (isBroken)
		{
			status = SysStatus.Disabled;
		}

		//Baseline status. Activity determined live. If turned off, or broken, needs to stay Disabled.
		if (status != SysStatus.Disabled)
		{
			status = SysStatus.Inactive;
		}

		lastCondition = condition;

		//Make the name
		Rename();

		//Helper AIs are weird
		if (function == SysFunction.GuideBot)
		{
			gameObject.AddComponent<HelperAI>();
		}
	}

	/** Used to check for effects from all keywords, to reduce total code */
	public bool keyCheck(SysKeyword k)
	{
		for (int i = keywords.Count - 1; i >= 0; i--)
		{
			if (keywords [i] == k)
			{
				return true;
			}
		}
		return false;
	}


	/**Rename this system by the relevant values.
	 * If keys is not provided (i.e., subbing in temp values), uses this instance's current keywords as the values.
	 * Also returns the name (for other uses).
	 */
	public string Rename(List<SysKeyword> keys = null)
	{
		if (keys == null)
			keys = keywords;

		var sb = new System.Text.StringBuilder();

		//then, attach keywords, if any
		for (int i = keys.Count - 1; i >= 0; i--)
		{
			if (keys [i] != SysKeyword.NOTDEFINED)
			{
				if (keys [i] == SysKeyword.Random)
					sb.Append("??");
				else
					sb.Append(keys [i].ToString());

				sb.Append(" ");
			}
		}

		//finally, put function title
		sb.Append(System.Text.RegularExpressions.Regex.Replace(function.ToString(), "([a-z])([A-Z])", "$1 $2"));

		return name = sysName = sb.ToString();
	}

	// Update is called once per frame
	void Update()
	{
		//Change behavior, if necessary (changes in inspector/ debugging, etc.)
		if (condition != lastCondition)
		{
			ConditionChange();
		}

		//Tick, if we can.
		if (ShipResources.res != null && status != SysStatus.Disabled && isAutomated && !ticking)
		{
			StartTick();
		}
	}

	/**Cumulative modifiers on Use() output are adjusted based on total amount already adjusted.
	 * First adjustment is full, later ones are scaled based on that. More bonus = less bonus gained.
	 * Gives minimum of 50% when total previous modifier is >= 1.
	 */
	float AddModifier(float startingModifier, float toAdd)
	{
		//Need one more value: the penalty (or bonus) to the new modifier
		float penalty = 1 - startingModifier > 0 ? 1 - startingModifier : 0;

		//Average the penalty against no penalty, then apply that to the modifier being added in
		return (startingModifier + ((penalty + 1) / 2 * toAdd));
	}

	/**Returns the time for a system to Tick
	 */
	public float GetTickTime()
	{
		return tickTime;
	}

	void OnEnable()
	{
		if (GameReference.r != null && !GameReference.r.allSystems.Contains(this))	//Safety in case of initialization order
			GameReference.r.allSystems.Add(this);

		GetComponent<PlayerInteraction>().onLeftClick += TogglePower;
	}

	void OnDisable()
	{
		if (GameReference.r != null)
			GameReference.r.allSystems.Remove(this);

		GetComponent<PlayerInteraction>().onLeftClick -= TogglePower;
	}
}
