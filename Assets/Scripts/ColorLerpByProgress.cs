using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ColorLerpByProgress : MonoBehaviour {

	Color step0, step1, step2;

	public int colorLevelToUse;

	//Choose at least one!
	public Image targetImage;
	public SpriteRenderer targetSprite;
	public Camera targetCamera;

	void Start(){
		//Set the proper colors
		switch (colorLevelToUse){
			//Blacks
			case 0:
				step0 = ColorPalette.cp.blue0;
				step1 = ColorPalette.cp.yellow0;
				step2 = ColorPalette.cp.red0;
				break;
			//Darks
			case 1:
				step0 = ColorPalette.cp.blue1;
				step1 = ColorPalette.cp.yellow1;
				step2 = ColorPalette.cp.red1;
				break;
			//Mains
			case 2:
				step0 = ColorPalette.cp.blue2;
				step1 = ColorPalette.cp.yellow2;
				step2 = ColorPalette.cp.red2;
				break;
			//Brights
			case 3:
				step0 = ColorPalette.cp.blue3;
				step1 = ColorPalette.cp.yellow3;
				step2 = ColorPalette.cp.red3;
				break;
			//Pales
			case 4:
				step0 = ColorPalette.cp.blue4;
				step1 = ColorPalette.cp.yellow4;
				step2 = ColorPalette.cp.red4;
				break;
		}
	}

	void Update(){
		float progress = (float)ShipResources.res.progress;

		if (targetImage != null){
			if (progress <= 50)
				targetImage.color = Color.Lerp(step0, step1, progress / 50);
			else
				targetImage.color = Color.Lerp(step1, step2, (progress - 50) / 50);
		}

		if (targetSprite != null){
			if (progress <= 50)
				targetSprite.color = Color.Lerp(step0, step1, progress / 50);
			else
				targetSprite.color = Color.Lerp(step1, step2, (progress - 50) / 50);
		}

		if (targetCamera != null){
			if (progress <= 50)
				targetCamera.backgroundColor = Color.Lerp(step0, step1, progress / 50);
			else
				targetCamera.backgroundColor = Color.Lerp(step1, step2, (progress - 50) / 50);
		}
	}
}
