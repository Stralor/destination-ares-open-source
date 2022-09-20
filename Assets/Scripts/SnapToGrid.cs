using UnityEngine;
using System.Collections;

public class SnapToGrid : SnapBase
{

	public float grid_scale;
	public Vector3 offset;
	public bool ignoreZ = true, adjustOffsetByZRot = true;

	/**Lock position to nearest range in grid.
	 */
	public override void Snap()
	{
		//Some variables
		var currentPos = transform.localPosition;
		Vector3 newPos = new Vector3();
		Vector3 adjustedOffset = offset;

		//Change the offset position based on where the object is facing (in 2D space)?
		if (adjustOffsetByZRot)
		{
			//Radians for the turn
			var zRads = transform.localEulerAngles.z * Mathf.Deg2Rad;

			//Calculate dat new offset
			adjustedOffset.x = Mathf.Sin(zRads) * offset.y + Mathf.Cos(zRads) * offset.x;
			adjustedOffset.y = Mathf.Sin(zRads) * offset.x + Mathf.Cos(zRads) * offset.y;
		}

		//Updates positions
		newPos.x = Mathf.Round((currentPos.x - adjustedOffset.x) / grid_scale) * grid_scale + adjustedOffset.x;
		newPos.y = Mathf.Round((currentPos.y - adjustedOffset.y) / grid_scale) * grid_scale + adjustedOffset.y;
		if (!ignoreZ)
			newPos.z = Mathf.Round((currentPos.z - adjustedOffset.z) / grid_scale) * grid_scale + adjustedOffset.z;

		//Set it
		transform.localPosition = newPos;
	}
}
