using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class PlacementManager
{
	public static Placement currentPlacement;

	public static System.Action<Placement> onPlace, cannotPlace, onClear, onPickUp, onLongPress, onRightClick, onAfterPlace;

	public static System.Func<Placement, bool> stateCheck, longPressPrep, canLongPress, rightClickPrep;

	public static float longPressTime = 0.5f;



	public static void LeftClick(Placement target)
	{
		//Place
		if (currentPlacement && !currentPlacement.isPlaced)
		{
			//Valid - Place!
			if (!currentPlacement.isBlocked && (currentPlacement.isConnected || currentPlacement.doesNotNeedConnections) && currentPlacement.Place())
			{
				//PLACE SFX
				AudioClipOrganizer.aco.PlayAudioClip("Construct", null);

				//Do any other actions needed when we place
				if (onPlace != null)
					onPlace.Invoke(currentPlacement);
			}
			//Clear Placement (off-area click)
			else if (target == null || (!currentPlacement.isBlocked && !currentPlacement.isConnected))
			{
				//CLEAR SFX
				AudioClipOrganizer.aco.PlayAudioClip("Pop", null);

				//Do any other actions needed when we clear
				if (onClear != null)
					onClear.Invoke(currentPlacement);
			}
			//Invalid
			else
			{
				//INVALID SFX
				AudioClipOrganizer.aco.PlayAudioClip("Invalid", null);

				//Do any actions for when we can't place
				if (cannotPlace != null)
					cannotPlace.Invoke(currentPlacement);
			}
		}
		//Nothing to place, so pick something up
		else
		{
			//Clear
			currentPlacement = null;

			//Make sure _currentPickup is a valid target for the layer
			if (stateCheck == null || stateCheck.Invoke(target))
			{		
				//Then pick it up
				if (target && target.PickUp())
				{
					currentPlacement = target;

					AudioClipOrganizer.aco.PlayAudioClip("Repair", null);

					//Let stuff know what we picked up
					if (onPickUp != null)
						onPickUp.Invoke(currentPlacement);
				}
			}

		}
	}

	public static void LongClick(Placement target)
	{
		if (onLongPress != null)
		{
			//Do long click if we don't have a conditional check or the target passes the check
			if (longPressPrep == null || longPressPrep.Invoke(target))
				onLongPress.Invoke(target);
			//Otherwise, it's just a Left click
			else
				LeftClick(target);
		}
	}

	public static void RightClick(Placement target)
	{
		//Rotate
		if (target == currentPlacement && !currentPlacement.isPlaced && currentPlacement.canRotate)
		{
			AudioClipOrganizer.aco.PlayAudioClip("Beep", null);

			var rot = (currentPlacement.mainObject.transform.localEulerAngles.z - 90) % 360;
			currentPlacement.mainObject.transform.localEulerAngles = new Vector3(0, 0, rot);
		}
		//Non-rotate commands
		else if (onRightClick != null)
		{
			if (rightClickPrep == null || rightClickPrep.Invoke(target))
			{
				onRightClick.Invoke(target);
			}
			//If it doesn't mass right-click muster, just send as LeftClick
			else
			{
				LeftClick(target);
			}
		}
	}

	public static void AfterPlace(Placement target)
	{
		if (onAfterPlace != null)
		{
			onAfterPlace.Invoke(target);
		}
	}
}
