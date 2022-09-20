using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class SpeechData : ScriptableObject
{
	/* Storage class for a single character speech item.
	 */

	public CharacterSpeech.Personality[] personalities;
	public Character.Thought[] triggers;

	[TextArea(1, 8)]
	public string text;
}
