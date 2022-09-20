using UnityEngine;
using System.Collections;

public class EventTree : MonoBehaviour
{

	public static EventTree s;

	void Awake()
	{
		//Set
		if (s == null)
		{
			s = this;
			DontDestroyOnLoad(gameObject);
		}
		//Or destroy
		if (s != this)
		{
			Destroy(gameObject);
		}
	}
}
