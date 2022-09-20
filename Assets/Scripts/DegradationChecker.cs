using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DegradationChecker : MonoBehaviour
{

	/**Singleton reference to this class.
	 */
	public static DegradationChecker dc;

	//Basic declarations
	public float tick;
	//The time to do a cycle of checks
	private bool ticking = false;
	//Currently doing a tick?
	private bool firstTick = true;
	//Is this the first time a tick was called since we loaded?
	private int objCount;
	//Number of objects needing to be checked for degradation
	private int currentObj = 0;
	//The current object we're trying to degrade.
	private float currentSpeed;
	//The speed at which each object needs to try to degrade
	//in order for every object to do so within a tick

	private List<object> toDegrade = new List<object>();


	void Update()
	{

		//Let's tick. If we aren't already.
		if (!ticking)
		{

			if (firstTick)
				//Pass on saving during the first tick.
				firstTick = false;
			else
				//Good time as any to save!
				SaveLoad.s.SaveGame();

			objCount = GameReference.r.allSystems.Count + GameReference.r.allCharacters.Count;
			//Don't continue if we'd be dividing by zero
			if (objCount == 0)
				return;
			//What's the current speed? Update it when we start a tick.
			currentSpeed = tick / objCount;

			//Clear then populate lists. It's a new tick!
			toDegrade.Clear();

			//Gather objects to degrade. We're gonna distribute them semi-randomly.
			List<object> unshuffled = new List<object>();
			unshuffled.AddRange(GameReference.r.allSystems.ToArray());
			unshuffled.AddRange(GameReference.r.allCharacters.ToArray());

			//Distribute and populate toDegrade!
			foreach (var obj in unshuffled)
			{
				//This doesn't need to be impressively random. Just needs to distribute systems and characters so they're not bunched together anymore.
				toDegrade.Insert(Random.Range(0, toDegrade.Count + 1), obj);
			}

			//Let's tick.
			ticking = true;
			Invoke("Tick", currentSpeed);
		}
	}

	//TODO possible optimization, if needed: convert to coroutine, iterate over the toDegrade list w/o needing the index value, pausing for currentSpeed each iteration
	void Tick()
	{

		//What we're degrading!
		object temp = toDegrade [currentObj];

		//If it's a system
		if (temp is ShipSystem)
		{
			ShipSystem target = (ShipSystem)temp;

			//Hit that system
			target.DurabilityCheck(false);
		}
		//If it's a character
		else if (temp is Character)
		{
			Character target = (Character)temp;

			//Hit every character with needs stress and increase hunger and sleepiness once per tick
			if (target.status != Character.CharStatus.Dead)
			{
				//Stress buildup
				int newStress = (int)(target.sleepiness + target.hunger + target.waste) / (int)(target.sleepinessResilience + target.hungerResilience + target.wasteResilience + 3);

				//Loneliness Stress
				int loneliness = 0;
				if (!target.roles.Contains(Character.CharRoles.Hermit))
				{
					//Base loneliness
					loneliness = Random.Range(0, 5) + StatTrack.stats.crewDied;

					//Worse when actually alone
					if (!GameReference.r.allCharacters.Exists(obj => obj != target && obj.status != Character.CharStatus.Dead))
						loneliness += 2;
					//Otherwise reduce by number of other crew
					else
						loneliness -= GameReference.r.allCharacters.FindAll(obj => obj != target && obj.status != Character.CharStatus.Dead).Count;

					//Minimum loneliness
					var minimumLoneliness = 1 * (StatTrack.stats.crewDied - StatTrack.stats.crewGivenFunerals);

					//Set our base value
					loneliness = loneliness < minimumLoneliness ? minimumLoneliness : loneliness;
				}

				//Loneliness effect reduced if unconscious
				if (target.status == Character.CharStatus.Unconscious)
					loneliness /= 2;

				//Also less bad if keeping busy
				if (target.task != Character.Task.Idle)
					loneliness /= 2;

				//Use a fraction of our loneliness 
				newStress += loneliness / (target.stressResilience + 3);

				//Indicate stress source if necessary
				if (((int)newStress) > 0)
				{
					//Unbridled hunger
					if (target.hunger > target.hungerResilience * 2 && target.hunger > target.sleepiness && target.hunger > target.waste)
					{
						target.lastTaskType = Character.CharSkill.Mechanical;
						target.result = "Starved";
					}
					//Super tired
					else if (target.sleepiness > target.sleepinessResilience * 2 && target.status != Character.CharStatus.Unconscious)
					{
						target.lastTaskType = Character.CharSkill.Command;
						target.result = "Overexerted";

						//Pass out at the extreme
						if (target.sleepiness > target.sleepinessResilience * 4)
							target.ToUnconscious();
					}
					//Otherwise, maybe bowel pressure!
					else if (target.waste > target.wasteResilience * 2)
					{
						target.lastTaskType = Character.CharSkill.Mechanical;
						target.result = "Ruptured Bowels";
						//TODO chance of spontaneous defecation
					}
					//Lastly, hit 'em with that loneliness if they're feeling it
					else if (loneliness > 0)
					{
						target.lastTaskType = Character.CharSkill.Command;
						target.result = "Loneliness";
						target.currentThought = Character.Thought.Lonely;
					}
				}

				//Do the stress (in int form), increment statuses
				target.Stress((int)newStress);
				if (target.GetCurrentThought() != Character.Thought.Sleeping && target.status != Character.CharStatus.Unconscious)
					target.sleepiness++;
				target.hunger++;

				//Hit that character
				target.StressCheck(true);
				//Consume air!
				if (!ShipResources.res.SetUsableAir(ShipResources.res.usableAir - 1))
				{
					//Do damage if they can't breathe!
					target.lastTaskType = Character.CharSkill.Mechanical;	//Physical damage
					target.result = "Suffocated";
					target.Damage();
				}
			}
		}

		//NEXT!
		currentObj++;
		//Still more to do?
		if (currentObj < objCount)
		{
			//Do it again, on the next object.
			Invoke("Tick", currentSpeed);
		}
		else
		{
			//Burn energy for the active AI
			int roll = Random.Range(0, 6) + Random.Range(0, 6) - 4;
			if (PlayerPrefs.GetInt("ResourceDrain") == 1)
				roll /= 2;

			//More command means less energy spend
			if (roll >= GameReference.r.commandValue && !ShipResources.res.SetEnergy(ShipResources.res.energy - 1))
			{
				//Blur the screen!
				CameraEffectsController.cec.BlurScreen(0.75f);
				//Flash red!
				CameraEffectsController.cec.FlashRed();
				//"Need Energy" warning!

				//Do damage!
				foreach (ShipSystem sys in GameReference.r.allSystems)
				{
					int chance = Random.Range(0, 2);
					if (chance == 0)
						sys.DurabilityCheck(false);
				}
			}

			//Now, we're done with this tick.
			currentObj = 0;
			ticking = false;
		}
	}


	void Awake()
	{
		if (dc == null)
		{
			dc = this;
		}
		else if (dc != this)
			Destroy(this);
	}
}

