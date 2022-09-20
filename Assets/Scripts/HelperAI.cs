using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelperAI : MonoBehaviour
{
	//Here's where we set all of the things this class will track
	static Dictionary<string, System.Func<bool>> helpConditions = new Dictionary<string, System.Func<bool>>()
	{
		{
			"Don't let your crew suffocate!\n\nTurn on a Scrubber to clean the air.\n(Left-click on it)",
			() => ShipResources.res.usableAir < 6 && ShipResources.res.energy > 0
			&& GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Scrubber && sys.status == ShipSystem.SysStatus.Disabled && !sys.isBroken)
			&& !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Scrubber && sys.status != ShipSystem.SysStatus.Disabled)
		},
		{
			"It's past time we got moving.\n\nTurn on some propulsion, like an engine.\n(Left-click on it)",
			() => GameClock.clock.day > 1 && ShipResources.res.speed == 0
			&& GameReference.r.allSystems.Exists(sys => sys.resourcesCreated.Contains("thrust") && sys.status == ShipSystem.SysStatus.Disabled && !sys.isBroken)
			&& !GameReference.r.allSystems.Exists(sys => sys.resourcesCreated.Contains("thrust") && sys.status != ShipSystem.SysStatus.Disabled)
		},
		{
			"Running low on power.\n\nActivate something that charges the batteries.\n\nAlso consider turning off any [Powered] systems that aren't needed right now.",
			() => ShipResources.res.energy < 6 && GameReference.r.allSystems.Exists(sys => sys.resourcesCreated.Contains("energy") && sys.status == ShipSystem.SysStatus.Disabled && !sys.isBroken
			&& sys.function != ShipSystem.SysFunction.Engine)
		},
		{
			"A crew member has been hurt!\n\nFind them, then set a Warning or Emergency alert on them to get the attention of another crew member." +
			"\n(Right-click on the hurt crew member to lock open the command panel at the bottom of the screen.",
			() => GameReference.r.allCharacters.FindAll(crew => crew.isControllable).Count > 1 && GameReference.r.communicationsAvailable
			&& GameReference.r.allCharactersLessIgnored.Exists(ch => ch.statusIsMedical && JobAssignment.ja.allPossibleAlerts.Exists(alert => alert.transform.parent == ch.transform
			&& !(alert.GetActivatedAlerts().Contains(AlertType.Emergency) || alert.GetActivatedAlerts().Contains(AlertType.Warning))))
		},
		{
			"A system has been broken!\n\nDo you want it fixed? You can tell your crew to prioritize it with a Warning or Emergency alert." +
			"\n(Right-click on the broken system to lock open the command panel at the bottom of the screen.)" +
			"\n\n(Hint: if you don't see the broken system, be sure to look for it under the crew!)",
			() => ShipResources.res.parts > 0 && GameReference.r.allCharacters.FindAll(crew => crew.isControllable).Count > 0 && GameReference.r.communicationsAvailable
			&& GameReference.r.allSystemsLessIgnored.Exists(sys => sys.condition == ShipSystem.SysCondition.Broken && JobAssignment.ja.allPossibleAlerts.Exists(alert => alert.transform.parent == sys.transform
			&& !(alert.GetActivatedAlerts().Contains(AlertType.Emergency) || alert.GetActivatedAlerts().Contains(AlertType.Warning))))
		},
		{
			"The ship has gone off course!\n\nYou'll need to change your vector if you don't want to spiral into the void." +
			"\n(Try a 'Use' alert on the Helm, from the command panel at the bottom of the screen)",
			() => ShipMovement.sm.GetOffCourse() > 0.75f && GameReference.r.allCharacters.FindAll(crew => crew.isControllable).Count > 0 && GameReference.r.communicationsAvailable
			&& GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Helm && !sys.isBroken) && !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Helm
			&& JobAssignment.ja.allPossibleAlerts.Exists(alert => alert.transform.parent == sys.transform && alert.GetActivatedAlerts().Contains(AlertType.Use)))
		},
		{
			"The Comms is offline.\n\nWithout it, you can't set alerts, or hear what the crew is trying to tell you. Turn it on!\n(Left-click on it)",
			() => ShipMovement.sm.GetOffCourse() > 0.75f && !GameReference.r.communicationsAvailable
			&& GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && !sys.isBroken)
		},
		{
			"The crew is getting hungry.\n\nThe Kitchen is a power hog, but it needs to be on for the crew to eat.\n(Left-click on it to turn it on)",
			() => ShipResources.res.energy > 0 && GameReference.r.allCharacters.Exists(crew => crew.hunger > crew.hungerResilience)
			&& GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Kitchen && !sys.isBroken && sys.status == ShipSystem.SysStatus.Disabled)
			&& !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Kitchen && sys.status != ShipSystem.SysStatus.Disabled)
		},
	};

	//Here are some helpful reminders that might come up, in the same style as above
	static Dictionary<string, System.Func<bool>> didYouKnowConditions = new Dictionary<string, System.Func<bool>>()
	{
		{
			"Did you know:\n\nYou can think lightning fast.\n(Pause with Spacebar)",
			() => !GameClock.clock.isPaused
		},
		{
			"Did you know:\n\nYou can be irresponsible and distract your processors.\n(Speed up time with F)",
			() => Time.timeScale <= 1
		},
		{
			"Did you know:\n\nYou can switch whether crew or systems are on top. (Tab)",
			() => PlayerPrefs.GetInt("Layering Toggle") == 0
		},
		{
			"Did you know:\n\nYour AI core is hidden in the bulkheads. Still, it frequently consumes energy. Letting it overdraw on empty batteries can cause a power surge.",
			() => ShipResources.res.capacityRemaining > ShipResources.res.capacityTotal / 2
		},
		{
			"Did you know:\n\n\"Warning\" alerts are low priority requests for repairs on systems or heals on crew.\n\nStressed and needy crew will take care of themselves " +
			"(as if they're even capable of that) before addressing the alert.",
			() => GameReference.r.communicationsAvailable
			&& (GameReference.r.allSystems.Exists(sys => sys.condition == ShipSystem.SysCondition.Broken) || GameReference.r.allCharacters.Exists(crew => crew.statusIsMedical || crew.statusIsPsychological))
		},
		{
			"Did you know:\n\n\"Emergency\" alerts are the highest priority requests, used for repairs and medical attention.\n\nThe crew will risk life and limb to make sure the target is fixed." +
			"\n\nThey're a great way to keep everyone from getting killed... or to ensure it.",
			() => GameReference.r.communicationsAvailable
			&& (GameReference.r.allSystems.Exists(sys => sys.condition == ShipSystem.SysCondition.Broken) || GameReference.r.allCharacters.Exists(crew => crew.statusIsMedical || crew.statusIsPsychological))
		},
		{
			"Did you know:\n\n\"Use\" alerts are very low priority requests, placed on [Manual] systems.\n\nThe crew will prioritize using that system when they have time.",
			() => GameReference.r.communicationsAvailable && GameReference.r.allSystems.Exists(sys => !sys.isBroken && sys.isManualProduction)
		},
		{
			"Did you know:\n\nThe \"Overdrive\" command pushes a system to the max.\n\nThe system works harder and faster, but will break faster.\n\nIt cannot be used on [Passive] systems. Duh.",
			() => GameReference.r.allSystems.Exists(sys => sys.status != ShipSystem.SysStatus.Disabled && !sys.isPassive)
		},
		{
			"Did you know:\n\n\"Ignore\" alerts let the crew know that you don't want them to mess with the target.\n\nThey won't interact with it in any way (including fixing it)." +
			"\n\nWant to play a prank on the crew? Set Ignore alerts on all of the beds. I'm sure they'll love that.",
			() => GameReference.r.communicationsAvailable
		},
		{
			"Did you know:\n\nYou can have the crew construct makeshift systems." +
			"\n\nIt'd be hilarious if you wasted resources and time building stuff you didn't need.",
			() => ShipResources.res.parts >= 2 && ShipResources.res.materials >= 6
			&& (!GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Communications && !sys.isBroken)
			|| !GameReference.r.allSystems.Exists(sys => sys.thrusts && !sys.isBroken)
			|| !GameReference.r.allSystems.Exists(sys => sys.storesEnergy && !sys.isBroken)
			|| !GameReference.r.allSystems.Exists(sys => sys.createsEnergy && !sys.isBroken)
			|| !GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Scrubber && !sys.isBroken)
			|| !GameReference.r.allSystems.Exists(sys => sys.resourcesCreated.Contains("heading") && !sys.isBroken))
		},
		{
			"Did you know:\n\n [Manual] systems are used when crew work on them. Crew can only use them if they're turned on.",
			() => GameReference.r.allSystems.Exists(sys => sys.isManualProduction)
		},
		{
			"Did you know:\n\n [Automated] systems will continuously activate, as long as they're turned on.",
			() => GameReference.r.allSystems.Exists(sys => sys.isAutomated)
		},
		{
			"Did you know:\n\n [Passive] systems provide benefits by simply not being broken. They cannot be turned on or off.",
			() => GameReference.r.allSystems.Exists(sys => sys.isPassive)
		},
		{
			"Did you know:\n\n [Flight] systems provide critical functionality for the ship. Pilots are specialized in their use and repair.",
			() => GameReference.r.allSystems.Exists(sys => sys.isFlightComponent)
		},
		{
			"Did you know:\n\n [Disabled] systems are turned off (or broken), and cannot be used.\n\nSeems obvious.",
			() => GameReference.r.allSystems.Exists(sys => sys.status == ShipSystem.SysStatus.Disabled)
		},
		{
			"Did you know:\n\nYou have a destroyed system.\n\nHa-ha.",
			() => GameReference.r.allSystems.Exists(sys => sys.condition == ShipSystem.SysCondition.Destroyed)
		},
		{
			"Did you know:\n\nYou can salvage unwanted systems for a few spare parts and materials." +
			"\n\nPlease, I beg you to end this misery.",
			() => GameReference.r.allSystems.Exists(sys => sys.condition == ShipSystem.SysCondition.Destroyed)
			|| GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Storage && ShipResources.res.storageRemaining > 20)
		},
		{
			"Did you know:\n\nDead crew are dead weight.\n\nYou might get a chance to get rid of the body. Or you could not let them die in the first place.",
			() => GameReference.r.allCharacters.Exists(ch => ch.status == Character.CharStatus.Dead)
		},
		{
			"Did you know:\n\nThe crew requires food to survive. They also need access to a Kitchen to eat that food.",
			() => GameReference.r.allCharacters.Exists(ch => ch.hunger > ch.hungerResilience)
		},
		{
			"Did you know:\n\nInjectors are great for rapidly building up speed, and spending fewer resources to do so.\n\nThis temporary bonus comes at a cost of crew time and consumed air pressure.",
			() => GameReference.r.allSystems.Exists(sys => sys.function == ShipSystem.SysFunction.Injector)
		},
		{
			"Did you know:\n\nYour flight vector is parabolic, not linear, since it's an orbital trajectory.\n\n" +
			"Regardless, any adjustments to the direction will result in an apparent loss of speed, relative to the new heading.",
			() => ShipMovement.sm.GetOffCourse() < 0.5f
		},
		{
			"Did you know:\n\nInterplanetary spaceflight is easiest (if slower) when using a Hohmann Transfer Ellipse. You don't have that luxury." +
			"\n\nStill, you must account for the forces of Sol's gravity, uneven engine acceleration/ propulsion distribution, " +
			"and imprecise initial conditions, leading to course drift.\n\nThis requires constant heading adjustments.",
			() => ShipMovement.sm.GetOffCourse() > 0.5f || ShipResources.res.speed > 60
		},
	};

	//Lists containing the shit from the Dictionary... for nice iterating (and sorting)
	static List<KeyValuePair<string, System.Func<bool>>> allHelps = new List<KeyValuePair<string, System.Func<bool>>>();
	static List<KeyValuePair<string, System.Func<bool>>> allDidYouKnows = new List<KeyValuePair<string, System.Func<bool>>>();
	public static List<KeyValuePair<string, System.Func<bool>>> usedTips = new List<KeyValuePair<string, System.Func<bool>>>();

	bool isHelping = false;
	bool lastPauseLockState = false;
	float currentTipTime = 0;
	const float TIP_TIME = 12;

	int didYouKnowTime = 2;
	const int DYKT_BASE = 2;

	//Cache
	ShipSystem sys;
	GenericTooltip tt;
	ShipSystemTooltip autoTipWriter;


	/**Resets tips that have been used this play session.
	 * If helps is true (default), it will reset the trip-specific helps, which pause the game and provide a safe tutorial.
	 * If didYouKnows is true, it will reset the (sometimes informative) Did You Knows.
	 */
	public static void ResetUsedTips(bool helps = true, bool didYouKnows = false)
	{
		if (helps)
		{
			usedTips.RemoveAll(obj => allHelps.Contains(obj));
		}
		if (didYouKnows)
		{
			usedTips.RemoveAll(obj => allDidYouKnows.Contains(obj));
		}
	}

	void Update()
	{
		//Safety / Customization
		if (GameClock.clock == null)
			return;

		//Track how long the tip has been open, fudged for gamespeed
		if (isHelping)
		{
			currentTipTime += Time.deltaTime * (1 + Time.timeScale) / 2;
		}
		else
		{
			currentTipTime = 0;
		}

		//Don't go further if the clock is locked in pause (aka, when an event is popped up)
		if (GameClock.clock.isPaused && GameClock.clock.pauseControlsLocked)
			return;

		//Check for new tips
		if (sys.status != ShipSystem.SysStatus.Disabled && !isHelping)
		{
			//Check conditions for helping
			foreach (var t in allHelps.Except(usedTips))
			{
				if (t.Value.Invoke())
				{
					Help(t, pause: true);
					return;
				}
			}

			//Then check Did You Knows, if it's time
			if (GameClock.clock.day > didYouKnowTime || ShipResources.res.progress > didYouKnowTime)
			{
				foreach (var t in allDidYouKnows.Except(usedTips))
				{
					if (t.Value.Invoke())
					{
						Help(t, pause: false);
						didYouKnowTime += DYKT_BASE;
						return;
					}
				}
			}
		}
	}


	void Help(KeyValuePair<string, System.Func<bool>> help, bool pause = true)
	{
		if (PlayerPrefs.GetInt("Tooltips") == 0)
		{
			PlayerPrefs.SetInt("Tooltips", 2);
		}

		//Time to help the player! Let's pause (if it's not a Did You Know)
		if (pause)
		{
			GameClock.clock.Pause(true);
			lastPauseLockState = GameClock.clock.pauseControlsLocked;
			GameClock.clock.pauseControlsLocked = true;
		}

		//Reset our didYouKnows if we've used most of them
		if (usedTips.FindAll(obj => allDidYouKnows.Contains(obj)).Count > allDidYouKnows.Count * 0.5f)
			ResetUsedTips(helps: false, didYouKnows: true);

		//Tracking
		usedTips.Add(help);

		//This behavior uses this sys
		sys.Use();

		//Tell the player what's up. HIJACK DAT TOOLTIP
		if (pause)
			tt.tooltipTitle = ColorPalette.ColorText(ColorPalette.cp.red4, "! TIP !");
		else
			tt.tooltipTitle = ColorPalette.ColorText(ColorPalette.cp.blue4, "- TIP -");
		
		tt.tooltipText = "\n" + help.Key + "\n\n" + ColorPalette.ColorText(ColorPalette.cp.yellow4, "Turn off Guide Bot to ignore all tips and safeties");
		SendMessage("UpdateText");
		tt.OpenTooltip();
		tt.lockedFromOpenClose = true;

		//Fading edge FX
		tt.ToggleBorder(pause);

		//Also don't let this get overwritten just yet
		autoTipWriter.enabled = false;

		//I"M HELPING
		isHelping = true;

		//Get ready for the finish
		StartCoroutine(FinishedHelping(help, pause));
	}


	IEnumerator FinishedHelping(KeyValuePair<string, System.Func<bool>> help, bool pause)
	{
		//So many ways to get out of this
		yield return new WaitWhile(() => help.Value.Invoke() && sys.status != ShipSystem.SysStatus.Disabled && isHelping && (pause || currentTipTime < TIP_TIME) && !GameEventManager.gem.eventIsActive);

		isHelping = false;

		//Resume normal behavior
		GameClock.clock.Unpause(!(GameEventManager.gem.eventIsActive || lastPauseLockState));
		GameClock.clock.pauseControlsLocked = GameEventManager.gem.eventIsActive || lastPauseLockState;

		//Fading edge FX
		tt.ToggleBorder(false);

		//Close tooltip
		tt.lockedFromOpenClose = false;
		tt.CloseTooltip();
	}

	void Start()
	{
		if (GameClock.clock != null)
		{		
			//Adjust didYouKnowTime by current time when we're resetting the scene
			didYouKnowTime = Mathf.Max(GameClock.clock.day / DYKT_BASE, ShipResources.res.progress / DYKT_BASE) * DYKT_BASE - 1;
		}

		//Yay, now we'll have proper fading!
		tt.onFadedOut += () => autoTipWriter.enabled = true;
	}

	void Awake()
	{
		sys = GetComponent<ShipSystem>();
		tt = GetComponent<GenericTooltip>();
		autoTipWriter = GetComponent<ShipSystemTooltip>();

		//Add the helps
		allHelps.AddRange(helpConditions);

		//Add the Did You Knows randomly, so they don't all trigger in the same order
		foreach (var t in didYouKnowConditions)
		{
			allDidYouKnows.Insert(Random.Range(0, allDidYouKnows.Count), t);
		}
	}
}
