using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class ModuleArtSource : MonoBehaviour
{

	public static ModuleArtSource art;

	public List<Sprite> any = new List<Sprite>();
	public List<Sprite> edge = new List<Sprite>();
	public List<Sprite> corner = new List<Sprite>();
	public List<Sprite> hall = new List<Sprite>();
	public List<Sprite> center = new List<Sprite>();

	private List<List<Sprite>> metaList = new List<List<Sprite>>();



	/**Find and return a specific sprite from among the lists by position in the lists.
	 * Index of 0 will return a null - a blank sprite.
	 */
	public Sprite GetSpriteFromAllListsAtIndex(int index)
	{

		//First, index all lists together into one 'length'
		int indexSize = 0;
		foreach (var t in metaList)
		{
			indexSize += t.Count;
		}

		//If our given index is outside the pool, or is just a simple 0, use a blank!
		if (index > indexSize || index == 0)
		{
			//Return the None sprite!
			return null;
		}

		//Otherwise, we'll find a sprite

		//Move through each list
		foreach (var t in metaList)
		{
			//Check if this the index refers to this list
			if (index <= t.Count)
				//Good, we found our result!
				return t [index - 1];
			//Otherwise, we need to cut this list's length out for the next one
			index -= t.Count;
		}

		//If the loop didn't find the proper index, for whatever stupid reason, return null (AKA, just resolving syntax)
		return null;
	}

	/**Find the index of a sprite from this class's lists.
	 * If the sprite is null, will return 0.
	 * If the sprite isn't in the lists, will return a value outside the total index.
	 * (GetSpriteFromAllListsAtIndex, the companion method, handles both of the latter cases the same way: returns a null)
	 */
	public int GetIndexOfSprite(Sprite sprite)
	{

		//Safety for nulls
		if (sprite == null)
			return 0;

		//We're gonna find this value
		int index = 1;

		//Go through the lists, find the sprite, adding to index as we go
		foreach (var t in metaList)
		{
			//Found the sprite?
			if (t.Contains(sprite))
			{
				//Finish the index!
				index += t.FindIndex(obj => obj == sprite);
				break;
			}
			//Otherwise, add the list's length and keep going
			index += t.Count;
		}

		return index;
	}


	void Awake()
	{
		if (art == null)
		{
			art = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (art != this)
			Destroy(gameObject);


		//A little reflection
		System.Type typ = typeof(ModuleArtSource);
		
		//Search this class for its fields
		foreach (var t in typ.GetFields())
		{
			//Find the sprite lists
			if (t.FieldType.Equals(typeof(List<Sprite>)))
				//Add art's instances to the metalist
				metaList.Add((List<Sprite>)t.GetValue(art));
		}
	}
}
