using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MetaGameManager
{
	internal static List<MetaGameKey> keys = new List<MetaGameKey>();
	internal static List<Unlockable> unlockables = new List<Unlockable>();

	internal static int _tot = 0, _cur = 0;

	internal static int totalUnlockPoints
	{
		get
		{
			return _tot;
		}
		set
		{
			_tot = value;
		}
	}

	internal static int currentUnlockPoints
	{
		get
		{
			return _cur;
		}
		set
		{
			//Pos clamp
			value = value < 0 ? 0 : value;

			//Only add to total
			if (value > _cur)
				_tot += value - _cur;

			//New current
			_cur = value;
		}
	}

	/**Adds the key. Won't add duplicates.
	 */
	public static void AddKey(string name)
	{
		if (!keys.Exists(obj => obj.name == name))
		{
			MetaGameKey key = Resources.Load<MetaGameKey>("Keys/" + name);
			AddKey(key);
		}
	}

	/**Adds the key. Won't add duplicates.
	 */
	public static void AddKey(MetaGameKey key)
	{
		if (!keys.Contains(key))
		{
			keys.Add(key);
		}
	}
}
