using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[RequireComponent(typeof(Character))]
public class CharacterTooltip : MonoBehaviour
{
	public bool showTeam = true, showStatus = true, showNeeds = true, showRole = true, showSkills = true, showCost = false, showRightClick = false;


	GenericTooltip tooltip;
	Character character;


	void Update()
	{
		if (tooltip.activeTip)
		{
			//Title
			tooltip.tooltipTitle = character.title + " " + character.firstName + " " + character.lastName;
			
			//Start a StringBuilder, we'll be adding lots to it
			var text = new StringBuilder();
			
			//Job Team
			if (showTeam)
				text.Append(GetTeamText(character.team));

			//Status
			if (showStatus)
			{
				text.Append("\n\nStatus: ");

				//Color based on result
				var color = ColorPalette.cp.yellow4;
				if (character.status == Character.CharStatus.Good)
					color = ColorPalette.cp.blue4;
				if (!character.isControllable)
					color = ColorPalette.cp.red4;

				text.Append(ColorPalette.ColorText(color, character.status.ToString()));

				if (character.status == Character.CharStatus.Dead)
				{
					text.Append("\nCause: " + character.result);
				}
			}

			//Needs
			if (showNeeds && character.status != Character.CharStatus.Dead && PlayerPrefs.GetInt("HardMode") == 0)
			{
				int food = (int)character.hunger / character.hungerResilience;
				int toilet = (int)character.waste / character.wasteResilience;
				int sleep = (int)character.sleepiness / character.sleepinessResilience;

				//Use to color a little at a time
				var needsBuilder = new StringBuilder();

				if (food > 0 || toilet > 0 || sleep > 0)
					text.Append("\nNeeds: ");
				if (food > 0)
				{
					needsBuilder.Append("Food");
					for (int i = 1; i < food; i++)
					{
						needsBuilder.Append("!");
					}

					if (food > 1)
						text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, needsBuilder.ToString()));
					else
						text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, needsBuilder.ToString()));

					needsBuilder.Remove(0, needsBuilder.Length);

					if (toilet > 0 || sleep > 0)
						text.Append(", ");
				}
				if (toilet > 0)
				{
					needsBuilder.Append("Toilet");
					for (int i = 1; i < toilet; i++)
					{
						needsBuilder.Append("!");
					}

					if (toilet > 1)
						text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, needsBuilder.ToString()));
					else
						text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, needsBuilder.ToString()));

					needsBuilder.Remove(0, needsBuilder.Length);

					if (sleep > 0)
						text.Append(", ");
				}
				if (sleep > 0)
				{
					needsBuilder.Append("Sleep");
					for (int i = 1; i < sleep; i++)
					{
						needsBuilder.Append("!");
					}

					if (sleep > 1)
						text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, needsBuilder.ToString()));
					else
						text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, needsBuilder.ToString()));
				}
			}

			//Role (if they have any)
			if (showRole)
			{
				//Random
				if (character.isRandomCrew)
				{
					text.Append("\n\nRANDOM: Cannot be edited.");
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(" Roles and skills will be determined when trip begins.");
				}

				//Normal
				text.Append(GetRolesText(character.roles));
			}
			
			//Skills (if they have any)
			if (showSkills)
				text.Append(GetSkillsText(character.skills));

			//Cost
			if (showCost)
			{
				int val = Customization_CurrencyController.GetAssetsCost(character);
				
				//Color it
				string valColored = val > Customization_CurrencyController.c.effectiveCurrency
					? ColorPalette.ColorText(ColorPalette.cp.red4, val.ToString())
					: ColorPalette.ColorText(ColorPalette.cp.blue4, val.ToString());
				
				//Cost text
				text.Append("\n\n" + "Cost: " + valColored);
			}

			if (showRightClick)
			{
				text.Append("\n\n" + ColorPalette.ColorText(ColorPalette.cp.yellow4, "Right-click to customize"));
			}
			
			//Export
			tooltip.tooltipText = text.ToString();

			SendMessage("UpdateText");
		}
	}

	public static string GetTeamText(Character.Team team)
	{
		var text = new StringBuilder();

		switch (team)
		{
		case Character.Team.Engineering:
			
			text.Append(" [Engineer] ");

			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "Repairs systems.");
			break;
		case Character.Team.Medical:
			
			text.Append(" [Medical Officer] ");

			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "Tends to crewmates.");
			break;
		case Character.Team.Science:
			
			text.Append(" [Scientist] ");

			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "Uses manual systems.");
			break;
		default:
			
			text.Append(" [Unassigned] ");

			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "Fills in where needed.");
			break;
		}

		return text.ToString();
	}

	public static string GetRolesText(List<Character.CharRoles> roles)
	{
		var text = new StringBuilder();

		if (roles.Count > 0)
		{
			//Output each role
			foreach (var t in roles)
			{
				text.AppendLine();

				switch (t)
				{
				case Character.CharRoles.Affluent:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\nAffluent"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Will not automatically do work");
					break;

				case Character.CharRoles.Captain:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nCaptain"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Immune to psychotic breaks");
					break;

				case Character.CharRoles.Doctor:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nDoctor"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": +Sci (2) when healing crew");
					break;

				case Character.CharRoles.Pilot:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nPilot"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": +Sci and +Mech for flight components");
					break;

				case Character.CharRoles.Prisoner:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\nPrisoner"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Skill checks are occasionally impaired");
					break;

				case Character.CharRoles.Psychologist:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nPsychologist"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Improves crewmates' psychological states more easily");
					break;

				case Character.CharRoles.Refugee:
					
					text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\nRefugee"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Less resilient to stress");
					break;

				case Character.CharRoles.Military:

					text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\nMilitary"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Increased tasking priority");
					break;

				case Character.CharRoles.Electrician:

					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nElectrician"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": +Mech (2) for components that consume energy");
					break;

				case Character.CharRoles.Athlete:

					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nAthlete"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Increased movement speed, less likely to get maimed");
					break;

				case Character.CharRoles.Maimed:

					text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\nMaimed"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Reduced movement speed");
					break;

				case Character.CharRoles.Hermit:

					text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\nHermit"));
					if (PlayerPrefs.GetInt("Tooltips") == 1)
						text.Append(": Immune to loneliness");
					break;

				default:
					break;
				}
			}
		}

		return text.ToString();
	}

	public static string GetSkillsText(List<Character.CharSkill> skills)
	{
		var text = new StringBuilder();

		if (skills.Count > 0)
		{
			text.AppendLine();
			//Search by skills
			foreach (var t in System.Enum.GetValues(typeof(Character.CharSkill)))
			{
				int count = 0;
				//Add skills they have
				foreach (var s in skills)
				{
					if (s.Equals(t))
						count++;
				}

				//Output
				if (count == 1)
					text.Append("\n+" + t.ToString());
				if (count > 1)
					text.Append("\n+" + t.ToString() + " (" + count + ")");
			}
		}

		return text.ToString();
	}

	/**Is this currently placeable? We should set the prereq */
	public void SetTooltipPrerequisite()
	{
		//Tooltip restriction: placed or placement isn't in use
		var p = GetComponent<Placement>();
		if (p != null)
			tooltip.prerequisiteToOpen = () => !p.isActiveAndEnabled || p.isPlaced;
		else
			tooltip.prerequisiteToOpen = null;
	}

	void Start()
	{
		tooltip = GetComponentInChildren<GenericTooltip>();
		character = GetComponent<Character>();

		SetTooltipPrerequisite();
	}
}
