using UnityEngine;
using System.Collections;

public class LoadingChecker : MonoBehaviour
{

	private bool loading = false, rescan = false;


	void Update()
	{
		//Stuff to do on a load!
		if (loading)
		{
			//Load in new data
			SaveLoad.s.LoadGame();

			//Set camera bounds
			CameraBoundsSetter.cbs.SetCameraBounds();

			//We're done
			loading = false;

			//Next frame rescan
			rescan = true;
		}
		else if (rescan)
		{
			//Scan pathing
			AstarPath.active.Scan();
			//Definitely only do this once
			rescan = false;
		}
	}

	void Start()
	{
		//We're starting the game. Carry on the course with a fresh game. What about a load?
		if (SaveLoad.s != null && SaveLoad.s.currentData != null && SaveLoad.s.currentData ["runExists"])
		{
			loading = true;
			//Update will now change our stats and scene
		}
	}
}
