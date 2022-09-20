using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StarSpawner : MonoBehaviour
{

	/**The star object to be instantiated repeatedly. */
	public GameObject starPrefab;

	/**How many to instantiate. */
	public int maxNumberOfStars;

	public float xClamp, yClamp;

	/**What sprites to use. */
	public Sprite[] starTypes;

	/**How large each "pixel" of my assets are in Unity's unit system, when imported at intended 96 pixels-per-unit. */
	private const float PIXEL_SIZE = 0.0625f;

	private List<GameObject> starList = new List<GameObject>();


	//	void Update()
	//	{
	//		foreach (var star in starList)
	//		{
	//			//this can't be good for processing... HINT IT'S NOT (+6ms/frame)
	//			star.transform.eulerAngles = CameraEffectsController.cec.transform.parent.eulerAngles;
	//		}
	//	}

	void Start()
	{
		//k, make stars
		GridControl.SpawnGameObjects(starPrefab, maxNumberOfStars, xClamp, yClamp, ref starList, PIXEL_SIZE, parent: this.transform);
		//Set the images
		starList.ForEach(obj => obj.GetComponent<SpriteRenderer>().sprite = starTypes [Random.Range(0, starTypes.Length)]);
		starList.ForEach(obj => SetStarAlpha(obj.GetComponent<SpriteRenderer>()));
	}

	void SetStarAlpha(SpriteRenderer sprite)
	{
		float alpha = Mathf.Abs((1 - (Mathf.Abs(sprite.transform.position.x) / xClamp)) * (1 - (Mathf.Abs(sprite.transform.position.y) / yClamp)));
		sprite.color = new Color(1, 1, 1, alpha);
	}
}
