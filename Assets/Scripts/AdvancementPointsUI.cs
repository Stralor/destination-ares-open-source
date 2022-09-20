﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvancementPointsUI : MonoBehaviour
{

	Text text;

	// Use this for initialization
	void Start()
	{
		text = GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update()
	{
		text.text = ColorPalette.ColorText(ColorPalette.cp.gry2, "Advancement Points\n") + MetaGameManager.currentUnlockPoints.ToString();
	}
}
