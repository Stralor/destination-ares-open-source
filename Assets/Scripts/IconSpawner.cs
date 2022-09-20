using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IconSpawner : MonoBehaviour
{

	/**Singleton reference */
	public static IconSpawner ico;

	public GameObject iconToSpawn;

	public enum Direction
	{
		None,
		Increment,
		Decrement,
		NegativeDecrement
	}

	public Sprite airSymbol, energySymbol, foodSymbol, fuelSymbol, headingSymbol, materialsSymbol, partsSymbol, thrustSymbol, wasteSymbol, invalidSymbol;

	/**Pool of available icons. Icons will return here after use. */
	private List<GameObject> iconPool = new List<GameObject>();



	/**Spawns a moving resource icon representing increment or decrement. The icon then fades out.
	 */
	public void SpawnResourceIcon(string resourceName, Transform targetLocation, Direction direction)
	{
		//No None anim. Don't make the icon. Same goes for null locations. We don't like those.
		if (direction == Direction.None || targetLocation == null)
			return;

		//Choose the proper icon
		Sprite symbol;
		switch (resourceName.ToLower())
		{
		case "air":
			symbol = airSymbol;
			break;
		case "energy":
			symbol = energySymbol;
			break;
		case "food":
			symbol = foodSymbol;
			break;
		case "fuel":
			symbol = fuelSymbol;
			break;
		case "heading":
			symbol = headingSymbol;
			break;
		case "materials":
			symbol = materialsSymbol;
			break;
		case "parts":
			symbol = partsSymbol;
			break;
		case "thrust":
			symbol = thrustSymbol;
			break;
		case "waste":
			symbol = wasteSymbol;
			break;
		case "invalid":
			symbol = invalidSymbol;
			break;
			
		default :
			symbol = null;
			break;
		}

		//Get an icon from the pool
		GameObject icon = GetIconFromPool();
		//Set Active!
		icon.SetActive(true);
		//Set a name, for sorting! Name will reset shortly into anim.
		if (direction == Direction.Decrement || direction == Direction.NegativeDecrement)
			icon.name = "Down";
		else if (direction == Direction.Increment)
			icon.name = "Up";
		
		//Assign to target
		icon.transform.parent = targetLocation;
		icon.transform.position = new Vector3(targetLocation.position.x, targetLocation.position.y, 0);
		
		//Stagger position if there are multiple on the same target headed in the same direction
		int othersInSameDirection = 0;
		//Search the pool
		foreach (var other in iconPool)
		{
			//Is this one also on this object?
			if (other.transform.IsChildOf(targetLocation) && other != icon)
			{
				//Is it headed in the same direction? (Sorted by assigned name!)
				if (icon.name.Equals(other.name))
				{
					//Great! Count it.
					othersInSameDirection++;
					//Move it.
					other.transform.position = new Vector3(other.transform.position.x - 0.2f, other.transform.position.y, other.transform.position.z);
					//Move self.
					icon.transform.position = new Vector3(icon.transform.position.x + 0.2f * othersInSameDirection, icon.transform.position.y, icon.transform.position.z);
				}
			}
		}
		
		//Set the symbol and the shadow
		SpriteRenderer sprite = icon.GetComponentInChildren<SpriteRenderer>();
		SpriteRenderer shadow = sprite.transform.GetChild(0).GetComponent<SpriteRenderer>();
		sprite.sprite = symbol;
		shadow.sprite = symbol;

		//Activate, based on direction
		if (direction == Direction.Increment)
		{
			sprite.color = ColorPalette.cp.blue3;
			sprite.GetComponent<Animator>().SetTrigger("Increment");
		}
		else if (direction == Direction.Decrement)
		{
			sprite.color = ColorPalette.cp.yellow3;
			sprite.GetComponent<Animator>().SetTrigger("Decrement");
		}
		else if (direction == Direction.NegativeDecrement)
		{
			sprite.color = ColorPalette.cp.red3;
			sprite.GetComponent<Animator>().SetTrigger("Decrement");
		}
	}

	/**Return an icon that's finish its task to the pool for future use.
	 * (This class used to remove icons from pool, but now just uses SetActive on them.
	 *  This method still handles both circumstances.)
	 */
	public void ReturnToPool(GameObject icon)
	{
		//Reset transform to this
		icon.transform.parent = transform;
		//Put in pool
		if (!iconPool.Contains(icon))
			iconPool.Add(icon);

		//Deactivate the icon
		icon.SetActive(false);
	}

	/**Grabs an available icon from the pool. If it can't, it calls CreateIconInPool, then returns the results.
	 */
	private GameObject GetIconFromPool()
	{

		foreach (var i in iconPool)
		{
			if (!i.activeInHierarchy)
			{
				return i;
			}
		}

		//None found. Make one!
		return CreateIconInPool();
	}

	/**Creates and adds an icon to the pool.
	 */
	private GameObject CreateIconInPool()
	{

		//Create the icon
		GameObject icon = Instantiate(iconToSpawn) as GameObject;
		//Establish the parent
		icon.transform.parent = transform;

		//Add to pool
		iconPool.Add(icon);

		//Start not active.
		icon.SetActive(false);

		//Return our new icon
		return icon;
	}


	void Start()
	{
		//Create some icons for the pool
		for (int i = 0; i < 10; i++)
		{
			CreateIconInPool();
		}
	}

	void Awake()
	{
		if (ico == null)
		{
			ico = this;
		}
		else if (ico != this)
			Destroy(this);
	}

}
