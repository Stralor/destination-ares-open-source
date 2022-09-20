using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Unlockable : ScriptableObject
{
	public MetaGameKey keyUnlocked;
	public string colorName = "";
	public bool unlocksPersonality, unlocksRole, unlocksKeyword;
	public CharacterSpeech.Personality personality;
	public Character.CharRoles role;
	public ShipSystem.SysKeyword keyword;

	[TextArea]
	public string tooltipText;

	//TODO event unlockables (after upgrade to scriptableobj events)
	//public EventStore;

	public int cost
	{
		get
		{
			int value = 0;

			if (keyUnlocked != null)
				value += keyUnlocked.cost;
			if (colorName != "")
				value += 10;
			if (unlocksKeyword)
				value += 20;
			if (unlocksPersonality)
				value += 10;
			if (unlocksRole)
				value += 20;

			return value;
		}
	}

	public void Unleash()
	{
		if (keyUnlocked != null)
		{
			MetaGameManager.AddKey(keyUnlocked);
		}

		if (colorName != "")
		{
			if (!CharacterColors.unlockedColors.Contains(colorName))
				CharacterColors.unlockedColors.Add(colorName);
		}

		if (unlocksPersonality)
		{
			if (!CharacterSpeech.unlockedPersonalities.Contains(personality))
				CharacterSpeech.unlockedPersonalities.Add(personality);
		}

		if (unlocksRole)
		{
			if (!Character.unlockedRoles.Contains(role))
				Character.unlockedRoles.Add(role);
		}

		if (unlocksKeyword)
		{
			if (!ShipSystem.unlockedKeywords.Contains(keyword))
				ShipSystem.unlockedKeywords.Add(keyword);
		}

		if (!MetaGameManager.unlockables.Contains(this))
			MetaGameManager.unlockables.Add(this);
	}

	public bool CanBuyUnlockable()
	{
		//Can buy it?
		if (MetaGameManager.currentUnlockPoints >= cost)
		{
			return true;
		}

		//All other cases
		return false;
	}

	public void BuyUnlockable()
	{
		//Can buy it?
		if (CanBuyUnlockable())
		{
			//Buy it
			MetaGameManager.currentUnlockPoints -= cost;

			Unleash();

			//Audio
			AudioClipOrganizer.aco.PlayAudioClip("Quality Up", null);

			//Save
			SaveLoad.s.SaveMetaGame();
		}
		else
		{
			//Invalid, can't afford
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
		}
	}
}
