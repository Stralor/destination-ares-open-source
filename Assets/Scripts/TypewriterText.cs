using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

public static class TypewriterText
{

	/**Fills the target Text with the words/ chunks (formerly characters) of inputText, one at a time, at rate typingSpeed.
	 * Basically, it's a typewriter effect.
	 */
	public static IEnumerator TypeText(Text target, string inputText, float typingSpeed = 0.1f, bool ignoreTimeScale = false)
	{
		//Don't bother if we have nulls
		if (target == null || inputText == null)
			yield break;

		var typeWriter = new System.Text.StringBuilder();

		float lastMatchModifier = 1;

		//Let's change this to words
		foreach (var match in Regex.Matches(inputText, @"\W*\w{0,4}\W*"))
		{
			//Audioo
			target.SendMessageUpwards("PlayTypewriterAudio", SendMessageOptions.DontRequireReceiver);

			//Cache the lastMatchModifier for this match, because we're about to change it
			float thisModifier = lastMatchModifier;

			foreach (var letter in match.ToString())
			{
				typeWriter.Append(letter);

				target.text = typeWriter.ToString();

				if (ignoreTimeScale)
					yield return new WaitForSecondsRealtime(typingSpeed * thisModifier / match.ToString().Length / 2);
				else
					yield return new WaitForSeconds(typingSpeed * thisModifier / match.ToString().Length / 2);
			}

			//Slow after a punctuation break
			if (Regex.IsMatch(match.ToString(), @"\W*\w+\W+"))
			{
				//Less slow if it's just a space
				if (Regex.IsMatch(match.ToString(), @"\W*\w+\s+"))
					lastMatchModifier = 0.8f;
				else
					lastMatchModifier = 0.5f;
			}
			//Otherwise go fast
			else
			{
				lastMatchModifier = 1.2f;
			}

			yield return 0;
			if (ignoreTimeScale)
				yield return new WaitForSecondsRealtime(typingSpeed * thisModifier / 2);
			else
				yield return new WaitForSeconds(typingSpeed * thisModifier / 2);
		}

//		//Iterate through the letters
//		foreach (char letter in inputText.ToCharArray())
//		{
//
//			if (letter.Equals(" "))
//				continue;
//
//			typeWriter.Append(letter);
//
//			//target.text += letter;
//			target.text = typeWriter.ToString();
//
//			target.SendMessageUpwards("PlayTypewriterAudio", SendMessageOptions.DontRequireReceiver);
//
//			yield return 0;
//			if (ignoreTimeScale)
//				yield return target.StartCoroutine(CoroutineUtil.WaitForRealSeconds(typingSpeed));
//			else
//				yield return new WaitForSeconds(typingSpeed);
//		}
	}
}
