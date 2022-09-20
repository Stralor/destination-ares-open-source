using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JobAssignment : MonoBehaviour
{

	/**Singleton-like reference to the JobAssignment object.
	 */
	public static JobAssignment ja;

	[HideInInspector] public List<Alert> allPossibleAlerts = new List<Alert>();
	//Populated and depopulated by the Alert class, live.
	private List<Character> inactiveCharacters = new List<Character>();
	//Characters without tasks
	public List<Job> activeJobs = new List<Job>();
	//Jobs currently in progress

	/**Alerts that are for "target needs repairs/healing"*/
	List<AlertType> alarmsForFixing = new List<AlertType>()
	{
		AlertType.Warning,
		AlertType.Alert,
		AlertType.Emergency
	};



	void Update()
	{
		//Check for if comms are on
		var commsOn = GameReference.r.allSystems.Exists(obj => obj != null && obj.function == ShipSystem.SysFunction.Communications && obj.status != ShipSystem.SysStatus.Disabled);

		//For every active alert, we need to have someone assigned to handle it (if we have the available labor)
		List<Alert> alertsNeedingAttention = new List<Alert>();
		//Find active alerts
		foreach (Alert alert in allPossibleAlerts)
		{
			//Clear "Use" alerts when comms breaks
			if (!commsOn && alert.GetActivatedAlerts().Contains(AlertType.Use))
			{
				alert.EndAlert(AlertType.Use);
			}

			//All others get to stay and become assigned
			if (alert.GetVisibleAlerts().Count > 0)
			{
				alertsNeedingAttention.Add(alert);
			}
		}

		//Old WIP. Not sure where this was going
//		{
//			foreach (var t in alertsNeedingAttention)
//			{
//				if (t.GetActivatedAlerts().Contains(Alert.Alerts.Use))
//				{
//					t.EndAlert(Alert.Alerts.Use);
//				}
//			}
//		}

		//The list of unassigned characters should also be updated
		List<Character> activeCharacters = new List<Character>();
		//Throw out any active alerts found in active jobs, and find active characters.
		foreach (Job job in activeJobs)
		{
			//Alerts being handled can be ignored
			if (alertsNeedingAttention.Contains(job.alert))
			{
				alertsNeedingAttention.Remove(job.alert);
			}
			//Populate list of active characters
			if (job.character.bHand.target != null)	//New change. Might reduce lockup?
				activeCharacters.Add(job.character);
		}
		
		//Viola, by process of elimination, we have inactive characters...
		foreach (Character character in GameReference.r.allCharacters)
		{
			if (character.canAct && !activeCharacters.Contains(character) && !inactiveCharacters.Contains(character))
			{
				inactiveCharacters.Add(character);
				character.priority = 100;
			}
		}

		//If we need this safety check, it should be rewritten. Likely a cause of job leaks and character freezes
		//(Double check that we aren't recycling destroyed character objects! And by that, I mean just remove them)
//		inactiveCharacters.FindAll(obj => !GameReference.r.allCharacters.Contains(obj)).ForEach(ch => inactiveCharacters.Remove(ch));

		//...And can assign to unassigned alerts (once we figure out what kind of object the alert is attached to)
		foreach (Alert alert in alertsNeedingAttention)
		{

			//The thing and what we're doing to it
			Transform target = null;
			Character.Task task = Character.Task.Idle; //(Default init, for syntax)

			//The alert(s) signalled
			List<AlertType> requestedTasks = alert.GetVisibleAlerts();
			//Things it might be!
			ShipSystem ss = alert.GetComponentInParent<ShipSystem>();
			Character ch = alert.GetComponentInParent<Character>();

			//First handle alert types that prevent acting
			//Danger!
			if (requestedTasks.Contains(AlertType.Danger))
			{
				//TODO Do not approach!
			}
			//Ignore. Go straight to Ignore. Do not pass Go.
			else if (requestedTasks.Contains(AlertType.Ignore))
			{
				continue;
			}

			//Let's check specific types
			if (ss != null)
			{
				//It's a ship system!
				target = ss.transform;

				//What type of order is it?
				//Repair alarms
				if (requestedTasks.Exists(element => alarmsForFixing.Contains(element)))
				{
					//Construction subtype?
					if (ss.quality == ShipSystem.SysQuality.UnderConstruction)
					{
						task = Character.Task.Construction;
					}
					else
					{
						task = Character.Task.Repair;
					}
				}
				//Use the system!
				else if (requestedTasks.Contains(AlertType.Use))
				{
					//Is it the right kind of system?
					if (ss.isManualProduction && ss.status != ShipSystem.SysStatus.Disabled)
						task = Character.Task.Using;
					//Nope
					else
						alert.EndAlert(AlertType.Use);
				}
				else if (requestedTasks.Contains(AlertType.Salvage))
				{
					task = Character.Task.Salvage;
				}
			}
			else if (ch != null)
			{
				//It's a character! Give it medical aid!
				target = ch.transform;
				task = Character.Task.Heal;
			}
			else
			{
				//Well, that didn't work.
				Debug.Log("Alert System Response: Target object's type could not be resolved.");
			}

			//Let's assign!
			if (target != null)
			{
				Job job = SetTask(task, target);
				//If we assigned successfully, add it to our list!
				if (job != null)
				{
					activeJobs.Add(job);
				}
			}
		}

		//Finally, assign any remaining characters as idlers
		while (inactiveCharacters.Count > 0)
		{

			//First, if any character on inactiveCharacters is not in GameReference's allCharacters, let's reset inactiveCharacters
			if (inactiveCharacters.Exists(ch => !GameReference.r.allCharacters.Contains(ch)))
			{
				inactiveCharacters.Clear();
				//Try again after we repopulate inactiveCharacters next Update
				break;
			}

			//Assign
			Job job = SetTask(Character.Task.Idle, null);
			//Add to list when we assign
			if (job != null)
			{
				activeJobs.Add(job);
			}
			//Or get out if unable to assign further
			else
			{
				break;
			}
		}
	}

	/**Use a target to create a job.
	 * Returns the Job listing, if created successfully.
	 */
	private Job SetTask(Character.Task task, Transform target)
	{
		//Job team list to be assigned from
		List<Character> teamList = new List<Character>();
		//Character has been assigned or nah
		Job job = null;

		//Define teamList based on task
		if ((task == Character.Task.Repair && target.GetComponent<ShipSystem>().canAffordRepair))
		{
			teamList.AddRange(GameReference.r.allCharacters.FindAll(ch => ch.team == Character.Team.Engineering));
		}
		else if (task == Character.Task.Salvage)
		{
			teamList.AddRange(GameReference.r.allCharacters.FindAll(ch => ch.team == Character.Team.Engineering));
		}
		else if (task == Character.Task.Heal)
		{
			teamList.AddRange(GameReference.r.allCharacters.FindAll(ch => ch.team == Character.Team.Medical));
		}
		else if (task == Character.Task.Using)
		{
			teamList.AddRange(GameReference.r.allCharacters.FindAll(ch => ch.team == Character.Team.Science));
		}
		else if (task == Character.Task.Construction && target.GetComponent<ShipSystem>().canAffordConstruction)
		{
			teamList.AddRange(GameReference.r.allCharacters.FindAll(ch => ch.team == Character.Team.Science));
		}
		//Idle
		else if (task == Character.Task.Idle)
		{
			//Just idling. Assign an idler.
			teamList.AddRange(inactiveCharacters);
			//No inherent bonus priority for idling. Dig through Job.Idle() for specific instances.
		}

		//Assign the task to someone on the teamList!
		if ((job = AssignTask(teamList, task, target)) == null)
		{
			//Didn't assign? Change the teamList to the whole crew, then find someone
			teamList.Clear();
			teamList.AddRange(GameReference.r.allCharacters);
			//Reduce importance/ priority in this AssignTask
			job = AssignTask(teamList, task, target, 2);
		}

		//Signal if there's danger
		if (target != null && target.GetComponentInChildren<Alert>().GetVisibleAlerts().Contains(AlertType.Danger))
		{
			//TODO Warn characters
		}
		
		//Finally, return result
		return job;
	}

	/**Choose a character to assign to the target, based on a supplied pool of characters.
	 * Will also choose a character to idle, when not given a target.
	 */
	private Job AssignTask(List<Character> searchRange, Character.Task task, Transform target, int priorityPlus = 0)
	{
		//List of viable candidates. Will be sorted by distance.
		List<Character> candidates = new List<Character>();
		//Are we still searching?
		bool searching = true;
		//Priority of the task. Set while searching, but initialized for syntax.
		int taskPriority = 10;

		//First, we just have to choose any old schmuck that's inactive, if we're idling
		if (task == Character.Task.Idle && searchRange.Count > 0)
		{
			//Don't do a full search, even if this fails
			searching = false;
			//Define priority
			taskPriority = AlertPriority(searchRange [0], task, target, priorityPlus);
			//See if we can displace. If not, let this method run itself out (probably to null).
			if (searchRange [0].priority > taskPriority)
			{
				//Great! We have a candidate.
				candidates.Add(searchRange [0]);
			}
		}

		//Otherwise try and find an ideal candidate for our task
		if (searching)
		{
			foreach (Character schmuck in searchRange)
			{
				if (schmuck.status == Character.CharStatus.Good)
				{
					//Get the priority of the task for comparison
					taskPriority = AlertPriority(schmuck, task, target, priorityPlus);
					//Is this guy available?
					if (schmuck.priority > taskPriority && schmuck.task == Character.Task.Idle || schmuck.task == Character.Task.Maintenance)
					{
						//Is the target available?
						if (CanTarget(schmuck, taskPriority, target, true))
						{
							//Add him to the list of candidates!
							candidates.Add(schmuck);
							//Also, we've found at least one candidate, so we shouldn't search among the less desirables
							searching = false;
						}
					}
				}
			}
		}

		//Next try someone with a not-so-good status
		if (searching)
		{
			foreach (Character schmuck in searchRange)
			{
				if (schmuck.isControllable)
				{
					//Get the priority of the task for comparison
					taskPriority = AlertPriority(schmuck, task, target, priorityPlus);
					//Is this guy available?
					if (schmuck.priority > taskPriority && IdlePriority(schmuck) > taskPriority && schmuck.task == Character.Task.Idle || schmuck.task == Character.Task.Maintenance)
					{
						//Is the target available?
						if (CanTarget(schmuck, taskPriority, target, true))
						{
							//Add him to the list of candidates!
							candidates.Add(schmuck);
							//Also, we've found at least one candidate, so we shouldn't search among the less desirables
							searching = false;
						}
					}
				}
			}
		}

		//If we haven't found someone yet, let's open up to chars with lesser tasks already assigned
		if (searching)
		{
			foreach (Character schmuck in searchRange)
			{
				if (schmuck.isControllable)
				{
					//Get the priority of the task for comparison
					taskPriority = AlertPriority(schmuck, task, target, priorityPlus);
					//If he's doing a less important task (and not needing self care), we can replace it
					if (schmuck.priority > taskPriority && IdlePriority(schmuck) > taskPriority)
					{
						//Is the target available?
						if (CanTarget(schmuck, taskPriority, target, true))
						{
							//Add him to the list of candidates!
							candidates.Add(schmuck);
							//Also, we've found at least one candidate, so we shouldn't search among the less desirables
							searching = false;
						}
					}
				}
			}
		}

		//Let's process the candidates!
		if (candidates.Count > 0)
		{
			//The character that's currently closest
			Character closestCharacter = null;

			//Go through the candidates, if there's more than one
			if (candidates.Count > 1)
			{
				//The distance-to-beat. Useless temp value to initialize.
				float distance = 1000;
				//Search by distance
				foreach (Character schmuck in candidates)
				{
					//Temp distance value
					float d;
					//If this is the first character on the list, he's the best choice so far
					if (closestCharacter == null)
					{
						closestCharacter = schmuck;
						distance = closestCharacter.GetComponent<SimplePath>().DistanceCheck(target);
					}
					//Otherwise, let's compare and see if this one is closer
					else if ((d = schmuck.GetComponent<SimplePath>().DistanceCheck(target)) < distance)
					{
						closestCharacter = schmuck;
						distance = d;
					}
				}
			}
			//Or just use that one
			else
			{
				closestCharacter = candidates [0];
			}

			//Set any old job the character had to needs attention, unless it's idling
			BehaviorHandler bHand = closestCharacter.GetComponent<BehaviorHandler>();
			if (bHand.currentJob != null)
			{
				if (bHand.currentJob.task != Character.Task.Idle && bHand.currentJob.task != Character.Task.Maintenance)
					bHand.currentJob.EndTask(true);
				else
					bHand.currentJob.EndTask(false);
			}

			//Remove the character from inactiveCharacters
			inactiveCharacters.Remove(closestCharacter);
			//Create the open job listing
			Job job = new Job(closestCharacter, task, target, taskPriority);
			//We're done. Send it off.
			return job;
		}

		//No candidates! Everyone must all be toooo busy! (dying)
		return null;
	}


	/*
	 * Utility Methods
	 */

	/**What is the priority on this target?
	 * Returns an int. Lower is more important!
	 */
	private int AlertPriority(Character character, Character.Task task, Transform target, int priorityPlus)
	{
		int priority;

		//Is it an idle task?
		if (task == Character.Task.Idle)
		{
			priority = IdlePriority(character);
		}
		//Maintenance is just idle repair
		else if (task == Character.Task.Maintenance)
		{
			priority = 8;

			//Priority increased for non-self-care, if military
			if (character.roles.Contains(Character.CharRoles.Military))
				priority -= 2;
		}
		//Or is there an actual alert?
		else
		{
			List<AlertType> alertStatus = target.GetComponentInChildren<Alert>().GetVisibleAlerts();
			if (alertStatus.Contains(AlertType.Emergency))
			{
				priority = 2;
			}
			//Current peak of IdlePriority is 3
			else if (alertStatus.Contains(AlertType.Alert))
			{
				priority = 3;	//Unused
			}
			else if (alertStatus.Contains(AlertType.Use))
			{
				priority = 8;
			}
			//All standard, non-idle behavior falls in here (warning, research, etc.)
			else
			{
				priority = 7;
			}

			//Priority increased for non-self-care, if military
			if (character.roles.Contains(Character.CharRoles.Military))
				priority -= 2;
		}

		//Adjust for off-target tasking
		priority += priorityPlus;

		return priority;
	}

	/**Check if we can target this transform (characters with shared targets must face-off; only one will be valid. If displace is true, this will kick off the other!).
	 * Helps prevent space-sharing by characters.
	 * Returns whether or not we can assign the target, regardless if we have to override (true means can assign).
	 */
	public bool CanTarget(Character character, int priority, Transform target, bool displace)
	{
		//Gotta have a valid target (non-null, and not self)
		if (target == null || target == character.transform)
		{
			return false;
		}
		//Go ahead and return true if there's only this character (no possible conflict)
		if (GameReference.r.allCharacters.Count < 1)
			return true;
		//We're gonna check all other characters' current targets and priorities against ours
		foreach (Character ch in GameReference.r.allCharacters)
		{
			//Ignore this characer
			if (ch == character)
				continue;
			//lastProcessedTarget (what the character is currently targeting)
			Transform lpt = ch.sPath.lastProcessedTarget;
			//Is this character already assigned to this target?
			if (lpt != null && lpt == target)
			{
				//Conflict! Is it's priority just as stronk as the priority we're trying to assign?
				if (ch.priority <= priority)
				{
					//Back off
					return false;
				}
				else
				{
					//Displace
					if (displace && ch.bHand.currentJob != null)
						ch.bHand.currentJob.EndTask(true);
					return true;
				}
			}
		}
		//If we get here, we didn't find any conflicts. We can assign.
		return true;
	}

	/**How important is it that this character be allowed to idle?
	 */
	public int IdlePriority(Character c)
	{	
		//"Minimum" priority to break idle tasking = 10. Becomes more significant as the character is in more dire condition (needs more personal time)
		int ip = 10;

		//Any stress
		if (c.hasStress)
			ip--;
		//Not good
		if (c.status != Character.CharStatus.Good)
			ip--;
		//Injured
		if (c.injured)
			ip -= 2;
		//Needs
		if (c.waste > c.wasteResilience)
			ip--;
		if (c.hunger > c.hungerResilience)
			ip--;
		if (c.sleepiness > c.sleepinessResilience)
			ip--;

		return ip;
	}

	/** A character is no longer performing a job. The job is done, for better or worse!
	 */
	public void JobShutdown(Job job)
	{
		//The character is done with the task!
		if (!inactiveCharacters.Contains(job.character) && job.character.canAct)
			inactiveCharacters.Add(job.character);

		//Debug.Log("activeJobs.Count = " + activeJobs.Count);
		activeJobs.Remove(job);

		//Also double-check that this character isn't listed in other jobs that somehow didn't get shutdown
		//TODO WHERE ARE THESE COMING FROM?!! FIND THE LEAK
		List<Job> dupeJobs = new List<Job>();
		foreach (Job j in activeJobs)
		{
			if (j.character == job.character)
				dupeJobs.Add(j);
		}
		foreach (Job j in dupeJobs)
		{
			activeJobs.Remove(j);
		}

		//Debug.Log(job.character.name + "\'s job(s) removed. New count = " + activeJobs.Count);
	}

	/**Helper, overload method.
	 * Identify job(s) involving given character. Shut down.
	 */
	public void JobShutdown(Character character)
	{
		//Char has no current task
		character.priority = 100;

		//Find a job involving this character
		Job targetToShutDown = null;

		foreach (Job j in activeJobs)
		{
			if (j.character == character)
			{
				targetToShutDown = j;
				break;
			}
		}
		//Shut it (and it's compatriots) down
		if (targetToShutDown != null)
			JobShutdown(targetToShutDown);
	}



	void Awake()
	{
		if (ja == null)
		{
			ja = this;
		}
		else if (ja != this)
		{
			Destroy(this);
		}
	}
}
