using UnityEngine;
using System.Collections;

/**For inspector events.
 */
public class AudioReference : MonoBehaviour
{

	public void PlayClipGameSFX(string clip)
	{
		AudioClipOrganizer.aco.PlayAudioClip(clip, transform);
	}

	public void PlayClipUISFX(string clip)
	{
		AudioClipOrganizer.aco.PlayAudioClip(clip, null);
	}
}
