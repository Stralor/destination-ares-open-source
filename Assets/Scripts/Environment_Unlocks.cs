using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Environment_Unlocks : Environment
{

	public Canvas menu;
	public Button cancelSceneButton;
	public GameObject tooltipWarning;

	public override void PressedCancel()
	{
		cancelSceneButton.Select();
	}

	public void LoadStart()
	{
		StartCoroutine(Level.MoveToScene("Start Menu"));
	}

	public void TryBuyTech(Unlockable tech)
	{
		tech.BuyUnlockable();
	}

	public void TurnOnTooltips()
	{
		PlayerPrefs.SetInt("Tooltips", 1);	
	}

	protected override void Start()
	{
		base.Start();

		if (PlayerPrefs.GetInt("Tooltips") == 0)
		{
			tooltipWarning.SetActive(true);
		}
	}

	void Awake()
	{
		menu.worldCamera = Camera.main;
	}
}
