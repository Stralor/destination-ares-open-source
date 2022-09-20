using UnityEngine;
using System.Collections;

public class LineUpWithBottom : MonoBehaviour {

	public RectTransform targetToLineUpWith;
	public float padding;
	private RectTransform thisRect;

	void Start(){
		thisRect = GetComponent<RectTransform>();
	}

	void Update () {
		float yOffset = targetToLineUpWith.rect.min.y + targetToLineUpWith.anchoredPosition.y - padding;

		if (thisRect.offsetMax.y != yOffset)
			thisRect.offsetMax = new Vector2(thisRect.offsetMax.x, yOffset);
	}
}
