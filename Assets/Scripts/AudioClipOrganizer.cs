using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class AudioClipOrganizer : MonoBehaviour
{

	/**Singleton-like reference to the AudioClipOrganizer component.
	 */
	public static AudioClipOrganizer aco;

	//Audio lists
	public List<AudioClip> beep = new List<AudioClip>(), warn = new List<AudioClip>(), voices = new List<AudioClip>(), talk = new List<AudioClip>(), ambient = new List<AudioClip>(), eventStart = new List<AudioClip>(),
		emergencyAlert = new List<AudioClip>(), warningAlert = new List<AudioClip>(), useAlert = new List<AudioClip>(), click = new List<AudioClip>(), pop = new List<AudioClip>(), transmission = new List<AudioClip>(),
		construct = new List<AudioClip>(), repair = new List<AudioClip>(),	heal = new List<AudioClip>(), hurt = new List<AudioClip>(), kickOff = new List<AudioClip>(),
		succeed = new List<AudioClip>(), fail = new List<AudioClip>(), qualityUp = new List<AudioClip>(), qualityDown = new List<AudioClip>(),
		hover = new List<AudioClip>(), hoverEnd = new List<AudioClip>(), press = new List<AudioClip>(), invalid = new List<AudioClip>(), death = new List<AudioClip>(), systemBreak = new List<AudioClip>(),
		electrolyser = new List<AudioClip>(), engine = new List<AudioClip>(), generator = new List<AudioClip>(), gym = new List<AudioClip>(), helm = new List<AudioClip>(),
		hydroponics = new List<AudioClip>(), radar = new List<AudioClip>(),	reactor = new List<AudioClip>(), scrubber = new List<AudioClip>(), toilet = new List<AudioClip>();

	//Meta list of these fields from reflections
	List<FieldInfo> metaList = new List<FieldInfo>();

	//Be sure these are all lowercase
	private List<string> pitchShiftThese
	{
		get
		{
			return new List<string>()
			{
				"talk",
				"repair",
				"heal",
				"kickoff",
				"construct",
				"softping",
				"warn",
				"hover",
				"click"
			};
		}
	}


	/*
	 * PUBLIC METHODS
	 */

	/**Play the audio clip. 3D from parent if parent is not null, else 2D.
	 * Also sets the pitch, etc, based on the clip.
	 */
	public void PlayAudioClip(AudioClip clip, Transform parent, bool pitchShift = false)
	{
		//If the chosen clip is null, just exit. This is intentional, not every clip slot may be in the game.
		if (!clip)
			return;

		//Get the AudioSource
		var src = GetParentedSource(parent);

		//Settings
		PitchShift(pitchShift, src);
		src.clip = clip;

		//Play
		StartCoroutine(PlayWhenActiveThenReturnToPool(src));
	}

	/**Assign and play the audio clip on the chosen source.
	 * Overload method that finds the proper clip by name (case insensitive, random from chosen list) and passes it to the main version of this method.
	 */
	public void PlayAudioClip(string name, Transform parent, int priority = 128)
	{
		bool pitchShift = false;

		//Get the clip
		AudioClip clip = GetClip(name);

		//Pitch shift?
		if (pitchShiftThese.Contains(name.ToLower()))
			pitchShift = true;

		//Pass the clip to the main method
		PlayAudioClip(clip, parent, pitchShift);
	}

	/**Custom PlayAudioClip for locations w/o transforms to child to (the audio will be stationary!)
	 * Still treats the audio as GameSFX
	 */
	public void PlayAudioClip(string name, Vector2 position, int priority = 128)
	{
		bool pitchShift = false;

		//Pitch shift?
		if (pitchShiftThese.Contains(name.ToLower()))
			pitchShift = true;

		//The sauce
		var src = GetSourceForCustomPlay(clipName: name, randomPitch: pitchShift);

		//Location
		src.transform.position = position;

		//Treat as GameSFX
		src.spatialBlend = 0.7f;
		src.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Game") [0];
		src.priority = priority;

		//Play
		StartCoroutine(PlayWhenActiveThenReturnToPool(src));
	}

	/**Returns an AudioSource to manage yourself.
	 * You'll have to call play and return it to the pool when done.
	 * "clip" has priority over "clipName" for setting source clip.
	 * Note: parent still determines Game SFX or UI (but now you can change that).
	 */
	public AudioSource GetSourceForCustomPlay(AudioClip clip = null, string clipName = "", bool randomPitch = false, Transform parent = null)
	{
		var src = GetParentedSource(parent);

		//Decide clip, if appropriate
		if (clip != null)
			src.clip = clip;
		else if (clipName != "")
			src.clip = GetClip(clipName);

		//Set default pitchShift (likely to change anyway, with a custom)
		PitchShift(randomPitch, src);

		return src;
	}

	public IEnumerator PlayWhenActiveThenReturnToPool(AudioSource src)
	{
		// Make sure active gets called
		src.gameObject.SetActive(true);

		//Wait until it's active (if the parent isn't, then, well, we'll be here awhile)
		yield return new WaitUntil(() => src == null || src.isActiveAndEnabled);

		//Lost it
		if (src == null)
			yield break;

		//Play
		src.Play();

		//Prep to return
		StartCoroutine(ReturnToPoolWhenNotPlaying(src));
	}

	public IEnumerator ReturnToPoolWhenNotPlaying(AudioSource src)
	{
		yield return new WaitUntil(() => !src || !src.isPlaying);

		if (!src)
			//The pool is self-cleaning, so we don't need to do any extra work here
			yield break;
		else
		{
			src.priority = 128;
			AudioSourcePool.ReturnSource(src.gameObject);
		}
	}

	/**Random clip from named list in the ACO
	 */
	public AudioClip GetClip(string listName)
	{
		List<AudioClip> list = null;

		//Compare and set list (search FieldInfos for matching list, then get the value of it from this instance of the ACO, casted)
		if (metaList.Exists(obj => obj.Name.ToLower().Equals(listName.ToLower())))
			list = (List<AudioClip>)metaList.Find(obj => obj.Name.ToLower().Equals(listName.ToLower())).GetValue(this);

		//Issues that prevent playing this clip
		if (list == null || list.Count == 0)
		{
			//Category doesn't exist
			//if (list == null)
			//	Debug.Log("AudioClip of type \"" + listName + "\" cannot be found.");

			//No viable result
			return null;
		}

		//No issues. Find one and return it!
		return list [Random.Range(0, list.Count)];
	}


	/*
	 * PRIVATE METHODS
	 */

	/**Adjust the pitch on the source.
	 * If true, random, else 1.
	 */
	void PitchShift(bool pitchShift, AudioSource src)
	{
		//Random pitch shift, break the monotony
		if (pitchShift)
			src.pitch = Random.Range(0.9f, 1.1f);
		else
			src.pitch = 1;
	}

	/**Gets an AudioSource from AudioSourcePool, sets initial values, incl. parenting.
	 */
	AudioSource GetParentedSource(Transform parent)
	{
		//AudioSource
		var src = AudioSourcePool.GetFreshSource().GetComponent<AudioSource>();
		//Use parent (Game SFX)
		if (parent != null)
		{
			src.transform.SetParent(parent);
			src.transform.localPosition = Vector3.zero;
			src.spatialBlend = 0.7f;
			src.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Game") [0];
		}
		//UI
		else
		{
			src.transform.SetParent(transform);
			src.spatialBlend = 0.0f;
			src.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("UI") [0];
		}



		return src;
	}

	void Awake()
	{
		//Set the ACO
		if (aco == null)
		{
			aco = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (aco != this)
		{
			Destroy(gameObject);
		}

		//Time for a little reflection!
		System.Type typ = typeof(AudioClipOrganizer);

		//Search this class for its fields
		foreach (var t in typ.GetFields())
		{
			//Find the appropriate lists
			if (t.FieldType.Equals(typeof(List<AudioClip>)))
				//Add these instances to the metalist
				metaList.Add(t);
		}
	}

}
