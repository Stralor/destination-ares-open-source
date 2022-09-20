using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{

	public GameObject wallPrefab, minePrefab;

	public Renderer minigameWindow;

	//Doubles as location check (use .Exists and search the transforms)
	public List<GameObject> spawnedObstacles = new List<GameObject>();


	/**Returns how many were successfully spawned
	 */
	public int SpawnWalls(int count, List<Vector3> blockedLocations = null)
	{
		return GridControl.SpawnGameObjects(wallPrefab, count, minigameWindow.bounds.extents.x - 0.4f, minigameWindow.bounds.extents.y - 0.4f, ref spawnedObstacles,
		                                    Environment_EventGame.GRID_SPACE_SIZE, blockedLocations: blockedLocations, parent: this.transform, centerOfGridIsASpace: true);
	}

	/**Returns how many were successfully spawned
	 */
	public int SpawnMines(int count, List<Vector3> blockedLocations = null)
	{
		return GridControl.SpawnGameObjects(minePrefab, count, minigameWindow.bounds.extents.x - 0.4f, minigameWindow.bounds.extents.y - 0.4f, ref spawnedObstacles,
		                                    Environment_EventGame.GRID_SPACE_SIZE, blockedLocations: blockedLocations, parent: this.transform, centerOfGridIsASpace: true);
	}
}