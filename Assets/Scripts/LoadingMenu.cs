using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class LoadingMenu : MonoBehaviour
{

	public GameObject defaultShipToggle, customShipToggle;

	public ToggleGroup contentObject;

	public Button loadButton;
	public Text failText;

	List<string> toggleNameList = new List<string>();

	public string currentShipLoadName;

	public List<System.Action<GameObject>> actionsOnValueChange = new List<System.Action<GameObject>>();

	[HideInInspector] public List<GameObject> defaultToggles = new List<GameObject>();

	List<ShipKey> shipKeys = new List<ShipKey>();



	void SetLoadName()
	{
		if (contentObject.AnyTogglesOn())
		{
			//Active toggle is the load name
			currentShipLoadName = contentObject.ActiveToggles().ToList() [0].GetComponentInChildren<Text>().text;
			loadButton.interactable = true;
		}
		else
		{
			//Nothing selected
			currentShipLoadName = "";
			loadButton.interactable = false;
		}
	}

	GameObject GetNewToggle(string fileName, GameObject toggleToUse)
	{
		var go = (GameObject)Instantiate(toggleToUse, contentObject.transform.position, Quaternion.identity, contentObject.transform);

		var toggle = go.GetComponent<Toggle>();
		toggle.group = contentObject;
		toggle.GetComponentInChildren<Text>().text = fileName;
		toggleNameList.Add(fileName);

		//Add a listener, ignore the bool arg
		toggle.onValueChanged.AddListener((bool arg0) => SetLoadName());

		//Add externally provided actions
		foreach (var t in actionsOnValueChange)
		{
			toggle.onValueChanged.AddListener(((bool arg0) => t.Invoke(toggle.gameObject)));
		}

		return go;
	}

	void AddTogglesToMenu()
	{
		//Add all premade ship files, drop the extension
		DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath + "/PremadeShips");
		foreach (var t in dir.GetFiles("*.ship"))
		{
			var n = t.Name.Remove(t.Name.IndexOf("."));
			if (!toggleNameList.Contains(n))
			{
				defaultToggles.Add(GetNewToggle(n, defaultShipToggle));
			}
		}

		//Do the same with all the custom ship files
		dir = new DirectoryInfo(Application.persistentDataPath);
		foreach (var t in dir.GetFiles("*.ship"))
		{
			var n = t.Name.Remove(t.Name.IndexOf("."));
			if (!toggleNameList.Contains(n))
			{
				GetNewToggle(n, customShipToggle);
			}
		}
	}

	void OnEnable()
	{
		AddTogglesToMenu();
		failText.gameObject.SetActive(false);

		shipKeys.AddRange(Resources.LoadAll<ShipKey>("ShipKeys/"));

		//Set keys on all our default ships, to make sure they're available
		foreach (var t in defaultToggles)
		{
			string keyName = t.GetComponentInChildren<Text>().text;
			var shipKey = shipKeys.Find(obj => obj != null && obj.shipFileName != null && keyName.Contains(obj.shipFileName.Replace(".ship", "")));
			if (shipKey != null)
			{
				var metaLock = t.GetComponentInChildren<MetaGameLock>();
				metaLock.requiredKey = shipKey.requiredKey;

				if (shipKey.customLockText.TrimEnd() != "")
					metaLock.tooltipTextWhileLocked = shipKey.customLockText;

				if (!shipKey.showWhenLocked && !metaLock.IsUnlocked)
					t.gameObject.SetActive(false);

			}
		}
	}

	//We have to repopulate this every time or we get bugs
	void OnDisable()
	{
		defaultToggles.Clear();
		toggleNameList.Clear();
		shipKeys.Clear();

		foreach (var t in GetComponentsInChildren<Toggle>())
		{
			Destroy(t.gameObject);
		}
	}
}
