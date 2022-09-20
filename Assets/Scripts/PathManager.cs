using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathManager : MonoBehaviour {

	/* This class is used to create the movement grid around the ship from available waypoints
	 */

	public GameObject map;	//Assign the ship to this.
	public bool showDots;	//Shows the pathing dots
	List<Waypoint> waypoints = new List<Waypoint>();	//The list of all waypoints

	// Use this for initialization
	void Start () {
		waypoints.AddRange (map.GetComponentsInChildren<Waypoint>());
		if (showDots) {
			foreach (Waypoint wp in waypoints){
				wp.GetComponent<SpriteRenderer>().enabled = true;
			}
		}
	}
}
