using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ShipSelectionToggle : MonoBehaviour
{
	Text shipName;

	/**Delete this ship's save file
	 */
	public void DeleteShip()
	{
		//Find it
		DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
		var file = dir.GetFiles(shipName.text + ".ship");

		//Get rid of it
		if (file != null && file.Length > 0)
		{
			file [0].Delete();
		}

		//Achievement
		AchievementTracker.UnlockAchievement("DELETION");

	}

	void Start()
	{
		shipName = GetComponentInChildren<Text>();
	}
}
