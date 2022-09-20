using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class Environment : MonoBehaviour
{

	/**The name of the next scene to load. Change this to change the target. */
	public string nextScene;

	private List<GameObject> pingsPool = new List<GameObject>();

	/**Controls when the new scene is loaded. You can also use this to do final adjustments.
	 * Set to true when ready for the change (may still have a bit of loading left, depending when called.)
	 */
	protected bool activateNextScene = false;

	protected AsyncOperation sceneLoadingOp;

	public bool doPings = true;

	/**Command to load in the next scene went through! Let's gooo!
	 * Used when we're not instantly moving to the next scene.
	 */
	public void ReadyForNextScene()
	{
		activateNextScene = true;
		if (sceneLoadingOp != null)
			sceneLoadingOp.allowSceneActivation = true;
	}

	/**Player hit ESC or whatever they bound cancel to.
	 */
	public virtual void PressedCancel()
	{
		Environment_PauseScreen pause = GameObject.FindObjectOfType<Environment_PauseScreen>();

		//Do nothing if Pause is open. Pause will handle closing itself
		if (pause != null)
		{
			//Do nothing
			;
		}
		//Open Pause!
		else
		{
			if (GameClock.clock)
				GameClock.clock.Pause(true);
			Level.AddScene("Pause Menu");
		}
	}

	protected void Update()
	{
		//Always allow for escape!
		if (Input.GetButtonDown("Cancel") || Input.GetKeyDown("escape"))
		{
			PressedCancel();
		}

		//Click event
		if (doPings && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)))
		{
			//We're gonna spawn in some VFX on mouse clicks, anywhere on the screen.

			//Pool management
			pingsPool.RemoveAll(obj => obj == null);

			//Get a ping
			GameObject ping = pingsPool.Find(obj => !obj.activeSelf);
			//Or make one
			if (ping == null)
			{
				ping = (GameObject)Instantiate(Resources.Load("Ping"));
				ping.transform.SetParent(transform);
				pingsPool.Add(ping);
			}

			//Set it off
			ping.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
	
			//Left
			if (Input.GetMouseButtonUp(0))
				ping.GetComponent<MousePing>().Ping(slerp: true);
			//Right
			else
				ping.GetComponent<MousePing>().ReversePing(slerp: true);

			AudioClipOrganizer.aco.PlayAudioClip("click", ping.transform, 150);
		}

		if (sceneLoadingOp != null && activateNextScene)
			sceneLoadingOp.allowSceneActivation = true;
	}

	protected virtual void Start()
	{
		//Be sure achievements are loaded and available
		foreach (var t in Resources.LoadAll<Achievement>("Achievements"))
		{
			if (!AchievementTracker.allAchievements.Contains(t))
				AchievementTracker.allAchievements.Add(t);
		}

		Resources.UnloadUnusedAssets();

		var fade = GetComponent<FadeChildren>();
		if (fade != null)
			fade.FadeIn();
	}

	void OnApplicationQuit()
	{
		SaveLoad.s.SaveMetaGame();
	}
}
