using UnityEngine;
using System.Collections;

public class ColorPalette : MonoBehaviour
{

	/**Singleton-like reference to the ColorPalette component.
	 */
	public static ColorPalette cp;

	//Change these as needed by your palette
	/**Higher numbers are lighter.
	 * 0s are black replacements.
	 * 1s are the most saturated.
	 * 2s are the base colors.
	 * 3s are bright!
	 * 4s are pale (great for text). */
	public Color blue0, blue1, blue2, blue3, blue4, yellow0, yellow1, yellow2, yellow3, yellow4, red0, red1, red2, red3, red4, blk, gry1, gry2, gry3, wht;

	public static string ColorToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex;
	}

	public static string ColorText(Color32 color, string text)
	{
		return "<color=#" + ColorToHex(color) + ">" + text + "</color>";
	}

	void Awake()
	{
		//Set the CP
		if (cp == null)
		{
			cp = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (cp != this)
		{
			Destroy(gameObject);
		}
	}

}
