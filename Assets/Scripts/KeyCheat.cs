using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyCheat : MonoBehaviour
{

	public List<MetaGameKey> keysToAdd = new List<MetaGameKey>();
	public List<MetaGameKey> keysToRemove = new List<MetaGameKey>();
	public List<MetaGameKey> currentKeys = new List<MetaGameKey>();
	[SerializeField] bool addap;

	void Update()
	{
		foreach (var t in keysToAdd)
		{
			MetaGameManager.AddKey(t);
		}

		foreach (var t in keysToRemove)
		{
			MetaGameManager.keys.Remove(t);
		}

		currentKeys = MetaGameManager.keys;

		keysToAdd.Clear();
		keysToRemove.Clear();

		if (addap)
		{
			MetaGameManager.currentUnlockPoints += 100;
			addap = false;
			AudioClipOrganizer.aco.PlayAudioClip("Beep", null);
		}
	}

	void Awake()
	{
		Update();
	}
}
