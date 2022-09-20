using UnityEngine;
using System.Collections;

public class Customization_ModuleMenuTooltip : MonoBehaviour
{

	GenericTooltip tooltip;
	public string moduleSize;

	void Start()
	{
		//Get tooltip
		tooltip = GetComponent<GenericTooltip>();


		//Set title and text
		tooltip.tooltipTitle = moduleSize + " Room";

		Module.Size sizeEnum = (Module.Size)System.Enum.Parse(typeof(Module.Size), moduleSize, true);

		tooltip.tooltipText = "\n" + "Mass: " + Module.ModuleMassDictionary [sizeEnum];

		tooltip.tooltipText += "\n\n" + "Base Storage: " + Module.ModuleStorageDictionary [sizeEnum];

		int val = Customization_CurrencyController.ModuleValueDictionary [(int)sizeEnum];
		string valColored = val > Customization_CurrencyController.c.effectiveCurrency
			? ColorPalette.ColorText(ColorPalette.cp.red4, val.ToString())
			: ColorPalette.ColorText(ColorPalette.cp.blue4, val.ToString());

		//Cost text
		tooltip.tooltipText += "\n\n" + "Cost: " + valColored;
	}
}
