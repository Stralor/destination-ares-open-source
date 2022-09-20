using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipSystemArtSpawner : MonoBehaviour
{
	public Vector3 standardSystemOffset = Vector3.zero, largeSystemOffset = new Vector3(0.5f, 0.5f, 0);

	//Hull art nodes positioning
	public float standardNodeDistance = 0.5f, largeNodeDistance = 1;

	//Art and ArtNodes (Clockwise, starting at left)
	public List<Transform> nodes = new List<Transform>();
	List<GameObject> art = new List<GameObject>();

	public LayerMask alwaysBlockedBy;

	bool initialSetupComplete = false;
	Placement mainPlacement;



	/** Updates the system's colliders, SnapToGrid offset, anims, and any other relevant values */
	public void UpdateValues()
	{
		//In case this hasn't been started yet
		InitialSetup();

		//Name Setup
		var sys = GetComponent<ShipSystem>();
		sys.Rename();

		var function = sys.function;

		//Collider sizes
		foreach (var t in GetComponentsInChildren<ShipSystemBoxColliderController>())
		{
			t.SetSize(function);
		}

		//Grid offset & node distances
		if (ShipSystemBoxColliderController.largeSystems.Contains(function))
		{
			GetComponent<SnapToGrid>().offset = largeSystemOffset;

			UnityEngine.Assertions.Assert.IsTrue(nodes.Count == 4);

			nodes [0].localPosition = new Vector3(-largeNodeDistance, 0);
			nodes [1].localPosition = new Vector3(0, largeNodeDistance);
			nodes [2].localPosition = new Vector3(largeNodeDistance, 0);
			nodes [3].localPosition = new Vector3(0, -largeNodeDistance);
		}
		else
		{
			GetComponent<SnapToGrid>().offset = standardSystemOffset;

			UnityEngine.Assertions.Assert.IsTrue(nodes.Count == 4);

			nodes [0].localPosition = new Vector3(-standardNodeDistance, 0);
			nodes [1].localPosition = new Vector3(0, standardNodeDistance);
			nodes [2].localPosition = new Vector3(standardNodeDistance, 0);
			nodes [3].localPosition = new Vector3(0, -standardNodeDistance);
		}

		//Anim
		GetComponent<ShipSystemAnim>().SetSystemFunction(function);

		//Rotating NOTE: Irrelevant, anim overrides rotation anyway. Nothing gets to rotate.
//		if (function == ShipSystem.SysFunction.Engine)
//			mainPlacement.canRotate = false;
//		else
//			mainPlacement.canRotate = true;

		//Hull Art
		UpdateHullArt();
	}

	/** Gets called when the system changes and when anything gets placed */
	public void UpdateHullArt(Placement ignore = null)
	{
		var function = GetComponent<ShipSystem>().function;
		Transform chosenNode = null;	//Sometimes matters (for enforcing only one node)

		//Potential locations to turn on art
//		nodes.ForEach(obj => obj.GetComponent<Collider2D>().enabled = true);
		var possibleNodes = nodes.FindAll(obj => !obj.GetComponent<Collider2D>().IsTouchingLayers());

		//Clear any arts that are now blocked
		foreach (var t in nodes)
		{
			if (t.GetComponent<Collider2D>().IsTouchingLayers(alwaysBlockedBy) || (!mainPlacement.isPlaced && t.GetComponent<Collider2D>().IsTouchingLayers()))
			{
				art [nodes.IndexOf(t)].SetActive(false);
			}
			//Also prevent dupes where possible
			else if (mainPlacement.isPlaced && art [nodes.IndexOf(t)].activeSelf)
			{
				chosenNode = t;
			}
		}

		//used for some cases
		int index = 0;

		//SET ART BITCHEZ
		switch (function)
		{
		//Engines and their ilk only care about left, but it better be there
		case ShipSystem.SysFunction.Engine:
		case ShipSystem.SysFunction.WasteCannon:

			//Placement setup
			var leftPlace = nodes [0].GetComponent<Placement>();
			leftPlace.enabled = true;

			//Sync
			if (!mainPlacement.othersToSyncWith.Contains(leftPlace))
			{
				mainPlacement.Sync(new List<Placement>(){ leftPlace });
				leftPlace.Sync(new List<Placement>(){ mainPlacement });
			}

			//We want it visible for placement, since it's required
			if (possibleNodes.Contains(nodes [0]))
			{
				if (function == ShipSystem.SysFunction.Engine)
					index = 1;
				else if (function == ShipSystem.SysFunction.WasteCannon)
					index = 6;

				SetArt(art [0], index);
				art [0].SetActive(true);
			}
			break;



		//Comms like any one match, preferring pointies or forward
		case ShipSystem.SysFunction.Communications:

			//We only care if we're already able to place
			if (mainPlacement.isConnected && !mainPlacement.isBlocked)
			{
				//Set if not already set - Prevent Dupes
				if (chosenNode == null)
				{
					//Pointies (three sides open)
					if (possibleNodes.Count == 3)
					{
						chosenNode = possibleNodes [1];
					}
					//Forward
					else if (possibleNodes.Contains(nodes [2]))
					{
						chosenNode = nodes [2];
					}
					//Others
					else if (possibleNodes.Count > 0)
					{
						chosenNode = possibleNodes [0];
					}
				}

				//Now set art
				if (chosenNode)
				{
					SetArt(art [nodes.IndexOf(chosenNode)], 0, yOffset: 0.5f);
					art [nodes.IndexOf(chosenNode)].SetActive(true);					
				}

				//Clear others
				foreach (var t in possibleNodes)
				{
					if (t != chosenNode)
						art [nodes.IndexOf(t)].SetActive(false);
				}
			}
			//Or clear the clutter
			else
				art.ForEach(obj => obj.SetActive(false));
			break;



		//Helm would like a match or two or three, especially if they're adjacent
		case ShipSystem.SysFunction.Helm:

			//We only care if we're already able to place
			if (mainPlacement.isConnected && !mainPlacement.isBlocked)
			{
				//Clear chosen node for helms, we'll recalc each time
				chosenNode = null;

				//Only search nodes if there's something to find
				if (possibleNodes.Count > 0)
				{
					//Search for doubles and triples
					foreach (var t in possibleNodes)
					{
						//Search for adjacent matches
						int thisIndex = nodes.IndexOf(t);
						int nextIndex = thisIndex + 1;
						int lastIndex = thisIndex - 1;

						//Circle back around the 0 / 3 threshold
						nextIndex = nextIndex >= nodes.Count ? 0 : nextIndex;
						lastIndex = lastIndex < 0 ? 3 : lastIndex;

						//Check for doubles and triples
						if (possibleNodes.Contains(nodes [nextIndex]))
						{
							//We might pick this one
							chosenNode = t;

							//Triple?
							if (possibleNodes.Contains(nodes [lastIndex]))
							{
								//Won't get better than this. Assign now
								SetArt(art [thisIndex], 8, yOffset: -0.5f);
								art [thisIndex].SetActive(true);

								break;
							}
							//At least double is true
							else
							{
								//Tentative yes
								SetArt(art [thisIndex], 3, yOffset: -0.5f);
								art [thisIndex].SetActive(true);

								//Prefer front-facing doubles, if there are no triples
								if (possibleNodes.Count < 3 && (nextIndex == 2 || nextIndex == 3))
									break;
							}
						}
					}

					//Otherwise, just a single window
					if (!chosenNode)
					{
						chosenNode = possibleNodes [0];

						SetArt(art [nodes.IndexOf(chosenNode)], 4);
						art [nodes.IndexOf(chosenNode)].SetActive(true);
					}
				}

				//Clear others
				foreach (var t in possibleNodes)
				{
					if (t != chosenNode)
						art [nodes.IndexOf(t)].SetActive(false);
				}
			}
			//Or clear the clutter
			else
				art.ForEach(obj => obj.SetActive(false));
			break;


		
		//Reactors will take all matches, if available
		case ShipSystem.SysFunction.Reactor:

			//We only care if we're already able to place
			if (mainPlacement.isConnected && !mainPlacement.isBlocked)
			{
				foreach (var t in possibleNodes)
				{
					SetArt(art [nodes.IndexOf(t)], 2);
					art [nodes.IndexOf(t)].SetActive(true);
				}
			}
			//Or clear the clutter
			else
				art.ForEach(obj => obj.SetActive(false));
			break;



		//Solar (panels and sails) must have any one match
		case ShipSystem.SysFunction.Sail:
		case ShipSystem.SysFunction.Solar:

			//Only show it if a valid placement
			if (mainPlacement.isConnected && !mainPlacement.isBlocked)
			{
				//Set if not already set - Prevent Dupes
				if (chosenNode == null)
				{
					//Get a match, prefer middles when three available
					if (possibleNodes.Count == 3)
						chosenNode = possibleNodes [1];
					//All other matches
					else if (possibleNodes.Count > 0)
					{
						index = 0;
					
						//Prefer tops and bottoms
						if (possibleNodes.Contains(nodes [1]))
							index = possibleNodes.IndexOf(nodes [1]);
						else if (possibleNodes.Contains(nodes [3]))
							index = possibleNodes.IndexOf(nodes [3]);
					
						chosenNode = possibleNodes [index];
					}
				}

				//Set it
				if (chosenNode)
				{
					//Art
					if (function == ShipSystem.SysFunction.Solar)
						SetArt(art [nodes.IndexOf(chosenNode)], 5, yOffset: 2);
					else if (function == ShipSystem.SysFunction.Sail)
						SetArt(art [nodes.IndexOf(chosenNode)], 7, yOffset: 1);

					art [nodes.IndexOf(chosenNode)].SetActive(true);

					//Put it in the BG, since it's long and can get covered.
					art [nodes.IndexOf(chosenNode)].GetComponent<SpriteRenderer>().sortingOrder = -7;

					//Active placement
					var place = chosenNode.GetComponent<Placement>();
					place.enabled = true;

					//Sync
					if (!mainPlacement.othersToSyncWith.Contains(place))
					{
						mainPlacement.Sync(new List<Placement>(){ place });
						place.Sync(new List<Placement>(){ mainPlacement });
					}
				}

				//Clear others
				foreach (var t in possibleNodes)
				{
					if (t != chosenNode)
					{
						art [nodes.IndexOf(t)].SetActive(false);
					}
				}
			}
			//Or clear the clutter
			else
				art.ForEach(obj => obj.SetActive(false));

			//Placement management
			foreach (var t in nodes)
			{
				if (!mainPlacement.isConnected || (t != chosenNode && chosenNode != null))
				{
					//Placement management
					var place = t.GetComponent<Placement>();
					place.enabled = false;
					
					//Desync
					if (mainPlacement.othersToSyncWith.Contains(place))
					{
						mainPlacement.Desync(new List<Placement>(){ place });
						place.Desync(new List<Placement>(){ mainPlacement });
					}
				}
			}

			break;



		default:
			break;
		}

		//Turn off nodes we're not using, so they don't bother other systems
//		foreach (var t in art)
//		{
//			if (!t.activeSelf)
//			{
//				nodes [art.IndexOf(t)].GetComponent<Collider2D>().enabled = false;
//			}
//		}
	}

	/**Instantiate a HullExtension object, assign its parent, etc. */
	static GameObject CreateArt(Transform parent, int artIndex)
	{
		var go = Instantiate(Resources.Load("HullExtension")) as GameObject;
		go.transform.SetParent(parent);
		go.transform.localPosition = Vector3.zero;
		go.transform.localEulerAngles = Vector3.zero;

		SetArt(go, artIndex);

		return go;
	}

	/**When passed a valid HullExtension prefab object, this updates the art on it to the sprite designated by the index */
	static void SetArt(GameObject go, int artIndex, float xOffset = 0, float yOffset = 0)
	{
		var hull = go.GetComponent<HullExtension>();
		hull.artIndex = artIndex;
		hull.SetArt();

		go.transform.localPosition = new Vector2(xOffset, yOffset);
	}


	/**Set private values. Spawn HullExtensions to nodes, and track them. Start listeners. */
	void InitialSetup()
	{
		if (!initialSetupComplete)
		{
			mainPlacement = GetComponentInChildren<Placement>();

			//Create enough art to match all the nodes
			nodes.ForEach(obj => art.Add(CreateArt(obj, 0)));

			//Match up sprites to any placements
			for (int i = 0; i < nodes.Count; i++)
			{
				var p = nodes [i].GetComponent<Placement>();
				if (p != null)
					p.sprites.Add(art [i].GetComponent<SpriteRenderer>());
			}

			//Hide it. We'll show it later.
			art.ForEach(obj => obj.SetActive(false));
			
			//Assertion: they're numbered in order, identically
			for (int i = 0; i < nodes.Count; i++)
			{
				UnityEngine.Assertions.Assert.AreEqual(nodes [i].position, art [i].transform.position);
			}

			//Listeners
			PlacementManager.onPlace += UpdateHullArt;
			mainPlacement.onSnap += UpdateHullArt;

			initialSetupComplete = true;
		}
	}

	void OnEnable()
	{
		InitialSetup();
	}

	void OnDisable()
	{
		//End listeners
		PlacementManager.onPlace -= UpdateHullArt;
		mainPlacement.onSnap -= UpdateHullArt;
	}
}
