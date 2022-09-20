using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDWarning : MonoBehaviour
{
	public void Hideaway()
	{
		var man = GetComponentInParent<HUDWarningManager>();

		gameObject.SetActive(false);

		man.StartCoroutine(CoroutineUtil.DoAfter(() => man.activeWarnings.Remove(gameObject), 30));
	}
}
