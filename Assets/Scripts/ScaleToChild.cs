using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/**DOESN'T WORK FOR ANCHORS WITH ANY STRETCH.
 */
public class ScaleToChild : MonoBehaviour {

	[Tooltip ("Will auto-assign by getting the first child. May be assigned in inspector to prevent incorrect assignment.")]
	public Transform child;
	private ILayoutElement childElement;
	private RectTransform mainElement;

	public bool scaleWidth, scaleHeight;
	public float ratio = 1;
	public float bufferSize;
	
	void Update () {

		//Confirm transforms
		if (mainElement == null)
			mainElement = GetComponent<RectTransform>();
		if (child == null)
			child = transform.GetChild(0);
		if (childElement == null)
			childElement = child.GetComponent<ILayoutElement>();

		//Safety
		if (mainElement == null || childElement == null){
			Debug.LogWarning("ScaleToChild only works when the GameObject has a RectTransform, " +
				"and the child has a component with preferred sizes (try using LayoutElements or ContentSizeFitters on UI Elements like Texts and Images).");
			return;
		}

		//Get base values
		Vector2 anchorDistance = mainElement.anchorMax - mainElement.anchorMin;
		float width = mainElement.rect.width;
		float height = mainElement.rect.height;

		//Assign new values
		if (scaleWidth)
			width = childElement.preferredWidth * ratio + bufferSize;
		if (scaleHeight)
			height = childElement.preferredHeight * ratio + bufferSize;


		//Set sizeDelta. SizeDelta scales (or not) based on the anchors, so here we're gonna counter that.
		mainElement.sizeDelta = new Vector2(Mathf.Pow(width, 1 - anchorDistance.x), Mathf.Pow(height, 1 - anchorDistance.y));
	}
}
