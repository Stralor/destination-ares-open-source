using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MetaGameLock : MonoBehaviour
{
	public bool showWhenLocked = true, calculatePointsByLockedValue = false, costAsTitle = false, tooltipFromItems = false;
	public MetaGameKey requiredKey, passKey;
	public List<Achievement> requiredAchievements = new List<Achievement>();
	public int minimumPoints;
	public Unlockable lockedItems;
	public Selectable lockedUI;

	[TextArea]
	public string tooltipTextWhileLocked = "";
	string stashedTooltipText = "";

	GenericTooltip tip;

	bool lastUnlockStatus = false;

	public bool IsUnlocked
	{
		get
		{
			//Only bother checking if we don't have the passKey
			if (passKey == null || !MetaGameManager.keys.Contains(passKey))
			{
				//False if any required achievement isn't met
				foreach (var t in requiredAchievements)
				{
					if (!AchievementTracker.unlockedAchievements.Contains(t))
						return false;
				}
				
				//Same if not enough points
				if (MetaGameManager.currentUnlockPoints < minimumPoints)
					return false;
				
				//Same if there's a key and MetaGameMaaaaan doesn't have it
				if (requiredKey != null && !MetaGameManager.keys.Contains(requiredKey))
					return false;
			}

			//Otherwise true
			return true;
		}
	}

	void Start()
	{
		tip = GetComponent<GenericTooltip>();

		//Initial set
		UpdateLockStatus();
	}

	void Update()
	{
		//Only update on change
		if (lastUnlockStatus ^ IsUnlocked)
			UpdateLockStatus();
	}

	void UpdateLockStatus()
	{
		//Quick calculation of value
		if (calculatePointsByLockedValue && lockedItems != null)
			minimumPoints = lockedItems.cost;

		//update lastUnlockStatus so we don't do this again
		lastUnlockStatus = IsUnlocked;

		//Tooltip
		if (tip != null)
		{
			//Stashed
			if (!IsUnlocked)
			{
				if (costAsTitle)
				{
					tip.tooltipTitle = "Cost: " + ColorPalette.ColorText(ColorPalette.cp.red4, minimumPoints.ToString());
				}

				if (tooltipTextWhileLocked != "")
				{
					stashedTooltipText = tip.tooltipText;
					tip.tooltipText = tooltipTextWhileLocked;
				}
				else if (tooltipFromItems)
				{
					tip.tooltipText = "";
					if (tip.tooltipTitle != "")
						tip.tooltipText += "\n";
					tip.tooltipText += lockedItems.tooltipText;
				}
			}
			//Unstashed
			else
			{
				if (costAsTitle)
				{
					tip.tooltipTitle = "Cost: " + ColorPalette.ColorText(ColorPalette.cp.yellow4, minimumPoints.ToString());
				}

				if (tooltipFromItems)
				{
					tip.tooltipText = "";
					if (tip.tooltipTitle != "")
						tip.tooltipText += "\n";
					tip.tooltipText += lockedItems.tooltipText;
				}
				else if (stashedTooltipText != "")
				{
					tip.tooltipText = stashedTooltipText;
					stashedTooltipText = "";
				}
			}

			if (passKey != null && MetaGameManager.keys.Contains(passKey))
			{
				if (costAsTitle)
					tip.tooltipTitle = ColorPalette.ColorText(ColorPalette.cp.blue4, "Unlocked!");
			}

			if (tip.tooltipText != "" || tip.tooltipTitle != "")
				tip.enabled = true;
		}

		//UI
		if (lockedUI != null)
		{
			lockedUI.interactable = IsUnlocked;
		}

		//Skip the line
		if (passKey != null && MetaGameManager.keys.Contains(passKey))
		{
			if (lockedUI != null)
			{
				lockedUI.interactable = true;
			}

			if (lockedItems != null)
			{
				lockedItems.Unleash();
			}
		}
		else if (!IsUnlocked && !showWhenLocked)
		{
			gameObject.SetActive(false);
		}
	}
}
