using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Environment_Memorial : Environment
{

	public Canvas menu;
	public ScrollRect memorial;

	public int memorialsToSpawnPerFrame = 5;

	public override void PressedCancel()
	{
		var fade = GetComponent<FadeChildren>();
		fade.onFadeOutFinish.AddListener(() => Level.CloseScene("Memorial"));
		fade.FadeOut();

		var startMenu = FindObjectOfType<Environment_StartMenu>();
		if (startMenu != null)
			startMenu.doPings = true;
	}

	protected override void Start()
	{
		base.Start();

		SaveData.UnloadStatTrack(SaveLoad.s.Peek());

		//Setup the memorial
		StartCoroutine("StaggeredSpawn");

		//Select Button
		GetComponentInChildren<Button>().Select();
		
		SceneManager.SetActiveScene(SceneManager.GetSceneByName("Memorial"));
	}


	//TODO Preload? This is a ridiculous and user-distracting, if technically effective, solution to the mass instantiation overloading the main thread.
	IEnumerator StaggeredSpawn()
	{
		int count = 0;

		foreach (var t in StatTrack.stats.memorialReversed)
		{
			GameObject go = DisplayMemorialListing(StatTrack.stats.memorial.FindIndex(obj => obj.Equals(t)));
			go.transform.SetParent(memorial.content);
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = Vector3.zero;

			count++;

			if (count > memorialsToSpawnPerFrame)
			{
				count = 0;
				yield return new WaitForEndOfFrame();
			}
		}
	}

	/**Spawn and return a single memorial listing, from StatTrack.stats.memorial[index].
	 * Remember to set the parent! (And maybe reset pos)
	 */
	public static GameObject DisplayMemorialListing(int index)
	{
		//Safety
		if (index >= StatTrack.stats.memorial.Count)
		{
			Debug.LogWarning("Invalid memorial number! Cannot load the entry at index " + index);
			return null;
		}

		StatTrack.Vessel t = StatTrack.stats.memorial [index];

		//Create each vessel object
		GameObject go = Instantiate(Resources.Load("Vessel") as GameObject);
		//Set values
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		VesselRef ves = go.GetComponent<VesselRef>();
		ves.vessel.text = t.vesselName;
		
		//Including the result!
		ves.result.text = t.result;
		if (t.survived)
			ves.result.color = ColorPalette.cp.blue4;
		else
			ves.result.color = ColorPalette.cp.red4;

		//TODO load screenshot of ship at end of journey --- pos -80y -500z and scale 0.2 worked pretty well as a child of an empty gameobject above vessel name (scrolls faster than body text, cool effect)
		
		//Now do the crew
		foreach (var u in t.crew)
		{
			//Create each vessel object
			GameObject go2 = Instantiate(Resources.Load("Crew Member") as GameObject);
			//Set values
			go2.transform.SetParent(ves.crewList);
			go2.transform.localScale = Vector3.one;
			go2.transform.localPosition = Vector3.zero;
			CrewRef crew = go2.GetComponent<CrewRef>();
			crew.crew.text = u.crewName;
			
			//Including the result!
			crew.result.text = u.result;
			if (u.survived)
				crew.result.color = ColorPalette.cp.blue4;
			else
				crew.result.color = ColorPalette.cp.red4; 
		}

		return go;
	}


	void Awake()
	{
		menu.worldCamera = Camera.main;
		menu.sortingLayerName = "Pop Ups";
		
		var startMenu = FindObjectOfType<Environment_StartMenu>();
		if (startMenu != null)
			startMenu.doPings = false;
	}
}
