using UnityEngine;
using System.Collections;

public class CenterGroup : MonoBehaviour
{
	//Our eventual shift
	public Vector2 offset { get; private set; }

	/** Center every child rigidbody based on all their positions
	 * Returns the Vector2 of how everything moved, in case you need that.
	 */
	public Vector2 UpdateGroupPosition()
	{
		//Individual values. We don't need average of all, we need average of extremes.
		float xMin = Mathf.Infinity, xMax = Mathf.NegativeInfinity, yMin = Mathf.Infinity, yMax = Mathf.NegativeInfinity;

		//Find mins and maxes.
		for (int i = 0; i < transform.childCount; i++)
		{
			var temp = (Vector2)transform.GetChild(i).position;

			xMin = xMin > temp.x ? temp.x : xMin;
			xMax = xMax < temp.x ? temp.x : xMax;
			yMin = yMin > temp.y ? temp.y : yMin;
			yMax = yMax < temp.y ? temp.y : yMax;
		}

		//The offset value is the negative of the rounded, averaged extreme values
		offset = new Vector2(-(int)(xMin + xMax) / 2, -(int)(yMin + yMax) / 2);

		MoveChildrenRigidbody2Ds(transform, offset);

		//Update camera bounds
		if (CameraEffectsController.cec != null)
		{
			//Scroll width and height based on ship size and shape (yay, it's auto-centered!)
			CameraEffectsController.cec.scrollWidth = xMax - xMin;
			CameraEffectsController.cec.scrollHeight = yMax - yMin;
		}

		return offset;
	}

	/**Do the actual MovePosition call on the target's children (that have rigidbody2Ds)
	 */
	public static void MoveChildrenRigidbody2Ds(Transform parent, Vector2 movement)
	{
		//Apply the offset to each rigidbody in the immediate children
		for (int i = 0; i < parent.childCount; i++)
		{
			var rigid = parent.GetChild(i).GetComponent<Rigidbody2D>();
			if (rigid)
				rigid.MovePosition((Vector2)parent.GetChild(i).position + movement);
		}
	}
}
