using UnityEngine;
using System.Collections;

public class MousePing : MonoBehaviour
{
	//Ping image component
	SpriteRenderer _sprite;

	public float time = 1;
	public float growthSpeed = 0.01f;
	public bool reserved = false;

	public Vector3 adjustedScale
	{
		get
		{
			return new Vector3(1 / transform.parent.localScale.x, 1 / transform.parent.localScale.y, 1 / transform.parent.localScale.z);
		}
	}

	void Awake()
	{
		_sprite = GetComponent<SpriteRenderer>();
	}

	//Public facing trigger method
	public void Ping(bool slerp = false, float delay = 0)
	{
		reserved = true;

		//Make it active
		gameObject.SetActive(true);

		//Initial settings (invisible, but present)
		transform.localScale = Vector3.zero;
		_sprite.color = Color.clear;

		//Grow
		StartCoroutine(Grow(slerp, delay));
	}

	public void ReversePing(bool slerp = false, float delay = 0)
	{
		reserved = true;

		//Make it active
		gameObject.SetActive(true);

		//Initial settings (invisible, but present)
		transform.localScale = adjustedScale * growthSpeed * (time / Time.unscaledDeltaTime);
		_sprite.color = Color.clear;

		//Grow
		StartCoroutine(Grow(slerp, delay, true));
	}

	/**Actual effect method
	 */
	IEnumerator Grow(bool slerp, float delay, bool reverse = false)
	{
		yield return new WaitForSecondsRealtime(delay);

		float normalizedTime = reverse ? 1 : 0;

		Color col = ColorPalette.cp.wht;

		_sprite.color = new Color(col.r, col.g, col.b, reverse ? 0 : col.a);

		while ((!reverse && _sprite.color.a > 0) || (reverse && _sprite.color.a < 1))
		{
			_sprite.color = Color.Lerp(ColorPalette.cp.wht, new Color(col.r, col.g, col.b, 0), normalizedTime > 1 ? 1 : normalizedTime < 0 ? 0 : normalizedTime);

			if (slerp)
				transform.localScale = Vector3.Slerp(Vector3.zero, adjustedScale * growthSpeed * (time / Time.unscaledDeltaTime), 1 - _sprite.color.a);
			else
			{
				var change = reverse ? adjustedScale * -growthSpeed : adjustedScale * growthSpeed;
				transform.localScale += change;
			}

			var adjustment = Time.unscaledDeltaTime / time;
			normalizedTime = reverse ? normalizedTime - adjustment : normalizedTime + adjustment;
			yield return null;
		}

		//Done
		gameObject.SetActive(false);
		reserved = false;
	}
}
