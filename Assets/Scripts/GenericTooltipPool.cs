using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GenericTooltipPool
{

	/**Set tooltip values using this classes waitDelay and fades?*/
	public static bool useTheseTimes = true;
	public static float waitDelay = 0f, fadeIn = 0.1f, fadeOut = 0.3f;

	static List<GameObject> tooltips = new List<GameObject>();

	public static GameObject GetFreshTooltip()
	{
		GameObject tooltip;

		//Clear nulls
		tooltips.RemoveAll(obj => obj == null);

		//Find available pre-created tooltip
		if (tooltips.Exists(obj => !obj.activeInHierarchy))
			tooltip = tooltips.Find(obj => !obj.activeInHierarchy);
		//Or create one
		else
		{
			tooltip = GameObject.Instantiate(Resources.Load("Tooltip")) as GameObject;
			tooltip.GetComponent<Canvas>().worldCamera = Camera.main;
			tooltips.Add(tooltip);
		}

		//Make it active!
		tooltip.SetActive(true);

		//Give it
		return tooltip;
	}

	public static void ReturnTooltip(GameObject tooltip)
	{
		//Don't add in nulls
		if (tooltip == null)
			return;

		//Turn it off
		tooltip.SetActive(false);

		//Don't add it it we have it
		if (!tooltips.Contains(tooltip))
			tooltips.Add(tooltip);
	}
}
