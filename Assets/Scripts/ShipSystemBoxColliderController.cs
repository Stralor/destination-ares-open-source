using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipSystemBoxColliderController : MonoBehaviour
{
	public float smallSystemSize = 0.9f, largeSystemSize = 1.8f;

	public static List<ShipSystem.SysFunction> largeSystems = new List<ShipSystem.SysFunction>()
	{
		ShipSystem.SysFunction.Engine,
		ShipSystem.SysFunction.Electrolyser,
		ShipSystem.SysFunction.Processor,
		ShipSystem.SysFunction.Reactor,
		ShipSystem.SysFunction.WasteCannon
	};

	void Start()
	{
		if (GetComponent<ShipSystem>() != null)
			SetSize(GetComponent<ShipSystem>().function);
	}

	/**Set the BoxCollider2D size based on the provided SysFunction
	 */
	public void SetSize(ShipSystem.SysFunction function)
	{
		if (largeSystems.Contains(function))
			GetComponent<BoxCollider2D>().size = new Vector2(largeSystemSize, largeSystemSize);
		else
			GetComponent<BoxCollider2D>().size = new Vector2(smallSystemSize, smallSystemSize);
	}
}
