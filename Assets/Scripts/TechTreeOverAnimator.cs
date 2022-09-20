using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TechTreeOverAnimator : MonoBehaviour
{
	public TechTreeTrails trailManager;
	public LineRenderer myLine;
	public CanvasGroup canvasGroup;

	Animator anim;
	MetaGameLock mgLock;
	Button myButton;
	bool bought = false;

	public bool disabled { get; private set; }

	public void Normal()
	{
		if (!bought && !disabled)
		{
			//Full visibility
			canvasGroup.alpha = 1;

			//Line colors
			var color = ColorPalette.cp.gry1;

			myLine.startColor = new Color(color.r, color.g, color.b, 0.2f);
			myLine.endColor = color;

			foreach (var t in trailManager.lines)
			{
				t.endColor = color;
			}

			//myLine positions
			BumpLineZs(0);
		}
	}

	public void Highlighted()
	{
		if (!bought && !disabled)
		{
			//Line colors
			var color = ColorPalette.cp.blue4;

			myLine.startColor = new Color(color.r, color.g, color.b, 0.4f);
			myLine.endColor = color;

			foreach (var t in trailManager.lines)
			{
				t.endColor = color;
			}

			//myLine positions
			BumpLineZs(-20);
		}
	}

	public void Pressed()
	{
		if (!bought && !disabled)
		{
			//Line colors
			var color = ColorPalette.cp.yellow3;

			myLine.startColor = new Color(color.r, color.g, color.b, 0.4f);
			myLine.endColor = color;

			foreach (var t in trailManager.lines)
			{
				t.endColor = color;
			}

			//myLine positions
			BumpLineZs(-20);
		}
	}

	public void Disabled()
	{
		if (!bought)
		{
			//Fade it and children
			canvasGroup.alpha = 0.5f;

			//Line colors
			var color = ColorPalette.cp.blk;

			myLine.startColor = new Color(color.r, color.g, color.b, 0.2f);
			myLine.endColor = color;

			foreach (var t in trailManager.lines)
			{
				t.endColor = color;
			}

			//myLine positions
			BumpLineZs(10);
		}
	}

	public void Bought()
	{
		//Full visibility
		canvasGroup.alpha = 1;

		//Change anims and lock as bought
		anim.SetBool("Bought", true);
		bought = true;

		//Disable the button
		myButton.interactable = false;

		//Title change
		GetComponent<GenericTooltip>().tooltipTitle = ColorPalette.ColorText(ColorPalette.cp.blue4, "Unlocked!");

		//Line color
		var color = ColorPalette.cp.yellow1;
		myLine.startColor = myLine.endColor = color;

		//Child line colors
		foreach (var t in trailManager.lines)
		{
			t.endColor = ColorPalette.cp.gry1;
		}

		//myLine positions
		BumpLineZs(-10);
	}

	/**Returns whether it's 'enabled'
	 */
	public bool StateCheck()
	{
		var state = anim.GetNextAnimatorStateInfo(0);

		disabled = !myButton.interactable && !bought;

		if (state.IsName("Normal"))
		{
			Normal();
		}
		else if (state.IsName("Highlighted"))
		{
			Highlighted();
		}
		else if (state.IsName("Pressed"))
		{
			Pressed();
		}
		else if (state.IsName("Disabled"))
		{
			Disabled();
		}

		return !disabled;
	}

	void BumpLineZs(float z)
	{
		//Get the positions
		Vector3[] positions = new Vector3[myLine.positionCount];
		myLine.GetPositions(positions);

		//Change the z positions
		for (int i = 0; i < positions.Length; i++)
		{
			positions [i].z = z;
		}

		//Set the positions
		myLine.SetPositions(positions);
	}

	void Update()
	{
		//Check for buy (likely from a save)
		if (!bought && mgLock.passKey != null && MetaGameManager.keys.Contains(mgLock.passKey))
		{
			Bought();
		}

		//Early exit
		if (bought)
			return;

		//State machine
		StateCheck();
	}

	void Start()
	{
		anim = GetComponent<Animator>();
		mgLock = GetComponent<MetaGameLock>();
		myButton = GetComponent<Button>();
	}
}
