using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WaitCircle : MonoBehaviour
{
	Image _image;

	public float time;

	void Awake()
	{
		_image = GetComponentInChildren<Image>();
	}

	public void Activate(float timeSet = 1)
	{
		time = timeSet;

		//Make it visible
		gameObject.SetActive(true);

		//Make sure it's positioned centered
		transform.localPosition = Vector3.zero;

		//Do it
		StartCoroutine(Fill());
	}

	/**Actual effect method
	 */
	IEnumerator Fill()
	{
		float currentTime = 0;
		_image.color = ColorPalette.cp.wht;

		Color colA = ColorPalette.cp.yellow4, colB = ColorPalette.cp.wht;

		while (currentTime < 1 && gameObject.activeInHierarchy)
		{
			_image.color = Color.Lerp(new Color(colA.r, colA.g, colA.b, 0), new Color(colB.r, colB.g, colB.b, 0.5f), currentTime * currentTime);
			_image.fillAmount = currentTime;

			currentTime += Time.unscaledDeltaTime / time;
			yield return null;
		}

		//Done
		gameObject.SetActive(false);
	}
}

public static class WaitCircleManager
{
	public static List<GameObject> pool = new List<GameObject>();

	public static GameObject GetFreshCircle()
	{
		//Clean pool
		pool.RemoveAll(obj => obj == null);

		var circle = pool.Find(obj => !obj.activeSelf);

		if (!circle)
		{
			circle = GameObject.Instantiate(Resources.Load("WaitCircle")) as GameObject;
			pool.Add(circle);
		}

		return circle;
	}
}
