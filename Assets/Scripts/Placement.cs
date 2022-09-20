using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class Placement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
	/** Is currently on a layer allowed to be selected? */
	public bool isSelectable;

	public bool isMovable = true;

	/** Is locked on grid? */ 
	public bool isPlaced;

	/** Is prevented from locking validly on grid (overrides isConnected)? */
	public bool isBlocked;

	/** Is near enough to another piece to lock on grid? */
	public bool isConnected, doesNotNeedConnections;

	public bool canRotate = true;

	public bool blockedByNonPlacements = true;

	/** Object this is placing, might be a parent */
	public GameObject mainObject;
	public Rigidbody2D rigid;
	public List<SpriteRenderer> sprites = new List<SpriteRenderer>();
	SnapBase snap;

	[Tooltip("Defaults to \"Default\".")]
	public string placementLayer;

	public float unplaceableAlpha = 0.7f;

	[Tooltip("Other placements to listen to. Only will allow placement when they all allow placement, and will change color in unison. Link can be one-directional (listener/ slave) or two-directional (if they're both in each other's list).")]
	public List<Placement> othersToSyncWith = new List<Placement>();

	/** Used to call for color change in LateUpdate() */
	bool _changeColor;

	/** The other colliders blocking placement */
	List<Collider2D> _blockers = new List<Collider2D>();
	/** All other colliders, incl. nonblockers when blockedByNonPlacements is false */
	List<Collider2D> _others = new List<Collider2D>();

	/**Shit that may or may not exist that happens when we snap */
	public System.Action<Placement> onSnap;
	/**Other shit that happens when we do things */
	public System.Action onPlace, onPickup, onBlockOrConnectionChange;

	bool newPointerDown = false, justPlaced = false, mouseHere = false;

	GameObject waitCircle;


	/*
	 * TRIGGER EVENTS
	 */

	public void OnPointerDown(PointerEventData eventData)
	{
		newPointerDown = true;

		//Can we long-press on this?
		if (PlacementManager.canLongPress != null && PlacementManager.canLongPress.Invoke(this))
		{
			StartCoroutine(LongClickTimer());

			//Special-ass VFX code if long-presses are valid
			waitCircle = WaitCircleManager.GetFreshCircle();
			waitCircle.transform.SetParent(transform);
			waitCircle.GetComponent<WaitCircle>().Activate(PlacementManager.longPressTime);
		}
	}

	IEnumerator LongClickTimer()
	{
		yield return new WaitForSecondsRealtime(PlacementManager.longPressTime);

		if (isPlaced && newPointerDown)
		{
			newPointerDown = false;
			PlacementManager.LongClick(this);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		//Clear old waitcircle
		if (waitCircle)
		{
			waitCircle.SetActive(false);
			waitCircle = null;
		}

		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (newPointerDown)
			{
				newPointerDown = false;
				PlacementManager.LeftClick(this);
			}
		}

		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (newPointerDown)
			{
				newPointerDown = false;
				PlacementManager.RightClick(this);
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		mouseHere = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		mouseHere = false;

		if (justPlaced)
		{
			justPlaced = false;

			PlacementManager.AfterPlace(this);
		}
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (!enabled) return;
		
		var op = other.GetComponent<Placement>();

		//New other
		_others.Add(other);

		if (blockedByNonPlacements || (op != null && op.enabled))
		{
			//New Blocker!
			_blockers.Add(other);

			isBlocked = true;

			_changeColor = true;

			if (onBlockOrConnectionChange != null)
				onBlockOrConnectionChange.Invoke();

			//Call out forced overlap (like for doors dragged around modules, etc.)
//			if (isPlaced)
//			{
//				if (op != null && op.isPlaced)
//				{
//					PickUp();
//				}
//			}
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (!enabled) return;
		
		//Other gone! (Maybe even a blocker)
		_blockers.Remove(other);
		_others.Remove(other);

		//Remove nulls
		CleanBlockersAndOthers();

		//Not blocked if no others!
		if (_blockers.Count == 0)
		{
			isBlocked = false;
		}

		_changeColor = true;

		if (onBlockOrConnectionChange != null)
			onBlockOrConnectionChange.Invoke();
	}


	/*
	 * PUBLIC METHODS
	 */

	/**Set this piece into position.
	 * Returns success.
	 */
	public bool Place()
	{
		if (isSelectable && !othersToSyncWith.Exists(obj => obj != null && obj.isBlocked))
		{
			PlaceAction();

			justPlaced = true;

			if (onPlace != null)
				onPlace.Invoke();

			//if mouse isn't in collider, call AfterPlace
			if (!mouseHere)
				PlacementManager.AfterPlace(this);

			return true;
		}

		return false;
	}

	/**Allow this piece to be moved around.
	 * Returns success.
	 */
	public bool PickUp()
	{
		if (isSelectable)
		{
			PickUpAction();

			if (onPickup != null)
				onPickup.Invoke();

			return true;
		}

		return false;
	}

	/**Announce that this piece is within connection range (req'd for valid placement).
	 */
	public void Connect()
	{
		isConnected = true;

		_changeColor = true;

		if (onBlockOrConnectionChange != null)
			onBlockOrConnectionChange.Invoke();
	}

	/**Announce that this piece is no longer within connection range (req'd for valid placement).
	 */
	public void Disconnect()
	{
		//We only care if we're not already validly placed
		//(i.e., don't disconnect a Placement that has other valid connections when it's already locked in; that messes with other code)
		if (!isPlaced)
		{
			isConnected = false;
			
			_changeColor = true;
			
			if (onBlockOrConnectionChange != null)
				onBlockOrConnectionChange.Invoke();
		}
	}

	/**Removes private flags to prevent stacked calls like repeats */
	public void ClearOldFlags()
	{
		justPlaced = false;
		newPointerDown = false;
		mouseHere = false;
	}


	/*
	 * UTILITY AND PRIVATE METHODS
	 */

	public bool BlockersContainsTag(string tag)
	{
		return _blockers.Exists(obj => obj != null && obj.CompareTag(tag));
	}

	public bool OthersContainsTag(string tag)
	{
		return _others.Exists(obj => obj != null && obj.CompareTag(tag));
	}

	void PlaceAction()
	{
		isPlaced = true;

		_changeColor = true;

		sprites.ForEach(obj => obj.sortingLayerName = placementLayer == "" ? "Default" : placementLayer);
	}

	void PickUpAction()
	{
		isPlaced = false;

		_changeColor = true;

		sprites.ForEach(obj => obj.sortingLayerName = "Pop Ups");
	}

	void LateUpdate()
	{
		//Follow mouse if currently picked up
		if (PlacementManager.currentPlacement == this && !isPlaced)
		{
			var newPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
			mainObject.transform.position = new Vector3(newPos.x, newPos.y, mainObject.transform.position.z);

			//Snap
			if (snap)
			{
				snap.Snap();

				if (onSnap != null)
					onSnap.Invoke(this);
			}
		}

		//Color change logic
		if (_changeColor)
			ColorChange();
	}

	void ColorChange()
	{
		//Is currently being moved?
		if (!isPlaced)
		{
			//Blocked. Placement invalid. (Also check sync)
			if (isBlocked || othersToSyncWith.Exists(obj => obj != null && obj.isBlocked))
			{
				Color color = ColorPalette.cp.red4;
				color.a = unplaceableAlpha;
				sprites.ForEach(obj => obj.color = color);
			}
			//Connected, not blocked. Placement valid. (Also check sync)
			else if ((isConnected || doesNotNeedConnections) && !othersToSyncWith.Exists(obj => obj != null && (!obj.isConnected && !obj.doesNotNeedConnections)))
			{
				sprites.ForEach(obj => obj.color = ColorPalette.cp.blue4);
			}
			//Unconnected, not blocked. Placement not quite valid.
			else
			{
				Color color = ColorPalette.cp.yellow4;
				color.a = unplaceableAlpha;
				sprites.ForEach(obj => obj.color = color);
			}
		}
		//Don't color placed pieces
		else
		{
			foreach (var t in sprites)
			{
				if (t.name == "Outline")
					t.color = ColorPalette.cp.blk;
				else
					t.color = ColorPalette.cp.wht;
			}
		}

		_changeColor = false;
	}

	/**Clean out nulls from _others (usually from colliders that got disabled before OnTriggerExit)
	 */
	void CleanBlockersAndOthers()
	{
		while (_blockers.Contains(null))
		{
			_blockers.Remove(null);
		}
		while (_others.Contains(null))
		{
			_others.Remove(null);
		}
	}

	void Awake()
	{
		if (mainObject == null)
			mainObject = gameObject;

		if (rigid == null)
			rigid = mainObject.GetComponent<Rigidbody2D>();

		if (!sprites.Exists(obj => obj != null) && GetComponent<SpriteRenderer>() != null)
			sprites.Add(GetComponent<SpriteRenderer>());

		snap = mainObject.GetComponent<SnapBase>();
		if (!snap)
			snap = rigid.GetComponent<SnapBase>();
	}

	public void Sync(List<Placement> others)
	{
		//Add listeners and track the placements
		foreach (var t in others)
		{
			if (!othersToSyncWith.Contains(t))
			{
				t.onPlace += PlaceAction;
				t.onPickup += PickUpAction;
				t.onBlockOrConnectionChange += ColorChange;
			
				othersToSyncWith.Add(t);
			}
		}
	}

	public void Desync(List<Placement> others)
	{
		//Safety for OnDisable
		List<Placement> toRemove = new List<Placement>();

		//Clear listeners and prep for removal
		foreach (var t in others)
		{
			if (othersToSyncWith.Contains(t))
			{
				t.onPlace -= PlaceAction;
				t.onPickup -= PickUpAction;
				t.onBlockOrConnectionChange -= ColorChange;

				toRemove.Add(t);
			}
		}

		//Do Removal
		foreach (var t in toRemove)
		{
			othersToSyncWith.Remove(t);
		}
	}

	void OnEnable()
	{
		Sync(othersToSyncWith);
	}

	void OnDisable()
	{
		Desync(othersToSyncWith);
		_others.Clear();
		_blockers.Clear();
	}
}
