using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EventGameParameters : MonoBehaviour
{

	private static EventGameParameters _s;

	/**Object passed between scenes to set parameters like difficulty.
	 */
	public static EventGameParameters s
	{
		get
		{
			if (_s == null)
			{
				_s = FindObjectOfType<EventGameParameters>();

				if (_s == null)
				{
					GameObject gO = new GameObject();
					gO.name = "Parameters";
					
					_s = gO.AddComponent<EventGameParameters>();
					DontDestroyOnLoad(gO);
				}
			}
			return _s;
		}
	}


	//Declarations
	public bool gameStarted, gameEnded;
	private bool victory;
	public Difficulty EASY, MEDIUM, HARD;

	public Difficulty current { get; private set; }

	private float tr;

	public float timeRemaining
	{
		get
		{
			return tr;
		}

		private set
		{
			tr = value >= 0 ? value : 0;
		}
	}

	//Cache
	public GetTime timeText;
	private Animator penAnim, enviroAnim;

	//Difficulty Struct
	public struct Difficulty
	{
		public string name;
		public int minesToSpawn, wallsToSpawn;
		public float startingTime, timePenalty;
		public Color background;

		public Difficulty(string title, int mines, int walls, float time, float penalty, Color bg)
		{
			name = title;
			minesToSpawn = mines;
			wallsToSpawn = walls;
			startingTime = time;
			timePenalty = penalty;
			background = bg;
		}
	}


	/*
	 * GAME CONTROLS
	 */

	/// <summary>
	/// Sets the difficulty. Also resets timeRemaining, since it assumes a new instance.
	/// </summary>
	/// <param name="difficulty">Difficulty to set (recommendation: use the hardcoded values).</param>
	public void SetDifficulty(Difficulty difficulty, int commandValue)
	{
		current = difficulty;
		timeRemaining = current.startingTime + (Mathf.Log10(commandValue + 1) * 20);
	}

	/// <summary>
	/// Penalize the player. Reduces time remaining/ score.
	/// </summary>
	public void Penalize()
	{
		//Consequence
		timeRemaining -= current.timePenalty;

		//VFX
		if (timeText != null)
		{
			penAnim.GetComponent<Text>().text = "-" + current.timePenalty.ToString();

			penAnim.SetTrigger("Fade");
		}
	}

	public void EndGame(bool success)
	{
		gameEnded = true;
		victory = success;
		GameClock.clock.Pause(true);

		if (success)
		{
			AudioClipOrganizer.aco.PlayAudioClip("Succeed", null);
			AudioClipOrganizer.aco.PlayAudioClip("EventStart", null);

			if (enviroAnim)
				enviroAnim.SetTrigger("Zoom In");

			//Achievement
			if (timeRemaining < 1)
				AchievementTracker.UnlockAchievement("CLOSE_CALL");
		}
		else
		{
			AudioClipOrganizer.aco.PlayAudioClip("Fail", null);
			AudioClipOrganizer.aco.PlayAudioClip("EventStart", null);

			if (enviroAnim)
				enviroAnim.SetTrigger("Zoom In");
		}
	}

	public void WrapUpEventGame()
	{
		//What was the score?
		int score = (int)timeRemaining;
		if (!victory)
		{
			score = -1;
			AchievementTracker.UnlockAchievement("FAILED");
		}
		
		//Start our trek to return to the main game
		StartCoroutine(MiniGameBridge.b.ReturnToMainScene(score));

		//Used to call Destroy here, but the Coroutine outlived the EventGameParameters object.
		//Now destroyed by MiniGameBridge when it dies.
	}


	/*
	 * UTILITY
	 */

	/// <summary>
	/// Sets the cache values and the difficulty text.
	/// </summary>
	public void SetCacheAndDifficultyText(GetTime timeRef = null)
	{
		if ((timeText = timeRef) != null || (timeText = FindObjectOfType<GetTime>()) != null)
		{
			//Cache
			penAnim = timeText.gameObject.GetComponentInChildren<Animator>();

		}
	}

	void Update()
	{

		//Adjust time remaining
		if (gameStarted)
			timeRemaining -= Time.deltaTime;

		//Has the game ended?
		if (!gameEnded && timeRemaining <= 0)
			EndGame(false);

		//Check if the game has started!
		if (!gameStarted && Input.anyKeyDown)
		{
			gameStarted = true;
			//Let's not allow pausing
			GameClock.clock.Unpause(true);
			GameClock.clock.pauseControlsLocked = true;

			//Fade out the tutorial
			var tut = FindObjectOfType<Environment_EventGame>().tutorial;
			tut.onFadeOutFinish.AddListener(() => tut.gameObject.SetActive(false));
			tut.FadeOut();
		}

		//Update difficulty text (stop all that ridiculous artifacting)
		if (timeText != null)
			timeText.transform.GetChild(0).GetComponent<Text>().text = "[ " + current.name + " ]";

		if (enviroAnim == null)
		{
			var enviro = FindObjectOfType<Environment_EventGame>();
			if (enviro)
				enviroAnim = enviro.GetComponent<Animator>();
		}
	}

	void Start()
	{
		//Reset declarations
		gameEnded = gameStarted = false;

		//Cache
		SetCacheAndDifficultyText();

		//Start paused!
		GameClock.clock.Pause(true);

		//Clear input to start, so the game stays paused
		Input.ResetInputAxes();
	}

	void Awake()
	{
		//Establish hard-coded difficulties
		EASY = new Difficulty("easy", 12, 8, 45, 5, ColorPalette.cp.blue0);
		MEDIUM = new Difficulty("medium", 14, 10, 50, 6, ColorPalette.cp.yellow0);
		HARD = new Difficulty("hard", 18, 12, 55, 6, ColorPalette.cp.red0);
	}
}
