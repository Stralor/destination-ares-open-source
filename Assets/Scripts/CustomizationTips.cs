using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomizationTips : MonoBehaviour
{
	public int changeTime;

	[TextArea()] public List<string> moduleTips = new List<string>();
	[TextArea()] public List<string> systemTips = new List<string>();
	[TextArea()] public List<string> crewTips = new List<string>();

	UnityEngine.UI.Text text;

	void Awake()
	{
		text = GetComponent<UnityEngine.UI.Text>();
	}

	public void ChangeTip()
	{
		string tipToUse = "";

		if (Environment_Customization.cust.tab_Module)
		{
			tipToUse = GetString(moduleTips.FindAll(obj => obj != ""));
		}
		else if (Environment_Customization.cust.tab_System)
		{
			tipToUse = GetString(systemTips.FindAll(obj => obj != ""));
		}
		else if (Environment_Customization.cust.tab_Crew)
		{
			tipToUse = GetString(crewTips.FindAll(obj => obj != ""));
		}

		text.text = tipToUse;

		if (IsInvoking("ChangeTip"))
			CancelInvoke("ChangeTip");

		Invoke("ChangeTip", changeTime);
	}

	string GetString(List<string> searchMe)
	{
		string tipToUse = "";
		if (searchMe.Count > 0)
		{
			do
			{
				tipToUse = searchMe [Random.Range(0, searchMe.Count)];
			}
			while (searchMe.Count > 1 && tipToUse == text.text);
		}

		return tipToUse;
	}
}
