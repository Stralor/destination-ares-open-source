using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PingPool
{
	static List<GameObject> pool = new List<GameObject>();


	public static void PingHere(Transform here, GameObject pingPrefab = null, float seconds = 1, float growthRate = 0.01f, float delay = 0, bool putInPool = true)
	{
		GameObject go;

		//Clean pool
		pool.RemoveAll(obj => obj == null);

		//Find one from a pool
		if (pool.Exists(obj => !obj.activeSelf && !obj.GetComponent<MousePing>().reserved))
		{
			go = pool.Find(obj => !obj.activeSelf && !obj.GetComponent<MousePing>().reserved);
		}
		//Or make one
		else
		{
			//Default prefab:
			if (pingPrefab == null)
				pingPrefab = Resources.Load("Shockwave Ping") as GameObject;

			go = GameObject.Instantiate(pingPrefab, here);
		}

		//Reset pos
		go.transform.SetParent(here);
		go.transform.localPosition = Vector3.zero;

		//Handle the ping
		var ping = go.GetComponent<MousePing>();

		if (ping == null)
		{
			Debug.Log("Ping prefab was invalid. Ping cancelled.");
			return;
		}

		if (putInPool && !pool.Contains(go))
			pool.Add(go);

		//Settings
		ping.time = seconds;
		ping.growthSpeed = growthRate;

		//Do it
		ping.Ping(slerp: true, delay: delay);
	}
}
