using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HullExtension : MonoBehaviour
{
	public List<Sprite> art = new List<Sprite>();

	public int artIndex;

	public void SetArt()
	{
		GetComponent<SpriteRenderer>().sprite = art [artIndex];

		//Solar to back TODO this is kinda temp until I get off my lazy ass and make ShipSystemArtSpawner work in Main and remove HullExtensions from SaveLoad
		if (artIndex == 5)
			GetComponent<SpriteRenderer>().sortingOrder = -7;
	}

	void OnEnable()
	{
		if (GameReference.r != null && !GameReference.r.allHullExtensions.Contains(this))	//Safety in case of initialization order
			GameReference.r.allHullExtensions.Add(this);
	}

	void OnDisable()
	{
		if (GameReference.r != null)
			GameReference.r.allHullExtensions.Remove(this);
	}
}
