using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControlsReference : MonoBehaviour
{

	public Image symbol;
	public Text controlName, needs;


	public void SetNeedsText(string text)
	{
		if (needs != null)
			needs.text = text;
	}

	public void EnableComponents()
	{
		if (symbol != null)
			symbol.enabled = true;
		if (controlName != null)
			controlName.enabled = true;
		if (needs != null)
			needs.enabled = true;
	}

	public void DisableComponents()
	{
		if (symbol != null)
			symbol.enabled = false;
		if (controlName != null)
			controlName.enabled = false;
		if (needs != null)
			needs.enabled = false;
	}
}
