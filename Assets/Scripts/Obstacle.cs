using UnityEngine;
using System.Collections;

public class Obstacle : MonoBehaviour
{

	public enum Zone
	{
		Core,
		Freeze,
		Penalty,
		Warning
	}

	public Zone zone;
	public Animator anim;
	public int warningCount = 1;
	public SpriteList warningSprites;

	private bool allowAnim = true;
	private Spin spin;
	private const int SPIN_RATE_BASE = 60;


	void OnTriggerEnter2D(Collider2D other)
	{

		switch (zone)
		{
		case Zone.Core:
			if (other.tag == "Player")
			{
				//SFX
				AudioClipOrganizer.aco.PlayAudioClip("Invalid", transform);
				anim.SetTrigger("Warn");
			}
			break;
		case Zone.Freeze:
			if (other.tag == "Player")
			{
				//TODO does this feel too much like framerate stutter?
				//GameClock.clock.Pause(true);
				Time.timeScale = 0.1f; //Just go super slow, not perfect 0 so we don't lose frames for pings
				StartCoroutine(Unfreeze());
			}
			break;
		case Zone.Penalty:
			if (other.tag == "Player")
			{
				//SFX
				AudioClipOrganizer.aco.PlayAudioClip("Ambient", transform);
			}
			break;
		case Zone.Warning:

			//Pips
			if (other.tag == "Obstacle")
			{
				var otherObstacle = other.GetComponent<Obstacle>();
				//Change only if it's another warning
				if (otherObstacle.zone == Zone.Warning)
				{
					//Get how many pips we need
					warningCount++;
					//Limit to the sprite pool
					if (warningCount > warningSprites.sprites.Count)
						warningCount = warningSprites.sprites.Count;

					//Set sprite
					GetComponent<SpriteRenderer>().sprite = warningSprites.sprites [warningCount - 1];
				}
			}

			//Color change
			if (allowAnim && other.tag == "Player")
			{
				//SFX
				if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Start"))
					AudioClipOrganizer.aco.PlayAudioClip("Warn", transform);
				anim.SetTrigger("Warn");

				//Spin
				if (spin)
				{
					spin.enabled = true;
					spin.rate = SPIN_RATE_BASE * warningCount;
				}
			}

			//Walls
			if (other.tag == "Wall")
				//Turn off anim on those in walls
				allowAnim = false;

			break;
		}
	}

	static IEnumerator Unfreeze()
	{
		yield return new WaitForSecondsRealtime(0.01f);

		GameClock.clock.Unpause(true);
	}

	void Start()
	{
		//Turn off animation for warnings that overlap with cores. TODO: make less inefficient.
		if (zone == Zone.Warning && FindObjectOfType<ObstacleSpawner>().spawnedObstacles.Exists(obj => obj.transform.position == this.transform.position))
			allowAnim = false;

		spin = GetComponent<Spin>();
	}

	void Awake()
	{
		if (anim == null)
			anim = GetComponent<Animator>();
	}
}
