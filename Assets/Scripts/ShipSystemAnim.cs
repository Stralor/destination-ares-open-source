using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShipSystemAnim : MonoBehaviour
{

	/* 
	 * Sort by system type. Set necessary values. (Oxygen needs to spin. Bed doesn't. Everything needs the proper broken vs. functional graphic.)
	 */


	//Values
	public bool inUse, isBroken, isDestroyed, isStrained, isOverdriven, isFunctional, isActive, isInactive, isIntermittent, isPassive, isUnderConstruction;

	public bool doColorUpdates = true;

	//Cache
	public ParticleSystem strainedParticles, damageParticles;
	public GameObject powerSymbol;
	private Image powerSymbolImage;
	private ShipSystem sys;
	//This system
	private Animator anim;
	//The animator
	private SpriteRenderer sysSprite, colorSprite;
	//The system sprite and color sprite, NOTE: strictly enforced as child objects named "Sprite" and "Color" for animator purposes


	void Start()
	{
		anim = GetComponent<Animator>();

		if (powerSymbol != null)
		{
			foreach (var t in powerSymbol.GetComponentsInChildren<Image>())
			{
				if (t.name == "Symbol")
				{
					powerSymbolImage = t;
					break;
				}
			}
		}

		foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
		{
			if (sysSprite != null && colorSprite != null)
				break;

			if (sr.name == "Sprite")
			{
				sysSprite = sr;
				continue;
			}
			if (sr.name == "Color")
			{
				colorSprite = sr;
				continue;
			}
		}

		//Set the appropriate animator bool correlating to function
		if (sys != null)
			SetSystemFunction(sys.function, sys.isAutomated);

		//*** Sprites set to initial state in Update ***
	}

	void Update()
	{

		//Update values as appropriate, if we can
		if (sys != null)
		{
			inUse = sys.inUse;
			isBroken = sys.condition == ShipSystem.SysCondition.Broken;
			isDestroyed = sys.condition == ShipSystem.SysCondition.Destroyed;
			isStrained = sys.condition == ShipSystem.SysCondition.Strained;
			isFunctional = sys.condition == ShipSystem.SysCondition.Functional;
			isUnderConstruction = sys.quality == ShipSystem.SysQuality.UnderConstruction;
			isOverdriven = sys.overdriven;
			isActive = sys.status == ShipSystem.SysStatus.Active;
			isInactive = sys.status == ShipSystem.SysStatus.Inactive;
			isIntermittent	= sys.status == ShipSystem.SysStatus.Intermittent;
			isPassive = sys.isPassive;
		}

		//Still true if intermittent and in use!
		anim.SetBool("Active", inUse);

		/*
		 * Condition updates
		 */

		//Broken and Destroyed
		if (isBroken || isDestroyed)
		{
			anim.SetBool("Broken", true);
			anim.SetBool("Damaged", false);

			if (strainedParticles != null && strainedParticles.isPlaying)
				strainedParticles.Stop();
		}
		//Damaged
		else if (isStrained)
		{
			anim.SetBool("Broken", false);
			anim.SetBool("Damaged", true);
			anim.speed = 0.5f;

			if (strainedParticles != null && !strainedParticles.isPlaying)
				strainedParticles.Play();
		}
		//Not damaged
		else
		{
			anim.SetBool("Broken", false);
			anim.SetBool("Damaged", false);
			anim.speed = 1f;

			if (strainedParticles != null && strainedParticles.isPlaying)
				strainedParticles.Stop();
		}
		//Overdriven (speed change only)
		if (isOverdriven)
			anim.speed *= 1.5f;
		//Injected (thrust systems only, and only when sys is valid)
		if (sys != null && sys.thrusts && ShipMovement.sm && ShipMovement.sm.injected)
			anim.speed *= 2;

		/*
		 * Status Updates
		 */

		if (doColorUpdates)
		{
			if (powerSymbol != null && !isPassive)
				powerSymbol.SetActive(PlayerPrefs.GetInt("HardMode") == 0);

			//Under Construction
			if (isUnderConstruction)
			{
				if (sysSprite.color != ColorPalette.cp.blue3)
					sysSprite.color = ColorPalette.cp.blue3;
				if (powerSymbol != null && powerSymbolImage.color != ColorPalette.cp.red3)
				{
					powerSymbol.SetActive(false);
					powerSymbolImage.color = ColorPalette.cp.red3;
				}
			}
			//Other disabled colors
			else if (!inUse && !isInactive)// && !isBroken && !isDestroyed)
			{
				if (sysSprite.color != ColorPalette.cp.gry2)
					sysSprite.color = ColorPalette.cp.gry2;

				if (powerSymbol != null && powerSymbolImage.color != ColorPalette.cp.red3)
				{
					powerSymbol.SetActive(true);
					powerSymbolImage.color = ColorPalette.cp.red3;
				}
			}
			//Normal colors
			else
			{
				if (sysSprite.color != ColorPalette.cp.wht)
					sysSprite.color = ColorPalette.cp.wht;
				
				if (powerSymbol != null && powerSymbolImage.color != ColorPalette.cp.blue4)
				{
					powerSymbol.SetActive(true);
					powerSymbolImage.color = ColorPalette.cp.blue4;
				}
			}
			
			//Color the color sprite!
			if (colorSprite != null)
			{
				//Overdriven
				if (isOverdriven)
				{
					if (GameReference.r != null)
						colorSprite.color = Color.Lerp(ColorPalette.cp.wht, ColorPalette.cp.blue2, GameReference.r.systemColorFade);
					else
						colorSprite.color = ColorPalette.cp.blue2;
				}
				//Active
				else if (isFunctional)
				{
					if (GameReference.r != null)
						colorSprite.color = Color.Lerp(ColorPalette.cp.gry2, ColorPalette.cp.wht, GameReference.r.systemColorFade);
					else
						colorSprite.color = ColorPalette.cp.wht;
				}
				//Inactive
			//			else if (isInactive)
			//			{
			//				colorSprite.color = cp.gry2;
			//			}
				//Intermittent
				else if (isStrained)
				{
					if (GameReference.r != null)
						colorSprite.color = Color.Lerp(ColorPalette.cp.yellow2, ColorPalette.cp.gry2, GameReference.r.systemColorFade);
					else
						colorSprite.color = ColorPalette.cp.yellow2;
				}
				//Broken
				else if (isBroken)
				{
					if (GameReference.r != null)
						colorSprite.color = Color.Lerp(ColorPalette.cp.blk, ColorPalette.cp.red2, GameReference.r.systemColorFade);
					else
						colorSprite.color = ColorPalette.cp.red2;
				}
				else if (isUnderConstruction)
				{
					if (GameReference.r != null)
						colorSprite.color = Color.Lerp(ColorPalette.cp.blk, ColorPalette.cp.yellow3,
							GameReference.r.systemColorFade);
					else
						colorSprite.color = ColorPalette.cp.yellow3;
				}
				//Otherwise Destroyed
				else
				{
					colorSprite.color = ColorPalette.cp.blk;
				}
			}
		}
		//Ignoring the power symbol?
		if (!doColorUpdates || isPassive)
		{
			if (powerSymbol != null && powerSymbol.activeSelf)
				powerSymbol.SetActive(false);
		}
	}

	public void DamageSparks()
	{
		damageParticles.Emit(Random.Range(2, 5));
	}

	/**Sets the bools for system function in animator. Also tells animator if the system is automated.
	 */
	public void SetSystemFunction(ShipSystem.SysFunction function, bool isAutomated = false)
	{
		if (anim == null)
			anim = GetComponent<Animator>();
			
		//Set the anim bools
		anim.SetBool("TYPE_" + function.ToString(), true);
		
		//Also set if Automated!
		anim.SetBool("Automated", isAutomated);
	}

	public void SetAnimSpeed(float speed)
	{
		if (anim == null)
			anim = GetComponent<Animator>();
		
		anim.speed = speed;
	}

	void OnEnable()
	{
		sys = GetComponent<ShipSystem>();

		if (sys != null)
			sys.onDamage += DamageSparks;
	}

	void OnDisable()
	{
		if (sys != null)
			sys.onDamage -= DamageSparks;

	}
}
