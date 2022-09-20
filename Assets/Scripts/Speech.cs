using UnityEngine;
using System.Collections;

public class Speech : MonoBehaviour
{

	/* Storage class for a single character speech item.
	 */

	public CharacterSpeech.Personality[] personalities;
	public Character.Thought[] triggers;

	[TextArea(1, 8)]
	public string text;
}
