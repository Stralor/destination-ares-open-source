using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class SimplePath : MonoBehaviour
{


	//Primary values
	[SerializeField] private Transform target;
	//The movement target
	[SerializeField] private float baseSpeed = 2;
	//Movement speed
	[SerializeField] private float baseTurnSpeed = 2;
	//Turning speed
	private float colliderSpeedModifier = 1;
	//Modifier to create current speed due to conditions
	public float delay = 0.5f;
	//Delay between path searches
	public bool interrupted { get; private set; }
	//Stops movement, clears path.

	//Cached elements
	private Seeker seeker;
	private Transform tr;
	private Path path;
	private Rotation rot;
	private BehaviorHandler bHand;
	private Character me;
	

	//Minutae
	private bool hasPath = false;
	//Has a path to the current target been set?
	private bool isPathing = false;
	//Is a path being made at the moment?
	private bool arrived = true;
	//If the character has already called the BehaviorHandler.Arrived() method
	private int pathIndex;
	//Counter for current position on path. Current code leaves this as zero. Do not adjust.
	private List<Vector2> pathVectors = new List<Vector2>();
	//The path, represented as a series of points
	private Vector2 lastVector;
	//The last point visited on the path

	private Vector2 targetPos;
	//Where the target is, in 2D space
	public bool isMoving = false;
	//Signal to animation that character is actively moving.
	public Transform lastProcessedTarget { get; private set; }
	//Cache the last processed target, to compare against current target
	

	//Frictions
	public float charFriction = 0.5f;
	public float pinFriction = 0.8f;
	public float doorFriction = 0.5f;
	public float sysFriction = 0.75f;


	public float speed
	{
		get
		{
			var realSpeed = baseSpeed;

			if (me.roles.Contains(Character.CharRoles.Maimed))
				realSpeed -= 0.8f;
			if (me.roles.Contains(Character.CharRoles.Athlete))
				realSpeed += 0.5f;

			//Also adjust by injury, tight spaces, etc.
			if (me.injured)
			{
				realSpeed *= me.injuredSpeed;
			}

			realSpeed *= colliderSpeedModifier;

			return realSpeed;
		}
	}

	public float turnSpeed
	{
		get
		{
			var realTurnSpeed = baseTurnSpeed;

			if (me.roles.Contains(Character.CharRoles.Maimed))
				realTurnSpeed -= 1;
			if (me.roles.Contains(Character.CharRoles.Athlete))
				realTurnSpeed += 0.5f;

			return realTurnSpeed;
		}
	}





	/**Interrupt movement. End in next space.
	 */
	public void Interrupt()
	{
		interrupted = true;
		lastProcessedTarget = null;
		//End the task!
		if (bHand)
			bHand.CallEndTask(true);
	}

	/**Resume normal behavior after being interrupted.
	 */
	public void EndInterrupt()
	{
		interrupted = false;
	}

	/**Set the target. Protects the field and allows for closure of old pathfinding.
	 */
	public void SetTarget(Transform t)
	{
		//The target
		target = t;
		//No path yet
		isPathing = false;
		hasPath = false;
		//Not there yet
		arrived = false;
//		//No waypoints yet for the not yet existant path
//		if (pathVectors != null) pathVectors.Clear();
	}

	void Start()
	{
		seeker = GetComponent<Seeker>();	//Cache the seeker component
		tr = transform;						//Cache the transform
		rot = GetComponentInChildren<Rotation>();
		bHand = GetComponent<BehaviorHandler>();
		me = GetComponent<Character>();
		//rigid = tr.GetComponent<Rigidbody2D>();	//Cache the rigidbody
		pathIndex = 0;
	}

	void FixedUpdate()
	{

		//Establish a path, if there's a new target to move to.
		if (target != null && !hasPath && !isPathing)
		{
			
			//Process the target
			lastProcessedTarget = target;
			targetPos = (Vector2)AstarPath.active.GetNearest(target.position).position;
			
			//Make it!
			//If there's an old path still doing stuff, overwrite it.
			if (pathVectors.Count > 0)
				//OK! Modify the path from our next location!
				seeker.StartPath(pathVectors [pathIndex], targetPos, OnPathComplete);
			//Otherwise make a fresh path
			else
				seeker.StartPath(tr.position, targetPos, OnPathComplete);
				
			//It's being made. We can start to try pathing
			isPathing = true;
		}

		//Do any movement
		else if (target != null && pathVectors.Count > 0)
		{
			
			Move();
		}

		//Overwrite path when the target doesn't stay in one location. Must already have a path.
		if (target != null && hasPath && !isPathing && targetPos != (Vector2)AstarPath.active.GetNearest(target.position).position)
		{
			
			//New location
			targetPos = (Vector2)AstarPath.active.GetNearest(target.position).position;
			
			//(Safety check)
			if (pathVectors.Count > 0)
				//OK! Modify the path from our next location!
				seeker.StartPath(pathVectors [pathIndex], targetPos, OnPathComplete);
			
			//Path being updated
			isPathing = true;
		}


		//Check if the character is at the target location
		if (Vector2.Distance((Vector2)tr.position, targetPos) < 0.02f)
		{

			//Clear the target and path
			target = null;
			pathVectors.Clear();

			//Rotate to face up!
			if (Mathf.Abs(rot.GetFacing() - rot.GetAngle(Vector2.up)) > 5)
				rot.RotateTo(Vector2.up, turnSpeed);

			//Be sure we haven't already called arrived
			if (!arrived)
			{
				//We've arrived!
				arrived = true;
				hasPath = false;
				//Tell the character's TaskHandler that it's time to worrrk
				if (bHand != null)
					bHand.Arrived();
			}
		}
	}

	void OnTriggerEnter2D(Collider2D otherObject)
	{
		//Set speed modifiers when sharing tight spaces
		//Characters and Systems
		if (otherObject.GetComponent<Character>() != null)
			colliderSpeedModifier *= charFriction;
		else if (otherObject.GetComponent<ShipSystem>() != null)
			colliderSpeedModifier *= sysFriction;

		//Doors
		if (otherObject.name.Equals("Door"))
		{
			colliderSpeedModifier *= doorFriction;	//TODO Replace with door waiting anim
			Animator objAnim = otherObject.GetComponent<Animator>();
			objAnim.SetBool("Open", true);
		}
		//Pins
		if (otherObject.name.Equals("Pin"))
			colliderSpeedModifier *= pinFriction;
	}

	void OnTriggerExit2D(Collider2D otherObject)
	{
		//Set speed modifiers when sharing tight spaces
		//Characters and Systems
		if (otherObject.GetComponent<Character>() != null)
			colliderSpeedModifier /= charFriction;
		if (otherObject.GetComponent<ShipSystem>() != null)
			colliderSpeedModifier /= sysFriction;

		//Doors
		if (otherObject.name.Equals("Door"))
		{
			colliderSpeedModifier /= doorFriction;	//TODO Replace with door waiting anim
			Animator objAnim = otherObject.GetComponent<Animator>();
			objAnim.SetBool("Open", false);
		}
		//Pins
		if (otherObject.name.Equals("Pin"))
			colliderSpeedModifier /= pinFriction;
	}

	public void OnPathComplete(Path p)
	{
		//Path returned!
		isPathing = false;

		if (p.error)
		{
			//Cannot path
			hasPath = false;
			target = null;
			if (bHand)
				bHand.CallEndTask(false);
			return;
		}
		else
		{
			//Path get!
			hasPath = true;

			//Claim the path
			p.Claim(this);
			//Release any previous path
			if (path != null)
				path.Release(this);
			if (pathVectors != null)
				pathVectors.Clear();

			//Set the path
			path = p;
			//Temporary Vector3 vectorpath list
			List<Vector3> temp = path.vectorPath;
			//Populate the Vector2 list
			foreach (Vector3 vector in temp)
			{
				pathVectors.Add((Vector2)vector);
			}
			//Set the start position to lastVector, the remove it from pathVectors (if it's present)
			lastVector = (Vector2)tr.position;
			pathVectors.Remove((Vector2)tr.position);
		}
	}

	/**Check the distance from this character to any target.
	 * TODO Return spaces of movement, rather than direct path through walls. 
	 */
	public float DistanceCheck(Transform target)
	{
		float distance = transform.InverseTransformPoint(target.position).sqrMagnitude;
		return distance;
	}

	void Move()
	{
		//Don't bother if we're interrupted
		if (interrupted)
			return;

		//Is there a point to move to?
		if (pathIndex < pathVectors.Count)
		{
			//Where is it relative to us?
			Vector2 dir = (pathVectors [pathIndex] - new Vector2(tr.position.x, tr.position.y)).normalized;

			//First, let's rotate to it
			bool doneRotating = rot.RotateTo(dir, turnSpeed);
			//Don't move until we're done rotating
			if (!doneRotating)
				return;

			//Moving!
			isMoving = true;

			//Get movement values
			dir *= speed;
			//Move to it!
			tr.Translate(dir * Time.deltaTime);
			//rigid.velocity = dir*Time.deltaTime;

			//Used for interrupts
			bool reached = false;

			//Have we reached the waypoint (or close enough)?
			if (Vector2.Distance((Vector2)tr.position, pathVectors [pathIndex]) < 0.02f)
			{
				reached = true;
				isMoving = false;
				//pathIndex++;
				lastVector = pathVectors [pathIndex];
				pathVectors.Remove(pathVectors [pathIndex]);
				//rigid.velocity = new Vector2(0, 0);
			}

			//Have we passed the waypoint?
			else
			{

				//Where are we moving?
				Vector2 lineDirection = pathVectors [pathIndex] - lastVector;
				//How far?
				float magn = lineDirection.magnitude;

				//If the magnitude is 0 (aka, we JUST started a new movement), get out
				//We don't want NaNs, thanks
				if (magn == 0)
					return;
				//Normalize lineDirection to not include the distance
				lineDirection /= magn;

				//Find where we are in our movement
				//This float is greatest when closest to lineDirection (our goal)
				float closestVector = Vector2.Dot((Vector2)tr.position - lastVector, lineDirection);
				//Normalize it
				closestVector /= magn;
				//Clamp it to between 0 and 1 (normalized distance between start and end)
				//Basically, this says to chop off anything greater than 1 or less than 0
				closestVector = closestVector > 1 ? 1 : closestVector < 0 ? 0 : closestVector;
				
				//Guess what? We now know if it's at or past the waypoint (or stuck)
				if (closestVector == 1)
				{	//The waypoint's normalized position
					reached = true;
					isMoving = false;
					//Move on. Also, reset the object's position to the waypoint.
					lastVector = tr.position = pathVectors [pathIndex];
					pathVectors.Remove(pathVectors [pathIndex]);
				}
			}
			
			//Interrupt movement if we're at a waypoint and interrupt is true
			if (interrupted && reached)
			{
				target = null;
				pathVectors.Clear();
				isMoving = false;
			}
		}
		else
		{
			Debug.Log(name + " does not have a next waypoint. Not moving.");
		}
	}



}
