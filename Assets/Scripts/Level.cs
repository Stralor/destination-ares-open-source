using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public static class Level
{

	/**Unload old scene after loading new scene. Call as coroutine to guarantee save data safety.
	 */
	public static IEnumerator MoveToScene(string sceneToOpen)
	{
		//Be sure we aren't saving
		yield return new WaitUntil(() => !SaveLoad.s.currentlySaving);

		SceneManager.LoadScene(sceneToOpen, LoadSceneMode.Single);
	}

	/**Load a scene.
	 */
	public static void AddScene(string sceneToOpen)
	{
		//Load the scene
		SceneManager.LoadScene(sceneToOpen, LoadSceneMode.Additive);
	}

	/**Close the target scene. If sceneToClose is empty, will close active scene.
	 */
	public static void CloseScene(string sceneToClose = "")
	{
		//Default case (active scene)
		if (sceneToClose == "")
			sceneToClose = SceneManager.GetActiveScene().name;

		//Now targetted, unload
		if (SceneManager.GetSceneByName(sceneToClose).isLoaded)
			SceneManager.UnloadSceneAsync(sceneToClose);
	}

	/**Checks to see if Manager is open. If not, opens it.
	 */
	//	public static void AddManager()
	//	{
	//		if (!SceneManager.GetSceneByName("Manager").isLoaded)
	//			AddScene("Manager", false);
	//	}

	/**Destructive. Unloads old scene once new scene is loaded in. (Always sets new scene as the active scene)
	 * Returns the AsyncOperation if you want to track loading.
	 * An anchor is necessary to call a coroutine, since Level does not derive from MonoBehaviour.
	 */
	//	public static AsyncOperation MoveToScene(string sceneToOpen, string sceneToClose, MonoBehaviour anchor)
	//	{
	//		var asyncScene = AddScene(sceneToOpen);
	//
	//		anchor.StartCoroutine(WaitToClose(sceneToClose, asyncScene));
	//
	//		return asyncScene;
	//
	//		return SceneManager.LoadSceneAsync(sceneToOpen, LoadSceneMode.Single);
	//	}

	/**Load a scene.
	 * Returns the AsyncOperation if you want to track loading.
	 */
	//	public static AsyncOperation AddScene(string sceneToOpen, bool setToActive = true)
	//	{
	//		//The operation
	//		var asyncScene = SceneManager.LoadSceneAsync(sceneToOpen, LoadSceneMode.Additive);
	//
	//		//Set the scene to active?
	//		if (setToActive)
	//			SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToOpen));
	//
	//		//Return
	//		return asyncScene;
	//	}

	/**Delays closing until the next scene is ready and active.
	 */
	//	static IEnumerator WaitToClose(string sceneToClose, AsyncOperation asyncOp)
	//	{
	//		//Is the scene done? Is it activated? Yes and yes? Move on.
	//		yield return new WaitUntil(() => asyncOp.isDone && asyncOp.allowSceneActivation);
	//
	//		//Close!
	//		CloseScene(sceneToClose);
	//	}



	//	/*
	//	 * OLD SHIT FROM HERE BELOW
	//	 */
	//
	//	//Singleton stuff
	//
	//	private static Level _s;
	//
	//	[System.Obsolete("Originally used old Application loading. Moved to SceneManager, stopped functioning perfectly. New static functions are streamlined and more effective.")]
	//	/**Singleton ref. Unlike some singletons in my solution, this one is created when called.
	//	 * It also destroys itself when it's done with a load.
	//	 */
	//	public static Level loader
	//	{
	//		get
	//		{
	//			if (_s == null)
	//			{
	//				GameObject gO = new GameObject();
	//				gO.name = "Level Loader";
	//
	//				_s = gO.AddComponent<Level>();
	//
	//				DontDestroyOnLoad(gO);
	//			}
	//			return _s;
	//		}
	//	}
	//
	//	//Declarations
	//
	//	/**Is the async load finished and ready to be activated? */
	//	public bool loaded { get; private set; }
	//
	//	/**What's the async load's progress? */
	//	public float progress
	//	{
	//		get
	//		{
	//			if (asyncLoad != null)
	//				return asyncLoad.progress / 0.9f;
	//			else
	//				return 0;
	//		}
	//	}
	//
	//	//The async process
	//	AsyncOperation asyncLoad;
	//
	//
	//	//Methods
	//
	//	/**Load in a new scene. We'll change over to it!
	//	 * Won't activate by default! Call ActivateLoadedScene when ready.
	//	 */
	//	public void StartLoadingAsync(string levelName, bool withManager = true)
	//	{
	//		StartCoroutine(LoadAsync(levelName, withManager));
	//	}
	//
	//	IEnumerator LoadAsync(string levelName, bool withManager)
	//	{
	//		asyncLoad = null;
	//
	//		asyncLoad = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);
	//		asyncLoad.allowSceneActivation = false;
	//
	//		//Are we preserving the manager scene? (Make sure it doesn't already exist)
	//		if (withManager)
	//		{
	//			bool hasManager = SceneManager.GetSceneByName("Manager").isLoaded;
	////			for (int i = 0; i < SceneManager.sceneCount; i++)
	////			{
	////				if (SceneManager.GetSceneAt(i).name == "Manager")
	////					hasManager = true;
	////			}
	//
	//			if (!hasManager)
	//				SceneManager.LoadScene("Manager", LoadSceneMode.Additive);
	//		}
	//
	//		yield return asyncLoad;
	//	}
	//
	//	/**Load in another scene in addition to what's open!
	//	 * Won't activate by default! Call ActivateLoadedScene when ready.
	//	 */
	//	public void StartLoadingAdditive(string levelName)
	//	{
	//		StartCoroutine(LoadAdditive(levelName));
	//	}
	//
	//	IEnumerator LoadAdditive(string levelName)
	//	{
	//		asyncLoad = null;
	//		asyncLoad = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
	//		asyncLoad.allowSceneActivation = false;
	//		yield return asyncLoad;
	//	}
	//
	//	public void ActivateLoadedScene()
	//	{
	//		asyncLoad.allowSceneActivation = true;
	//	}
	//
	//	void Update()
	//	{
	//
	//		if (asyncLoad != null)
	//		{
	//
	//			//Are we loaded? (Eh heh heh)
	//			if (asyncLoad.progress >= 0.9f)
	//			{
	//				//Let shit know
	//				loaded = true;
	//			}
	//
	//			//Load is finished, called, and wrapped up. We're done here, until next tiiiime.
	//			if (asyncLoad.isDone)
	//			{
	//				Destroy(gameObject);
	//			}
	//		}
	//	}
	//
	//	void Awake()
	//	{
	//		if (_s == null)
	//		{
	//			_s = this;
	//			DontDestroyOnLoad(gameObject);
	//		}
	//		else if (this != _s)
	//			Destroy(gameObject);
	//	}
}