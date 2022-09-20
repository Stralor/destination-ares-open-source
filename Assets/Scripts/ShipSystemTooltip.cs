using UnityEngine;
using System.Collections;
using System.Text;

public class ShipSystemTooltip : MonoBehaviour
{

	GenericTooltip tooltip;
	ShipSystem sys;

	public bool showCategories = true, showFunction = true, showQuality = true, showCondition = true, showKeywords = true,  showRightClick = false;

	public ShipSystemCost costToShow = ShipSystemCost.None;
	
	void Update()
	{
		if (tooltip.activeTip)
		{
			//Title
			tooltip.tooltipTitle = sys.sysName;
			
			//Start a StringBuilder, we'll be adding lots to it
			var text = new StringBuilder();
			
			//Categories
			if (sys.condition != ShipSystem.SysCondition.Destroyed)
			{
				if (showCategories)
				{
					if (sys.status == ShipSystem.SysStatus.Disabled)
						text.Append(" [Disabled] ");
					if (sys.usesEnergy)
						text.Append(" [Powered] ");
					if (sys.isAutomated)
						text.Append(" [Automated] ");
					if (sys.isManualProduction)
						text.Append(" [Manual] ");
					if (sys.isFlightComponent)
						text.Append(" [Flight] ");
					if (sys.isPassive)
						text.Append(" [Passive] ");
				}
				
				text.AppendLine();
				
				//Function
				if (showFunction)
				{
					text.Append(GetFunctionText(sys.function));
				}
				
				//Quality
				if (showQuality)
				{
					text.Append(GetQualityText(sys.quality));
				}
			}
			
			//Condition
			if (showCondition && PlayerPrefs.GetInt("HardMode") == 0)
			{
				text.Append(GetConditionText(sys.condition, sys.overdriven, sys.keyCheck(ShipSystem.SysKeyword.Reliable), sys.thrusts));
			}

			//Keywords
			if (showKeywords && sys.condition != ShipSystem.SysCondition.Destroyed)
			{
				foreach (var key in sys.keywords)
				{
					text.Append(GetKeywordText(key));
				}
			}


			
			switch (costToShow)
			{
				case ShipSystemCost.Assets:
					int val = Customization_CurrencyController.GetAssetsCost(sys);
					string valColored = val > Customization_CurrencyController.c.effectiveCurrency
						? ColorPalette.ColorText(ColorPalette.cp.red4, val.ToString())
						: ColorPalette.ColorText(ColorPalette.cp.blue4, val.ToString());

					text.Append("\n\n" + "Cost: " + valColored);
					break;
				case ShipSystemCost.Resources:
					int partsVal = Customization_CurrencyController.GetPartsCost(sys);
					int materialsVal = Customization_CurrencyController.GetMaterialsCost(sys);
					
					string partsColored = partsVal > ShipResources.res.parts
						? ColorPalette.ColorText(ColorPalette.cp.red4, $"{partsVal.ToString()} Parts")
						: ColorPalette.ColorText(ColorPalette.cp.blue4, $"{partsVal.ToString()} Parts");
					string materialsColored = materialsVal > ShipResources.res.materials
						? ColorPalette.ColorText(ColorPalette.cp.red4, $"{materialsVal.ToString()}+ Materials")
						: ColorPalette.ColorText(ColorPalette.cp.blue4, $"{materialsVal.ToString()}+ Materials");
					
					text.Append($"\n\nCost: {partsColored}, {materialsColored}");
					break;
			}
				
			if (showRightClick)
			{
				text.Append("\n\n" + ColorPalette.ColorText(ColorPalette.cp.yellow4, "Right-click to customize"));
			}
			else if (sys.isPassive)
			{
				text.Append("\n\n" + ColorPalette.ColorText(ColorPalette.cp.yellow4, "Passive systems can't be turned on or off"));
			}

			//Export
			tooltip.tooltipText = text.ToString();
			
			SendMessage("UpdateText");
		}
	}

	public static string GetFunctionText(ShipSystem.SysFunction function)
	{
		switch (function)
		{
		case ShipSystem.SysFunction.Battery:
			return ("Stores energy.");
		case ShipSystem.SysFunction.Bed:
			return ("Allows crew to sleep when tired.");
		case ShipSystem.SysFunction.Communications:
			return ("Processes all alerts and crew chatter. Note: crew responds faster to alerts proportional to output.");
		case ShipSystem.SysFunction.Electrolyser:
			return ("Creates air pressure and fuel from materials.");
		case ShipSystem.SysFunction.Engine:
			return ("Produces thrust and energy from fuel.");
		case ShipSystem.SysFunction.Fabricator:
			return ("Creates parts from materials.");
		case ShipSystem.SysFunction.FuelCell:
			return ("Stores and produces energy, by reverting fuel into materials.");
		case ShipSystem.SysFunction.Generator:
			return ("Produces energy from fuel. Dirties air.");
		case ShipSystem.SysFunction.Gym:
			return ("Helps the crew de-stress.");
		case ShipSystem.SysFunction.Helm:
			return ("Adjusts course to reduce drift.");
		case ShipSystem.SysFunction.GuideBot:
			return ("Gives tips and occasionally warns of imminent catastrophe.\n(Requires Tooltips set to Full or Simple, and will force them on.)");
		case ShipSystem.SysFunction.Hydroponics:
			return ("Grows food from waste.");
		case ShipSystem.SysFunction.Injector:
			return ("Temporarily boosts engines output at cost of air pressure.");
		case ShipSystem.SysFunction.Kitchen:
			return ("Prepares food for crew consumption.");
		case ShipSystem.SysFunction.Processor:
			return ("Excretes materials from waste.");
		case ShipSystem.SysFunction.Radar:
			return ("Prevents accumulation of course drift.");
		case ShipSystem.SysFunction.Reactor:
			return ("Produces energy and waste from materials.");
		case ShipSystem.SysFunction.Sail:
			return ("Produces thrust, slowly.");
		case ShipSystem.SysFunction.Scrubber:
			return ("Cleans the air for crew reuse.");
		case ShipSystem.SysFunction.Solar:
			return ("Gathers small amounts of energy.");
		case ShipSystem.SysFunction.Still:
			return ("Creates fuel from materials.");
		case ShipSystem.SysFunction.Storage:
			return ("Extra space for resources, hardlined into the pipes.");
		case ShipSystem.SysFunction.Toilet:
			return ("Accepts crew's waste into resource supply.");
		case ShipSystem.SysFunction.WasteCannon:
			return ("Produces thrust by propelling waste out of the ship.");
		default:
			return "";
		}
	}

	public static string GetQualityText(ShipSystem.SysQuality quality)
	{
		var text = new StringBuilder();

		switch (quality)
		{
			case ShipSystem.SysQuality.Exceptional:

				text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Exceptional Quality"));
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" + "(+Output)");
				break;

			case ShipSystem.SysQuality.Average:

				text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Average Quality"));
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" + "(No bonuses)");
				break;

			case ShipSystem.SysQuality.Shoddy:

				text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Shoddy Quality"));
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" + "(-Durability)");
				break;

			case ShipSystem.SysQuality.Makeshift:

				text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Makeshift Quality"));
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" + "(-Durability, -Output)");
				break;

			case ShipSystem.SysQuality.UnderConstruction:

				text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\n\n" + "Under Construction"));
				break;
		}

		return text.ToString();
	}

	public static string GetConditionText(ShipSystem.SysCondition condition, bool isOverdriven, bool reliableKeycheck = false, bool makesThrust = false)
	{
		var text = new StringBuilder();

		if (isOverdriven)
		{
			text.Append("\n\n" + "In Overdrive.");
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "+Output, -Durability");
		}

		if (makesThrust && ShipMovement.sm != null && ShipMovement.sm.injected)
		{
			text.Append("\n\n" + "Injected!");
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append("\n" + "Thrust Output x 2");
		}

		switch (condition)
		{
		case ShipSystem.SysCondition.Functional:
			break;

		case ShipSystem.SysCondition.Strained:
			if (!reliableKeycheck)
			{
				text.Append("\n\n" + "Under increased strain.");
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" + "Output halved, -Durability");
			}
			else
			{
				text.Append("\n\n" + "Under increased strain.");
				if (PlayerPrefs.GetInt("Tooltips") == 1)
					text.Append("\n" +	"-Durability");
			}
			break;
			
		case ShipSystem.SysCondition.Broken:
			text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\n\n" + "Broken. Repairs required."));
			break;

		case ShipSystem.SysCondition.Destroyed:
			text.Append(ColorPalette.ColorText(ColorPalette.cp.red4, "\n" + "Destroyed beyond repair."));
			break;

		}

		return text.ToString();
	}

	public static string GetKeywordText(ShipSystem.SysKeyword key)
	{
		var text = new StringBuilder();

		if (key == ShipSystem.SysKeyword.Random)
		{
			text.Append("\n\n" + "Random (??): 50% chance of having a random keyword at mission start");
		}
		if (key == ShipSystem.SysKeyword.Basic)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Basic"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Repair (2), -Output");
		}
		if (key == ShipSystem.SysKeyword.Durable)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Durable"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Durability");
		}
		if (key == ShipSystem.SysKeyword.Efficient)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Efficient"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Output");
		}
		if (key == ShipSystem.SysKeyword.Hardened)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Hardened"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Durability (2) when overdriven");
		}
//		if (key == ShipSystem.SysKeyword.Ion)
//		{
//			text.Append("\n\n" + "Ion: rapid, consumes energy instead of fuel");
//		}
		if (key == ShipSystem.SysKeyword.Nonstandard)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Nonstandard"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": repairs cost materials instead of parts, -Repair");
		}
		if (key == ShipSystem.SysKeyword.Performant)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Performant"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Output (2) when overdriven");
		}
		if (key == ShipSystem.SysKeyword.Prototype)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Prototype"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Output (2), -Durability, -Repair");
		}
		if (key == ShipSystem.SysKeyword.Reliable)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Reliable"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": full output when strained");
		}
		if (key == ShipSystem.SysKeyword.Resilient)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Resilient"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Durability (2) when strained");
		}
		if (key == ShipSystem.SysKeyword.Simple)
		{
			text.Append(ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n\n" + "Simple"));
			if (PlayerPrefs.GetInt("Tooltips") == 1)
				text.Append(": +Repair");
		}		
		return text.ToString();
	}

	/**Is this currently placeable? We should set the prereq */
	public void SetTooltipPrerequisite()
	{
		//Tooltip restriction: placed or placement isn't in use
		var p = GetComponentInChildren<Placement>();
		if (p != null)
			tooltip.prerequisiteToOpen = () => !p.isActiveAndEnabled || p.isPlaced;
		else
			tooltip.prerequisiteToOpen = null;
	}

	void Start()
	{
		tooltip = GetComponent<GenericTooltip>();
		sys = GetComponent<ShipSystem>();

		SetTooltipPrerequisite();
	}
}

public enum ShipSystemCost
{
	None,
	Assets,		//Customization
	Resources,	//Materials & Parts, etc.
}
