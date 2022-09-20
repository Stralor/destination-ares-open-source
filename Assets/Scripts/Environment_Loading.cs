using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment_Loading : Environment
{
	public Transform loadingChar;

	public void Tutorial()
	{
		//Reset events
		if (EventTree.s != null)
			Destroy(EventTree.s.gameObject);

		if (StartingResources.sRes == null)
		{
			var go = new GameObject();
			go.AddComponent<StartingResources>();
		}

		//Let's prep our resources!
		SaveLoad.s.LoadShip("Caelifera (beginner)", null, SetResources, willCatchError: false, spritesOnly: true);

		//Set the ship to load
		StartingResources.sRes.shipName = "Epimetheus";
		StartingResources.sRes.shipLoadFile = "Caelifera (beginner)";
		StartingResources.sRes.isReady = true;

		//Story
		StoryChooser.story.ChooseStory("Refugee Ship");

		//To Setup we GO!
		StartingResources.sRes.StartCoroutine(Level.MoveToScene("Main"));
	}

	void SetResources(int[] res)
	{
		UnityEngine.Assertions.Assert.IsTrue(res.Length == 6);

		StartingResources.sRes.air = res [0];
		StartingResources.sRes.food = res [1];
		StartingResources.sRes.fuel = res [2];
		StartingResources.sRes.materials = res [3];
		StartingResources.sRes.parts = res [4];
		StartingResources.sRes.waste = res [5];
	}

	protected override void Start()
	{
		base.Start();

		loadingChar.GetComponent<CharacterColors>().Randomize();

		Tutorial();
	}
}
