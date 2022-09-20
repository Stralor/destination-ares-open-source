using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Environment_EventGame : Environment
{

	//Declarations
	public int playerAngleOffset;
	public Vector2 playerSpawn;
	public float tutorialTextSpeed = 0.05f, tutorialTextSpacing = 1;

	public System.Action onPlayerReset;

	private bool endProcessed = false;

	public const float GRID_SPACE_SIZE = 0.5f;

	//Cache
	public SpriteRenderer playSpace;
	public FadeChildren tutorial;
	public GameObject returnToGameButton;
	public GameObject playerPrefab, targetPrefab, mineIndicator, playerTrail;
	public Gradient failedPathGradient;
	private ObstacleSpawner obstacleSpawner;
	private Animator anim, maskAnim;
	private List<GameObject> indicators = new List<GameObject>();



	/*
	 * SETUP AND MANIPULATE SCENE
	 */

	/// <summary>
	/// Resets the player's position.
	/// </summary>
	/// <param name="player">Player object.</param>
	public void ResetPlayer(Transform player)
	{

		//Decouple old trail
		var trail = player.GetComponentInChildren<TrailRenderer>();
		if (trail != null)
		{
			trail.transform.SetParent(GameObject.Find("Old Trails").transform);
			trail.autodestruct = true;
			trail.colorGradient = failedPathGradient;
			trail.widthMultiplier = 0.5f;
		}

		//Reset angle and position
		player.localEulerAngles = new Vector3(0, 0, 0);
		ClampPosition(player, playerSpawn);

		//Add new trail
		Instantiate(playerTrail, player, false);

		//Delete old trail, delay a blip to start new trail.
//		trail.time = -trail.time;
//		StartCoroutine(ToggleTrails(trail));


		//Clear any remaining indicators
		ClearIndicators();

		//Set speed back to base
		var playerMovement = player.GetComponentInParent<PlayerMovement>();
		playerMovement.ResetSlow();
		playerMovement.ReduceSpeed();

		if (onPlayerReset != null)
			onPlayerReset.Invoke();
	}

	/// <summary>
	/// Adds a mine indicator.
	/// </summary>
	/// <param name="target">Target to child to.</param>
	public void AddIndicator(Transform target)
	{
		//Create an indicator
		GameObject go = (GameObject)Instantiate(mineIndicator, target.position, Quaternion.identity);
		go.transform.parent = target;

		//Scale based on how many are present
		float stepDistance = 0.1f + indicators.Count * 0.01f;
		float scale = 0.5f + (stepDistance * indicators.Count);
		go.transform.localScale = new Vector3(scale, scale, scale);

		//Add to our list
		indicators.Add(go);		
	}

	/// <summary>
	/// Removes the outermost mine indicator.
	/// </summary>
	public void RemoveIndicator()
	{
		if (indicators.Count > 0)
		{
			var target = indicators [indicators.Count - 1];
			indicators.Remove(target);
			Destroy(target);
		}
	}

	public void ClearIndicators()
	{
		indicators.ForEach(obj => Destroy(obj));
		indicators.Clear();
	}

	/// <summary>
	/// Shake the play area.
	/// </summary>
	public void Shake()
	{
		anim.SetTrigger("Shake");
	}

	public void ZoomIn()
	{
		anim.SetTrigger("Zoom In");
	}

	IEnumerator ToggleTrails(TrailRenderer trail)
	{
		yield return new WaitForSeconds(0.01f);
		trail.time = -trail.time;
	}

	void ClampPosition(Transform obj, Vector2 loc)
	{
		obj.position = new Vector2(GridControl.ClampToNearestPoint(loc.x, GRID_SPACE_SIZE, true), GridControl.ClampToNearestPoint(loc.y, GRID_SPACE_SIZE, true));
	}

	protected override void Start()
	{
		base.Start();

		//Get Cache
		anim = GetComponent<Animator>();
		obstacleSpawner = GetComponentInChildren<ObstacleSpawner>();

		//Difficulty safety
		if (!EventGameParameters.s.current.Equals(EventGameParameters.s.EASY)
		    && !EventGameParameters.s.current.Equals(EventGameParameters.s.MEDIUM)
		    && !EventGameParameters.s.current.Equals(EventGameParameters.s.HARD))
			EventGameParameters.s.SetDifficulty(EventGameParameters.s.HARD, 0);

		//Quick, do the color now because we can!
		if (playSpace != null)
			playSpace.color = EventGameParameters.s.current.background;

		//We need a list for storing gridspaces that start filled with objective, player, and anything else we want
		List<Vector3> blockedLocations = new List<Vector3>();

		//Spawn Player Container
		var playerContainer = (GameObject)Instantiate(playerPrefab);
		playerContainer.transform.eulerAngles = new Vector3(0, 0, playerAngleOffset);
		playerContainer.transform.parent = this.transform;

		//Set Player Position
		var player = playerContainer.transform.GetChild(0);
		ResetPlayer(player);
		maskAnim = player.GetComponentInChildren<Animator>();

		//Add player blocked spaces
		GridControl.AddLocalGridSpacesInArea(ref blockedLocations, 5, 5, GRID_SPACE_SIZE, player.position, centerOfGridIsASpace: true);

		//Spawn Target (Opposite Player)
		var gameTarget = (GameObject)Instantiate(targetPrefab);
		ClampPosition(gameTarget.transform, new Vector2(-playerSpawn.x, -playerSpawn.y));
		gameTarget.transform.parent = this.transform;

		//Add target blocked spaces
		GridControl.AddLocalGridSpacesInArea(ref blockedLocations, 3, 3, GRID_SPACE_SIZE, gameTarget.transform.position, centerOfGridIsASpace: true);

		//Safety before obstacles
		if (obstacleSpawner == null)
			obstacleSpawner = GetComponent<ObstacleSpawner>();

		//Once the objective and player have been spawned, spawn the obstacles VVV
	
		//Walls need to also be blocked off from the middle portion of the map
		GridControl.AddLocalGridSpacesInArea(ref blockedLocations, 7, 7, GRID_SPACE_SIZE, playSpace.transform.position, centerOfGridIsASpace: true);

		//Spawn walls
		obstacleSpawner.SpawnWalls(EventGameParameters.s.current.wallsToSpawn, blockedLocations);

		//Clear the blocked locations in the middle portion of the map
		GridControl.RemoveLocalGridSpacesInArea(ref blockedLocations, 7, 7, GRID_SPACE_SIZE, playSpace.transform.position);

		//Spawn mines
		obstacleSpawner.SpawnMines(EventGameParameters.s.current.minesToSpawn, blockedLocations);

		//Set the song
//		MusicController.mc.SetSong(MusicController.mc.eventStandard);

		//Start the tutorial
		tutorial.gameObject.SetActive(true);
	}

	//OVERRIDES

	new void Update()
	{
		base.Update();

		if (EventGameParameters.s.gameEnded)
		{
			//Guarantee the Pause
			if (!GameClock.clock.isPaused)
				GameClock.clock.Pause(true);

			//Set final pose in scene
			if (!endProcessed)
			{
				//Fade out mask
				maskAnim.SetTrigger("Fade Out");
				//Stop coroutines (notably the tutorial, if it's still running)
				StopAllCoroutines();

				//Activate the endgame button!
				returnToGameButton.SetActive(true);

				endProcessed = true;
			}

			//Accept other buttons for returning to main game
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
				PressedCancel();
		}
	}

	/**Calls Pause Menu during play, return to game after play. 
	 * returnToGameButton will call this when pressed.
	 */
	public override void PressedCancel()
	{
		if (!EventGameParameters.s.gameEnded)
		{
			//Pause when not paused, since menu
			if (!GameClock.clock.isPaused)
				base.PressedCancel();
			//Prevent lock-up from pause menu
			else
				GameClock.clock.Unpause(true);
		}
		else
		{
			EventGameParameters.s.WrapUpEventGame();

			var fade = GetComponent<FadeChildren>();
			fade.onFadeOutFinish.AddListener(() => StartCoroutine(Level.MoveToScene(nextScene)));
			fade.FadeOut();
		}
	}
}
