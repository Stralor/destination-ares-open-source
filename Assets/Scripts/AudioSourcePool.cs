using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**Requires "AudioSrc" prefab in Resources.
 */
public static class AudioSourcePool
{
	static List<GameObject> srcs = new List<GameObject>();


	/**Fetches an AudioSrc with unknown settings. Be sure to calibrate it.
	 */
	public static GameObject GetFreshSource()
	{
		GameObject src;

		//Clear nulls
		srcs.RemoveAll(obj => obj == null);

		//Find available pre-created instance
		if (srcs.Exists(obj => !obj.activeSelf))
			src = srcs.Find(obj => !obj.activeSelf);
		//Or create one
		else
		{
			src = GameObject.Instantiate(Resources.Load("AudioSrc")) as GameObject;
		}

		//Be sure the pool has it
		if (!srcs.Contains(src))
			srcs.Add(src);

		//Make it active!
		src.SetActive(true);

		//Give it
		return src;
	}

	/**Put an AudioSrc back in the pool.
	 */
	public static void ReturnSource(GameObject src)
	{
		//Don't add in nulls
		if (src == null)
			return;

		src.GetComponent<AudioSource>().loop = false;

		//Turn it off
		src.SetActive(false);

		//Add it if we don't have it
		if (!srcs.Contains(src))
			srcs.Add(src);
	}
}
