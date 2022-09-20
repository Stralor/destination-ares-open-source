using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPingTracker : MonoBehaviour
{
	public List<GameObject> pingsPool = new List<GameObject>();
	public GameObject pingCounterPrefab;

	/**Add pings to the available pool for the player.
	 */
	public void AddPings(int count)
	{
		for (int i = 0; i < count; i++)
		{
			//Find an used one from the pool
			if (pingsPool.Exists(obj => obj != null && !obj.activeSelf))
				pingsPool.Find(obj => obj != null && !obj.activeSelf).SetActive(true);
			//Or make one
			else
			{
				var p = (GameObject)Instantiate(pingCounterPrefab, transform);
				pingsPool.Add(p);
			}
		}
	}

	void Update()
	{
		//Ping inputs, only when game is running
		if (Time.timeScale > GameClock.PAUSE_SPEED && (Input.GetMouseButtonDown(1) || Input.GetButtonDown("Pause")))
		{
			//Valid if there's a ping available
			if (pingsPool.FindAll(obj => obj != null && obj.activeSelf).Count > 0)
			{
				FindObjectOfType<PlayerPing>().Ping();

				pingsPool.Find(obj => obj != null && obj.activeSelf).SetActive(false);
			}
			//Otherwise no go
			else
			{
				AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
			}
		}
	}
}
