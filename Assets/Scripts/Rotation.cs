using UnityEngine;
using System.Collections;

public class Rotation : MonoBehaviour {

	/* Place on the parent object of all the character's sprites.
	 * Receives signal to rotate the sprites and rotates to that angle.
	 * Movement happens elsewhere, where this cannot affect it.
	 */

	//Exposed Variables and Hooks
	public bool isRotating = false;
	public int counterclockwise { get; private set; }

	[Tooltip("The range when the rotation is 'close enough', prevents shakiness. Higher turn speeds might need a higher snap angle. (Default is 15)")]
	public float snapAngle = 15;
	
	//Cache
	private Transform spriteTransform;

	void Awake(){
		spriteTransform = GetComponent<Transform>();
	}

	/**Rotate in a circle around the z-axis, using the shortest distance.
	 * WARNING: Final angles will NOT be PERFECT 45s and 90s, etc., due to movement system.
	 */
	public bool RotateTo(Vector2 dir, float turnSpeed){
		//Where does that mean we need to face?
		float angle = GetAngle(dir);
		//Where are we currently facing?
		float facing = GetFacing();

		//Which direction should we turn?
		float diff = facing - angle;
		//Don't turn if we're close enough
		if (Mathf.Abs(diff) < snapAngle)
			counterclockwise = 0;
		//Move CCW from high positives to high negatives
		else if (diff > 180)
			counterclockwise = 1;
		//Move CW from high negatives to high positives
		else if (diff < -180)
			counterclockwise = -1;
		//Move CW from low pos to lower pos or low neg
		else if (diff >= 0)
			counterclockwise = -1;
		//Move CCW from low neg to lower neg or low pos
		else if (diff < 0)
			counterclockwise = 1;


		//Are we rotating?
		if (Mathf.Abs(diff) >= snapAngle){
			isRotating = true;
		}


		Vector3 turnVector = new Vector3(0, 0, counterclockwise * turnSpeed * Time.deltaTime * 60);
		spriteTransform.Rotate(turnVector, Space.Self);
		
		//Have we passed it?
		float oldFacing = facing;
		facing = GetFacing();
		//Basic passing conditions
		if ((facing > angle && oldFacing < angle && counterclockwise == 1)
		    || (facing < angle && oldFacing > angle && counterclockwise == -1)
		    || (counterclockwise == 0))
			spriteTransform.localRotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
		
		//Check if we're angled close enough to be done rotating
		diff = Mathf.Abs(facing - angle);
		if (diff > snapAngle / 3)
			return false;
		else{
			//No longer rotating
			isRotating = false;
			return true;
		}
	}

	/**Returns the character's current facing. -180 to 180, with 0 on right and 90 up, etc.
	 */
	public float GetFacing(){
		float facing = spriteTransform.localEulerAngles.z + 90;	//Add 90 to put 0 on the right, to match "angle"
		//Trim excess rotation and make facing match angle
		NormPM180(ref facing);
		return facing;
	}

	/**Converts Vector2 into an angle value matching GetFacing().
	 */
	public float GetAngle(Vector2 dir){
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;	//Returns -180 to 180, with 0 on the right, positives up, extremes on left
		if (angle == -180)
			angle = 180;
		return angle;
	}

	/**Recursive method that normalizes an input value to between +/- 180. If -180, will return 180 (to prevent shakiness at due west).
	 */
	void NormPM180(ref float value){
		if (Mathf.Abs(value) > 180){
			if (value > 0)
				value -= 360;
			else if (value < 0)
				value += 360;
		}
		if (Mathf.Abs(value) > 180)
			NormPM180(ref value);
		if (value == -180)
			value = 180;
	}

}
