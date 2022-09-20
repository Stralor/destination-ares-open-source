using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipMovement : MonoBehaviour
{

	/**Singleton ref.
	 */
	public static ShipMovement sm;

	//How often to tick
	public float tickDelay = 0.5f;

	//How much thrust didn't convert into speed (the remainder)
	private int unusedThrust = 0;

	/**How much thrust is in the queue*/
	public int thrustYetToBeAdded { get; private set; }

	//How far off course the ship is flying
	[Range(0, 1.5f)] public float offCourse = 0;
	//Part of the offCourse calculations. Min 1 so offCourse doesn't GROW from reductions.
	private float offCourseDivisor = 1;

	//Timer declarations
	public int injectCounter = 60;
	int tickCounter = 0;
	int injectEnd = 60;
	// tickEnd * tickDelay ideally lasts just as long as a normal system tick, so each engine at normal output creates constant thrust
	int tickEnd = 20;
	bool ticking = false;

	public bool injected { get; private set; }


	//How much thrust an engine creates (this amount may vary by engine type, but not by it's output. an engine's output determines how often it adds thrust)
	const int BASE_ENGINE_THRUST = 80;
	//Maximum offCourse value
	const float MAX_OFF_COURSE = 1.5f;
	//How much inertia can't be salvaged from correcting course. The idea is to not cut out ALL the speed from helm use.
	const float SPEED_LOSS_RATIO = 0.65f;

	//Current thrust coroutine tracking


	//Begin Methods


	/*
	 * OFF COURSE
	 */

	/**Helms calculate the ship's course when used, and make adjustments to heading.
	 * Without this, the ship will slowly slip off course further and further, eventually moving away from the destination.
	 * (Trims offCourse by a given amount. Default given as 0.1f)
	 */
	public void ReduceOffCourse(float amount = 0.1f, Transform iconSpawnTarget = null)
	{

		//Need to know how much we actually reduced by
		float actualReduction;

		//Reduce
		if (offCourse >= amount)
		{
			//All of it!
			offCourse -= amount;
			actualReduction = amount;
		}
		else
		{
			//Just what was left
			actualReduction = offCourse;
			offCourse = 0;	//offCourse should never go below 0
		}

		//Icon spawn
		if (iconSpawnTarget != null && amount > 0)
			IconSpawner.ico.SpawnResourceIcon("heading", iconSpawnTarget, IconSpawner.Direction.Increment);

		//Cut out some speed in the wrong direction (can still mean there's some negative momentum, if off course!)
		ShipResources.res.speed -= (int)Mathf.Abs(ShipResources.res.speed * actualReduction * SPEED_LOSS_RATIO);
	}

	/**Calls the method that sets the divisor for OffCourse calculation.
	 * Radars call this to reduce how fast a ship goes off course.
	 * (Higher values mean lower offCourse gains)
	 */
	public void AddToOffCourseDivisor(float value = 1, Transform iconSpawnTarget = null)
	{

		//Icon!
		if (iconSpawnTarget != null && value > 0)
			IconSpawner.ico.SpawnResourceIcon("heading", iconSpawnTarget, IconSpawner.Direction.Increment);

		StartCoroutine("CourseDivisorTimer", value);
	}

	private IEnumerator CourseDivisorTimer(float value)
	{

		//Set value
		offCourseDivisor += value;

		if (offCourseDivisor <= 1)
			offCourseDivisor = 1;

		//Timer
		for (int i = 0; i < tickEnd; i++)
		{
			yield return new WaitForSeconds(tickDelay);
		}

		//Remove value (via safety clamp)
		offCourseDivisor = offCourseDivisor - value < 1 ? 1 : offCourseDivisor - value;
	}

	/**Calculates and increases offCourse by small amounts. Most offCourse comes from added thrust, not from this.
	 * Current offCourse, speed, and offCourseDivisor values affect the result.
	 * "repeatPoolSize" is a divisor. Set it to how many times this call is repeated (default 1 if a non-standard, single call)
	 * ignoreDivisor causes a simple calculation unaffected by offCourseDivisor
	 */
	public void SetOffCourse(float repeatPoolSize = 1, bool ignoreDivisor = false)
	{
		
		//Force non-zero
		if (repeatPoolSize == 0)
			repeatPoolSize = 1;

		//New value to add
		float newOffCourse;

		//Get new passive offcourse (active offcourse from engines is handled in ConvertThrustToSpeedOverTimeAndMass)
		//Basic idea: a function of speed (portioned into amount of times this will be called from a given location) divided by offCourseDivisor will be our new value

		//New math: more speed is more detrimental. Negligible offcourse gain at low speeds. (OLD: speed / (1500 * portion))
		float speedEffect = Mathf.Pow((float)Mathf.Abs(ShipResources.res.speed), 1.5f);

		//This is how we spread the offcourse gain across a given set of ticks
		float repeatPoolSizeEffect = 1 / repeatPoolSize;

		//Less mass means more easily going off course, but more mass only offsets this partially
		float massEffect = 1 / Mathf.Sqrt((float)Mathf.Abs(GameReference.r.totalShipMass));

		newOffCourse = speedEffect * repeatPoolSizeEffect * massEffect / 3000;

		/* OLD Notes:
		 * where x is the divisor other than repeatPoolSize,
		 * 15,000 still has significant course drift (the old, hard rate)
		 * Currently testing 30,000 for long periods of engine-less drift Result so far: super drift
		 * To offset the heavy ships disadvantage, let's throw shipMass in this: easier to maintain direction with more mass
		 */


		//Adjust by divisor
		if (!ignoreDivisor)
			newOffCourse /= offCourseDivisor;

		offCourse = Mathf.Min(offCourse + newOffCourse, 1.5f);
	}


	/*
	 * THRUST AND SPEED
	 */

	/**Engines are pushing! This will increase the ship's speed for a while.
	 */
	public void AddThrust(Transform iconSpawnTarget = null)
	{

		AddThrustTimesModifier(1, iconSpawnTarget);
	}

	/**Engines are pushing! This will increase the ship's speed for a while.
	 * Multiply engine thrust by input 'modifier'.
	 */
	public void AddThrustTimesModifier(float modifier, Transform iconSpawnTarget = null)
	{
	
		if (iconSpawnTarget != null)
			IconSpawner.ico.SpawnResourceIcon("thrust", iconSpawnTarget, IconSpawner.Direction.Increment);

		//New thrust!
		StartCoroutine(ConvertThrustToSpeedOverTimeAndMass((int)(BASE_ENGINE_THRUST * modifier)));
	}

	public void AddSavedThrust(float savedThrust)
	{
		var mod = savedThrust / ((float)tickEnd * (float)BASE_ENGINE_THRUST);
		AddThrustTimesModifier(mod);
	}

	/**Add to current ship speed based on current mass, thrust, injection, etc.
	 */
	private IEnumerator ConvertThrustToSpeedOverTimeAndMass(int thrustToAdd)
	{
		//SAFETY Be sure we're loaded in
		yield return new WaitUntil(() => GameReference.r.totalShipMass > 0);

		//Update potential thrust
		thrustYetToBeAdded += thrustToAdd * tickEnd;

		for (int i = 0; i < tickEnd; i++)
		{
			int totalThrust = thrustToAdd;	//The total amount of thrust we're gonna use to calculate
			int speedGained = 0;			//How much speed the thrust created
			
			//Injection bonus
			if (injected)
				totalThrust *= 2;
			
			//Now add in old thrust that needs to be used
			totalThrust += unusedThrust;
			
			//Calculate
			speedGained = totalThrust / GameReference.r.totalShipMass;
			unusedThrust = totalThrust % GameReference.r.totalShipMass;

			//Update how much thrust we used
			var thrustUsed = speedGained * GameReference.r.totalShipMass;
			thrustYetToBeAdded -= injected ? thrustUsed / 2 : thrustUsed;

			//Add speedGained to total speed
			ShipResources.res.speed += speedGained;
			//And drift off course just a little bit
			offCourse = Mathf.Min(offCourse + (float)speedGained / (offCourseDivisor * (GameReference.r.totalShipMass / 3)), 1.5f);

			//Check for new max speeds
			int currentSpeed = (int)(ShipResources.res.speed * 60 / ShipMovement.sm.tickDelay / GameClock.clock.clockSpeed);
			if (StatTrack.stats.maxSpeed < currentSpeed)
				StatTrack.stats.maxSpeed = currentSpeed;
			if (StatTrack.stats.maxSpeed_total < currentSpeed)
				StatTrack.stats.maxSpeed_total = currentSpeed;

			int effectiveSpeed = CalculateEffectiveSpeed(currentSpeed);
			if (StatTrack.stats.maxEffectiveSpeed < effectiveSpeed)
				StatTrack.stats.maxEffectiveSpeed = effectiveSpeed;
			if (StatTrack.stats.maxEffectiveSpeed_total < currentSpeed)
				StatTrack.stats.maxEffectiveSpeed_total = effectiveSpeed;

			yield return new WaitForSeconds(tickDelay);
		}


	}

	/**Double thrust output for a bit
	 */
	public void Inject(Transform iconSpawnTarget = null)
	{

		if (iconSpawnTarget != null)
			IconSpawner.ico.SpawnResourceIcon("thrust", iconSpawnTarget, IconSpawner.Direction.Increment);

		injected = true;
		injectCounter -= injectEnd;
	}

	public int CalculateEffectiveSpeed(int speed)
	{
		return speed - (int)Mathf.Abs(speed * offCourse);
	}


	/*
	 * NON-COROUTINE TIMING AND UTILITY
	 */

	void Tick()
	{

		//Adjust distance remaining by speed and heading
		ShipResources.res.distance -= CalculateEffectiveSpeed(ShipResources.res.speed);

		//Drift off course!
		SetOffCourse(tickEnd);

		//Increments
		tickCounter++;

		if (injected)
			injectCounter = injectCounter + 1 > injectEnd ? injectEnd : injectCounter + 1;

		if (injectCounter >= injectEnd)
		{
			injected = false;
		}

		//End of this tick set
		if (tickCounter >= tickEnd)
		{
			ticking = false;
			tickCounter = 0;
		}
		//Or keep ticking
		else
		{
			Invoke("Tick", tickDelay);
		}
	}


	void Update()
	{
	
		//Start a new tick?
		if (!ticking)
		{
			ticking = true;
			Invoke("Tick", tickDelay);
		}
	}

	void Awake()
	{
		if (sm == null)
		{
			sm = this;
		}
		else if (sm != this)
		{
			Destroy(this);
		}
	}

	public float GetOffCourse()
	{
		return offCourse;
	}
}
