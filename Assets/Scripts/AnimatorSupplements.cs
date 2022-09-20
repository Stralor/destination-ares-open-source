using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AnimatorSupplements : MonoBehaviour
{

	/* This script supplements the event scripts provided to the UI components in Unity 4.6 Beta.
	 * Now events (e.g., OnClick for Buttons) can target animator conditions otherwise inaccessible.
	 * 
	 * Attach it to the same object as the Animator (often the UI element calling the events).
	 */
	
	Animator anim;
	//The animator component on this object

	/**A value set by player in Inspector (can also be set from events), used in some methods */
	public float modifier;


	//Find the animator
	void Awake()
	{
		anim = GetComponent<Animator>();
	}

	/* 
	 * BOOLEAN METHODS
	 */

	/**Set target bool to true */
	public void BoolTrue(string name)
	{
		anim.SetBool(name, true);
	}

	/**Set target bool to false */
	public void BoolFalse(string name)
	{
		anim.SetBool(name, false);
	}

	/**Toggle target bool */
	public void BoolToggle(string name)
	{
		anim.SetBool(name, !anim.GetBool(name));
	}

	/* 
	 * FLOAT METHODS
	 */

	/**Increment target float */
	public void FloatIncrement(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) + 1);
	}

	/**Decrement target float */
	public void FloatDecrement(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) - 1);
	}

	/**Increment or decrement a float. Acts as a bool toggle, but toggles between values 0 and 1, for blendtree params.
	 * Will toggle all positive numbers to 0, otherwise 1. */
	public void FloatToggle(string name)
	{
		if (anim.GetFloat(name) > 0f)
		{
			anim.SetFloat(name, 0f);
		}
		else
		{
			anim.SetFloat(name, 1f);
		}
	}

	/**Put the float to 0 */
	public void FloatReset(string name)
	{
		anim.SetFloat(name, 0f);
	}

	/**Add the modifier */
	public void FloatAddModifier(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) + modifier);
	}

	/**Subtract the modifier */
	public void FloatSubtractModifier(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) - modifier);
	}

	/**Multiply by the modifier */
	public void FloatMultiplyByModifier(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) * modifier);
	}

	/**Divide by the modifier */
	public void FloatDivideByModifier(string name)
	{
		anim.SetFloat(name, anim.GetFloat(name) / modifier);
	}

	/* 
	 * INTEGER METHODS
	 */

	/**Increment target int */
	public void IntegerIncrement(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) + 1);
	}

	/**Decrement target int */
	public void IntegerDecrement(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) - 1);
	}

	/**Increment or decrement an integer. Acts as a bool toggle, but toggles between values 0 and 1, for blendtree params.
	 * Will toggle all positive numbers to 0, otherwise 1. */
	public void IntegerToggle(string name)
	{
		if (anim.GetInteger(name) > 0)
		{
			anim.SetInteger(name, 0);
		}
		else
		{
			anim.SetInteger(name, 1);
		}
	}

	/**Put the int to 0 */
	public void IntegerReset(string name)
	{
		anim.SetInteger(name, 0);
	}

	/**Add the modifier. NOTE: the modifier is truncated to an int before processing. */
	public void IntegerAddModifier(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) + (int)modifier);
	}

	/**Subtract the modifier. NOTE: the modifier is truncated to an int before processing. */
	public void IntegerSubtractModifier(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) - (int)modifier);
	}

	/**Multiply by the modifier. NOTE: the modifier is truncated to an int before processing. */
	public void IntegerMultiplyByModifier(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) * (int)modifier);
	}

	/**Divide by the modifier. NOTE: the modifier is truncated to an int before processing. */
	public void IntegerDivideByModifier(string name)
	{
		anim.SetInteger(name, anim.GetInteger(name) / (int)modifier);
	}
	
	/*
	 * MODIFIER METHODS
	 */

	/**Set the modifier value */
	public void SetModifier(float value)
	{
		modifier = value;
	}

}
