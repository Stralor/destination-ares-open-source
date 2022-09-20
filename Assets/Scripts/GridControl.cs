using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GridControl
{

	public enum Anchor
	{
		BottomLeft,
		Center
	}


	/**Add spaces from the grid to a given list (e.g., to create a blocking list or a spawning zone or whatnot).
	 * Start point will be internally snapped to the grid with ClampToNearestPoint.
	 */
	public static void AddLocalGridSpacesInArea(ref List<Vector3> list, int gridWidth, int gridHeight, float gridSpaceSize, Vector2 startPoint,
	                                            Anchor anchor = Anchor.Center, bool centerOfGridIsASpace = false)
	{

		//This method iterates from the bottom left corner. Set the start points based on anchor.
		float xStart = 0, yStart = 0;
		switch (anchor)
		{
		case Anchor.BottomLeft:
			xStart = startPoint.x;
			yStart = startPoint.y;
			break;
		case Anchor.Center:
			xStart = startPoint.x - (gridWidth / 2 * gridSpaceSize);
			yStart = startPoint.y - (gridHeight / 2 * gridSpaceSize);
			break;
		}

		//Clamp the start points to the grid
		float clampedX = ClampToNearestPoint(xStart, gridSpaceSize, centerOfGridIsASpace);
		float clampedY = ClampToNearestPoint(yStart, gridSpaceSize, centerOfGridIsASpace);

		//Fill subgrid
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridHeight; j++)
			{
				list.Add(new Vector3(clampedX + (i * gridSpaceSize), clampedY + (j * gridSpaceSize), 0));
			}
		}
	}


	/**Remove spaces in a grid from a given list.
	 * Snapping not used. All valid points in the range will be cleared.
	 */
	public static void RemoveLocalGridSpacesInArea(ref List<Vector3> list, int gridWidth, int gridHeight, float gridSpaceSize, Vector2 startPoint,
	                                               Anchor anchor = Anchor.Center)
	{
		//Find the range min
		float xMin = startPoint.x;
		float yMin = startPoint.y;

		//Adjust for anchor
		switch (anchor)
		{
		case Anchor.Center:
			xMin -= gridWidth / 2 * gridSpaceSize;
			yMin -= gridHeight / 2 * gridSpaceSize;
			break;
		default:
			break;
		}

		//Find the range max
		float xMax = xMin + (gridWidth * gridSpaceSize);
		float yMax = yMin + (gridHeight * gridSpaceSize);

		//Find anything in that zone
		List<Vector3> clearList = new List<Vector3>();
		foreach (var t in list)
		{
			if (t.x > xMin && t.x < xMax && t.y > yMin && t.y < yMax)
			{
				clearList.Add(t);
			}
		}

		//Clear everything in that zone
		foreach (var t in clearList)
		{
			list.Remove(t);
		}
	}


	/// <summary>
	/// Spawns objects onto the grid.
	/// </summary>
	/// <returns>How many were successfully spawned.</returns>
	/// <param name="objectToSpawn">Object to spawn.</param>
	/// <param name="count">How many to try spawning.</param>
	/// <param name="xBounds">X bounds.</param>
	/// <param name="yBounds">Y bounds.</param>
	/// <param name="spawnedObjects">Objects already spawned.</param>
	/// <param name="gridSpaceSize">Grid space size.</param>
	/// <param name="onlyOnePerSpace">If set to <c>true</c> only one object per space.</param>
	/// <param name="blockedLocations">Explicitly blocked locations.</param>
	/// <param name="parent">Transform to child spawned objects to.</param>
	/// <param name="centerOfGridIsASpace">If set to <c>true</c> the center of the grid is in the middle of a space, rather than the intersection of corners.</param>
	public static int SpawnGameObjects(GameObject objectToSpawn, int count, float xBounds, float yBounds, ref List<GameObject> spawnedObjects, float gridSpaceSize = 1,
	                                   bool onlyOnePerSpace = true, List<Vector3> blockedLocations = null, Transform parent = null, bool centerOfGridIsASpace = false)
	{
		
		int successful = 0;

		//Set up an empty list if we didn't have one yet. It will be updated with previously spawned objects if necessary.
		if (blockedLocations == null)
			blockedLocations = new List<Vector3>();

		//Do a quick check for missing blocked spaces if we're including spawnedObjects
		if (onlyOnePerSpace)
			foreach (var t in spawnedObjects)
				if (!blockedLocations.Contains(t.transform.position))
					blockedLocations.Add(t.transform.position);

		//Let's make all of the objects!
		for (int i = 0; i < count; i++)
		{

			//Find a suitable spot
			Vector3 location = GetNewLocation(xBounds, yBounds, blockedLocations, gridSpaceSize, centerOfGridIsASpace: centerOfGridIsASpace);
			
			//Couldn't find one quickly enough. Move on to next iteration.
			if (location.z == 1)
				continue;
			
			//This one was successful since we didn't continue
			successful++;

			//Make the obstacle
			GameObject newObj = (GameObject)Object.Instantiate(objectToSpawn, location, Quaternion.identity);
			newObj.transform.parent = parent;

			//Put it in the relevant lists
			if (spawnedObjects != null)
			{
				spawnedObjects.Add(newObj);
				if (onlyOnePerSpace)
					blockedLocations.Add(newObj.transform.position);
			}
		}

		//Report if we didn't make as many as we hoped
		if (successful < count)
			Debug.Log("Imperfect " + objectToSpawn.name + " spawning report: " + successful + "/" + count + " completed.");
		
		return successful;
	}

	/**Create a location that's:
	 * 1) not blocked
	 * 2) along the grid alignments
	 * 3) within the bounds ±x, ±y
	 * Will return a useless Vector3 if it takes too many tries (10 if 'iteration' is left at default 0) so as not to lock up system.
	 * (Grid is currently centered at 0, 0: needs offset values if other functionality desired)
	 */
	public static Vector3 GetNewLocation(float xBounds, float yBounds, List<Vector3> blockedSpaces, float gridSpaceSize = 1, int iteration = 0, bool centerOfGridIsASpace = false)
	{
		//Create coordinates
		float x = ClampToNearestPoint(Random.Range(-xBounds, xBounds), gridSpaceSize, centerOfGridIsASpace);
		float y = ClampToNearestPoint(Random.Range(-yBounds, yBounds), gridSpaceSize, centerOfGridIsASpace);
		
		//Make the Vector3
		Vector3 location = new Vector3(x, y, 0);
		
		//Check if it's valid
		if (blockedSpaces == null || !blockedSpaces.Exists(obj => obj == location))
		{
			return location;
		}
		//Try again?
		else if (iteration < 10)
			return GetNewLocation(xBounds, yBounds, blockedSpaces, gridSpaceSize, iteration + 1, centerOfGridIsASpace);
		//Too many tries. Get out.
		else
			return new Vector3(0, 0, 1);
	}

	/**Align to the grid!
	 * (Assumes grid is centered to (0, 0), but allows for adjustment if the center of the grid is corner-aligned or mid-space)
	 */
	public static float ClampToNearestPoint(float value, float gridSpaceSize = 1, bool centerOfGridIsASpace = false)
	{
		//Get the new value
		float result = Mathf.Round(value / gridSpaceSize) * gridSpaceSize;

		//Alignment adjustment
		if (!centerOfGridIsASpace)
			result -= Mathf.Sign(result) * gridSpaceSize / 2;

		return result;
	}
}
