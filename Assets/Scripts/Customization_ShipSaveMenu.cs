using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Customization_ShipSaveMenu : MonoBehaviour
{
	public InputField fileNameInput;

	public GameObject warn_Expensive, warn_NoCrew, warn_NoResources, warn_MoreResources, warn_ShipDisconnected, warn_MissingDoors, warn_InvalidText, warn_Overwriting;

	public Button saveButton;


	public void CheckWarnings()
	{
		//Can we save? If false, there are critical warnings
		bool checkPassed = true;

		//Too Expensive?
		if (Customization_CurrencyController.c.effectiveCurrency < 0)
		{
			warn_Expensive.SetActive(true);
			checkPassed = false;
		}
		else
			warn_Expensive.SetActive(false);


		//No Crew?
		if (Environment_Customization.cust.GetCrewCount() == 0)
		{
			warn_NoCrew.SetActive(true);
			checkPassed = false;
		}
		else
			warn_NoCrew.SetActive(false);

		//Ship Disconnected?
		if (!Environment_Customization.cust.CheckModules())
		{
			warn_ShipDisconnected.SetActive(true);
			checkPassed = false;
		}
		else
			warn_ShipDisconnected.SetActive(false);

		//Doors?
		if (!Environment_Customization.cust.CheckDoors())
		{
			warn_MissingDoors.SetActive(true);
		}
		else
			warn_MissingDoors.SetActive(false);
		
		//No resources?
		if (Environment_Customization.cust.air + Environment_Customization.cust.waste + Environment_Customization.cust.fuel + Environment_Customization.cust.food + Environment_Customization.cust.materials + Environment_Customization.cust.parts == 0)
		{
			warn_NoResources.SetActive(true);
			checkPassed = false;
		}
		else
			warn_NoResources.SetActive(false);

		//Space for more resources?
		if (Environment_Customization.cust.storageRemaining > 0 && Customization_CurrencyController.c.effectiveCurrency > 0 && !warn_NoResources.activeSelf)
		{
			warn_MoreResources.SetActive(true);
		}
		else
			warn_MoreResources.SetActive(false);

		//File Name No Text?
		if (Environment_Customization.cust.shipSaveName == null || Environment_Customization.cust.shipSaveName.Trim() == "")
		{
			warn_InvalidText.SetActive(true);
			checkPassed = false;
		}
		else
			warn_InvalidText.SetActive(false);

		//File Name Already Exist?
		if (Environment_Customization.cust.shipSaveName != null && SaveLoad.DoesShipNameAlreadyExist(Environment_Customization.cust.shipSaveName))
		{
			warn_Overwriting.SetActive(true);
		}
		else
			warn_Overwriting.SetActive(false);

		//Update saveButton
		saveButton.interactable = checkPassed;
	}

	void OnEnable()
	{
		CheckWarnings();
	}
}
