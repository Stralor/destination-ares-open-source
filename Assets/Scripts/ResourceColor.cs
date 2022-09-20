using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ResourceColor : MonoBehaviour
{

	private Image symbol;
	bool _sel;

	public void ColorChange(bool selected)
	{
		if (selected)
		{
			symbol.color = ColorPalette.cp.yellow4;
			_sel = true;
		}
		else
		{
			symbol.color = ColorPalette.cp.blue4;
			_sel = false;
		}
	}

	public IEnumerator BurstColor(bool up, float duration = 1)
	{
		//Safety
		if (duration <= 0)
			duration = 1;

		//Early exit
		if (!_sel)
		{
			//Color refs
			Color burstColor;

			//Type
			if (up)
				burstColor = ColorPalette.cp.wht;
			else
				burstColor = ColorPalette.cp.red4;

			float time = 0;
			//Effect iteration
			do
			{
				symbol.color = Color.Lerp(burstColor, ColorPalette.cp.blue4, time / duration);
				time += Time.deltaTime;
				yield return null;
			}
			while (time / duration < 1);
		}

		//Syntax
		yield return null;
	}

	void Start()
	{
		symbol = GetComponent<Image>();
	}
}
