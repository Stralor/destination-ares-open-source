using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceHighlights : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public enum ResType
	{
		Air,
		Energy,
		Food,
		Fuel,
		Heading,
		Materials,
		Parts,
		Thrust,
		Waste
	}

	public ResType resType;

	public void OnPointerEnter(PointerEventData data)
	{
		foreach (var t in GameReference.r.allSystems)
		{
			//Resources it creates
			if (t.resourcesCreated.Contains(resType.ToString().ToLower()))
			{
				t.GetComponent<PlayerInteraction>().outline.ForEach(obj => obj.color = ColorPalette.cp.blue3);
			}
			//Resources it consumes
			else if (t.resourcesConsumed.Contains(resType.ToString().ToLower()))
			{
				t.GetComponent<PlayerInteraction>().outline.ForEach(obj => obj.color = ColorPalette.cp.red3);
			}
			//Energy stores for energy
			else if (resType == ResType.Energy && t.storesEnergy)
			{
				t.GetComponent<PlayerInteraction>().outline.ForEach(obj => obj.color = ColorPalette.cp.yellow3);
			}
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		foreach (var t in GameReference.r.allSystems)
		{
			var pI = t.GetComponent<PlayerInteraction>();

			//Return to last state
			if (pI.selected)
				pI.outline.ForEach(obj => obj.color = ColorPalette.cp.yellow4);
			else
				pI.outline.ForEach(obj => obj.color = ColorPalette.cp.blk);

		}
	}
}
