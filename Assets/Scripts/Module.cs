using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Module : MonoBehaviour
{

	public enum Size
	{
		UNDEFINED,
		Small,
		Medium,
		Large
	}

	public static Dictionary<Size, int> ModuleStorageDictionary = new Dictionary<Size, int>
	{
		{ Size.Small, 16 },	
		{ Size.Medium, 20 },	
		{ Size.Large, 24 }	
	};

	public static Dictionary<Size, int> ModuleMassDictionary = new Dictionary<Size, int>
	{
		{ Size.Small, 14 },	
		{ Size.Medium, 22 },	
		{ Size.Large, 30 }		
	};

	public Size size;

	void OnEnable()
	{
		if (GameReference.r != null && !GameReference.r.allModules.Contains(this))	//Safety in case of initialization order
			GameReference.r.allModules.Add(this);
	}

	void OnDisable()
	{
		if (GameReference.r != null)
			GameReference.r.allModules.Remove(this);
	}
}
