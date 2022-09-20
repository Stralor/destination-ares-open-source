using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour
{

	//Declarations
	bool lerpingColor = true;

	//Cache
	SpriteRenderer sprite;


	void OnTriggerEnter2D(Collider2D other)
	{

		if (other.tag == "Player" && other.attachedRigidbody != null)
		{
			//End the game
			EventGameParameters.s.EndGame(true);

			//Stop spinning, stand upright
			GetComponent<Spin>().StopSpin();
			transform.eulerAngles = new Vector3(0, 0, 0);

			//Color change
			lerpingColor = false;
			sprite.color = ColorPalette.cp.blue3;
		}
	}

	void Start()
	{

		sprite = GetComponent<SpriteRenderer>();

		GetComponent<Spin>().StartSpin();
	}

	void Update()
	{
		//Lerp the sprite color!
		if (lerpingColor)
		{
			float colorLerp = (Time.time * 2) % 2;
			colorLerp = colorLerp < 1 ? colorLerp : 2 - colorLerp;
			
			sprite.color = Color.Lerp(ColorPalette.cp.yellow4, ColorPalette.cp.yellow2, colorLerp);
		}
	}
}
