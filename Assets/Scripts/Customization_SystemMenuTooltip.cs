using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Customization_SystemMenuTooltip : MonoBehaviour
{
	GenericTooltip tooltip;
	public string systemType;
	public ShipSystemCost costToShow = ShipSystemCost.Assets;

	void Start()
	{
		//Get tooltip
		tooltip = GetComponent<GenericTooltip>();

		//Update it
		UpdateTooltip();
	}

	public void UpdateTooltip()
	{
		//Set title and text
		tooltip.tooltipTitle = systemType;

		ShipSystem.SysFunction functionEnum = (ShipSystem.SysFunction)System.Enum.Parse(typeof(ShipSystem.SysFunction), systemType.Replace(" ", ""), true);

		tooltip.tooltipText = "\n";

		//Locked text?
		var selectable = GetComponent<UnityEngine.UI.Selectable>();
		if (!selectable.IsInteractable())
			tooltip.tooltipText += ColorPalette.ColorText(ColorPalette.cp.red4, "LOCKED") + "\n\n";

		//Function text
		tooltip.tooltipText += ShipSystemTooltip.GetFunctionText(functionEnum);

		switch (costToShow)
		{
			case ShipSystemCost.Assets:
				int val = Customization_CurrencyController.SysQualityValueDictionary[1];

				//Special Case
				if (functionEnum != ShipSystem.SysFunction.Storage && PlayerPrefs.GetInt("RandomKeywords") == 1)
					val += Customization_CurrencyController.SysKeywordValueDictionary[1];

				//Multiplier
				val = (int) (val * Customization_CurrencyController.SysFunctionMultiplierDictionary[(int) functionEnum]);

				//Color it
				string valColored = val > Customization_CurrencyController.c.effectiveCurrency
					? ColorPalette.ColorText(ColorPalette.cp.red4, val.ToString())
					: ColorPalette.ColorText(ColorPalette.cp.blue4, val.ToString());

				//Cost text
				tooltip.tooltipText += "\n\n" + "Cost: " + valColored;
				break;
			case ShipSystemCost.Resources:
				var partsCost = Customization_CurrencyController.GetPartsCost(functionEnum,
					ShipSystem.SysQuality.UnderConstruction, new List<ShipSystem.SysKeyword>());
				var materialsCost = Customization_CurrencyController.GetMaterialsCost(functionEnum,
					ShipSystem.SysQuality.UnderConstruction, new List<ShipSystem.SysKeyword>());

				string partsColored = partsCost > ShipResources.res.parts
					? ColorPalette.ColorText(ColorPalette.cp.red4, $"{partsCost.ToString()} Parts")
					: ColorPalette.ColorText(ColorPalette.cp.blue4, $"{partsCost.ToString()} Parts");
				string materialsColored = materialsCost > ShipResources.res.materials
					? ColorPalette.ColorText(ColorPalette.cp.red4, $"{materialsCost.ToString()}+ Materials")
					: ColorPalette.ColorText(ColorPalette.cp.blue4, $"{materialsCost.ToString()}+ Materials");
				tooltip.tooltipText += ($"\n\nCost: {partsColored}, {materialsColored}");
				break;
		}

		//Locked text again
		if (!selectable.IsInteractable())
			tooltip.tooltipText += "\n\n" + ColorPalette.ColorText(ColorPalette.cp.red4, "LOCKED");
	}
}
