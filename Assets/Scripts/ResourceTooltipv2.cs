using UnityEngine;
using System.Collections;
using System.Text;

public class ResourceTooltipv2 : MonoBehaviour
{

	public enum ResType
	{
		Air,
		Energy,
		Food,
		Fuel,
		Materials,
		Parts,
		Progress,
		Speed,
		Time,
		Waste
	}

	public ResType resType;
	GenericTooltip tooltip;

	void Start()
	{
		tooltip = GetComponent<GenericTooltip>();
	}

	void Update()
	{
		if (tooltip.activeTip)
		{
			//Title
			tooltip.tooltipTitle = resType.ToString();

			//Body
			var text = new StringBuilder();

			switch (resType)
			{
			case ResType.Air:
					//Title override
				tooltip.tooltipTitle = resType.ToString();
					//Main
				text.Append("\n" + "Usable Air is the breathable air left." +
				"\n\n" + "Total Air is all the air pressure in the ship. It includes Usable Air, as well as any contaminated and oxygen-poor air.");
				break;
			case ResType.Energy:
				text.Append("\n" + "Many systems spend Energy." +
				"\n" + "The ship also requires a bit for AI upkeep." +
				"\n\n" + "Total Capacity can change due to systems (i.e. Batteries) breaking down.");
				break;
			case ResType.Food:
				text.Append("\n" + "Food is eaten by the crew, after being prepared in a Kitchen.");
				break;
			case ResType.Fuel:
				text.Append("\n" + "Fuel is consumed by conventional engines to increase speed.");
				break;
			case ResType.Materials:
				text.Append("\n" + "Materials are the base resource. They are used for simple tasks and can be converted to other resources.");
				break;
			case ResType.Parts:
				text.Append("\n" + "Parts are consumed to repair damaged and broken systems." +
				"\n\n" + "Parts are bulkier than other resources.");
				break;
			case ResType.Progress:
				text.Append("\n" + "Progress is how close you are to Ares. At 100%, the journey is successful." +
				"\n\n" + "Going off course can increase your distance from Ares, reducing Progress.");
				break;
			case ResType.Time:
				text.Append("\n" + "How long the journey has taken so far. Events will come more frequently as time passes.");
				break;
			case ResType.Waste:
				text.Append("\n" + "Waste is the mass leftover after the main value of other resources has been extracted.");
				break;
			default:
				break;
			}

			//Output
			tooltip.tooltipText = text.ToString();

			//Keep res value updated
			SendMessage("UpdateText");
		}
	}
}
