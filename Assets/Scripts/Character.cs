using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


public class Character : MonoBehaviour
{

	/* This class contains the values of a character. Each character has one.
	 * Requires that a TaskHandler and SimplePath also be attached.
	 */

	//Define enums
	public enum CharStatus
	{
		//Ideal
		Good,
		//At risk, if pushed. -Interpersonal (An intermediate status)
		Stressed,
		//Counts as stressed. -Mechanical (& -Interpersonal)
		Injured,
		//Uncontrollable. May injure or kill others.
		Psychotic,
		//Uncontrollable. Stressed, needs calming. No actions.
		Restrained,
		//Uncontrollable. May be at risk of death. No actions.
		Unconscious,
		//Welp, best do something with the body.
		Dead
	}

	//The three skill checks
	public enum CharSkill
	{
		//Bonus to research, advanced components, etc.
		Science,
		//Bonus to minigame time and AI energy expenditure
		Command,
		//Bonus to repairs, construction, etc.
		Mechanical
	}

	public enum CharRoles
	{
		//Free, will not automatically do work
		Affluent,
		//Will not become psychotic (or maybe reduced stress test?)
		Captain,
		//+2Sci on other crew
		Doctor,
		//+Sci and +Mech for flight components
		Pilot,
		//Free, occasionally reduced skill
		Prisoner,
		//Improves Research
		//Professor,
		//When involved, stress penalties are ignored
		Psychologist,
		//Free, lowered stress resilience
		Refugee,
		//Ignores loneliness stress
		Hermit,
		//Free, increased task priorities
		Military,
		//+2Mech for components that consume energy
		Electrician,
		//Moves faster, less likely to get crippled
		Athlete,
		//Free, reduces speed
		Maimed,

		//+Command for minigame time and +Sci for Stills

		//+Command for AI energy expenditure and +Mech for energy grid systems
		//Physicist? Technician?

		//Missionary

	}

	public static List<CharRoles> shittyRoles = new List<CharRoles>()
	{
		CharRoles.Affluent,
		CharRoles.Prisoner,
		CharRoles.Refugee,
		CharRoles.Military,
		CharRoles.Maimed
	};

	public static List<CharRoles> unlockedRoles = new List<CharRoles>()
	{
		CharRoles.Maimed,
		CharRoles.Refugee,
		CharRoles.Hermit
	};

	public static List<CharRoles> unlockedShittyRoles
	{
		get
		{
			List<CharRoles> list = new List<CharRoles>();
			foreach (var t in shittyRoles)
			{
				if (unlockedRoles.Contains(t))
					list.Add(t);
			}

			return list;
		}
	}

	public enum Team
	{
		None,
		//Maintains and repairs systems around ship
		Engineering,
		//Supplements other job teams by research, prototyping, comms, radar, etc.
		Science,
		//Maintains and cares for personnel (physical and mental)
		Medical
	}

	public enum Task
	{
		Idle,
		Maintenance,
		Repair,
		Heal,
		Using,
		Socializing,
		Construction,
		Salvage,
	}

	public enum Thought
	{
		//A pool of many different enums and strings and events and such
		//WARNING! Do not alter order, speeches will be affected. Tack new Thoughts on end.
		Good,
		Stressed,
		Injured,
		Psychotic,
		Restrained,
		Unconscious,
		Dead,
		Hungry,
		Tired,
		Tasking,
		Eating,
		Sleeping,
		Exercising,
		Wandering,
		Success,
		Failure,
		BrokeSomething,
		HurtSomeone,
		Toilet,
		TargetDestroyed,
		Clicked,
		Lonely
	}

	//Debug
	public bool debugSpeech = false;

	//Declarations
	//The character's rank/ title/ job/ identifier, etc.
	public string title;
	//The character's name
	public string firstName;
	public string lastName;
	//Condition of the character
	public CharStatus status;
	//Recent condition of the character
	private CharStatus lastStatus;
	//Bonus points in Sci, IntP, or Mech
	public List<CharSkill> skills = new List<CharSkill>();
	//Keywords, possibly allegiances
	public List<CharRoles> roles = new List<CharRoles>();
	//The team the character is on. Chosen by player.
	public Team team;
	//The character's current task
	public Task task = Task.Idle;
	//Current task's priority. Lower is more important
	public int priority;
	//Value of pent up stress. Often from number of recent jobs without rest.
	public float stressCounter = 0;
	//How good a character is at resisting the effects of stress. Minimum 0.
	public int baseStressResilience = 0;
	//How good a character is at resisting needs (sleep, hunger, waste)
	public int baseNeedsResilience = 3;
	//Changes when the characters takes damage and such. Provides reason for death (or survival) at end.
	public string result = "";
	//The last task a character did, as a type defined by character skills
	public CharSkill lastTaskType = CharSkill.Science;
	//Has the character not recovered from an injury?
	public bool injured = false;
	//Speed adjustment for injuries
	public float injuredSpeed = 0.8f;
	//Recent event bools, used for audio triggers
	public bool succeeded = false, failed = false;
	//Is the character a "random crew"? If so, we'll need to give it some basic stats when we start the game
	public bool isRandomCrew = false;
	
	public const float BaseSleepRate = 0.06f;
	
	/*
	 * Properties and their values.
	 */

	public int stressResilience
	{
		get
		{
			int value = baseStressResilience;

			//Modifiers
			value += GameReference.r.commandValue;
			
			if (roles.Contains(CharRoles.Refugee))
				value--;
			
			if (PlayerPrefs.GetInt("CrewSkill") > 0)
				value++;

			//Min 0, hardcapped
			if (value < 0)
			{
				value = 0;
			}

			return value;
		}
	}

	[SerializeField] private float slp;

	public int sleepinessResilience
	{
		get
		{
			int value = baseNeedsResilience * 2;
			//+/- modifiers
			return value;
		}
	}

	public float sleepiness
	{		//How sleep-deprived is the character?
		get
		{
			return slp;
		}
		set
		{
			slp = value;
			if (slp > sleepinessResilience + 1 && !hasNewThought && currentThought != Thought.Tired)
			{
				if (GameReference.r != null && GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Bed))
					currentThoughtTargetName = "the " + GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Bed).name;
				else
					currentThoughtTargetName = "a bed";
				currentThought = Thought.Tired;
			}
		}
	}

	[SerializeField] private float hng;

	public int hungerResilience
	{
		get
		{
			int value = baseNeedsResilience * 4;
			//+/- modifiers
			return value;
		}
	}

	public float hunger
	{			//How hungry is the character?
		get
		{
			return hng;
		}
		set
		{
			hng = value;
			if (hng > hungerResilience + 1 && !hasNewThought && currentThought != Thought.Hungry)
			{
				if (GameReference.r != null && GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Kitchen))
					currentThoughtTargetName = "the " + GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Kitchen).name;
				else
					currentThoughtTargetName = "a kitchen";
				currentThought = Thought.Hungry;
			}
		}
	}

	[SerializeField] private float wst;

	public int wasteResilience
	{
		get
		{
			int value = baseNeedsResilience;
			//+/- modifiers
			return value;
		}
	}

	public float waste
	{
		get
		{
			return wst;
		}
		set
		{
			wst = value;
			if (wst > wasteResilience && !hasNewThought && currentThought != Thought.Toilet)
			{
				if (GameReference.r != null && GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Toilet))
					currentThoughtTargetName = "the " + GameReference.r.allSystems.Find(sys => sys.function == ShipSystem.SysFunction.Toilet).name;
				else
					currentThoughtTargetName = "a toilet";

				currentThought = Thought.Toilet;
			}
		}
	}

	/**Does this character have stress (or is it under the effects of stress)?
	 * Returns true for: stressCounter > 0, psychotic, restrained, or stressed
	 * NOTE: Does not return true for injured + stressCounter == 0, since that requires medical aid from a second character for any improvement
	 */
	public bool hasStress
	{
		get
		{
			if (stressCounter > 0)
				return true;
			else
			{
				switch (status)
				{
				case CharStatus.Psychotic:
				case CharStatus.Restrained:
				case CharStatus.Stressed:
					return true;
				default :
					return false;
				}
			}
		}
	}

	public bool isControllable{ get; private set; }
	//Is this character controllable?

	private bool ca = true;
	//Used for canAct, below. Initialized to prevent stack overflow elsewhere.
	public bool canAct
	{	//Is this character able to take actions?
		get
		{
			return ca && !sPath.interrupted;
		}
		// Do stuff when there is a change.
		private set
		{
			//Interrupt character if it no longer can act.
			if (ca && !value)
				sPath.Interrupt();
			//Break interrupt if it can act and wasn't able to
			else if (!ca && value)// && sPath.interrupted) <- cut this part out, hopefully will reduce deer in headlights
				sPath.EndInterrupt();

			//Set canAct
			ca = value;
		}
	}

	/**Does the character's condition require medical aid?
	 * True for injured or unconscious.
	 */
	public bool statusIsMedical
	{		//Does the character's condition require medical aid
		get
		{
			if (status == CharStatus.Injured || status == CharStatus.Unconscious)
				return true;
			else
				return false;
		}
	}

	/**Does the character's condition require psychological aid?
	 * True for psychotic, restrained, or stressed.
	 */
	public bool statusIsPsychological
	{	//Does the character's condition require psychological aid
		get
		{
			if (status == CharStatus.Psychotic || status == CharStatus.Restrained || status == CharStatus.Stressed)
				return true;
			else
				return false;
		}
	}

	/**What was the last thing to affect this character?
	 * Be sure to check thoughtChange so you aren't grabbing something redundant!
	 * Also, this can only be retrieved once, then hasNewThought is false! Don't interrupt the flow!
	 */
	public Thought currentThought
	{
		get
		{
			hasNewThought = false;
			return thought;
		}
		set
		{
			if (thought != value || thoughtIsRepeatable)
				hasNewThought = true;
			if (value == Thought.Success)
				succeeded = true;
			if (value == Thought.BrokeSomething || value == Thought.Failure || value == Thought.HurtSomeone)
				failed = true;

			thought = value;
		}
	}

	/**Pass the thought's target here. Speech will grab it if it needs it.*/
	public string currentThoughtTargetName { get; set; }

	/**Use this to access the current thought from outside this class without tripping hasNewThought.
	 */
	public Thought GetCurrentThought()
	{
		return thought;
	}

	private Thought thought;

	/**Should this new thought be spoken, even if it's the same as the last one?
	 */
	private bool thoughtIsRepeatable
	{
		get
		{
			if (thought == Thought.Sleeping || thought == Thought.Tasking || thought == Thought.Wandering || thought == Thought.Psychotic ||
			    thought == Thought.Success || thought == Thought.Failure || thought == Thought.BrokeSomething || thought == Thought.HurtSomeone
			    || thought == Thought.Clicked)
				return true;
			else
				return false;
		}
	}

	/**Is this character thinking about something new?
	 */
	public bool hasNewThought { get; private set; }


	//Cached values
	public SimplePath sPath;
	//The character's pathfinder/ movement script
	public BehaviorHandler bHand;
	//The character's behavior script
	

	/*
	 * AFFECT THE CHARACTER
	 */

	/**Try to stress out the character. Force = true if we want to ignore the character's current status. */
	public void StressCheck(bool force)
	{
		//If forced, do it. Otherwise, check that the character is "controllable"
		if (force || isControllable)
		{
			//Figure out the value at which the stress event occurs
			float trigger = (float)(stressCounter - stressResilience) / (float)(stressCounter + 1);
			//Get a neutral value that might trigger it.
			float chance = Random.Range(0f, 1f);
			//If our chance value is strictly lower than the trigger threshold, initiate stress event
			if (chance < trigger)
			{
				//Just stress them, if they're fine
				if (status == CharStatus.Good)
					ToStressed();
				//OR BRING THE PAIN
				else
					Damage();

				//Reset the character's result if they survived
				if (status != CharStatus.Dead)
					result = "";
			}
		}
	}

	/**Try to destress the character. Not called on the clock like StressCheck, but rather whenever stress is reduced. */
	public void DeStressCheck(bool ignoreVirtualStress = false)
	{
		//We need a value to represent current stress state, in addition to actual stress levels.
		float virtualStress = stressCounter;
		//Let's set its value
		if (!ignoreVirtualStress || !statusIsPsychological)
		{
			switch (status)
			{
			case CharStatus.Stressed:
					//A bit stressed
				virtualStress += 6;
				break;
			case CharStatus.Restrained:
					//Super stressed and tied down. Not OK.
				virtualStress += 6;
				break;
			case CharStatus.Psychotic:
					//Super stressed. Pretty easy to restrain. Then the work begins. Might just want to incapacitate them and let them wake up.
				virtualStress += 4;
				break;
			default :
					//Good, Injured, Unconscious, and Dead characters can't be destressed. Don't continue.
				return;
			}
		}
		//Math time! Value at which destressing occurs:
		float trigger = (float)(virtualStress - stressResilience) / (float)(virtualStress + 1);
		//Get a neutral value that might trigger it.
		float chance = Random.Range(0f, 1f);
		//If our chance value is strictly higher than the trigger threshold, destress!
		if (chance > trigger)
		{
			Improve();
		}
	}

	/**Getting stressed out! */
	public void Stress(float value)
	{
		//Increase stress by value
		stressCounter += value;
	}

	/**Reduce stress levels. Try to destress. */
	public void DeStress(float value, bool ignoreVirtualStress = false)
	{
		//Reduce stress by value, minimum 0
		stressCounter = (stressCounter - value) >= 0 ? (stressCounter - value) : 0;
		DeStressCheck(ignoreVirtualStress);
	}


	/**Damage character based on last task type
	 * If the character passes out, adds in current stress as sleepiness.
	 */
	public void Damage()
	{

		//No more damage for the dead.
		if (status == CharStatus.Dead)
			return;

		int rand = Random.Range(0, 6);
		switch (lastTaskType)
		{
		case CharSkill.Mechanical:
			if (rand < 2)
			{
				ToUnconscious();
			}
			else
			{
				ToInjured();
			}
			break;
		case CharSkill.Science:
			if (rand < 2)
			{
				ToUnconscious();
			}
			else if (rand < 5)
			{
				ToInjured();
			}
			else
			{
				ToPsychotic();
			}
			break;
		case CharSkill.Command:
			if (rand < 4)
			{
				ToUnconscious();
			}
			else
			{
				ToPsychotic();
			}
			break;
		}

		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Hurt", transform);
	}

	/**Heal physical damage */
	public void Heal()
	{
		switch (status)
		{
		case CharStatus.Injured:
			ToStressed(true);
			break;
		case CharStatus.Unconscious:
			if (injured)
			{
				//Reset injured for a sec so we don't trigger death on heal
				ToStressed(true);
				ToInjured(true);
			}
			else
				ToStressed(true);
			break;
		default :
			Debug.Log("Healing " + firstName + " " + lastName + " had no effect.");
			return;
		}

		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Heal", transform);
	}

	/**Improve psychological state */
	public void Improve()
	{
		switch (status)
		{
		case CharStatus.Stressed:
			ToGood(false);
			break;
		case CharStatus.Psychotic:
			ToRestrained(false);
			break;
		case CharStatus.Restrained:
			ToStressed(true);
			break;
		default :
			Debug.Log("Improving " + firstName + " " + lastName + "'s mental state had no effect.");
			return;
		}

		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Heal", transform);
	}

	/**Add a random role or skill. */
	public void GiveRandomRoleOrSkill()
	{
		//Give a Role occasionally
		if (Random.Range(0, 3) == 0)
		{
			//Get a new role
			CharRoles newRole;
			do
			{
				newRole = unlockedRoles [Random.Range(0, unlockedRoles.Count)];
			}
			//That isn't on the shitty list
			while (shittyRoles.Contains(newRole));

			//Gimme
			roles.Add(newRole);
		}
		//Otherwise give a skill
		else
		{
			skills.Add(Utility.GetRandomEnum<CharSkill>());
		}
	}



	/*
	 * DIRECT STATE ADJUSTMENT
	 * 
	 */

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToGood(bool ignoreStatTrack = false)
	{

		//Set status
		status = CharStatus.Good;
		//Set properties. Good characers are active.
		isControllable = true;
		canAct = true;

		//Stress counter reset
		stressCounter = 0;

		//No longer injured
		injured = false;

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Good;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToStressed(bool ignoreStatTrack = false)
	{

		//Set status
		status = CharStatus.Stressed;
		//Set properties. Stressed characters are active.
		isControllable = true;
		canAct = true;

		//Stress counter reset
		stressCounter = 0;

		//No longer injured
		injured = false;

		//Change the thought, if it's worsening (Stressed is an intermediate status)
		if (lastStatus == CharStatus.Good && GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Stressed;

			//Track
			if (!ignoreStatTrack)
				StatTrack.stats.crewStressedOut++;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToInjured(bool ignoreStatTrack = false)
	{

		//Too much pain!
		if (injured)
		{
			ToUnconscious();
			//Chance to maim
			int maimChance = roles.Contains(CharRoles.Athlete) ? 6 : 4;
			if (!roles.Contains(CharRoles.Maimed) && !ignoreStatTrack && Random.Range(0, maimChance) == 0)
				roles.Add(CharRoles.Maimed);

			return;
		}

		//Stress counter reset
		stressCounter = 0;

		//Track
		if (!ignoreStatTrack)
			StatTrack.stats.crewInjured++;

		//Set status
		status = CharStatus.Injured;
		//Set properties. Injured characters are active.
		isControllable = true;
		canAct = true;

		//Injured!
		injured = true;

		//Ping
		PingPool.PingHere(transform, seconds: 1f, growthRate: 0.02f, delay: 0.5f);
		PingPool.PingHere(transform, seconds: 2);

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Injured;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToUnconscious(bool ignoreStatTrack = false)
	{

		//Wait, this might be more dire than first thought
		if (lastStatus == CharStatus.Unconscious)
		{
			if (injured)
			{
				//Bad shape got worse
				if (result == "")
					result = "Fatally Wounded";
				ToDead();
				return;
			}
			else
			{
				//Get injured
				injured = true;
				//Track
				if (!ignoreStatTrack)
					StatTrack.stats.crewInjured++;
			}
		}
		//OK, this just started
		else
		{
			//Start recovery
			Invoke("UnconsciousRecovery", 5);

			//Track
			if (!ignoreStatTrack)
				StatTrack.stats.crewKnockedUnconscious++;
		}

		//Add stress as sleepiness
		sleepiness += stressCounter;
		//Stress counter reset
		stressCounter = 0;

		//Set status
		status = CharStatus.Unconscious;
		//Set properties. Unconscious characters can't do anything.
		isControllable = false;
		//This will also Interrupt, if necessary
		canAct = false;

		//Can't act. Displace from task.
		//bHand.CallEndTask(true);

		//Ping
		PingPool.PingHere(transform, seconds: 1f, growthRate: 0.02f, delay: 0.5f);
		PingPool.PingHere(transform, seconds: 2);

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Unconscious;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToPsychotic(bool ignoreStatTrack = false)
	{
		//Captain bonus!
		if (roles.Contains(CharRoles.Captain))
		{
			ToStressed(true);
			return;
		}

		//Wake up and go crazy! But you're more hurt.
		if (lastStatus == CharStatus.Unconscious && !injured)
		{
			//Get injured
			injured = true;
			//Track
			if (!ignoreStatTrack)
				StatTrack.stats.crewInjured++;
		}
		//Or is already Psychotic and injured, bad news! Pushed too hard.
		else if (lastStatus == CharStatus.Psychotic && injured)
		{
			result = "Self Mutilated";

			ToDead();
			return;
		}

		//Track
		if (!ignoreStatTrack)
			StatTrack.stats.crewGoneInsane++;
		                                         
		//Stress counter reset
		stressCounter = 0;

		//Set status
		status = CharStatus.Psychotic;
		//Set properties. Psychotic characters are active, but can't be controlled.
		isControllable = false;
		//This will NOT call Interrupt. CallEndTask independently.
		canAct = true;
		//Won't do normal things. Displace from current task, then get reassignment.
		bHand.CallEndTask(true);

		//Ping
		PingPool.PingHere(transform, seconds: 1, growthRate: 0.02f);
		PingPool.PingHere(transform, seconds: 2, growthRate: 0.01f);

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Psychotic;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToRestrained(bool ignoreStatTrack = false)
	{
		//Track
		if (!ignoreStatTrack)
			StatTrack.stats.crewRestrained++;

		//Stress counter reset
		stressCounter = 0;

		//Set status
		status = CharStatus.Restrained;
		//Set properties. Restrained characters can't do anything.
		isControllable = false;
		//This will also Interrupt, if necessary
		canAct = false;

		//Can't act. Displace from task.
		//bHand.CallEndTask(true);

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Restrained;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}

	/**Direct status adjustment. Called internally.
	 * Can be called directly for very specific use outside the scope of normal behavior and events.
	 * Stick to just using Stress and DeStress for natural changes.
	 * StressCheck and DeStressCheck can be used for immediate chance to change.
	 * Damage, Heal, or Improve can be used for more significant effects.
	 */
	public void ToDead(bool ignoreStatTrack = false)
	{
		//Track
		if (!ignoreStatTrack)
		{
			StatTrack.stats.crewDied++;
			print(name + " has died. Reason: " + result + ".");

			if (StatTrack.stats.crewDied_total > 99)
			{
				AchievementTracker.UnlockAchievement("100_DEATHS");
			}

			//Update StatTrack entry
			//StatTrack.stats.UpdateCrewInMemorial(this, StatTrack.stats.GetCurrentVesselFromMemorial(), true);
		}

		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Death", transform);

		//Achievements
		AchievementTracker.UnlockAchievement("1_DEATH");

		switch (result.ToLower())
		{
		case "starved":
			AchievementTracker.UnlockAchievement("STARVATION");
			break;
		case "drowned":
		case "suffocated":
			AchievementTracker.UnlockAchievement("SUFFOCATION");
			break;
		case "overexerted":
			AchievementTracker.UnlockAchievement("NOSLEEP");
			break;
		case "ruptured bowels":
			AchievementTracker.UnlockAchievement("RUPTURED");
			break;
		case "committed suicide":
			AchievementTracker.UnlockAchievement("SUICIDE");
			break;
		case "overdose":
		case "treatment was botched":
			AchievementTracker.UnlockAchievement("BAD_MEDIC");
			break;
		case "caught in machine":
			AchievementTracker.UnlockAchievement("CAUGHT_IN_MACHINE");
			break;
		case "self mutilated":
			AchievementTracker.UnlockAchievement("SELF_MUTILATION");
			break;
		case "electrocuted":
			AchievementTracker.UnlockAchievement("ELECTROCUTION");
			break;
		case "loneliness":
			AchievementTracker.UnlockAchievement("LONELINESS");
			break;
		default:
			break;
		}

		//Stress counter reset
		stressCounter = 0;

		//Set status
		status = CharStatus.Dead;
		//Set properties. Dead characters can't do anything.
		isControllable = false;
		//This will also Interrupt, if necessary
		canAct = false;

		//Can't act. Displace from task.
		//bHand.CallEndTask(true);

		//Kick it back half a step in the draw stack (also for colliders)
		transform.position += new Vector3(0, 0, 0.0005f);

		//Pings
		PingPool.PingHere(transform, seconds: 2);
		PingPool.PingHere(transform, seconds: 1, growthRate: 0.03f, delay: 0.5f);
		PingPool.PingHere(transform, seconds: 2, growthRate: 0.02f, delay: 0.25f);

		//Change the thought
		if (GameReference.r != null)
		{
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
			currentThought = Thought.Dead;
		}

		//Mark that the status was changed already
		lastStatus = status;
	}


	/*
	 * BEHAVIOR HANDLING
	 * 
	 */

	/**Get the character's skill, in relation to a specific target.
	 * Requires the skill needed and the target's transform (for specific bonuses)
	 */
	//TODO Add special conditionals (e.g. Pilot bonus only for Flight components, Simple bonus for easy-to-repair systems)
	public int GetSkill(CharSkill skill, Transform target)
	{
		int totalSkill = 0;
		//Basic skill points
		foreach (Character.CharSkill sk in skills)
		{
			if (sk == skill)
			{
				totalSkill++;
			}
		}

		//General role adjustments
		foreach (var t in roles)
		{
			if (t == CharRoles.Prisoner && Random.Range(0, 3) == 0)
			{
				totalSkill--;
			}
		}

		//Possible Targets
		ShipSystem targetSys = target != null ? target.GetComponent<ShipSystem>() : null;	//If the target is a system
		Character targetChar = target != null ? target.GetComponent<Character>() : null;	//If the target is a character

		//Target-side bonuses
		if (targetSys != null)
		{
			//MECHANICAL
			if (skill == CharSkill.Mechanical)
			{
				//Simple System Bonus
				if (targetSys.keyCheck(ShipSystem.SysKeyword.Simple))
					totalSkill++;

				//Prototype Penalty
				if (targetSys.keyCheck(ShipSystem.SysKeyword.Prototype))
					totalSkill--;

				//Nonstandard Penalty
				if (targetSys.keyCheck(ShipSystem.SysKeyword.Nonstandard))
					totalSkill--;

				//Basic Bonus!
				if (targetSys.keyCheck(ShipSystem.SysKeyword.Basic))
					totalSkill += 2;
			}
		}

		//Specific bonuses
		//Pilot
		if (roles.Contains(CharRoles.Pilot) && targetSys != null && targetSys.isFlightComponent)
			totalSkill++;

		//Doctor
		if (roles.Contains(CharRoles.Doctor) && skill == CharSkill.Science && targetChar != null)
			totalSkill += 2;

		//Electrician
		if (roles.Contains(CharRoles.Electrician) && skill == CharSkill.Mechanical && targetSys.usesEnergy)
			totalSkill += 2;

		//Return it!
		return totalSkill;
	}

	/**The character passed out. Start reducing sleepiness and *maybe* recovering completely.
	 * Hunger (and therefore Stress) will still be building up.
	 */
	public void UnconsciousRecovery()
	{
		//Thought name (choose a random crew member!)
		if (GameReference.r != null)
			currentThoughtTargetName = GameReference.r.allCharacters [Random.Range(0, GameReference.r.allCharacters.Count)].name;
		bHand.Sleep(BaseSleepRate / 2, false);

		//Heal!
		if (sleepiness == 0)
			Heal();
		
		//If we're still unconscious, keep recovering
		if (status == Character.CharStatus.Unconscious)
			Invoke("UnconsciousRecovery", bHand.baseSpeed);
		//Otherwise, let's be sure to start moving again!
		else if (status != CharStatus.Restrained && status != CharStatus.Dead)
			sPath.EndInterrupt();
	}

	public void Clicked()
	{
		if (isControllable)
		{
			currentThought = Thought.Clicked;
		}
	}

	/*
	 * GENERAL UPDATE STUFF
	 * 
	 */

	/**Adjust behavior traits based on current status (and relative to last status).
	 * This is a redundancy method.
	 */
	void StatusChange()
	{
		switch (status)
		{
		case Character.CharStatus.Good:
			ToGood(true);
				//We're done
			break;
				
		case Character.CharStatus.Stressed:
			ToStressed(true);
				//We're done
			break;
				
		case Character.CharStatus.Injured:
			ToInjured(true);
				//We're done
			break;
				
		case Character.CharStatus.Unconscious:
			ToUnconscious(true);
				//We're done
			break;

		case Character.CharStatus.Psychotic:
			ToPsychotic(true);
				//We're done
			break;
				
		case Character.CharStatus.Restrained:
			ToRestrained(true);
				//We're done
			break;
				
		case Character.CharStatus.Dead:
			ToDead(true);
				//We're done
			break;
		}

		//Just in case
		Rename();
	}

	/**Returns result of character rename */
	public string Rename(List<Character.CharRoles> rolesForName = null)
	{
		if (rolesForName == null)
			rolesForName = roles;

		firstName = firstName == null || firstName.Trim() == "" ? "Nameless" : firstName;
		lastName = lastName == null || lastName.Trim() == "" ? "Crew" : lastName;

		if (rolesForName.Exists(obj => !shittyRoles.Contains(obj)))
		{
			name = rolesForName.Find(obj => !shittyRoles.Contains(obj)) + " " + firstName + " " + lastName;
		}
		else
			name = firstName + " " + lastName;

		return name;
	}

	void Awake()
	{
		//Cache
		sPath = GetComponent<SimplePath>();
		bHand = GetComponent<BehaviorHandler>();
	}

	void Start()
	{

		//Set the object name in hierarchy to the character's name
		Rename();

		//Set behavior to based on starting status
		StatusChange();
		//But don't say anything
		hasNewThought = false;
		//Priority needs to be cleared
		priority = 100;

		//Set random starting waste
		waste = Random.Range(0, wasteResilience);
	}

	void Update()
	{

		//Change behavior, if necessary (changes in inspector/ debugging, etc.)
		if (status != lastStatus)
		{
			StatusChange();
		}

		//Debug
		if (debugSpeech)
		{
			debugSpeech = false;
			currentThought = Thought.BrokeSomething;
		}
	}

	void OnEnable()
	{
		if (GameReference.r != null && !GameReference.r.allCharacters.Contains(this))	//Safety in case of initialization order
			GameReference.r.allCharacters.Add(this);

		GetComponent<PlayerInteraction>().onLeftClick += Clicked;
	}

	void OnDisable()
	{
		//Clear any jobs
		if (JobAssignment.ja != null)
			JobAssignment.ja.JobShutdown(this);
		//Clear from reference lists
		if (GameReference.r != null)
			GameReference.r.allCharacters.Remove(this);

		GetComponent<PlayerInteraction>().onLeftClick -= Clicked;

	}
}
