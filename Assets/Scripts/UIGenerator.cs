using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIGenerator : MonoBehaviour
{

	/* This script attaches the UI prefab to all systems and characters on screen when the game starts
	 */


	public static UIGenerator gen;


	List<GameObject> units = new List<GameObject>();
	//The chars and systems in game
	public GameObject fCanvas;
	//The canvas prefab to instantiate



	void Awake()
	{
		if (gen == null)
		{
			gen = this;
		}
		else if (gen != this)
		{
			Destroy(this);
		}
	}

	void Start()
	{
		//Populate the list of units
		units.AddRange(GameObject.FindGameObjectsWithTag("Char&Sys"));
		//Instantiate the canvases
		foreach (GameObject unit in units)
		{
			AttachFollowCanvas(unit);
		}
	}

	public void AttachFollowCanvas(GameObject target)
	{
		GameObject go = (GameObject)Instantiate(fCanvas);
		//Make the canvas a child of unit
		go.transform.SetParent(target.transform, false);
		//Center it
		go.transform.localPosition = new Vector3(0, 0, .001f);
		//Match canvas size to unit, change later to match with sprites
		if (target.name.Contains("Large"))
		{
			RectTransform goRect = (RectTransform)go.transform;
			goRect.sizeDelta = new Vector2(90, 90);
		}
		//Assign Event Camera
		go.GetComponent<Canvas>().worldCamera = Camera.main;
	}
}
