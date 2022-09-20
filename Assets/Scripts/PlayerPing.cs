using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPing : MonoBehaviour
{
	public GameObject pingPrefab;

	public void Ping()
	{
		var ping = Instantiate(pingPrefab, transform.position, Quaternion.identity) as GameObject;
		AudioClipOrganizer.aco.PlayAudioClip("Press", null);

		StartCoroutine(CoroutineUtil.DoAfter(() => Destroy(ping), 2, false));
	}
}
