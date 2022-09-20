using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Effect - ", menuName = "Events/Event Effect Data")]
public class EventEffectData : ScriptableObject
{

	/* This class handles a single effect of a given event. It will be called to perform if the event is triggered.
	 * Only one of the listed targets will be affected, by equal and random lot.
	 */

	//Declarations
	//Effect type
	public EventEffectType type;
	//Target types
	public List<EventTarget> targets = new List<EventTarget>();
	//Specific targets for DoEffects
	public List<MonoBehaviour> chosenTargets = new List<MonoBehaviour>();

	/**Filters to limit to specific targets. May get overridden in the rare case that a valid target doesn't exist.
	 * (Don't let that happen! Be smart with EventRequirements!) */
	public bool filtersOnly, allowDamagedOrInjured, allowBrokenOrUncontrollable, allowDestroyedOrDead,
		limitToExceptional, limitToAverage, limitToShoddy, limitToMakeshift;
	//Filters for target selection

	public float chance = 1.0f;
	//Default 100% chance to trigger or consume exact resources
	public int giveResources;
	//How much to give or, if negative, take (for resources)
	private EventTarget targettedType;
	//The targetted type!
	[HideInInspector]
	public string
		targetName;
	//Name of the chosen target, for use in any event text

	[Tooltip("When relevant, use this string to explain what happened (aka, crew death)")]
	public string resultString = "";

	//Prepped Values
	private bool prepped = false;
	//Have we already prepped?
	private bool resourceEvent;
	//Is this a resourceEvent?
	private int amount;
	//Amount of resources to give for resource events
	private bool ignoreFilters;
	//Ignore filters to open up more selection options. Done automatically if initial choosing fails.



	/**Get all of the values we need to Do the effect. Be sure everything is valid.
	 * Returns this EventEffect to store for calling DoEffects after the master EventStore has moved on.
	 */
	public EventEffectData PrepEffect()
	{

		//If there are no targets, we're in trouble. SAFETY CHECKK
		if (targets.Count == 0 && giveResources == 0 && type != EventEffectType.KnockOffCourse)
		{
			Debug.Log("ERROR: No targets listed in " + this.name);
			return null;
		}
		
		//We're gonna cause a ruckus! (probably) First, choose a target type, via index.
		if (targets.Count > 0)
			targettedType = targets [Random.Range(0, targets.Count)];
		
		//Is this a resource event?
		resourceEvent = type.ToString().Contains("Give") || type.ToString().Contains("Course");

		//If it ends up being a resource, mod amount by chance!
		if (resourceEvent)
			amount = (int)(giveResources * Random.Range(chance, 2f - chance));
		//Otherwise get target(s)!
		else
		{
			//Don't need the values from the last time this got called
			chosenTargets.Clear();

			//Everything?
			bool hitEach = targettedType.ToString().Contains("Each");

			//Fresh start. Listen to filters.
			ignoreFilters = false;

			//Try and populate list.
			while (chosenTargets.Count == 0)
			{

				//Systems
				if (IsSystemTarget(targettedType))
				{
					if (hitEach)
						chosenTargets.AddRange(GameReference.r.allSystems.ToArray());
					else
					{
						var ct = GetSystemOfChosenType();
						//Only add it if it's not null
						if (ct != null)
							chosenTargets.Add(ct);
					}
				}
				//Characters
				if (IsCharacterTarget(targettedType))
				{
					if (hitEach)
						chosenTargets.AddRange(GameReference.r.allCharacters.ToArray());
					else
					{
						var ct = GetCharacterOfChosenType();
						//Only add it if it's not null
						if (ct != null)
							chosenTargets.Add(ct);
					}
				}

				//Did we get it?
				if (chosenTargets.Count <= 0)
				{
					//This didn't work. Try again, and be less picky.
					if (!ignoreFilters)
					{
						//Huh. This could create "Psycho Zombies" in bad circumstances. Hahahahaha
						ignoreFilters = true;
						Debug.Log("Filters ignored! " + name + " couldn't fill the targettedType (" + targettedType.ToString() + ") on the first try.");
					}
					//Second time through failed! Uh oh.
					else
					{
						//Just gonna grab a generic.
						Debug.Log(name + " didn't find anything even with expanded filters. Just get a generic.");
						if (IsSystemTarget(targettedType))
						{
							targettedType = EventTarget.System;
							chosenTargets.Add(GetSystemOfChosenType());
						}
						if (IsCharacterTarget(targettedType))
						{
							targettedType = EventTarget.Character;
							chosenTargets.Add(GetCharacterOfChosenType());
						}

						//This better have worked.
						if (chosenTargets.Count == 0)
						{
							Debug.LogWarning(name + " completely failed. There must not be any systems/ characters at all. We'll try again on DoEffect and hope for the best.");
							return this;
						}
					}
				}
			}

			//Finally, get targetName!
			if (!hitEach)
				targetName = chosenTargets [0].name;
			else
				targetName = targettedType.ToString();
		}

		//Done prepping!
		prepped = true;

		return this;
	}


	/**Uh, pretty self explanatory. #uselesscomments #hashtagsincode
	 */
	public void DoEffect()
	{

		//Didn't prep? Prep!
		if (!prepped)
			PrepEffect();

		//Let's do stuff

		//Char/Sys effects. Won't do anything if there are no chosen targets. Oh darn.
		foreach (var t in chosenTargets)
		{
			//Don't bother if it misses the chance
			if (Random.Range(0f, 1f) <= chance)
			{
				//Cast and set!
				ShipSystem sys = t as ShipSystem;
				Character ch = t as Character;

				//Do shit.
				switch (type)
				{
				//Breaking!
				case EventEffectType.Break:
					if (sys != null)
						sys.Break(false);
					if (ch != null)
					{
						ch.lastTaskType = Character.CharSkill.Command;
						ch.result = resultString.Trim() == "" ? "Lost Control" : resultString.Trim();
						ch.Damage();
					}
					break;
						
				//Crit Break!
				case EventEffectType.CritBreak:
					if (sys != null)
						sys.Break(true);
					if (ch != null)
					{
						ch.result = resultString.Trim() == "" ? "Lost Control" : resultString.Trim();
						ch.ToPsychotic();
					}
					break;
						
						
				//Damaging
				case EventEffectType.Damage:
					if (sys != null)
						sys.Damage(true);
					if (ch != null)
					{
						ch.lastTaskType = Character.CharSkill.Mechanical;
						ch.result = resultString.Trim() == "" ? "Involved in Freak Accident" : resultString.Trim();
						ch.Damage();
					}
					break;
						
				//Repair
				case EventEffectType.Repair:
					if (sys != null)
						sys.Repair(false);
					if (ch != null)
					{
						if (ch.statusIsMedical)
							ch.Heal();
						else if (ch.statusIsPsychological)
							ch.Improve();
					}
					break;
						
						
				//Crit Repair!
				case EventEffectType.CritRepair:
					if (sys != null)
						sys.Repair(true, evenIfDestroyed: true);
					if (ch != null)
					{
						if (ch.statusIsMedical)
							ch.Heal();
						else if (ch.statusIsPsychological)
							ch.Improve();
					}
					break;
						
						
				//Disabling
				case EventEffectType.Disable:
					if (sys != null)
						sys.DisableFromLackOfResources();
					if (ch != null)
					{
						ch.result = resultString.Trim() == "" ? "Got Knocked Out" : resultString.Trim();
						ch.ToUnconscious();
					}
					break;


				//Use
				case EventEffectType.Use:
					if (sys != null)
					{
						sys.Use();
						//Also set EndUse
						if (!sys.isAutomated)
							sys.Invoke("EndUse", sys.GetTickTime());
					}
					if (ch != null)
						ch.StressCheck(true);
					break;

				//Stress
				case EventEffectType.Stress:
					if (sys != null)
					{
						sys.DurabilityCheck(true);
					}
					if (ch != null)
					{
						ch.Stress(ch.bHand.lowStress);
						ch.StressCheck(true);
					}
					break;

				//Destress
				case EventEffectType.Destress:
					if (sys != null)
					{
						sys.ClearHits();
					}
					if (ch != null)
					{
						ch.DeStress(ch.bHand.lowStress);
					}
					break;

				//Remove
				case EventEffectType.Remove:
					if (sys != null)
					{
						Destroy(sys.gameObject);
					}
					if (ch != null)
					{
						if (resultString != "" && ch.status != Character.CharStatus.Dead)
						{
							ch.result = resultString;
							ch.ToDead();
						}
						StatTrack.stats.lostCrew.Add(StatTrack.CreateCrewMemorialFromCharacter(ch, true));
						Destroy(ch.gameObject);
					}
					break;

				//Feed
				case EventEffectType.Feed:
					if (sys != null)
					{
						if (sys.isManualProduction)
						{
							//One process
							sys.Process();
						}
						if (sys.isAutomated)
						{
							//Insta-tick
							sys.StartTick(0);
						}
					}
					if (ch != null)
					{
						//Free feed
						ch.bHand.Eat(1);
					}
					break;

				//Improve!
				case EventEffectType.Improve:
					if (sys != null)
						sys.GiveKeyword();
					if (ch != null)
						ch.GiveRandomRoleOrSkill();
					break;

				//Destroy!
				case EventEffectType.Destroy:
					if (sys != null)
						sys.DestroySystem();
					if (ch != null)
						ch.ToDead();
					break;

				//No go.
				default :
					if (type != EventEffectType.GetSystem && type != EventEffectType.GetCharacter)
						Debug.Log(type + " didn't match any effect case.");
					break;
				}
			}
		}


		//Resources!
		if (resourceEvent)
		{
			switch (type)
			{
			//Give Random Resource
			case EventEffectType.GiveRandom:
				int choice = Random.Range(0, 5);
				switch (choice)
				{
				case 0:
					ShipResources.res.SetMaterials(ShipResources.res.materials + amount);
					targetName = "materials";
					break;
				case 1:
					ShipResources.res.SetFuel(ShipResources.res.fuel + amount);
					targetName = "fuel";
					break;
				case 2:
					ShipResources.res.SetParts(ShipResources.res.parts + (amount / ShipResources.partsVolume));
					targetName = "parts";
					break;
				case 3:
					ShipResources.res.SetWaste(ShipResources.res.waste + amount);
					targetName = "waste";
					break;
				case 4:
					ShipResources.res.SetEnergy(ShipResources.res.energy + amount);
					targetName = "energy";
					break;
				}
				break;
			
			//Specific resources
			case EventEffectType.GiveAir:
				ShipResources.res.SetTotalAir(ShipResources.res.totalAir + amount);
				break;
			
			case EventEffectType.GiveUsableAir:
				ShipResources.res.SetUsableAir(ShipResources.res.usableAir + amount);
				break;
			
			case EventEffectType.GiveDistance:
				ShipResources.res.distance -= amount;	//Negative because "distance" = distance left, not distance traveled.
				break;
			
			case EventEffectType.GiveEnergy:
				ShipResources.res.SetEnergy(ShipResources.res.energy + amount);
				break;
			
			case EventEffectType.GiveFood:
				ShipResources.res.SetFood(ShipResources.res.food + amount);
				break;
			
			case EventEffectType.GiveFuel:
				ShipResources.res.SetFuel(ShipResources.res.fuel + amount);
				break;
			
			case EventEffectType.GiveMaterials:
				ShipResources.res.SetMaterials(ShipResources.res.materials + amount);
				break;
			
			case EventEffectType.GiveParts:
				ShipResources.res.SetParts(ShipResources.res.parts + amount);
				break;
			
			case EventEffectType.GiveSpeed:
				ShipResources.res.speed += amount;
				break;
			
			case EventEffectType.GiveThrust:
				ShipMovement.sm.AddThrustTimesModifier(amount);
				break;
			
			case EventEffectType.GiveWaste:
				ShipResources.res.SetWaste(ShipResources.res.waste + amount);
				break;
				
			case EventEffectType.KnockOffCourse:
				ShipMovement.sm.SetOffCourse(1, true);
				break;

			case EventEffectType.ImproveCourse:
				ShipMovement.sm.ReduceOffCourse((float)amount / 10f);
				break;
			
			default :
				Debug.Log(type + " didn't match any effect case.");
				break;
			}
		}

		//DoEffect is done. No longer prepped for next use.
		prepped = false;
	}


	/**Check if the chosen target type is a kind of ShipSystem.
	 * Determines effect behavior.
	 */
	public bool IsSystemTarget(EventTarget targ)
	{
		bool returnValue = false;

		//Easy checks
		if (targ == EventTarget.System || targ == EventTarget.EachSystem
		    || targ == EventTarget.NonInertSystem)
			returnValue = true;
		//Iterate through SysFunctions
		else
		{
			foreach (ShipSystem.SysFunction sf in (ShipSystem.SysFunction[]) ShipSystem.SysFunction.GetValues(typeof(ShipSystem.SysFunction)))
			{
				if (sf.ToString().ToLower().Equals(targ.ToString().ToLower()))
					returnValue = true;
			}
		}

		//Return result
		return returnValue;
	}

	/**Check if the chosen target type is a kind of Character.
	 * Determines effect behavior.
	 */
	public bool IsCharacterTarget(EventTarget targ)
	{
		if (targ == EventTarget.Character || targ == EventTarget.EachCharacter
		    || targ == EventTarget.Engineer
		    || targ == EventTarget.Medical
		    || targ == EventTarget.Scientist)
			return true;
		
		//Guess it's not!
		return false;
	}

	/**Find a target of the chosen system type to do the effect to
	 */
	private ShipSystem GetSystemOfChosenType()
	{
		//Find viable targets
		List<ShipSystem> targetSys = new List<ShipSystem>();

		//We need to sort our options
		foreach (ShipSystem ss in GameReference.r.allSystems)
		{
			//Only act on anything matching the filters

			//Check quality filters if there are any (Skip if ignoreFilters)
			if (!ignoreFilters && limitToExceptional || limitToAverage || limitToShoddy || limitToMakeshift)
			{
				//Check system's quality against filters.
				if (ss.quality == ShipSystem.SysQuality.Exceptional && !limitToExceptional)
					continue;
				if (ss.quality == ShipSystem.SysQuality.Average && !limitToAverage)
					continue;
				if (ss.quality == ShipSystem.SysQuality.Shoddy && !limitToShoddy)
					continue;
				if (ss.quality == ShipSystem.SysQuality.Makeshift && !limitToMakeshift)
					continue;
			}

			//Then check condition before function
			if (ignoreFilters//Auto success if ignoreFilters
			    || (ss.condition == ShipSystem.SysCondition.Functional && !filtersOnly)//Standard filtering allowed
			    || (ss.condition == ShipSystem.SysCondition.Strained && allowDamagedOrInjured)//Damaged allowed
			    || (ss.condition == ShipSystem.SysCondition.Broken && allowBrokenOrUncontrollable)//Broken allowed
			    || (ss.condition == ShipSystem.SysCondition.Destroyed && allowDestroyedOrDead))//Destroyed allowed
			{	

				//Generic
				if (targettedType == EventTarget.System || targettedType == EventTarget.EachSystem)
				{
					targetSys.Add(ss);
					continue;
				}
				//Specific system
				if (targettedType.ToString().ToLower().Equals(ss.function.ToString().ToLower()))
				{
					targetSys.Add(ss);
					continue;
				}
				//Non-Inert
				if (targettedType == EventTarget.NonInertSystem && !ss.isPassive)
				{
					targetSys.Add(ss);
					continue;
				}
				
			}
		}

		//Choose one and return it
		if (targetSys.Count > 0)
			return targetSys [Random.Range(0, targetSys.Count)];
		else
			return null;
	}

	/**Find a target of the chosen character type to do the effect to
	 */
	private Character GetCharacterOfChosenType()
	{
		//Find viable targets
		List<Character> targetChar = new List<Character>();
		
		//We need to sort our options
		foreach (Character ch in GameReference.r.allCharacters)
		{
			//Only act on anything matching the filters
			if (ignoreFilters//Auto-success if ignoreFilters
			    || (ch.isControllable && !ch.injured && !filtersOnly)//Standard filtering allowed
			    || (ch.status == Character.CharStatus.Injured && allowDamagedOrInjured)//Injured allowed
			    || (!ch.isControllable && ch.status != Character.CharStatus.Dead && allowBrokenOrUncontrollable)//Uncontrollable allowed
			    || (ch.status == Character.CharStatus.Dead && allowDestroyedOrDead))
			{								//Dead allowed

				//Generic
				if (targettedType == EventTarget.Character || targettedType == EventTarget.EachCharacter)
				{
					targetChar.Add(ch);
					continue;
				}
				//Engineer
				if (targettedType == EventTarget.Engineer && ch.team == Character.Team.Engineering)
				{
					targetChar.Add(ch);
					continue;
				}
				//Medical
				if (targettedType == EventTarget.Medical && ch.team == Character.Team.Medical)
				{
					targetChar.Add(ch);
					continue;
				}
				//Science
				if (targettedType == EventTarget.Scientist && ch.team == Character.Team.Science)
				{
					targetChar.Add(ch);
					continue;
				}
			}
		}
		
		//Choose one and return it
		if (targetChar.Count > 0)
			return targetChar [Random.Range(0, targetChar.Count)];
		else
			return null;
	}

}
