using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ShipKey : ScriptableObject
{
	public string shipFileName;

	public MetaGameKey requiredKey;

	public bool showWhenLocked = true;

	[TextArea]public string customLockText = "";
}
