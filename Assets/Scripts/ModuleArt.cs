using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModuleArt : MonoBehaviour
{

	public enum space
	{
		Edge,
		//Any piece with three open sides (on 2x3s and 3x3s)
		Corner,
		//Pieces in the corner with two open sides (2x3s and 3x3s)
		Hall,
		//Pieces in the middle of 1x3s
		Center,
		//Pieces in the middle of 3x3s
		End
		//Pieces on the ends of 1x3s
	}

	public space areaType;
	private SpriteRenderer sprite;
	public bool lockToPredefined = false;
	public int spriteSignature = 0;

	void Start()
	{
		sprite = GetComponent<SpriteRenderer>();

		ChooseArt();
	}


	public void ChooseArt()
	{
		//Assign predefined art
		if (lockToPredefined)
		{
			//Safety, if called early
			if (sprite == null)
				sprite = GetComponent<SpriteRenderer>();
			//Alright, we're ready to get the index
			sprite.sprite = ModuleArtSource.art.GetSpriteFromAllListsAtIndex(spriteSignature);
		}

		//Or choose a module art randomly, if this piece is chosen to have one
		else if (Random.Range(0, 2) == 0)
		{
			//List of potential art
			List<Sprite> potentialArt = new List<Sprite>();
			//Add "any" art
			potentialArt.AddRange(ModuleArtSource.art.any);
			
			//Add specific art
			switch (areaType)
			{
			case space.Edge:
				potentialArt.AddRange(ModuleArtSource.art.edge);
				break;
			case space.Corner:
				potentialArt.AddRange(ModuleArtSource.art.corner);
				potentialArt.AddRange(ModuleArtSource.art.edge);
				break;
			case space.Hall:
				potentialArt.AddRange(ModuleArtSource.art.hall);
				break;
			case space.Center:
				potentialArt.AddRange(ModuleArtSource.art.center);
				break;
			case space.End:
				potentialArt.AddRange(ModuleArtSource.art.corner);
				potentialArt.AddRange(ModuleArtSource.art.hall);
				break;
			}
			
			//If no art, get out
			if (potentialArt.Count <= 0)
				return;
			
			//Choose a piece
			sprite.sprite = potentialArt [Random.Range(0, potentialArt.Count)];

			//Get the signature
			spriteSignature = ModuleArtSource.art.GetIndexOfSprite(sprite.sprite);
		}
		//If not selected, turn off the sprite renderer
		else
		{
			sprite.enabled = false;
		}
	}

	/**Reset this art to a blank slate, ready to be reassigned.
	 */
	public void ClearArt()
	{
		sprite.enabled = true;
		sprite.sprite = null;
		lockToPredefined = false;
	}
}
