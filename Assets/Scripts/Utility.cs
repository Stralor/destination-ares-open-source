using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Utility
{

	public static T GetRandomEnum<T>()
	{
		System.Array A = System.Enum.GetValues(typeof(T));
		T V = (T)A.GetValue(UnityEngine.Random.Range(0, A.Length));
		return V;
	}

	public static List<string> ListOfItemNames<T>(List<T> originalList)
	{
		List<string> strings = new List<string>();

		foreach (var t in originalList)
		{
			strings.Add(t.ToString());
		}

		return strings;
	}
}
