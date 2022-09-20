using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventControlData_RefugeeShip : EventControlData
{
	//Refs to the conditions storage
	bool doctor { get { return EventSpecialConditions.c.refugee_doctor; } set { EventSpecialConditions.c.refugee_doctor = value; } }

	bool engineer { get { return EventSpecialConditions.c.refugee_engineer; } set { EventSpecialConditions.c.refugee_engineer = value; } }

	bool scientist { get { return EventSpecialConditions.c.refugee_scientist; } set { EventSpecialConditions.c.refugee_scientist = value; } }

	bool retrofits { get { return EventSpecialConditions.c.refugee_retrofits; } set { EventSpecialConditions.c.refugee_retrofits = value; } }

	bool contactedByAI { get { return EventSpecialConditions.c.refugee_contactByAI; } set { EventSpecialConditions.c.refugee_contactByAI = value; } }

	bool sabotaged { get { return EventSpecialConditions.c.refugee_sabotaged; } set { EventSpecialConditions.c.refugee_sabotaged = value; } }

	bool spy { get { return EventSpecialConditions.c.refugee_spy; } set { EventSpecialConditions.c.refugee_spy = value; } }



	string specialCrew { get { return EventSpecialConditions.c.refugee_specialCrew; } set { EventSpecialConditions.c.refugee_specialCrew = value; } }


	/* 
	 * STORY ACTIONS
	 */

	/**A medical crew member gets +Command, and either +Doctor (if not Doctor) or +1 Science
	 */
	public void GiveDoctor()
	{
		if (!scientist && !doctor && !engineer)
		{
			doctor = true;
			
			var crew = GameReference.r.allCharacters.Find(obj => obj.team == Character.Team.Medical && !obj.roles.Contains(Character.CharRoles.Doctor));

			//Give doctor if we found a valid
			if (crew != null)
			{
				crew.skills.Add(Character.CharSkill.Command);
				crew.roles.Add(Character.CharRoles.Doctor);
			}
			//Otherwise find a medical crew and give other bonuses
			else
			{
				crew = GameReference.r.allCharacters.Find(obj => obj.team == Character.Team.Medical);
				crew.skills.Add(Character.CharSkill.Command);
				crew.skills.Add(Character.CharSkill.Science);
			}
			
			specialCrew = crew.name;

			SetCrewInText(crew);
		}
	}

	/**An engineer crew member gets +Mech, +1 stressResilience, and +1 needsResilience
	 */
	public void GiveEngineer()
	{
		if (!scientist && !doctor && !engineer)
		{
			engineer = true;

			var crew = GameReference.r.allCharacters.Find(obj => obj.team == Character.Team.Engineering);

			specialCrew = crew.name;

			crew.skills.Add(Character.CharSkill.Mechanical);
			crew.baseStressResilience++;
			crew.baseNeedsResilience++;
	
			SetCrewInText(crew);
		}
	}

	/**A scientist crew member gets +Sci, +1 stressResilience, and +1 baseNeedsResilience
	 */
	public void GiveScientist()
	{
		if (!scientist && !doctor && !engineer)
		{
			scientist = true;

			var crew = GameReference.r.allCharacters.Find(obj => obj.team == Character.Team.Science);

			specialCrew = crew.name;

			crew.skills.Add(Character.CharSkill.Science);
			crew.baseStressResilience++;
			crew.baseNeedsResilience++;
	
			SetCrewInText(crew);
		}
	}

	/**Make a few systems better.
	 */
	public void GiveSystemRetrofits()
	{
		retrofits = true;

		int breakCount = 0;
		for (int i = 0; i < 4; i++)
		{
			//Random system
			var sys = GameReference.r.allSystems [Random.Range(0, GameReference.r.allSystems.Count)];

			//Valid system?
			if ((breakCount > 8 || !sys.isPassive) && sys.quality != ShipSystem.SysQuality.Exceptional)
			{
				sys.Improve(false);
				if (sys.keywords.Count == 0)
				{
					sys.GiveKeyword();
				}
				GameEventManager.gem.eventBody.text += "\n" + sys.sysName;
			}
			//Otherwise keep trying
			else
			{
				i--;
				if (++breakCount > 16)
					break;
			}
		}
	}

	public void AIContact()
	{
		contactedByAI = true;
	}

	public void Sabotaged()
	{
		sabotaged = true;
	}

	public void Spaced()
	{
		var spacer = GameReference.r.allCharacters.Find(obj => obj.status == Character.CharStatus.Good);

		//The event is structured so this should never be null, even if we haven't protected it
		UnityEngine.Assertions.Assert.IsNotNull(spacer);

		//Update the text, this hijacks the *T*
		GameEventManager.gem.eventBody.text = ReplaceSymbolsInTargetText.ReplaceSymbols(GameEventManager.gem.eventBody.text, spacer.name, GameReference.r.shipName);

		//Kill 'em
		spacer.result = "Spaced Self";
		spacer.ToDead();
		StatTrack.stats.lostCrew.Add(StatTrack.CreateCrewMemorialFromCharacter(spacer, true));
		Destroy(spacer.gameObject);
	}

	public void Spy()
	{
		spy = true;
	}

	public void DoubleDamageEngineer()
	{
		var crew = GameReference.r.allCharacters.Find(obj => obj.name == specialCrew);

		crew.lastTaskType = Character.CharSkill.Mechanical;
		crew.Damage();
		crew.Damage();
		crew.baseStressResilience--;

		SetCrewInText(crew);
	}

	public void DestroyStorage()
	{
		GameReference.r.allSystems.Find(obj => obj.function == ShipSystem.SysFunction.Storage && obj.condition != ShipSystem.SysCondition.Destroyed).DestroySystem();
	}

	/*
	 * SUPPORTING METHODS
	 */

	void SetCrewInText(Character crew)
	{
		GameEventManager.gem.eventBody.text = ReplaceSymbolsInTargetText.ReplaceSymbols(GameEventManager.gem.eventBody.text, crew.name, GameReference.r.shipName);
	}
}
