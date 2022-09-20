using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class Job
{

	/* This class initiates and shuts down the tasking activity.
	 * It also stores the values associated with a particular task (the assigned character, the character's target, etc.).
	 * 
	 * In effect, it is the specific intermediary between BehaviorHandler (Individual Behavior) and JobAssignment (Group Organization).
	 * JobAssignment instantiates it with specific values as needed by the group. This passes them to TaskHandler and tells BehaviorHandler to begin.
	 * Then, BehaviorHandler indicates to this that the task is finished. This proceeds to shutdown, freeing its components for further use in JobAssignment.
	 * 
	 * Additionally, it defines idling tasks to characters that are told to idle. (Because: JobAssignment handles the details of alerts, not passive behavior)
	 */

	//Internal constants
	const int OFFTASK_ROLL = 6;

	//The character
	public Character character;
	private BehaviorHandler bHand;
	private SimplePath sPath;

	//The target
	public Transform target;
	//The target's alert system. Only non-null if this is an alert situation (aka, not an idle task).
	public Alert alert = null;

	//The task
	public Character.Task task;
	public int priority;
	bool responseToAlert = true;
	//Is this job created for an alert? Defaults to yes. Idle() will make this false.

	//Cache
	GameObject idlingTarget;
	//Instantiated target transform when idling
	

	/**Create a new Job. Will initiate activity.
	 */
	public Job(Character ch, Character.Task ta, Transform tg, int pr)
	{
		//Set internal values
		character = ch;
		bHand = character.bHand;
		sPath = character.sPath;
		target = tg;
		task = ta;
		priority = pr;
		bHand.currentJob = this;			//BehaviorHandler needs to know what to signal when it's done
		
		//Is this an idle job that we still need to define?
		if (task == Character.Task.Idle && character.canAct)
		{
			Idle();
		}

		//Now that we're sure about out values, push them out to the things needing them
		bHand.arrived = false;				//We're not there yet
		character.task = bHand.task = task;	//Everybody needs to know what we're doing
		character.priority = priority;		//The character needs to know how important it is
		bHand.lastTarget = bHand.target;	//We're changing targets
		bHand.target = target;				//BehaviorHandler needs to know the thing that is important

		//Let's get moving
		if (character.canAct && target != null)
		{
			character.GetComponent<SimplePath>().SetTarget(target);
			//If this is a job in response to an alert, establish the alert component and set the tasking thought
			if (responseToAlert)
			{
				alert = target.GetComponentInChildren<Alert>();
				//Thought change
				if (bHand.lastTarget != bHand.target) //Used to be 'character.sPath.lastProcessedTarget != target'
				{
					character.currentThoughtTargetName = target.name;
					character.currentThought = Character.Thought.Tasking;
				}
			}
		}
		else
		{
			//We need to shutdown if the character can't act
			EndTask(true);
		}

		//Set the anti-ignore coroutine
		ch.StartCoroutine(EndTaskIfIgnore());
	}

	/**Task is done (for better or worse). This will reset the Character and BehaviorHandler to untasked levels.
	 * It will also begin shutdown of this instance of Job, clear alerts, end system use, and allow target character to resume behavior.
	 * Bool is true if the task needs to be 'reset' (returned to queue and reassigned). False if done.
	 */
	public void EndTask(bool reset)
	{

		//Cancel any HandleIt invoke
		if (bHand.IsInvoking("HandleIt"))
			bHand.CancelInvoke("HandleIt");

		//Job is done. Shut it down.
		bHand.ClearStatusBools();
		bHand.StartCoroutine(bHand.CloseProgressBar(0.6f));
		JobAssignment.ja.JobShutdown(this);

		//Signal the target that we're done
		if (target != null)
		{
			//Target is system. EndUse only when necessary (not repairs!)
			ShipSystem targetSys = target.GetComponent<ShipSystem>();
			if (targetSys != null && task != Character.Task.Repair && task != Character.Task.Maintenance)
				targetSys.EndUse();

			//Target is character. End the interrupt!
			Character targetChar = target.GetComponent<Character>();
			if (targetChar != null)
				targetChar.sPath.EndInterrupt();
		}

		//Clear the job from the queue if we aren't resetting. Idle tasks are never in the queue.
		if (target == null || !reset)
		{
			//Signal the Alert itself.
			if (alert != null && !alert.GetActivatedAlerts().Contains(AlertType.Use)
			                  && !alert.GetActivatedAlerts().Contains(AlertType.Salvage))
			{
				alert.Clear();
			}
		}

		//Clear this out?
		bHand.currentJob = null;
		//Reset values
		character.priority = 100;

		//Delete any idlingTarget
		if (idlingTarget != null)
		{
			GameObject.Destroy(idlingTarget);
		}
	}

	
	/**Basic Idle action. This checks the character and figures out what the task will be.
	 */
	public void Idle()
	{
		//Nothing we assign here is a response to an alert
		responseToAlert = false;

		//Available action callbacks
		List<System.Func<bool, bool>> actions = new List<Func<bool, bool>>();

		/*
		 * MAJOR NEEDS
		 */

		//Get available controllable needs
		if (character.isControllable)
		{
			if (character.hunger > character.hungerResilience)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToEat(displace));
			}
			if (character.waste > character.wasteResilience)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToShit(displace));
			}
			if (character.sleepiness > character.sleepinessResilience)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToSleep(displace));
			}
			if (character.status == Character.CharStatus.Stressed)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToWorkout(displace));
			}
		}

		//Major need action round
		if (actions.Count > 0)
		{
			//Can we do any of these?
			if (TryAllActions(actions))
				return;
			//Clear it and try something else
			else
				actions.Clear();
		}



		/*
		 * SOCIAL & WORK
		 */

		//TODO SOCIAL UPDATE Interact with others


		//Work
		if (character.isControllable && !character.roles.Contains(Character.CharRoles.Affluent))
		{
			var currentOffTaskRoll = -1;

			//Medical Aid Offtask Check
			if (character.team != Character.Team.Medical)
			{
				//First check for what the target would be, so we can offset offtask by skill
				if (TryToHeal(false))
					//Then do the roll if it's valid
					currentOffTaskRoll = UnityEngine.Random.Range(0, Mathf.Min(1, OFFTASK_ROLL - (character.GetSkill(Character.CharSkill.Science, target) * 2)));
				else
					//Or reset if not
					currentOffTaskRoll = -1;
			}

			//Add Medical Aid
			if (character.team == Character.Team.Medical || currentOffTaskRoll == 0)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToHeal(displace));
			}


			//Repair Offtask Check
			if (character.team != Character.Team.Engineering)
			{
				//First check for what the target would be, so we can offset offtask by skill
				if (TryToRepair(false))
					//Then do the roll if it's valid
					currentOffTaskRoll = UnityEngine.Random.Range(0, Mathf.Min(1, OFFTASK_ROLL - (character.GetSkill(Character.CharSkill.Mechanical, target) * 2)));
				else
					//Or reset if not
					currentOffTaskRoll = -1;
			}

			//Add Repair
			if (character.team == Character.Team.Engineering || currentOffTaskRoll == 0)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToRepair(displace));
			}


			//Use/ Construct Offtask Check
			if (character.team != Character.Team.Science)
			{
				//First check for what the target would be, so we can offset offtask by skill
				if (TryToUse(false))
					//Then do the roll if it's valid
					currentOffTaskRoll = UnityEngine.Random.Range(0, Mathf.Min(1, OFFTASK_ROLL - (character.GetSkill(Character.CharSkill.Science, target) * 2)));
				else
					//Or reset if not
					currentOffTaskRoll = -1;
			}

			//Add Use/ Construct
			if (character.team == Character.Team.Science || currentOffTaskRoll == 0)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToUse(displace));
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToConstruct(displace));
			}
		}

		//Work action round
		if (actions.Count > 0)
		{
			//Can we do any of these?
			if (TryAllActions(actions))
				return;
			//Clear it and try something else
			else
				actions.Clear();
		}


		/*
		 * MINOR NEEDS AND MISC
		 */


		//Set minor needs for lighter options
		if (character.isControllable)
		{
			if (character.hunger > character.hungerResilience / 2)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToEat(displace));
			}
			if (character.waste > character.wasteResilience / 2)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToShit(displace));
			}
			if (character.sleepiness > character.sleepinessResilience / 2)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToSleep(displace));
			}
			if (character.hasStress)
			{
				actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToWorkout(displace));
			}
		}

		//Or just wander around the ship. Don't need to be controllable for this!
		actions.Insert(UnityEngine.Random.Range(0, actions.Count + 1), (bool displace) => TryToWander(false));

		//Final action round
		if (!TryAllActions(actions))
		{
			//Safety net
			EndTask(false);
		}
	}

	bool TryAllActions(List<System.Func<bool, bool>> actions)
	{
		bool success = false;

		//Try each in order for realsies until success or end
		foreach (var t in actions)
		{
			if (t != null)
				success = t.Invoke(true);
			
			if (success)
				break;
		}

		//return success
		return success;
	}

	bool TryToEat(bool displace)
	{
		task = Character.Task.Idle;
		//If there's food, search all ship systems for functional kitchens, then assign to the nearest one
		return ShipResources.res.food > 0 && AssignToNearestTarget(FindAllUsableSystems(ShipSystem.SysFunction.Kitchen, true), displace);
	}

	bool TryToShit(bool displace)
	{
		task = Character.Task.Idle;
		//Search all ship systems for functional toilets, then assign to the nearest one
		return AssignToNearestTarget(FindAllUsableSystems(ShipSystem.SysFunction.Toilet, true), displace);
	}

	bool TryToSleep(bool displace)
	{
		task = Character.Task.Idle;
		//Search all ship systems for functional beds, then assign to the nearest one
		return AssignToNearestTarget(FindAllUsableSystems(ShipSystem.SysFunction.Bed, true), displace);
	}

	bool TryToWorkout(bool displace)
	{
		task = Character.Task.Idle;
		//Search all ship systems for functional gyms, then assign to the nearest one
		return AssignToNearestTarget(FindAllUsableSystems(ShipSystem.SysFunction.Gym, true), displace);
	}

	bool TryToHeal(bool displace)
	{
		//Reduce priority (++) if incorrect team
		if (character.team != Character.Team.Medical)
			priority++;

		//Find the damaged crew member
		List<Character> crew = new List<Character>();

		//Prioritize the wounded and crazy
		foreach (var ch in GameReference.r.allCharactersLessIgnored)
		{
			if (ch.statusIsMedical || ch.status == Character.CharStatus.Psychotic)
			{
				crew.Add(ch);
			}
		}

		//Super important work
		if (crew.Count > 0)
		{			
			priority -= 2;
		}
		//Otherwise add crewmembers who are restrained
		else
		{
			foreach (var ch in GameReference.r.allCharactersLessIgnored)
			{
				if (ch.status == Character.CharStatus.Restrained)
				{
					crew.Add(ch);
				}
			}
		}

		//Also important work
		if (crew.Count > 0)
		{			
			priority--;
		}
		//Otherwise MAYBE look at stressed crew
		else if (UnityEngine.Random.Range(0, OFFTASK_ROLL) == 0)
		{
			foreach (var t in GameReference.r.allCharactersLessIgnored)
			{
				if (t.status == Character.CharStatus.Stressed)
					crew.Add(t);
			}
		}

		//Get list of crew's transforms
		List<Transform> transforms = new List<Transform>();
		foreach (var t in crew)
		{
			if (t != character)
				transforms.Add(t.transform);
		}

		task = Character.Task.Heal;

		//Find the nearest crewmember needing attention (Hah! Triage? What's that? It's on the player to do that!)
		return AssignToNearestTarget(transforms, displace);
	}

	bool TryToRepair(bool displace)
	{
		//Reduce priority (++) if incorrect team
		if (character.team != Character.Team.Engineering)
			priority++;

		//Find a damaged system
		List<ShipSystem> damagedSystems = new List<ShipSystem>();

		//Search for broken things
		foreach (ShipSystem ss in GameReference.r.allSystemsLessIgnored)
		{
			if (ss.condition == ShipSystem.SysCondition.Broken && ss.canAffordRepair)
			{
				damagedSystems.Add(ss);

				priority--;
			}
		}

		//Try a broader search if nothing is broken
		if (damagedSystems.Count <= 0)
		{
			foreach (ShipSystem ss in GameReference.r.allSystemsLessIgnored)
			{
				if (ss.isRepairable && ss.canAffordRepair)
					damagedSystems.Add(ss);
			}
		}

		//Any actually damaged systems (broken or damaged)?
		if (damagedSystems.Count > 0)
		{
			//Higher priority fix
			priority--;
		}
		//No? Heck, find something with at least a hit!
		else
		{
			foreach (ShipSystem ss in GameReference.r.allSystemsLessIgnored)
			{
				if (ss.conditionHit > 0)
				{
					damagedSystems.Add(ss);
				}
			}
		}

		//Get list of systems' transforms
		List<Transform> transforms = new List<Transform>();
		foreach (var t in damagedSystems)
		{
			transforms.Add(t.transform);
		}

		task = Character.Task.Maintenance;

		//Find the nearest unattended system
		return AssignToNearestTarget(transforms, displace);
	}

	bool TryToConstruct(bool displace)
	{
		//Reduce priority (++) if incorrect team
		if (character.team != Character.Team.Science)
			priority++;
		
		List<ShipSystem> constructableSystems = new List<ShipSystem>();
		
		foreach (ShipSystem ss in GameReference.r.allSystemsLessIgnored)
		{
			if (ss.quality == ShipSystem.SysQuality.UnderConstruction && ss.canAffordConstruction)
			{
				constructableSystems.Add(ss);
			}
		}
		
		List<Transform> transforms = new List<Transform>();
		foreach (var t in constructableSystems)
		{
			transforms.Add(t.transform);
		}

		task = Character.Task.Construction;
		
		return AssignToNearestTarget(transforms, displace);
	}

	bool TryToUse(bool displace)
	{
		//Reduce priority (++) if incorrect team
		if (character.team != Character.Team.Science)
			priority++;

		//What systems can we use to make resources or do other sciencey tasks?
		List<ShipSystem> productionSystems = new List<ShipSystem>();
		foreach (ShipSystem sys in GameReference.r.allSystemsLessIgnored)
		{
			//Needs to not be disabled, and be of the right type
			if (sys.status != ShipSystem.SysStatus.Disabled && sys.isManualProduction)
				productionSystems.Add(sys);
		}

		//What resources can we make?
		List<string> makeableResources = new List<string>();
		//Check our systems
		foreach (var sys in productionSystems)
		{
			switch (sys.function)
			{
			//Be sure to check if we have the require resources!
			case ShipSystem.SysFunction.Fabricator:
				if (ShipResources.res.materials >= ShipResources.partsVolume && ShipResources.res.energy > 0)
					makeableResources.Add("parts");
				break;
			case ShipSystem.SysFunction.Helm:
				makeableResources.Add("heading");
				break;
			case ShipSystem.SysFunction.Injector:
				if (ShipResources.res.usableAir > 0)
					makeableResources.Add("speed");
				break;
			case ShipSystem.SysFunction.Processor:
				if (ShipResources.res.waste > 0 && ShipResources.res.energy >= 2)
					makeableResources.Add("materials");
				break;
			case ShipSystem.SysFunction.Still:
				if (ShipResources.res.materials > 0)
					makeableResources.Add("fuel");
				break;
			}
		}

		//Find what resource we should make.

		//Make a list of Anonymous Types that include only the name and value of the resource. This list will contain our needed data.
		var neededResources = new[] { new { Name = "Name", Value = 0 } }.ToList();	//Casted by example
		//Wipe the example
		neededResources.Clear();
		//We're gonna reflect ShipResources
		Type type = typeof(ShipResources);	//Type pointer for reflection

		//Look through ShipResource's properties, one by one
		foreach (PropertyInfo resource in type.GetProperties())
		{
			//The value associated with the PropertyInfo
			object temp = null;

			//Be sure it's not deprecated or obsolete. Otherwise, Unity throws exceptions and whines like a bitch for dayyyys.
			if (!resource.IsDefined(typeof(ObsoleteAttribute), true))
				//For the resource at this index, get the value from the one in our singleton instance of ShipResource: 'res'
				temp = resource.GetValue(ShipResources.res, null);

			//If it's a consumable resource, we want to look at it!
			if (temp != null && temp is int && ShipResources.res.IsConsumableResource(resource.Name))
			{
				//The resource's value
				int value = (int)temp;
				//Adjust for parts
				if (resource.Name.ToLower().Equals("parts"))
					value *= ShipResources.partsVolume;
				//Prefer materials
				if (resource.Name.ToLower().Equals("materials"))
					value = (int)(value / 1.5f);

				//Is this resource on the makeable resources list?
				if (makeableResources.Contains(resource.Name.ToLower()))
				{
					//Add it to the needed resources list! Include the name and the value!
					neededResources.Add(new {Name = resource.Name, Value = value});
				}
			}
		}

		//Check a couple special values, too. Gotta know when to use special systems!
		//Helm
		if (makeableResources.Contains("heading"))
		{
			//Value of current heading
			int value = 1000;
			var offCourse = ShipMovement.sm.GetOffCourse();
			//More dire if going backwards, and most dire if it's doing that quickly
			if (offCourse > 0 && ShipResources.res.speed != 0)
				value = Mathf.Abs((int)(value / offCourse / ShipResources.res.speed));

			//Add to the list!
			neededResources.Add(new {Name = "heading", Value = value});
		}

		//Injector
		if (makeableResources.Contains("speed"))
		{
			//TODO Logic that won't kill all the crew from suffocation
			//Add to the list!
			neededResources.Add(new {Name = "speed", Value = ShipResources.res.speed});
		}

		//Let's make it!
		if (neededResources.Count > 0)
		{
			//Sort the list's elements by ascending value!
			if (neededResources.Count > 1)
				neededResources.Sort((obj1, obj2) => obj1.Value.CompareTo(obj2.Value));

			//Target systems by the need order of the resources they make
			foreach (var nr in neededResources)
			{
				//What system did we need again?
				ShipSystem.SysFunction neededSystem;

				switch (nr.Name.ToLower())
				{
				case "parts":
					neededSystem = ShipSystem.SysFunction.Fabricator;
					break;
				case "heading":
					neededSystem = ShipSystem.SysFunction.Helm;
					break;
				case "speed":
					neededSystem = ShipSystem.SysFunction.Injector;
					break;
				case "materials":
					neededSystem = ShipSystem.SysFunction.Processor;
					break;
				case "fuel":
					neededSystem = ShipSystem.SysFunction.Still;
					break;
				default:
					//Somehow, we're here. I don't know how. This is a safety net. It's also to allay the Parser's syntax worries. Better log it!
					Debug.Log("\"" + nr.Name + "\" is not a resource for which there is a creating system designated. Check the code!");
					continue;
				}

				task = Character.Task.Using;

				//Search all ship systems for a functional one of that system type, then assign to it
				if (AssignToNearestTarget(FindAllUsableSystems(neededSystem, true), displace))
					return true;
			}
		}

		//Exit if we get here
		return false;
	}

	bool TryToWander(bool displace)
	{
		//Get a random direction to move
		int moveDirection = UnityEngine.Random.Range(0, 8);
		int moveDistance = UnityEngine.Random.Range(1, 8);
		//Vector3 location where we're moving
		Vector3 targ;
		//Do it
		switch (moveDirection)
		{
		default: //UP
			targ = new Vector3(character.transform.position.x, character.transform.position.y + moveDistance, character.transform.position.z);
			break;
		case 1:	//UP + RIGHT
			targ = new Vector3(character.transform.position.x + moveDistance / 2, character.transform.position.y + moveDistance / 2, character.transform.position.z);
			break;
		case 2:	//RIGHT
			targ = new Vector3(character.transform.position.x + moveDistance, character.transform.position.y, character.transform.position.z);
			break;
		case 3:	//DOWN + RIGHT
			targ = new Vector3(character.transform.position.x + moveDistance / 2, character.transform.position.y - moveDistance / 2, character.transform.position.z);
			break;
		case 4:	//DOWN
			targ = new Vector3(character.transform.position.x, character.transform.position.y - moveDistance, character.transform.position.z);
			break;
		case 5:	//DOWN + LEFT
			targ = new Vector3(character.transform.position.x - moveDistance / 2, character.transform.position.y - moveDistance / 2, character.transform.position.z);
			break;
		case 6:	//LEFT
			targ = new Vector3(character.transform.position.x - moveDistance, character.transform.position.y, character.transform.position.z);
			break;
		case 7:	//UP + LEFT
			targ = new Vector3(character.transform.position.x - moveDistance / 2, character.transform.position.y + moveDistance / 2, character.transform.position.z);
			break;
		}
		//Create the transform to target from our Vector3
		idlingTarget = MonoBehaviour.Instantiate(GameObject.Find("Idling Target"), targ, Quaternion.identity) as GameObject;
		idlingTarget.transform.parent = GameObject.Find("Idle").transform;
		//If we can target it, do so
		if (JobAssignment.ja.CanTarget(character, priority, idlingTarget.transform, displace)
		    && (idlingTarget.transform.position.x != character.transform.position.x || idlingTarget.transform.position.y != character.transform.position.y))
		{
			//Debug.Log("IdlingTarget selected.");
			task = Character.Task.Idle;
			target = idlingTarget.transform;

			//Thought change
			character.currentThoughtTargetName = "";
			character.currentThought = Character.Thought.Wandering;

			//Supes low priority
			priority = 10;

			return true;
		}

		return false;
	}

	/**The moment the target has an ignore, cancel this job!
	 */
	IEnumerator EndTaskIfIgnore()
	{
		if (target == null)
			yield break;

		//Get the alert
		Alert temp = target.GetComponentInChildren<Alert>();

		if (temp == null)
			yield break;

		yield return new WaitUntil(() => temp.GetVisibleAlerts().Contains(AlertType.Ignore));

		EndTask(true);
	}

	/**Finds and returns a list of transforms for all non-disabled systems of a chosen type.
	 */
	List<Transform> FindAllUsableSystems(ShipSystem.SysFunction type, bool excludeIgnored = false)
	{
		List<Transform> output = new List<Transform>();

		List<ShipSystem> source;

		if (excludeIgnored)
			source = GameReference.r.allSystemsLessIgnored;
		else
			source = GameReference.r.allSystems;

		foreach (ShipSystem ss in source)
		{
			if (ss.function == type && ss.status != ShipSystem.SysStatus.Disabled)
			{
				output.Add(ss.transform);
			}
		}

		return output;
	}

	/**Find nearest target among the given list. 
	 * Choose a random one if there's a tie.
	 * 
	 * Bonus! This calls JobAssignment.CanTarget so you don't have to.
	 */
	bool AssignToNearestTarget(List<Transform> pool, bool displace)
	{

		//Target(s) we've chosen
		List<Transform> sortedByDistance = new List<Transform>();

		foreach (Transform targ in pool)
		{
			//Compare to existing targets
			if (sortedByDistance.Count > 0)
			{
				//How far is this target?
				float currentTargetDist = sPath.DistanceCheck(targ);
				//Compare distance to already processed targets
				for (int i = 0; i < sortedByDistance.Count; i++)
				{
					//How far is this already processed target?
					float tempDist = sPath.DistanceCheck(sortedByDistance [i]);
					//Is this closer?
					if (currentTargetDist < tempDist)
					{
						//Insert before
						if (!sortedByDistance.Contains(targ))
							sortedByDistance.Insert(i, targ);
						//Inserted; break!
						break;
					}
					//Is it the same?
					else if (currentTargetDist == tempDist)
					{
						//Random chance to insert it anyway
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							if (!sortedByDistance.Contains(targ))
								sortedByDistance.Insert(i, targ);
							break;
						}
						//Otherwise we'll just keep processing
					}
					//Are we at the end of the list?
					else if (i + 1 == sortedByDistance.Count)
					{
						//We need to insert it at the end
						if (!sortedByDistance.Contains(targ))
							sortedByDistance.Insert(i + 1, targ);
					}
					//Otherwise, keep processing till we find where we should be.
				}
			}
			//Or add it if the list is empty
			else
			{
				sortedByDistance.Add(targ);
			}
		}

		bool foundValidTarget = false;

		//Return the nearest that CanTarget
		if (sortedByDistance.Count > 0)
		{
			foreach (var targ in sortedByDistance)
			{
				if (JobAssignment.ja.CanTarget(character, priority, targ, displace))
				{
					//Assign! We've found it.
					target = targ;
					foundValidTarget = true;
					break;
				}
			}
		}
		//Return if we assigned
		return foundValidTarget;
	}
}
