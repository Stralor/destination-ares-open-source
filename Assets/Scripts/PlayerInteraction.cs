using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{

	/*
		 * This script is used to catch and handle player interaction with objects.
		 * It sends signals to Systems, Characters, and other things the player can click on for menus and options.
		 * It is attached to said objects.
		*/

	[HideInInspector] public bool selected, highlighted = false;
	//Has it been locked open? Or is the mouse hovering?
	//private Animation tooltip;						//The animation component of this object's tooltip
	//private Animator menu;							//The animator component of the alert menu

	public List<SpriteRenderer> outline { get; private set; }
	//The object's outline(s)

	public System.Action onRightClick, onLeftClick, onHoverStart, onHoverEnd;


	/*
	 * MOUSE STUFF
	 */

	//When the mouse enters the collider
	void OnMouseEnter()
	{
		//Seriously, why the fuck does this script still get called when the PlayerInteraction is disabled?
		if (this.isActiveAndEnabled == false)
			return;

		if (onHoverStart != null)
			onHoverStart.Invoke();

		if (!GameEventManager.gem || !GameEventManager.gem.eventIsActive)
		{
			highlighted = true;
			//Is it unlocked?
			if (!selected)
			{
				foreach (SpriteRenderer sr in outline)
				{
					sr.color = ColorPalette.cp.blue4;	//Highlight color
				}
			
				/* OLD ANIMS
				//Is it playing an animation already?
				if (tooltip.isPlaying){
					//Fade in the scroll up anim
					tooltip.CrossFade("ScrollUp");
				}
				//Not doing anything?
				else{
					//Just play the anim
					tooltip.Play ("ScrollUp");
				}
				*/
			}
		}
	}

	//When the mouse leaves the collider
	void OnMouseExit()
	{
		//Seriously, why the fuck does this script still get called when the PlayerInteraction is disabled?
		if (this.isActiveAndEnabled == false)
			return;

		if (onHoverEnd != null)
			onHoverEnd.Invoke();

		highlighted = false;
		//Is it unlocked?
		if (!selected)
		{
			
			foreach (SpriteRenderer sr in outline)
			{
				sr.color = ColorPalette.cp.blk;	//Base color
			}

			/* OLD ANIMS
			//Is it playing an animation already?
			if (tooltip.isPlaying){
				//Fade in the scroll down anim
				tooltip.CrossFade("ScrollDown");
			}
			//Not doing anything?
			else{
				//Just play the anim
				tooltip.Play ("ScrollDown");
			}
			*/

			//Also clear the alert menu
			//menu.SetBool("Open", false);
		}
	}

	void OnMouseOver()
	{
		//Seriously, why the fuck does this script still get called when the PlayerInteraction is disabled?
		if (this.isActiveAndEnabled == false)
			return;

		if (GameEventManager.gem && GameEventManager.gem.eventIsActive)
			return;
			
		//When the player right-clicks the object
		if (Input.GetMouseButtonDown(1))
		{
			if (onRightClick != null)
				onRightClick.Invoke();

			//Tell every other PlayerInteraction that's locked open to close
			foreach (PlayerInteraction pi in InputHandler.allSelectables)
			{
				//Only do it for all OTHER PlayerInteractions
				if (!pi.Equals(this))
				{
					pi.Deselect();
				}
			}

			//Toggle the lock
			selected = !selected;

			//Set the outline color
			if (selected)
			{
				foreach (SpriteRenderer sr in outline)
					sr.color = ColorPalette.cp.yellow4;	//Selected color
			}
			else
				OnMouseEnter();

			//Toggle the alert menu
			//menu.SetBool("Open", !menu.GetBool("Open"));
		}

		//Left click
		if (Input.GetMouseButtonDown(0))
		{
			if (onLeftClick != null)
				onLeftClick.Invoke();
		}
	}

	//This instance of PlayerInteraction has been told to unlock (aka deselect)
	public void Deselect()
	{
		//Only bother if actually locked
		if (selected)
		{
			//Unlock
			selected = false;
			//Do appropriate color change
			if (highlighted)
				OnMouseEnter();
			else
				OnMouseExit();
		}
	}


	/*
	 * GENERIC
	 */

	void Awake()
	{
		//Calling this early for safety, before it can be interacted with
		outline = new List<SpriteRenderer>();
	}

	void Start()
	{
		//Gather this object's outlines
		foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
		{
			if (sr.name.ToUpper() == "OUTLINE")
				outline.Add(sr);
		}

		//Confirm it's on InputHandler's list
		if (!InputHandler.allSelectables.Contains(this))
			InputHandler.allSelectables.Add(this);

		/*
		 * OLD
		 * 
		//Establish this object's screens
		Animation[] anims = GetComponentsInChildren<Animation>();
		foreach (Animation a in anims) {
			if (a.gameObject.name == "Tooltip"){
				tooltip = a;
				break;
			}
		}

		Animator[] mators = GetComponentsInChildren<Animator> ();
		foreach (Animator a in mators) {
			if (a.gameObject.name == "Menu"){
				menu = a;
				break;
			}
		}
		 *
		 * OLD
		 */
	}

	void OnEnable()
	{
		if (!InputHandler.allSelectables.Contains(this))
			InputHandler.allSelectables.Add(this);
	}

	void OnDisable()
	{
		if (InputHandler.allSelectables.Contains(this))
			InputHandler.allSelectables.Remove(this);
	}
}
