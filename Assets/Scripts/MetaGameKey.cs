using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MetaGameKey : ScriptableObject
{
	public int cost;

	public bool CanBuyKey()
	{
		//Can buy it?
		if (MetaGameManager.currentUnlockPoints >= cost)
		{
			//Only if we don't already have it, too
			if (!MetaGameManager.keys.Contains(this))
			{
				return true;
			}
		}

		//All other cases
		return false;
	}

	public void BuyKey()
	{
		//Can buy it?
		if (CanBuyKey())
		{
			//Buy it
			MetaGameManager.currentUnlockPoints -= cost;
			MetaGameManager.AddKey(this);

			//Audio
			AudioClipOrganizer.aco.PlayAudioClip("Quality Up", null);
		}
		else
		{
			//Invalid, can't afford
			AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);
		}
	}
}
