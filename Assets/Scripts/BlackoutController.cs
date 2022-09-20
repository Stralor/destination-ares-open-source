using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BlackoutController : MonoBehaviour
{
	public List<Image> blackouts = new List<Image>();

	public float fadeTime = 5;

	public void SetAllBlackouts(bool blackedOut)
	{
		blackouts.ForEach(obj => obj.gameObject.SetActive(blackedOut));
	}

	public void FadeBlackout(int index)
	{
		FadeAction(index, fadeTime);
	}

	public void FadeBlackoutFast(int index)
	{
		FadeAction(index, fadeTime / 3);
	}

	void FadeAction(int index, float time)
	{
		if (index < blackouts.Count)
		{
			if (blackouts [index].isActiveAndEnabled)
			{
				blackouts [index].CrossFadeAlpha(0, fadeTime, true);
				StartCoroutine(Toggle(blackouts [index].gameObject, false));
			}
		}
		else
		{
			Debug.LogWarning("There is not a blackout at index " + index);
		}
	}

	public void ShowBlackout(int index)
	{
		if (index < blackouts.Count)
		{
			if (blackouts [index].isActiveAndEnabled)
			{
				blackouts [index].gameObject.SetActive(true);
				blackouts [index].CrossFadeAlpha(1, fadeTime, true);
			}
		}
		else
		{
			Debug.LogWarning("There is not a blackout at index " + index);
		}
	}

	IEnumerator Toggle(GameObject target, bool active)
	{
		yield return new WaitForSecondsRealtime(fadeTime);

		target.SetActive(active);
	}
}
