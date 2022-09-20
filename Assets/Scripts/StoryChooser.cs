using UnityEngine;
using System.Collections;

public class StoryChooser : MonoBehaviour
{
	private static StoryChooser _story;

	public static StoryChooser story
	{
		get
		{
			if (_story == null)
			{
				_story = new GameObject().AddComponent<StoryChooser>();
				DontDestroyOnLoad(_story.gameObject);
			}
			return _story;
		}
	}

	/**Start a story series! Waits until stuff has loaded in Main, so feel free to call it early. Case insensitive.
	 */
	public void ChooseStory(string storyName)
	{
		StartCoroutine(ChooseStoryWhenReady(storyName));
	}


	IEnumerator ChooseStoryWhenReady(string storyName)
	{
		//Wait
		yield return new WaitUntil(() => GameReference.r != null && ShipResources.res != null);

		//Find the possible stories
		bool foundit = false;
		foreach (var t in Resources.LoadAll<GameEventSeriesData>("GameEventSeriesData"))
		{
			//Get the one we want
			if (t.storyName.ToLower() == storyName.ToLower())
			{
				//Start it!
				t.ChooseMe();
				foundit = true;
				break;
			}
		}

		//Debug
		if (!foundit)
			Debug.Log(storyName + " not found. Did not start story series.");
	}
}
