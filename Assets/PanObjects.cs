using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PanObjects : MonoBehaviour
{

	public Vector2 restPosition, startPosition, endPosition;

	public float speed;

	public List<Transform> objectsToMove = new List<Transform>();

	public List<GameObject> objectsToTurnOffWhileMoving = new List<GameObject>();
	public List<MonoBehaviour> scriptsToTurnOffWhileMoving = new List<MonoBehaviour>();

	/**Do the pan action. Set starting position, begin movement.
	 */
	void Pan()
	{
		//Turn shit off
		foreach (var t in objectsToTurnOffWhileMoving)
		{
			t.SetActive(false);
		}

		foreach (var t in scriptsToTurnOffWhileMoving)
		{
			t.enabled = false;
		}

		//Set pos
		foreach (var t in objectsToMove)
		{
			t.localPosition = startPosition;
		}

		//Begin
		StartCoroutine(PanSteps());
	}

	/**Pan movement. Ends once it reaches the end position.
	 */
	IEnumerator PanSteps()
	{
		float time = 0;

		//Done moving? No, move.
		while (objectsToMove.Count > 0 && (Vector2)objectsToMove [0].localPosition != endPosition)
		{
			//Iterative
			yield return null;

			time += Time.unscaledDeltaTime / 100;

			//Move it all
			foreach (var t in objectsToMove)
			{
				t.localPosition = Vector2.Lerp(startPosition, endPosition, Mathf.Clamp01(time * speed));
			}
		}
	}

	/**Reset position of objects to rest pos.
	 */
	void Reset()
	{
		foreach (var t in objectsToTurnOffWhileMoving)
		{
			t.SetActive(true);
		}

		foreach (var t in scriptsToTurnOffWhileMoving)
		{
			t.enabled = true;
		}

		foreach (var t in objectsToMove)
		{
			t.localPosition = restPosition;
		}
	}
}
