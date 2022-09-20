using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class Waypoint : MonoBehaviour {

	/* This is the individual waypoint script for pathfinding and grid movement.
	 * The A* tools create nodes from these (when tagged properly).
	 * This also contains easier-to-use boolean flags for node status (occupied, valid).
	 */

	//enum
	public enum WPType{
		Regular,
		Door,
		Pin,
		Outside
	}

	public WPType kind;	//Type of waypoint
	public bool valid;	//Accessible?
	public bool occupied;	//Another char/ object here?
	private GraphNode thisNode;	//This node!


	/** Is it occupied? */
	public bool IsOccupied(){
		return occupied;
	}

	/** Set occupied status. */
	public void Occupied(bool b){
		occupied = b;
		UpdateStatus();
	}

	/** Is it valid (Walkable)? */
	public bool IsValid(){
		return valid;
	}

	/** Set validity. */
	public void Valid(bool v){
		valid = v;
		UpdateStatus();
	}

	//Update status and visuals if valid or not
	private void UpdateStatus(){
		if (thisNode == null)
			return;
		if (valid && !occupied) {
			GetComponent<SpriteRenderer>().color = Color.green;
			thisNode.Walkable = true;
		}
		else if (valid){
			GetComponent<SpriteRenderer>().color = Color.yellow;
			thisNode.Walkable = true;
		}
		else{
			GetComponent<SpriteRenderer>().color = Color.red;
			thisNode.Walkable = false;
		}
	}


	void Start(){
		//Find node
		if (AstarPath.active != null)
			thisNode = AstarPath.active.GetNearest(transform.position).node;
		//What kind of waypoint is it?
		if (gameObject.name == "Door") {
			kind = WPType.Door;
		}
		else if (gameObject.name == "Pin"){
			kind = WPType.Pin;
		}
		else {
			kind = WPType.Regular;
		}
		//Update
		UpdateStatus();
	}
}
