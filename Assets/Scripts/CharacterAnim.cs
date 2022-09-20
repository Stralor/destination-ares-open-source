using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterAnim : MonoBehaviour
{

	//Variables
	public bool isRotating, isMoving, isWorking, isEating, isExercising, isUnconscious, isDead, isPsychotic, isInjured, succeeded, failed;
	public int hunger, sleepiness;

	//Cache
	private Character me;
	private Animator anim;
	private Rotation rot;
	private SimplePath move;
	private BehaviorHandler behav;
	private PlayerInteraction pI;


	void Start()
	{
		me = GetComponentInParent<Character>();
		anim = GetComponent<Animator>();
		rot = GetComponent<Rotation>();
		move = GetComponentInParent<SimplePath>();
		behav = GetComponentInParent<BehaviorHandler>();
		pI = GetComponentInParent<PlayerInteraction>();
	}

	void Update()
	{

		//Get values from character components, if they're all available. If not, these will presumably be set from outside.
		if (me != null && behav != null && rot != null && move != null)
		{
			isMoving = move.isMoving;
			isRotating = rot.isRotating;
			isWorking = behav.isWorking;
			isEating = behav.isEating;
			isExercising = behav.isExercising;
			isPsychotic = me.status == Character.CharStatus.Psychotic;
			isDead = me.status == Character.CharStatus.Dead;
			isUnconscious = me.status == Character.CharStatus.Unconscious || behav.isSleeping;
			isInjured = me.injured;
			succeeded = me.succeeded;
			failed = me.failed;
			hunger = (int)me.hunger;
			sleepiness = (int)me.sleepiness;
		}

		/*
		 * Movement
		 */

		//Rotation
		anim.SetBool("Rotate", isRotating);
		//In this direction!
		if (isRotating)
			anim.SetInteger("Rot_Counterclockwise", rot.counterclockwise);

		//Kick Off
		anim.SetBool("KickOff", isMoving);

			
		/*
		 * Action
		 */

		//General Work
		anim.SetBool("Work", isWorking);

		//Working Out
		anim.SetBool("Exercise", isExercising);

		//Eating
		anim.SetBool("Eat", isEating);
		


		/*
		 * Expression and Status
		 */

		//Crazy
		anim.SetBool("Psychotic", isPsychotic);

		//Sleepiness Level
		anim.SetInteger("Sleepiness", sleepiness);

		//Hunger Level
		anim.SetInteger("Hunger", hunger);

		//Injured
		anim.SetBool("Injured", isInjured);
			
		//Unconsious
		anim.SetBool("Unconscious", isUnconscious);

		//Living or Dead
		anim.SetBool("Alive", !isDead);
		//Color change for the dead
		if (isDead)
		{
			if (pI != null && !pI.selected && !pI.highlighted)
				foreach (SpriteRenderer sr in pI.outline)
					sr.color = ColorPalette.cp.gry1;	//Highlight color
		}


		/*
		 * Succeed and Fail
		 */

		//Success!
		if (succeeded)
		{
			AudioClipOrganizer.aco.PlayAudioClip("succeed", transform);
			succeeded = false;
			if (me != null)
				me.succeeded = false;
		}

		//Fail!
		if (failed)
		{
			AudioClipOrganizer.aco.PlayAudioClip("fail", transform);
			failed = false;
			if (me != null)
				me.failed = false;
		}
	}

	public void SetAnimSpeed(float speed)
	{
		if (anim == null)
			anim = GetComponent<Animator>();
			
		anim.speed = speed;
	}
}

