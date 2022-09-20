using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/**Lets the Pointer and Keyboard/ Controller inputs alternate their hovers. Avoids the double-highlight effect on the UI when the player uses two different inputs.
 * Put on each of your button and selectable prefabs. Or on each one by hand if that's how you get your kicks.
 */
public class OnSelectPointerClear : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IPointerExitHandler
{
	[Tooltip("Should we have the keyboard selection update to the mouse hover when over this? InputFields probably want this to be false.")]
	public bool selectOnPointerEnter = true;

	//The sister selectable game object
	Selectable thisSelectable;

	public void OnSelect(BaseEventData data)
	{
		if (thisSelectable.interactable)
		{
			//New keyboard selection to track
			SelectionTracker.currentKeyboardSelection = thisSelectable;
			
			//Clear the pointer selection
			SelectionTracker.ClearPointerSelection();
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (thisSelectable.interactable)
		{
			//New pointer selection to track
			SelectionTracker.currentPointerSelection = thisSelectable;
			
			//Clear the keyboard selection
			SelectionTracker.ClearKeyboardSelection();
			
			if (selectOnPointerEnter)
				//Select this! (Now keyboard selectable will match pointer's position)
				thisSelectable.Select();
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		//Since we used Select() for the pointer (to merge with keyboard input), we need to clear the highlight when the mouse leaves
		SelectionTracker.ClearKeyboardSelection();
	}

	//This Update method *could* be moved to another Monobehaviour that only exists once in the scene. Probably not worth that optimization and scene management headache.
	void Update()
	{
		//Since we clear the highlight when we move the pointer off, we need to check if we have input that doesn't change the current selection.
		//If we do, we have to restore the selection (in the case of clicking off with mouse) or restore the highlight (in the case of dead-end keyboard input)
		if (Input.anyKeyDown && SelectionTracker.currentKeyboardSelection != null && SelectionTracker.lastKeyboardSelection == SelectionTracker.currentKeyboardSelection)
		{
			//Only call Select again if we no longer have a currently selected object
			if (EventSystem.current.currentSelectedGameObject == null)
				SelectionTracker.currentKeyboardSelection.Select();
			
			//Otherwise, we can just restore the highlight
			else
				SelectionTracker.currentKeyboardSelection.OnSelect(new BaseEventData(EventSystem.current));

			//Set lastKeyboardSelection to null so only the first OnSelectPointerClear.Update will do all this
			SelectionTracker.lastKeyboardSelection = null;
		}
	}

	void Awake()
	{
		//Set thisSelectable (it should be on the same gameObject)
		thisSelectable = GetComponent<Selectable>();
	}
}

/**Static tracker class associated with OnSelectPointerClear.
 */
public static class SelectionTracker
{
	//Current selections by the two input systems
	public static Selectable currentPointerSelection, currentKeyboardSelection;
	/**The keyboard selection state when we cleared its hover */
	public static Selectable lastKeyboardSelection;

	/**We're doing keyboard/ controller input now. Clear the pointer hover.
	 */
	public static void ClearPointerSelection()
	{
		//Only do this if the pointer isn't actually over this object
		if (currentPointerSelection != null && currentPointerSelection != currentKeyboardSelection)
			currentPointerSelection.OnPointerExit(new PointerEventData(EventSystem.current));
	}

	/**We're doing pointer input now. Clear the keyboard/ controller's hover.
	 */
	public static void ClearKeyboardSelection()
	{
		if (currentKeyboardSelection != null)
		{
			currentKeyboardSelection.OnDeselect(new BaseEventData(EventSystem.current));

			//Mark what the last selection was, we might need it
			lastKeyboardSelection = currentKeyboardSelection;
		}
	}
}
