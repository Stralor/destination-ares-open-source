using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeChildren : MonoBehaviour
{

	public static float fadeTime = 0.25f;

	public UnityEngine.Events.UnityEvent onFadeOutFinish;

	public void FadeIn()
	{
		foreach (var t in GetComponentsInChildren<MaskableGraphic>())
		{
			t.CrossFadeAlpha(1, fadeTime, true);
		}
	}

	public void FadeOut()
	{
		foreach (var t in GetComponentsInChildren<MaskableGraphic>())
		{
			t.CrossFadeAlpha(0, fadeTime, true);
		}

		StartCoroutine(CoroutineUtil.DoAfter(() => FinishFadeOut(), fadeTime));
	}

	void FinishFadeOut()
	{
		if (onFadeOutFinish != null)
			onFadeOutFinish.Invoke();
	}

	void OnEnable()
	{
		foreach (var t in GetComponentsInChildren<MaskableGraphic>())
		{
			t.CrossFadeAlpha(0, 0, true);
		}

		FadeIn();
	}
}
