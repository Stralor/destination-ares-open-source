using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BehaviorHandler : MonoBehaviour
{

	/* Attached to a character. Handle's that character's behavior for jobs and stress. */

	private Character me; //The attached character component
	public Job currentJob = null; //Character's current job
	public Character.Task task; //The character's task
	public Transform target; //The object targeted
	public Transform lastTarget; //Last object targeted
	public bool arrived; //At the task location
	private float startingProgress; //Any bonus to total progress (often based on target's current state)
	private float progress; //Progress in finishing task (repairs, healing, work, etc.)
	private int bonus; //Bonus stats for future tries
	private float speed; //Character's tick time, aka delays between actions
	public float baseSpeed; //The standard speed for characters
	public int highStress = 3; //Stress value of a high stress event
	public int medStress = 2; //Stress value of a medium stress event
	public int lowStress = 1; //Stress value of a low stress event
	private int skillRollMax = 3; //Number of sides on skill roll dice (there are two) (starts at 0)
	private Character.CharSkill skillUsed; //The skill at hand for the given task

	private int chancesToNotCritFail = 4; //Increase this to reduce incidence of crit fails
	
	private const float SkillAssistModifier = 0.5f;

	//Progress bar for tasking
	public UnityEngine.UI.Image progressBar;
	bool progressBarFade;

	//Properties and dynamic fields
	public bool isWorking { get; private set; }

	public bool isSleeping { get; private set; }

	public bool isExercising { get; private set; }

	public bool isEating { get; private set; }

	private int dChance = 2;
	//Inverse chance of destress (higher value means lower chance). (1/3)
	private bool destressChance
	{		//Random check against dChance
		get
		{
			if (Random.Range(0, dChance) == 0)
			{
				return true;
			}
			else
				return false;
		}
	}


	void Start()
	{
		me = GetComponent<Character>();
		ClearStatusBools();

		speed = baseSpeed;
	}

	/**Do a round of sleep! Value is how much sleepiness to get rid of.
	 * Changes character's thought. Please be sure to declare currentThoughtTargetName
	 */
	public void Sleep(float value, bool allowDestress = true)
	{
		//Sleeping doesn't require any resource use.
		me.currentThought = Character.Thought.Sleeping;
		var sleepinessReduction = value * me.sleepinessResilience;
		me.sleepiness = (me.sleepiness - sleepinessReduction) >= 0 ? (me.sleepiness - sleepinessReduction) : 0;
		//DeStress ever so slowly
		if (destressChance && destressChance)
			me.DeStress(ClampMax(value, lowStress));

	}

	public void Eat(float value, bool allowDestress = true)
	{
		//Thought change
		me.currentThought = Character.Thought.Eating;

		//Get rid of a bunch of hunger, based on kitchen's effectiveness!
		me.hunger = me.hunger - (value * me.hungerResilience / 2);
		//But overeating isn't as effective
		me.hunger = me.hunger < 0 ? me.hunger / 2 : me.hunger;

		StatTrack.stats.foodEaten++;

		//Give the character some waste.
		me.waste++;

		//Maybe DeStress
		if (destressChance)
			me.DeStress(ClampMax(value / 8, lowStress));
	}

	void Update()
	{
		//Have an enabled progress bar and not fading it? Let's update it
		if (progressBar.isActiveAndEnabled && !progressBarFade)
		{
			//Current progress
			progressBar.fillAmount = (startingProgress + progress) / 100;
			//Make sure it's visible
			progressBar.CrossFadeAlpha(1, 0, true);
			//Base color
			progressBar.color = ColorPalette.cp.gry2;
		}
	}


	/** Signal start of task. Called by pathfinder. */
	public void Arrived()
	{

		//Confirm that we can act on this
		if (!JobAssignment.ja.CanTarget(me, me.priority, target, true))
		{
			//Otherwise, get out!
			CallEndTask(true);
			return;
		}

		//We're there!
		arrived = true;
		bonus = 0;
		progress = 0;
		startingProgress = 0;

		//Do work
		Invoke("HandleIt", speed);
		SetStatusBools();
	}

	/**Handle task logic. End job when finished.
	 * This is called whenever a character arrives at a target and is recursive for when a task is not yet done.
	 * Type of work done is determined by target's values and character's assigned task category.
	 * (e.g., fixing a kitchen when task "Repair", eating when task "Idle")
	 * INVOKE THIS (using 'speed')*/
	void HandleIt()
	{

		//We need to be at the task
		if (!arrived)
		{
			//This might come up if HandleIt was invoked on a different target, but the task was changed
			Debug.Log(name + " has not arrived at the target. HandleIt() cannot commence.");
			return;
		}

		//Can the character still act? If not, we need to cancel this. 
		if (!me.canAct)
		{
			Debug.Log(name + " cannot act. HandleIt() cancelled.");
			CallEndTask(true);
			return;
		}

		//Show progress bar
		progressBar.enabled = true;

		var skillAssist = 1f;
		switch (PlayerPrefs.GetInt("CrewSkill"))
		{
			case 0:
				break;
			case 1:
				skillAssist += SkillAssistModifier;
				break;
			case 2:
				skillAssist += SkillAssistModifier / 2;
				break;
		}
		

		//Task types

		/*
		 * Idle
		 */
		if (task == Character.Task.Idle)
		{
			/* NOTE: Most idle actions (e.g. Sleep) won't change the character's lastTaskType. Others (i.e., Gym -> Mechnical) might.
			 * This may complicate things. Remember to change lastTaskType when appropriate. */

			//Get target type to determine what we're idling TODO SOCIAL UPDATE character if idling on characters
			ShipSystem sys = null;
			if (target != null)
				sys = target.GetComponent<ShipSystem>();
			//Get a target if none actually established. AKA, If taskless, give task. Or, break when system cannot be used. We were wandering.
			if (sys == null || sys.status == ShipSystem.SysStatus.Disabled || target == null)
			{
				//We're done
				if (sys != null)
					Debug.Log(me.name + " cannot idle on an invalid target.");	//IdlingTargets are valid, but would otherwise trip this!
				else if (Random.Range(0, 4) == 0)
				{
					//Wandering is a light way to have a chance at destressing
					me.DeStressCheck(false);
				}
				CallEndTask(false);
				return;
			}

			//Sleeping?
			if (sys.function == ShipSystem.SysFunction.Bed)
			{
				//Thought change
				me.currentThoughtTargetName = "the " + sys.name;

				//Get rid of sleepiness equal to the bed's raw effectiveness
				float output = sys.Use();

				//Do action
				Sleep(output * Character.BaseSleepRate * skillAssist);

				//Repeat or check for task end as required
				CheckTaskEnd(() => me.sleepiness <= 0);

				//Done
				return;
			}

			//Eating?
			if (sys.function == ShipSystem.SysFunction.Kitchen)
			{
				//Eating requires energy and food
				if (!ShipResources.res.SetEnergy(ShipResources.res.energy - 1, transform) || !ShipResources.res.SetFood(ShipResources.res.food - 1, transform))
				{
					//Can't do this
					CallEndTask(false);
					//The system lied. Shut it down.
					sys.DisableFromLackOfResources();
					return;
				}

				//Thought change
				me.currentThoughtTargetName = "the " + sys.name;

				//Get the kitchen's effectiveness as base for eating
				float output = sys.Use();

				//Eat once
				Eat(output * skillAssist);

				//And again? (for free energy!)
				if (ShipResources.res.SetFood(ShipResources.res.food - 1))
					Eat(output * skillAssist);

				//Repeat or check for task end as required
				CheckTaskEnd(() => me.hunger <= 0);

				//Done
				return;
			}

			//Working Out?
			if (sys.function == ShipSystem.SysFunction.Gym)
			{
				//Working out doesn't require resources
				//Thought change
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Exercising;
				//DeStress like a boss
				float output = sys.Use();
				if (destressChance)
					me.DeStress(output * 1.5f * skillAssist);
				//Set lastTaskType
				me.lastTaskType = Character.CharSkill.Mechanical;

				//Repeat or check for task end as required
				CheckTaskEnd(() => !me.hasStress);

				//Done
				return;
			}

			//Using Restroom?
			if (sys.function == ShipSystem.SysFunction.Toilet)
			{
				//We're creating waste, whether we want to or not!
				if (!ShipResources.res.SetWaste(ShipResources.res.waste + 1, transform))
					Debug.Log("Waste has spilled over! Hey Pat, add consequences!");	//TODO waste overflow consequences
				//Thought change
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Wandering;
				float output = sys.Use();
				//Do a bit of destress, based on toilet output
				if (destressChance)
					me.DeStress(output * skillAssist);
				//Reduce character's internal waste.
				//Importantly, always reduce waste by 1, as it correlates to food consumed (and thus ship waste produced) and not to hunger that was sated.
				me.waste = (me.waste - 1) >= 0 ? (me.waste - 1) : 0;

				//Repeat or check for task end as required
				CheckTaskEnd(() => me.waste <= 0);

				//Done
				return;
			}
		}
		
		/*
		 * Repair and Maintenance
		 */
		if (task == Character.Task.Repair || task == Character.Task.Maintenance)
		{

			//Set the skill to be used
			skillUsed = Character.CharSkill.Mechanical;
			//Set character's last task type, so we know what kind of damage to do (MWUAHAHAHA)
			me.lastTaskType = skillUsed;
			//ShipSystem Objects are targets of repair
			ShipSystem sys = target.GetComponent<ShipSystem>();

			//Safety
			if (sys == null)
			{
				Debug.Log(me.name + "'s repair target is null. Exiting behavior.");
				CallEndTask(false);
				return;
			}

			//Always clear any hits just for showing up
			sys.ClearHits();

			//We need to figure out how much to fix it
			switch (sys.condition)
			{
				//Started at the bottom
				case ShipSystem.SysCondition.Broken:
					startingProgress = 0;
					break;
				//Maybe we're here
				case ShipSystem.SysCondition.Strained:
					startingProgress = 50;
					break;
				//Nothing more to do on functionals
				case ShipSystem.SysCondition.Functional:
					CallEndTask(false);
					//Completely leave the method. Do not pass Go.
					return;
				//Nothing to do for Destroyed sytems
				case ShipSystem.SysCondition.Destroyed:
					me.currentThoughtTargetName = "the " + sys.name;
					me.currentThought = Character.Thought.TargetDestroyed;
					CallEndTask(false);
					return;
			}

			//Time to roll. Used in skill, and crit fail check
			int roll = Random.Range(0, skillRollMax) + Random.Range(0, skillRollMax);
			//Skill check value
			int skill = roll + me.GetSkill(skillUsed, target) + bonus;
			skill = (int) (skill * skillAssist);
			//CRIT FAIL?!
			if (roll == 0 && Random.Range(0, (int)(chancesToNotCritFail * skillAssist)) == 0)
			{
				//BREAK IT
				sys.Break(true);
				//Thought change!
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.BrokeSomething;
				//Tell the console (for now). TODO UI UPDATE Tell the player in game.
				Debug.Log(me.name + " further damaged the " + sys.name + " while trying to repair it!");
				//This is all very stressful
				me.result = "Caught in Machine";
				me.Stress(medStress);
				//End the job, idiot. It will probably just be reassigned back.
				CallEndTask(false);
				//Update Progress Bar
				StartCoroutine(FadeProgressBar(false));
				//Don't continue.
				return;
			}
			//Or calculate result
			else
			{
				//Failure
				if (roll == 0)
				{
					//On the wrong foot. At least you don't need to start over!
					progress -= 5;	//OLD: -10
					sys.Damage();
					me.result = "Caught in Machine";
					me.Stress(lowStress);
				}
				//Doing more harm than good.
				else if (skill <= 1)
				{
					//progress -= 5;  //OLD: -5
					sys.DurabilityCheck(true);
					if (Random.Range(0, 2) == 0)
						me.Stress(lowStress);
					//Worst part of this, the character is now more confused.
					bonus = bonus - 1 >= 0 ? bonus - 1 : 0;
				}
				//Stumped, but trying
				else if (skill == 2)
				{
					//Early degrade. Possibly means multiple degrades, but probably not.
					sys.DurabilityCheck(false);
					//Not much progress, but take a bonus for the next try
					//progress += 5;  //OLD: 5
					bonus += 1;
				}
				//Some progress!
				else if (skill == 3)
				{
					progress += 5;	//OLD: 10
				}
				//Great progress! In the flow!
				else
				{
					progress += 15;
					bonus += 1;
				}
			}

			//Repair if we can!
			if (progress >= 50)
			{
				//Nonstandard!
				if (sys.keyCheck(ShipSystem.SysKeyword.Nonstandard) && !ShipResources.res.SetMaterials(ShipResources.res.materials - 1, transform))
				{
					CallEndTask(false);
					return;
				}

				//Consuming parts!
				else if (!ShipResources.res.SetParts(ShipResources.res.parts - 1, transform))
				{
					CallEndTask(false);
					return;
				}

				//Was our last hit an extra good one?
				//No!
				if (skill - bonus < 5)
				{
					//That component was fixed a step!
					sys.Repair(false);
				}
				//Yes!
				else
				{
					//Super success! Improve the component.
					sys.Repair(true);
					//Awesome job. Feel great.
					me.DeStress(lowStress);
				}
			}

			//Clean Up
			if (CheckTaskEnd(() => startingProgress + progress >= 100))
			{
				//Thoughts
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Success;

				//Update Progress Bar
				StartCoroutine(FadeProgressBar(true));
			}
			else
			{
				//Remove some progress if we repaired
				if (progress >= 50)
				{
					progress -= 50;
					startingProgress += 50;
				}
			}

			//Done
			return;
		}

		/*
		 * Medical and Psych
		 */
		if (task == Character.Task.Heal)
		{
			//Target character
			Character ch = target.GetComponent<Character>();

			//Safety
			if (ch == null)
			{
				Debug.Log(me.name + "'s heal target is null. Exiting behavior.");
				CallEndTask(false);
				return;
			}

			//Set skill to be used, determined by target condition. Also determine target's lastTaskType!
			bool doPhysicalWork;
			//Physical
			if (ch.statusIsMedical)
			{
				me.lastTaskType = skillUsed = Character.CharSkill.Science;
				ch.lastTaskType = Character.CharSkill.Mechanical;
				doPhysicalWork = true;
			}
			//Psychological
			else if (ch.statusIsPsychological && ch.status != Character.CharStatus.Stressed)
			{
				me.lastTaskType = skillUsed = Character.CharSkill.Science;
				ch.lastTaskType = Character.CharSkill.Command;
				doPhysicalWork = false;
			}
			//Good, Stressed, and Dead
			else
			{
				if (ch.status == Character.CharStatus.Dead)
				{
					me.currentThoughtTargetName = ch.name;
					me.currentThought = Character.Thought.TargetDestroyed;
				}
				CallEndTask(false);
				return;
			}

			//If we haven't returned, we need to interrupt the other character so they don't run around on us
			ch.sPath.Interrupt();
			//Based on skill used, we'll do work
			//Time to roll. Used in skill, and crit fail check
			int roll = Random.Range(0, skillRollMax) + Random.Range(0, skillRollMax);
			//Skill check value
			int skill = roll + me.GetSkill(skillUsed, target) + bonus;
			skill = (int) (skill * skillAssist);
			//CRIT FAIL?!
			if (roll == 0 && Random.Range(0, (int)(chancesToNotCritFail * skillAssist)) == 0)
			{
				//HURT IT MORE
				//ch.Damage();
				if (ch.statusIsMedical)
					ch.result = "Treatment was Botched";
				else if (ch.statusIsPsychological)
					ch.result = "Overdose";
				ch.Stress(highStress);
				ch.StressCheck(true);
				//Thought change!
				me.currentThoughtTargetName = ch.name;
				me.currentThought = Character.Thought.HurtSomeone;
				//Tell the console (for now)
				Debug.Log(me.name + " further damaged " + ch.name + "!");
				//This is all very stressful
				me.result = "Committed Suicide";
				me.Stress(highStress);
				//Update Progress Bar
				StartCoroutine(FadeProgressBar(false));
			}
			//Or calculate result
			else
			{
				//Failure
				if (skill <= 0)
				{
					ch.Stress(lowStress);
					ch.StressCheck(true);
					me.Stress(lowStress);
					progress -= 5;	//OLD: 0
				}
				//Bad show
				if (skill == 1)
				{
					if (Random.Range(0, 2) == 0)
						me.Stress(lowStress);
					//Worst part of this, the character is now more confused.
					bonus = bonus - 1 >= 0 ? bonus - 1 : 0;
				}
				//Stumped, but trying
				else if (skill == 2)
				{
					//Little progress, but take a bonus for the next try
					progress += 5;	//OLD: 7
					bonus += 1;
				}
				//Success!
				else if (skill == 3)
				{
					progress += 10;	//OLD: 13
					bonus += 1;
				}
				//Great job!
				else
				{
					progress += 25;	//OLD: 20
					bonus += 1;
				}
			}

			//Heal if we can!
			if (progress >= 100)
			{
				//Update Progress Bar
				StartCoroutine(FadeProgressBar(true));

				//Was the last hit an extra good one?
				//No
				if (skill - bonus < 5)
				{
					//Heal a step!
					if (doPhysicalWork)
						ch.Heal();
					//Destress. If we're a psychologist, cut out the virtual stress
					else
						ch.DeStress(medStress, me.roles.Contains(Character.CharRoles.Psychologist));
				}
				//Yes
				else
				{
					//Super success!
					if (doPhysicalWork)
						ch.Heal();
					else
						ch.Improve();
					//Awesome job. Everyone feel great. Even ignore ch's virtual stress if we're a psychologist.
					ch.DeStressCheck(me.roles.Contains(Character.CharRoles.Psychologist));
					me.DeStressCheck();
				}

				//Clear the progress, in case we still need to do more
				progress -= 100;
			}
			
			//Clean up if there's not more of the same work to do
			if (CheckTaskEnd(() => !((ch.statusIsMedical && doPhysicalWork)
			    || (ch.statusIsPsychological && ch.status != Character.CharStatus.Stressed && !doPhysicalWork))))
			{
				me.currentThoughtTargetName = ch.name;
				me.currentThought = Character.Thought.Success;
				ch.sPath.EndInterrupt();
				ch.GetComponent<BehaviorHandler>().CallEndTask(false);
			}

			//Done
			return;
		}

		/*
		 * Processing
		 */
		if (task == Character.Task.Using)
		{
			//ShipSystem Objects are targets of processing
			ShipSystem sys = target.GetComponent<ShipSystem>();

			//Safety
			if (sys == null)
			{
				Debug.Log(me.name + "'s system target is null. Exiting behavior.");
				CallEndTask(false);
				return;
			}

			//Double check that we can use this system
			if (sys.status == ShipSystem.SysStatus.Disabled)
			{
				//We can't process on a disabled system
				CallEndTask(false);
				return;
			}

			//Are we doing the right thing here?
			if (!sys.isManualProduction)
			{
				//No, this can't be used like that. Change task?
				CallEndTask(false);
				return;
			}

			//Set the skill to be used
			skillUsed = Character.CharSkill.Science;
			//Set character's last task type, so we know what kind of damage to do (MWUAHAHAHA)
			me.lastTaskType = skillUsed;

			//Time to roll. Used in skill, and crit fail check
			int roll = Random.Range(0, skillRollMax) + Random.Range(0, skillRollMax);
			//Skill check value
			int skill = roll + me.GetSkill(skillUsed, target) + bonus;
			skill = (int) (skill * skillAssist);
			//CRIT FAIL?!
			if (roll == 0 && Random.Range(0, (int)(chancesToNotCritFail * skillAssist)) == 0)
			{
				//Damage the system
				sys.Damage();
				//Change thought
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Failure;
				Debug.Log(me.name + " damaged the " + sys.name + " while trying to use it.");
				//Add some stress!
				me.result = "Electrocuted";
				me.Stress(lowStress);
				//End the job, idiot. It will probably just be reassigned back.
				CallEndTask(false);
				//Update Progress Bar
				StartCoroutine(FadeProgressBar(false));
				//Don't continue.
				return;
			}
			//Or calculate result
			else
			{
				float output = sys.Use();	//Gonna affect progress rate

				//Fail
				if (skill <= 0)
				{
					//Stress, maybe
					if (Random.Range(0, 2) == 0)
					{
						me.result = "Electrocuted";
						me.Stress(lowStress);
					}
					//DuraCheck sys
					sys.DurabilityCheck(true);
					progress -= 5;	//OLD: 0
				}
				//Stumbling
				else if (skill == 1)
				{
					//Bonus
					bonus += 1;
				}
				//Success
				else if (skill == 2)
				{
					//Progress and bonus!
					if (sys.outputIncreasesSpeed)
						progress += (int)(10 * output); //OLD: 10 * output
					else
						progress += (int)(10 / output);
					bonus += 1;
				}
				//Good Success
				else if (skill == 3)
				{
					//Nice progress
					if (sys.outputIncreasesSpeed)
						progress += (int)(15 * output); //OLD: 20 * output
					else
						progress += (int)(15 / output);
				}
				//Great success
				else
				{
					//Awesome progress
					if (sys.outputIncreasesSpeed)
						progress += (int)(30 * output); //OLD: 35 * output
					else
						progress += (int)(30 / output);
				}
			}

			if (progress >= 100)
			{
				//Do it! Now hope we have the resources.
				sys.Process();
			}
						

			//Clean up
			if (CheckTaskEnd(() => startingProgress + progress >= 100))
			{
				//Thoughts GOT ANNOYING ON REPEAT 'USE'
//				me.currentThoughtTargetName = "the " + sys.name;
//				me.currentThought = Character.Thought.Success;

				//Update Progress Bar
				StartCoroutine(FadeProgressBar(true));
			}

			//Done
			return;
		}
		
		/*
		 * Construction
		 */
		if (task == Character.Task.Construction)
		{
			//ShipSystem Objects are targets of construction
			ShipSystem sys = target.GetComponent<ShipSystem>();
			
			if (sys == null)
			{
				Debug.Log(me.name + "'s system target is null. Exiting behavior.");
				CallEndTask(false);
				return;
			}
			
			if (sys.quality != ShipSystem.SysQuality.UnderConstruction)
			{
				Debug.Log($"{me.name}'s system target does not need constructing. Exiting behavior.");
				CallEndTask(false);
				return;
			}
			
			skillUsed = Character.CharSkill.Science;
			me.lastTaskType = skillUsed;
			
			int roll = Random.Range(0, skillRollMax) + Random.Range(0, skillRollMax);
			int skill = roll + me.GetSkill(skillUsed, target) + bonus;
			skill = (int) (skill * skillAssist);

			if (roll == 0 && Random.Range(0, (int) (chancesToNotCritFail * skillAssist)) == 0)
			{
				sys.DurabilityCheck(true);

				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Failure;
				Debug.Log(me.name + " damaged the " + sys.name + " while trying to build it.");

				me.result = "Caught in Machine";
				me.Stress(lowStress);
				
				CallEndTask(false);
				StartCoroutine(FadeProgressBar(false));

				return;
			}
			else
			{
				if (roll == 0)
				{
					progress -= 5;
					me.result = "Caught in Machine";
					if (Random.Range(0, 2) == 0)
						me.Stress(lowStress);
					bonus -= 1;
				}
				else if (skill <= 1)
				{
					bonus += 1;
				}
				else if (skill == 2)
				{
					progress += 5;
					bonus += 1;
				}
				else
				{
					progress += 10;
				}
			}

			//Consuming Resources
			if (progress >= 40)
			{
				var systemValue = Customization_CurrencyController.GetMaterialsCost(sys);
				if (!ShipResources.res.SetMaterials(ShipResources.res.materials - systemValue / 2, transform))
				{
					CallEndTask(false);
					return;
				}
			}

			//Clean Up
			if (CheckTaskEnd(() => startingProgress + progress >= 100))
			{
				//Thoughts
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Success;

				//Update Progress Bar
				StartCoroutine(FadeProgressBar(true));

				sys.Construct();
			}
			else
			{
				if (progress >= 40)
				{
					progress -= 40;
					startingProgress += 40;
				}
			}

			//Done
			return;
		}
		
		/*
		 * Salvage
		 */
		if (task == Character.Task.Salvage)
		{
			//ShipSystem Objects are targets of construction
			ShipSystem sys = target.GetComponent<ShipSystem>();
			
			if (sys == null)
			{
				Debug.Log(me.name + "'s system target is null. Exiting behavior.");
				CallEndTask(false);
				return;
			}

			skillUsed = Character.CharSkill.Mechanical;
			me.lastTaskType = skillUsed;
			
			int roll = Random.Range(0, skillRollMax) + Random.Range(0, skillRollMax);
			int skill = roll + me.GetSkill(skillUsed, target) + bonus;
			skill = (int) (skill * skillAssist);

			var finalResourcesModifier = 1f;
			
			if (roll == 0 && Random.Range(0, (int) (chancesToNotCritFail * skillAssist)) == 0)
			{
				sys.Break(false);

				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Failure;
				Debug.Log(me.name + " hurt themselves while trying to salvage the " + sys.name);

				me.result = "Crushed by Parts";
				me.Stress(medStress);
				
				CallEndTask(false);
				StartCoroutine(FadeProgressBar(false));

				return;
			}
			else
			{
				if (roll == 0)
				{
					progress -= 5;
					me.result = "Crushed by Parts";
					me.Stress(lowStress);
					bonus -= 1;
					finalResourcesModifier *= 0.9f;
				}
				else if (skill <= 0)
				{
					progress -= 5;
					bonus += 1;
					finalResourcesModifier *= 0.9f;
				}
				else if (skill <= 2)
				{
					progress += 10;
				}
				else
				{
					progress += 15;
					bonus += 1;
					finalResourcesModifier *= 1.1f;
				}
			}

			//Clean Up
			if (CheckTaskEnd(() => startingProgress + progress >= 100))
			{
				//Gain resources
				finalResourcesModifier = Mathf.Min(finalResourcesModifier, 1f);
				var partsValue =
					Mathf.FloorToInt(Customization_CurrencyController.GetPartsCost(sys) * finalResourcesModifier);
				var materialsValue = -1 + //reduced to make sure construction > salvage
					Mathf.FloorToInt(Customization_CurrencyController.GetMaterialsCost(sys) * finalResourcesModifier);

				ShipResources.res.SetParts(ShipResources.res.parts + partsValue, transform);
				if (sys.quality != ShipSystem.SysQuality.UnderConstruction)
				{
					ShipResources.res.SetMaterials(ShipResources.res.materials + materialsValue, transform);
				}

				//Thoughts
				me.currentThoughtTargetName = "the " + sys.name;
				me.currentThought = Character.Thought.Success;

				//Update Progress Bar
				StartCoroutine(FadeProgressBar(true));

				sys.Salvage();
			}

			//Done
			return;
		}


		//Close down the task if nothing was done SAFETY NET
		Debug.Log(name + " reached the end of HandleIt. Ending task.");
		CallEndTask(false);
	}


	/**Reducing verbosity of HandleIt(), yet still maintaining wonderful modularity and recursion!
	 * Call this after handling a task, and provide the condition for task end.
	 * Returns if it's ending, if you care.
	 */
	bool CheckTaskEnd(System.Func<bool> condition)
	{
		//Are we done?
		if (condition.Invoke())
		{
			//Task finished
			CallEndTask(false);

			return true;
		}
		else
		{
			//Keep going!
			Invoke("HandleIt", speed);

			return false;
		}
	}

	/**Flash a success or fail color then fade out on progress bar.
	 */
	IEnumerator FadeProgressBar(bool success)
	{
		//Fading
		progressBarFade = true;
	
		//Values
		float colorTime = 0.4f;
		float fadeTime = 0.2f;

		//Success?
		if (success)
		{
			//Full bar
			progressBar.fillAmount = 1;
			//Color
			progressBar.color = ColorPalette.cp.wht;
		}
		//Fail
		else
		{
			//At least some bar
			if (progressBar.fillAmount < 0.5f)
				progressBar.fillAmount = 0.5f;
			//Color
			progressBar.color = ColorPalette.cp.red3;
		}

		//Wait for color a bit
		yield return new WaitForSeconds(colorTime);

		//Fade out
		progressBar.CrossFadeAlpha(0, fadeTime, false);

		//Wait for fade out
		yield return new WaitForSeconds(fadeTime);

		//Close
		StartCoroutine(CloseProgressBar(0));
	}

	/**Closes the progress bar on screen.
	 */
	public IEnumerator CloseProgressBar(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);

		progressBar.enabled = false;
		progressBarFade = false;
	}

	/**Clamp a variable value to a maximum. Often used for limiting a system's output effect on stress changes.
	 * (e.g. me.DeStress(ClampMax(output, lowStress)) )
	 */
	float ClampMax(float value, float max)
	{
		return value < max ? value : max;
	}

	/**We're done with the job. Let's clear it out, tell the job.
	 * Just forwards an EndTask call to the currentJob.
	 */
	public void CallEndTask(bool reset)
	{
		//DON'T PUT ANYTHING ELSE IN HERE

		//Put anything you need called in Job.EndTask

		//Done doing work
		if (currentJob != null)
			currentJob.EndTask(reset);
		//This is basically a catch. We still need to shut any jobs involving this character down!
		else
			JobAssignment.ja.JobShutdown(me);
	}

	/**Sets the proper values for the status bools based on current activity.
	 */
	public void SetStatusBools()
	{
		if (task != Character.Task.Idle)
			isWorking = true;
		else if (target != null && target.GetComponent<ShipSystem>() != null)
		{
			switch (target.GetComponent<ShipSystem>().function)
			{
			case ShipSystem.SysFunction.Bed:
				isSleeping = true;
				break;
			case ShipSystem.SysFunction.Kitchen:
				isEating = true;
				break;
			case ShipSystem.SysFunction.Gym:
				isExercising = true;
				break;
			default :
				break;
			}
		}
	}

	/**Sets all those activities (isWorking, isEating, etc.) to false.
	 */
	public void ClearStatusBools()
	{
		isWorking = isSleeping = isExercising = isEating = false;
	}
}
