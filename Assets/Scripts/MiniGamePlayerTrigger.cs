using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MiniGamePlayerTrigger : MonoBehaviour
{

	public float bounciness;

	//Cache
	Rigidbody2D rigid;
	PlayerMovement movement;
	Environment_EventGame enviro;
	List<ParticleSystem> particleSystems = new List<ParticleSystem>();


	void OnTriggerEnter2D(Collider2D other)
	{

		//Effects when we hit an obstacle
		if (other.tag == "Obstacle")
		{
			switch (other.GetComponent<Obstacle>().zone)
			{
			
			case Obstacle.Zone.Core:
					//Die
				EventGameParameters.s.Penalize();
				PingPool.PingHere(transform, seconds: 3, growthRate: 0.04f);
				enviro.ResetPlayer(transform);
				enviro.Shake();
				break;
			
			case Obstacle.Zone.Penalty:
				//Time increase
				movement.Slow(true);
				//enviro.AddIndicator(transform);
				break;
			
			default :
				break;
			}
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{

		//Effects when we leave an obstacle
		var x = other.GetComponent<Obstacle>();
		if (other.tag == "Obstacle" && x.zone == Obstacle.Zone.Penalty)
		{
			//No more slow on the player!
			movement.Slow(false);
			enviro.RemoveIndicator();
		}
	}

	//Hard contact
	void OnCollisionEnter2D(Collision2D coll)
	{
		//Audio
		AudioClipOrganizer.aco.PlayAudioClip("Hover", coll.contacts [0].point);

		//Direction
		var dir = ((Vector2)transform.position - coll.contacts [0].point).normalized;

		//Bounciness
		rigid.AddForceAtPosition(dir * bounciness, coll.contacts [0].point, ForceMode2D.Impulse);

		//Find Particles
		var particles = particleSystems.Find(obj => !obj.IsAlive(true));
		if (!particles)
		{
			particles = ((GameObject)Instantiate(Resources.Load("Collision Particles"))).GetComponent<ParticleSystem>();
			particleSystems.Add(particles);
			particles.transform.SetParent(GameObject.Find("Play Space").transform);
		}

		//Do Particles
		particles.transform.position = coll.contacts [0].point + (dir / 10);
		particles.Play();
	}


	void Awake()
	{
		//Set cache
		rigid = GetComponent<Rigidbody2D>();
		movement = GetComponentInParent<PlayerMovement>();
		enviro = FindObjectOfType<Environment_EventGame>();
	}
}
