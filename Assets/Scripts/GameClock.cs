using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameClock: MonoBehaviour
{

	/**Singleton-like reference to the almighty GameClock component.
	 */
	public static GameClock clock;

	//Declarations
	//The text for in-game.
	public string clockText{ get; private set; }
	//Game play speed setting
	public float gameSpeed = 1.0f;
	[Tooltip("Minutes on game clock per second realtime")]
	public int clockSpeed = 60;
	//Day number on clock
	public int day = 0;
	//Hour number in day
	public int hour = 0;
	//Minute number in hour
	public int minute = 0;
	//Time since last clock adjustment
	private float timePassed = 0;
	//Lock the pause controls.
	public bool pauseControlsLocked;

	public const int FAST_FORWARD_SPEED = 3;
	public const float PAUSE_SPEED = 0.002f;

	private float defaultFixedDeltaTime;
	
	//Properties
	//Currently paused?
	public bool isPaused { get; private set; }

	public bool isSpedUp { get; private set; }

	public System.Action onPause;



	//Clock control (events, etc.)
	public void Pause(bool overrideLock = false)
	{
		if (!pauseControlsLocked || overrideLock)
		{
			isPaused = true;
			isSpedUp = false;
			Time.timeScale = PAUSE_SPEED;
			
			//Update clockText to be precise
			SetClockText(true);

			//Notify all onPause
			if (onPause != null)
				onPause.Invoke();
		}
	}

	public void Unpause(bool overrideLock = false)
	{
		if (!pauseControlsLocked || overrideLock)
		{
			isPaused = false;
			isSpedUp = false;
			Time.timeScale = 1;
			//Time.fixedDeltaTime = gameSpeed;
		}
	}

	public void FastForward()
	{
		if (!pauseControlsLocked)
		{
			isPaused = false;
			isSpedUp = true;
			gameSpeed = FAST_FORWARD_SPEED;
		}
	}

	public void Tock()
	{

		//change the time
		timePassed += Time.deltaTime * 1000; // add time that has passed, within context of game speed
		int increment = 1000 / clockSpeed; // minimum amount of time that must have passed to increment clock
		if (timePassed >= increment)
		{ // Wait until there's enough to increment
			float lockedTime = timePassed; // Need to not have a variable in the For Loop that is changed during the For Loop
			for (float i = lockedTime; i >= increment; i -= increment)
			{ // Do Work
				AddTime(0, 0, 1);

				timePassed -= increment; // Reduce pool of time left to convert
			}
		}
	}

	public void AddTime(int days, int hours, int minutes)
	{
		minute += minutes;
		hour += hours + (days * 24);	//Days stacked here here for easier tracking of day passage in the while loop

		while (minute >= 60)
		{
			minute -= 60;
			hour++;
		}

		while (hour >= 24)
		{
			hour -= 24;
			day++;

			StatTrack.stats.daysInSpace_total++;

			if (StatTrack.stats.longestJourney < day)
			{
				StatTrack.stats.longestJourney = day;

				if (StatTrack.stats.longestJourney > 179)
					AchievementTracker.UnlockAchievement("180_DAYS");

				if (StatTrack.stats.longestJourney > 364)
					AchievementTracker.UnlockAchievement("365_DAYS");
			}
		}

		SetClockText(false);
	}


	void SetClockText(bool precise)
	{
		var txt = new System.Text.StringBuilder();

		txt.Append("Day " + day + ", ");
		txt.Append(string.Format("{0:00}", hour) + ":");

		//Precise time
		if (precise)
			txt.Append(string.Format("{0:00}", minute));
		//Imprecise time (Reduce odd clutter. Only show 10s.)
		else
			txt.Append(string.Format("{0}0", minute / 10));

		clockText = txt.ToString();
	}

	void Update()
	{
		//Input Pause/ Unpause
		if (Input.GetButtonUp("Pause"))
		{
			if (isPaused)
				Unpause();
			else
				Pause();
		}

		//Input Speed Up
		if (Input.GetKeyUp(KeyCode.F))
		{
			if (isSpedUp)
				Unpause();
			else
				FastForward();
		}
		if (Input.GetButton("Speed Up") || isSpedUp)
			gameSpeed = FAST_FORWARD_SPEED;
		else if (!isPaused)
			gameSpeed = 1;

		//Update the timeScale
		if (!isPaused)
			Time.timeScale = gameSpeed;

		//Protect against short-hand pausing.
		if (gameSpeed <= PAUSE_SPEED)
		{
			Pause();
			gameSpeed = 1;
		}

		//Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
		
		//Change the clock
		Tock();
	}

	void Start()
	{
		isPaused = false;
	}

	void Awake()
	{
		//Set up the clock singleton reference
		if (clock == null)
		{
			clock = this;
		}
		else if (clock != this)
		{
			Destroy(this);
		}

		defaultFixedDeltaTime = Time.fixedDeltaTime;
	}
}
