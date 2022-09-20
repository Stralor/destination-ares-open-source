using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kernys.Bson;

public class SaveLoad : MonoBehaviour
{
	/**Increment this by a whole number when adding new Save/Load values (to keep from errors).
	 * Decrement by a fraction when removing values to keep compatibility, yet demarcate change.
	 * (To avoid incrementing this number, try dataToUnload.ContainsKey on new values)
	 */
	public const float SAVE_VERSION = 14;

	private static SaveLoad _s;

	public static SaveLoad s
	{
		get
		{
			if (_s == null)
			{
				_s = FindObjectOfType<SaveLoad>();
				if (_s == null)
				{
					GameObject gO = new GameObject();
					gO.name = "SaveLoad";
					
					_s = gO.AddComponent<SaveLoad>();
					DontDestroyOnLoad(gO);
				}
			}

			return _s;
		}
	}

	public BSONObject currentData;

	public bool currentlySaving { get; private set; }

	/**Save game file.
	 * If runExists, will mark as game in progress. Else, only new game available.
	 * If overwriteData, runExists will be ignored. Instead, overwriteData's value will be used.
	 */
	public bool SaveGame(bool runExists = true, BSONObject overwriteData = null)
	{
		if (!currentlySaving)
		{
			//Notify
			//Debug.Log("SAVING\t(time: " + Time.time.ToString("F2") + ")");
			currentlySaving = true;
			//TODO Insert saving anim call here
			
			//Open the formatter and file
			FileStream file = File.Create(Application.persistentDataPath + "/savegame.ares");
			
			//Organize data
			BSONObject data = overwriteData;
			if (data == null)
				data = SaveData.NewGameData(runExists);

			//Finish the save
			byte[] buffer = SimpleBSON.Dump(data);
			file.Write(buffer, 0, buffer.Length);
			file.Close();
			currentlySaving = false;
		}

		return !currentlySaving;
	}

	/**Rewrite save with updated Stats and Achievements
	 */
	public void SaveMetaGame()
	{
		//Safety
		if (currentData == null)
		{
			Debug.LogWarning("Stats and Achievements not saved: currentData is null and cannot be updated.");
			return;
		}

		//Overwrites
		currentData ["StatTrack"] = SaveData.ConvertStatTrackToBSON();
		currentData ["meta"] = SaveData.SaveMetagame();

		//Done
		SaveGame(overwriteData: currentData);
	}

	/**Save a custom ship file.
	 * ShipName is the file name. Resources are the ship's res (in alpha order): air, food, fuel, materials, parts, waste
	 */
	public void SaveShip(string shipName, int[] resources)
	{
		if (shipName.Trim() == "")
		{
			print("Ship name invalid.");
			return;
		}

		//Ship file
		FileStream file = File.Create(Application.persistentDataPath + "/" + shipName + ".ship");

		try
		{
			//Organize data
			BSONObject data = SaveData.NewShipData(resources);

			//Finish the save
			byte[] buffer = SimpleBSON.Dump(data);
			file.Write(buffer, 0, buffer.Length);
			file.Close();
			currentlySaving = false;
		}
		//Get rid of the file if it doesn't work
		catch (System.Exception e)
		{
			Debug.LogWarning(e.Source);

			file.Close();
			File.Delete(Application.persistentDataPath + "/" + shipName + ".ship");
		}
	}

	/**Load game file fully into scene. Should be called from inside main play scene. If you just want some values from file, use Peek.
	 */
	public void LoadGame(bool spritesOnly = false)
	{
		//Is there a save game to load?
		if (File.Exists(Application.persistentDataPath + "/savegame.ares"))
		{
			//Log it
			Debug.Log("LOADING\t(time: " + Time.time.ToString("F2") + ")");

			//Get the data
			BSONObject data;

			//Main file
			try
			{
				data = AccessFile("savegame.ares", true);
				//Start unloading into the scene
				SaveData.BuildFullScene(data, spritesOnly);

				//Save the backup if we got this far, and we're not just saving sprites
				if (!spritesOnly)
					File.Copy(Path.Combine(Application.persistentDataPath, "savegame.ares"), Path.Combine(Application.persistentDataPath, "bkp.ares"), true);
			}
			//Backup file
			catch (System.Exception e)
			{
				Debug.LogWarning("Exception: " + e.Source + "\n\n" + e.StackTrace);
				Debug.Log("Continuing with backup file.");

				if (File.Exists(Application.persistentDataPath + "/bkp.ares"))
				{
					data = AccessFile("bkp.ares", true);

					//Nest
					try
					{
						//Start unloading into the scene
						SaveData.BuildFullScene(data, spritesOnly);
					}
					catch (System.Exception ex)
					{
						//Welp, just wipe the bitch
						Debug.LogWarning("Another damn error. Run wiped, sry brah. Start a new one.");
						Debug.Log(ex.Message);
						WipeRun();

						//Update scene
						var menu = FindObjectOfType<Environment_StartMenu>();
						if (menu != null)
						{
							//Destroy anything that did get generated
							FindObjectsOfType<ClearContents>().ToList().ForEach(obj => obj.ClearAll());

							//Check for save (aka, reset menu)
							menu.CheckForSaveFile();
						}
					}
				}
			}
		}
	}

	/**Load the ship of the given file name into the scene.
	 * Pass a processor to handle prep if you need each of the objects in the ship tweaked somehow for the scene (i.e., customization screen prep).
	 * Pass a resource handler to take care of the ship's 6 hard-resource values (in alphabetical order) - used in cases where ShipRes is null in the scene.
	 */
	public void LoadShip(string shipName, System.Action<GameObject> processor = null, System.Action<int[]> resourceHandler = null, bool willCatchError = false, bool spritesOnly = false)
	{
		//Data to get
		BSONObject data;

		//Attempt
		try
		{
			//Is there a save file to load?
			if (File.Exists(Application.persistentDataPath + "/" + shipName + ".ship"))
			{
				data = AccessFile(shipName + ".ship", false);
			}
			else if (File.Exists(Application.streamingAssetsPath + "/PremadeShips/" + shipName + ".ship"))
			{
				data = AccessFile(shipName + ".ship", false, Application.streamingAssetsPath + "/PremadeShips");
			}
			else if (File.Exists(Application.streamingAssetsPath + "/LockedShips/" + shipName + ".ship"))
			{
				data = AccessFile(shipName + ".ship", false, Application.streamingAssetsPath + "/LockedShips");
			}
			else
			{
				//Can't do it.
				Debug.LogError("Ship file does not exist in either directory (custom nor premade): " + shipName + ".ship");
				//Get out before the rest of the processing.
				return;
			}

			//Got it!
			//Unload it into the scene
			SaveData.BuildShip(data, processor, resourceHandler, spritesOnly);
		}

		//Fail
		catch (System.Exception e)
		{
			if (willCatchError)
				throw e;
			else
				Debug.LogError("Exception: " + e.Source + "\n\n" + e.StackTrace);
		}
	}

	/**Returns the deserialized game data from the saved file, so that you can pull whatever individual values out to use.
	 * (e.g. Start menu will use it to determine if there is a saved game!)
	 */
	public BSONObject Peek()
	{
		//Is there a save game to peek at?
		if (File.Exists(Application.persistentDataPath + "/savegame.ares"))
		{
			//Log it
			Debug.Log("PEEKING\t(time: " + Time.time.ToString("F2") + ")");

			//Get the data
			BSONObject data;

			//Main file
			try
			{
				data = AccessFile("savegame.ares", true);
			}
			//Backup file
			catch (System.Exception ex)
			{
				Debug.LogWarning("Exception: " + ex.Message);
				Debug.Log("Continuing with backup file.");

				data = AccessFile("bkp.ares", true);
			}

			return data;
		}
		else
			return null;
	}

	/**Opens file, converts filestream to byte[] buffer, deserializes buffer into BSONObject data, and finally returns.
	 * Defaults to searching Application.persistentDataPath, unless an overridePath is given.
	 */
	BSONObject AccessFile(string fileName, bool updateCurrentData, string overridePath = null)
	{
		if (overridePath == null)
			overridePath = Application.persistentDataPath;

		//Open formatter and file
		FileStream file = File.Open(overridePath + "/" + fileName, FileMode.Open);
		byte[] buffer;

		try
		{
			// Read from file
			int length = (int)file.Length;
			buffer = new byte[length];
			int count;
			int sum = 0;
			while ((count = file.Read(buffer, sum, length - sum)) > 0)
				sum += count;
		}
		finally
		{
			file.Close();
		}

		// Deserialize data
		BSONObject data = SimpleBSON.Load(buffer);
		if (updateCurrentData)
			currentData = data;

		return data;
	}

	/**Clear the run from the record
	 */
	public void WipeRun(BSONObject overwrite = null)
	{
		//We're going to assign the data. If we have an overwrite file, that's priority.
		BSONObject data = overwrite;
		//No overwrite? Load the current one in.
		if (overwrite == null)
			data = Peek();
		//Wipe the run
		data ["runExists"] = false;
		//Add to the memorial
		StatTrack.stats.AddCurrentVesselToMemorial(false, data ["day"], "Abandoned", true);
		//Overwrite global, persistent StatTrack
		data ["StatTrack"] = SaveData.ConvertStatTrackToBSON();
		data ["meta"] = SaveData.SaveMetagame();
		//Save
		SaveGame(overwriteData: data);
	}

	/**Coroutine to recover alerts between saves
	 */
	public IEnumerator RestoreAlerts(BSONValue alertString, Transform parentObj)
	{
		//Wait until we've got an alert child
		yield return new WaitUntil(() => parentObj.GetComponentInChildren<Alert>() != null);

		//Get the alert component
		var x = parentObj.GetComponentInChildren<Alert>();

		//Add the alerts by dividing the alert string
		foreach (var t in alertString.stringValue.Split(' '))
		{
			if (t != " ")
				x.SetAlert(t.Trim());
		}
	}

	/**Compare the string to the available filenames. Returns true if it finds a match.
	 */
	public static bool DoesShipNameAlreadyExist(string shipName)
	{
		return File.Exists(Application.persistentDataPath + "/" + shipName + ".ship");
	}

	void Awake()
	{
		if (_s == null)
		{
			_s = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (this != _s)
			Destroy(gameObject);
	}
}

/**Handler class for BSON container of all game data. Sources data from the game files.
 */
public static class SaveData
{
	public static BSONObject NewGameData(bool runFound)
	{
		SaveLoad.s.currentData = new BSONObject();
		SaveLoad.s.currentData ["runExists"] = runFound;
		CaptureFullScene();
		return SaveLoad.s.currentData;
	}

	public static BSONObject NewShipData(int[] resources)
	{
		UnityEngine.Assertions.Assert.IsTrue(resources.Length == 6);

		var shipData = new BSONObject();
		CaptureShip(ref shipData);
		shipData ["air"] = resources [0];
		shipData ["food"] = resources [1];
		shipData ["fuel"] = resources [2];
		shipData ["materials"] = resources [3];
		shipData ["parts"] = resources [4];
		shipData ["waste"] = resources [5];

		return shipData;
	}

	/**Capture all values to be saved.
	 */
	static void CaptureFullScene()
	{
		//For iterators
		int count = 0;

		//Version number
		SaveLoad.s.currentData ["SaveVersion"] = SaveLoad.SAVE_VERSION;

		//StatTrack and achievements
		SaveLoad.s.currentData ["StatTrack"] = ConvertStatTrackToBSON();
		SaveLoad.s.currentData ["meta"] = SaveMetagame();

		//Ship Name
		if (GameReference.r.shipName != null)
			SaveLoad.s.currentData ["shipName"] = GameReference.r.shipName;
		else
			SaveLoad.s.currentData ["shipName"] = "Ship";

		SaveLoad.s.currentData ["score"] = StatTrack.stats.score;

		//Res
		SaveLoad.s.currentData ["offCourse"] = ShipMovement.sm.GetOffCourse();
		SaveLoad.s.currentData ["injectTime"] = ShipMovement.sm.injectCounter;
		SaveLoad.s.currentData ["savedThrust"] = ShipMovement.sm.thrustYetToBeAdded;
		SaveLoad.s.currentData ["distance"] = ShipResources.res.distance;
		SaveLoad.s.currentData ["startingDistance"] = ShipResources.res.startingDistance;
		SaveLoad.s.currentData ["progress"] = ShipResources.res.progress;
		SaveLoad.s.currentData ["speed"] = ShipResources.res.speed;
		SaveLoad.s.currentData ["usableAir"] = ShipResources.res.usableAir;
		SaveLoad.s.currentData ["totalAir"] = ShipResources.res.totalAir;
		SaveLoad.s.currentData ["energy"] = ShipResources.res.energy;
		SaveLoad.s.currentData ["food"] = ShipResources.res.food;
		SaveLoad.s.currentData ["fuel"] = ShipResources.res.fuel;
		SaveLoad.s.currentData ["materials"] = ShipResources.res.materials;
		SaveLoad.s.currentData ["parts"] = ShipResources.res.parts;
		SaveLoad.s.currentData ["waste"] = ShipResources.res.waste;

		//Clock
		SaveLoad.s.currentData ["clockSpeed"] = GameClock.clock.clockSpeed;
		SaveLoad.s.currentData ["day"] = GameClock.clock.day;
		SaveLoad.s.currentData ["hour"] = GameClock.clock.hour;
		SaveLoad.s.currentData ["minute"] = GameClock.clock.minute;

		//Events
		count = 0;
		SaveLoad.s.currentData ["events"] = new BSONObject();
		//Current
		if (GameEventManager.gem.eventIsActive)
		{
			SaveLoad.s.currentData ["events"] ["current"] = GameEventManager.gem.currentEvent.name;
			SaveLoad.s.currentData ["events"] ["lastAction"] = GameEventManager.gem.eventLastAction.text;

			//Color
			SaveLoad.s.currentData ["events"] ["borderColor"] = new BSONObject();
			SaveLoad.s.currentData ["events"] ["borderColor"] ["r"] = GameEventManager.gem.eventBorder.color.r;
			SaveLoad.s.currentData ["events"] ["borderColor"] ["g"] = GameEventManager.gem.eventBorder.color.g;
			SaveLoad.s.currentData ["events"] ["borderColor"] ["b"] = GameEventManager.gem.eventBorder.color.b;
			SaveLoad.s.currentData ["events"] ["borderColor"] ["a"] = GameEventManager.gem.eventBorder.color.a;
		}
		//Scheduled
		foreach (var t in GameEventManager.gem.scheduledEvents)
		{
			SaveLoad.s.currentData ["events"] [count.ToString()] = new BSONObject();
			SaveLoad.s.currentData ["events"] [count.ToString()] ["name"] = t.eventStoreData.name;
			SaveLoad.s.currentData ["events"] [count.ToString()] ["day"] = t.day;
			SaveLoad.s.currentData ["events"] [count.ToString()] ["hour"] = t.hour;
			SaveLoad.s.currentData ["events"] [count.ToString()] ["minute"] = t.minute;
			SaveLoad.s.currentData ["events"] [count.ToString()] ["progress"] = t.progress;
			SaveLoad.s.currentData ["events"] [count.ToString()] ["progressType"] = t.progressType;
			count++;
		}
		SaveLoad.s.currentData ["events_count"] = count;
		count = 0;
		SaveLoad.s.currentData["validLossEvents"] = new BSONObject();
		//Loss
		foreach (var t in GameEventManager.gem.lossEvents)
		{
			SaveLoad.s.currentData ["validLossEvents"] [count.ToString()] = new BSONObject();
			SaveLoad.s.currentData ["validLossEvents"] [count.ToString()] ["name"] = t.name;
			count++;
		}
		SaveLoad.s.currentData ["validLossEvents_count"] = count;

		//Stories
		SaveLoad.s.currentData ["specialConditions"] = new BSONObject();
		var members = typeof(EventSpecialConditions).GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)
			.Where(member => member != null && !member.IsDefined(typeof(System.ObsoleteAttribute), true))
			.ToList();

		//Dammit, dynamic casting doesn't work with this BSON API. Do it by hand, asshole.
		foreach (var member in members)
		{
			//Save the properties
			if (member is PropertyInfo)
			{
				PropertyInfo prop = (PropertyInfo)member;

				if (prop.CanRead && prop.CanWrite)
				{
					object value = prop.GetValue(EventSpecialConditions.c, null);

					if (value is bool)
						SaveLoad.s.currentData ["specialConditions"] [prop.Name] = (bool)value;
					else if (value is int)
						SaveLoad.s.currentData ["specialConditions"] [prop.Name] = (int)value;
					else if (value is float)
						SaveLoad.s.currentData ["specialConditions"] [prop.Name] = (float)value;
					else if (value is string)
						SaveLoad.s.currentData ["specialConditions"] [prop.Name] = (string)value;
				}
			}
			//Save the fields
			else if (member is FieldInfo)
			{
				FieldInfo field = (FieldInfo)member;

				object value = field.GetValue(EventSpecialConditions.c);

				if (value is bool)
					SaveLoad.s.currentData ["specialConditions"] [field.Name] = (bool)value;
				else if (value is int)
					SaveLoad.s.currentData ["specialConditions"] [field.Name] = (int)value;
				else if (value is float)
					SaveLoad.s.currentData ["specialConditions"] [field.Name] = (float)value;
				else if (value is string)
					SaveLoad.s.currentData ["specialConditions"] [field.Name] = (string)value;
			}
		}
		//availableEvents = GameEventManager.gem.allEvents;

		//EventClockCheck
		var ecc = GameEventManager.gem.GetComponent<EventClockCheck>();
		SaveLoad.s.currentData ["events"] ["minimumDay"] = ecc.minimumDay;
		SaveLoad.s.currentData ["events"] ["minimumHour"] = ecc.minimumHour;
		SaveLoad.s.currentData ["events"] ["minimumMinute"] = ecc.minimumMinute;

		//Save the whole ship
		CaptureShip(ref SaveLoad.s.currentData);

		//TODO Stars
	}

	/**Gets the ship & crew values saved into the given BSONObject
	 */
	static void CaptureShip(ref BSONObject dataBase)
	{
		//Character list
		int count = 0;
		dataBase ["characters"] = new BSONObject();
		foreach (var t in GameObject.FindObjectsOfType<Character>())
		{
			dataBase ["characters"] [count.ToString()] = CharacterData.NewData(t);
			count++;
		}
		dataBase ["characters_count"] = count;

		//System list
		count = 0;
		dataBase ["systems"] = new BSONObject();
		foreach (var t in GameObject.FindObjectsOfType<ShipSystem>().Where(sys => sys.enabled))
		{
			dataBase ["systems"] [count.ToString()] = ShipSystemData.NewData(t);
			count++;
		}
		dataBase ["systems_count"] = count;

		//Modules
		count = 0;
		dataBase ["modules"] = new BSONObject();
		foreach (var t in GameObject.FindObjectsOfType<Module>())
		{
			//Mod size and position data
			dataBase ["modules"] [count.ToString()] = new BSONObject();
			dataBase ["modules"] [count.ToString()] ["size"] = (int)t.size;
			dataBase ["modules"] [count.ToString()] ["x"] = t.transform.position.x;
			dataBase ["modules"] [count.ToString()] ["y"] = t.transform.position.y;
			dataBase ["modules"] [count.ToString()] ["z"] = t.transform.position.z;
			dataBase ["modules"] [count.ToString()] ["rot"] = t.transform.eulerAngles.z;
			//Module Art
			dataBase ["modules"] [count.ToString()] ["module_art"] = new BSONObject();
			int artCount = 0;
			foreach (var u in t.GetComponentsInChildren<ModuleArt>())
			{
				dataBase ["modules"] [count.ToString()] ["module_art"] [artCount.ToString()] = u.spriteSignature;
				artCount++;
			}
			dataBase ["modules"] [count.ToString()] ["module_art_count"] = artCount;
			count++;
		}
		dataBase ["modules_count"] = count;

		//Doors and Pins
		count = 0;
		dataBase ["openings"] = new BSONObject();
		foreach (var t in GameObject.FindObjectsOfType<Opening>())
		{
			//Opening type and position data
			dataBase ["openings"] [count.ToString()] = new BSONObject();
			dataBase ["openings"] [count.ToString()] ["type"] = (int)t.type;
			dataBase ["openings"] [count.ToString()] ["x"] = t.transform.position.x;
			dataBase ["openings"] [count.ToString()] ["y"] = t.transform.position.y;
			dataBase ["openings"] [count.ToString()] ["z"] = t.transform.position.z;
			dataBase ["openings"] [count.ToString()] ["rot"] = t.transform.eulerAngles.z;
			count++;
		}
		dataBase ["openings_count"] = count;

		//Hull Art
		count = 0;
		dataBase ["hullExtensions"] = new BSONObject();
		foreach (var t in GameObject.FindObjectsOfType<HullExtension>())
		{
			//Data
			dataBase ["hullExtensions"] [count.ToString()] = new BSONObject();
			dataBase ["hullExtensions"] [count.ToString()] ["artIndex"] = t.artIndex;
			dataBase ["hullExtensions"] [count.ToString()] ["x"] = t.transform.position.x;
			dataBase ["hullExtensions"] [count.ToString()] ["y"] = t.transform.position.y;
			dataBase ["hullExtensions"] [count.ToString()] ["z"] = t.transform.position.z;
			dataBase ["hullExtensions"] [count.ToString()] ["rot"] = t.transform.eulerAngles.z;
			count++;
		}
		dataBase ["hullExtensions_count"] = count;
	}

	/**Fill in scene with values from saved file.
	 */
	public static void BuildFullScene(BSONObject dataToUnload, bool spritesOnly)
	{

		if (dataToUnload == null)
		{
			Debug.LogWarning("The BSONObject \"dataToUnload\" is null. Cannot load scene without its values.");
		}

		//StatTrack
		UnloadStatTrack(dataToUnload);

		if (SaveLoad.SAVE_VERSION > SaveLoad.s.currentData ["SaveVersion"])
		{
			Debug.LogWarning("Invalid save version. Start a new game.");
			return;
		}

		//Populate the ship components
		//Make sure game ref exists
		if (GameReference.r != null || spritesOnly)
		{
			//Destroy old stuff if we aren't just loading sprites
			if (!spritesOnly)
			{
				ClearScene();
			}

			//Load in character datas
			for (int i = 0; i < dataToUnload ["characters_count"]; i++)
			{
				BSONObject dat = dataToUnload ["characters"] [i.ToString()] as BSONObject;
				//Create the new instance
				CharacterData.Unload(dat, spritesOnly);
			}
			
			//Load in system datas
			for (int i = 0; i < dataToUnload ["systems_count"]; i++)
			{
				BSONObject dat = dataToUnload ["systems"] [i.ToString()] as BSONObject;
				//Create the new instance
				ShipSystemData.Unload(dat, spritesOnly);
			}

			//Load in modules
			UnloadShipStructure(dataToUnload);
		}
		else
			Debug.LogWarning("GameReference instance does not exist. Cannot load objects.");


		//Time to load all the normal values
		if (!spritesOnly)
		{
			GameReference.r.shipName = dataToUnload ["shipName"].stringValue;

			//Res
			ShipMovement.sm.offCourse = dataToUnload ["offCourse"];
			ShipMovement.sm.injectCounter = dataToUnload ["injectTime"];
			ShipMovement.sm.AddSavedThrust(dataToUnload ["savedThrust"]);
			ShipResources.res.distance = dataToUnload ["distance"];
			ShipResources.res.startingDistance = dataToUnload ["startingDistance"];
			ShipResources.res.speed = dataToUnload ["speed"];
			ShipResources.res.SetTotalAir(dataToUnload ["totalAir"], updateStatTrack: false);
			ShipResources.res.SetUsableAir(dataToUnload ["usableAir"], updateStatTrack: false);
			ShipResources.res.SetEnergy(dataToUnload ["energy"], updateStatTrack: false);
			ShipResources.res.SetFood(dataToUnload ["food"], updateStatTrack: false);
			ShipResources.res.SetFuel(dataToUnload ["fuel"], updateStatTrack: false);
			ShipResources.res.SetMaterials(dataToUnload ["materials"], updateStatTrack: false);
			ShipResources.res.SetParts(dataToUnload ["parts"], updateStatTrack: false);
			ShipResources.res.SetWaste(dataToUnload ["waste"], updateStatTrack: false);

			//Initial camera adjustments
			CameraEffectsController.cec.InstantCameraRotation();

			//Clock
			GameClock.clock.clockSpeed = dataToUnload ["clockSpeed"];
			GameClock.clock.day = dataToUnload ["day"];
			GameClock.clock.hour = dataToUnload ["hour"];
			GameClock.clock.minute = dataToUnload ["minute"];

			//Events
			//Active Event if left while open (MiniGameBridge will overwrite this when returning)
			if (dataToUnload ["events"].ContainsKey("current"))
			{
				GameEventManager.gem.ResumeEvent(dataToUnload ["events"] ["current"], new Color(dataToUnload ["events"] ["borderColor"] ["r"],
				                                                                                dataToUnload ["events"] ["borderColor"] ["g"],
				                                                                                dataToUnload ["events"] ["borderColor"] ["b"],
				                                                                                dataToUnload ["events"] ["borderColor"] ["a"]));
				GameEventManager.gem.eventLastAction.text = dataToUnload ["events"] ["lastAction"];
			}
			//Scheduled Events
			for (int i = 0; i < dataToUnload ["events_count"]; i++)
			{
				//Progress type
				if (dataToUnload ["events"] [i.ToString()] ["progressType"])
					GameEventManager.gem.ScheduleProgressEvent(dataToUnload ["events"] [i.ToString()] ["progress"], specificEventName: dataToUnload ["events"] [i.ToString()] ["name"]);
				//Timed type
				else
					GameEventManager.gem.ScheduleTimedEvent(dataToUnload ["events"] [i.ToString()] ["day"], dataToUnload ["events"] [i.ToString()] ["hour"],
					                                        dataToUnload ["events"] [i.ToString()] ["minute"], specificEventName: dataToUnload ["events"] [i.ToString()] ["name"]);
			}
			//Loss Events
			for (int i = 0; i < dataToUnload ["validLossEvents_count"]; i++)
			{
				GameEventManager.gem.AddToLossEventsByName(dataToUnload ["validLossEvents"] [i.ToString()] ["name"]);
			}

			//Stories
			BSONObject package = (BSONObject)dataToUnload ["specialConditions"];
			//Get it and reflect
			var members = typeof(EventSpecialConditions)
				.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)
				.Where(member => member != null && !member.IsDefined(typeof(System.ObsoleteAttribute), true))
				.ToList();
			foreach (var key in package.Keys)
			{
				//Name match
				var member = members.Find(obj => obj.Name == key);

				if (member != null)
				{
					//Set prop
					if (member.MemberType == MemberTypes.Property)
					{
						PropertyInfo prop = member as PropertyInfo;

						//Set by type, if appropro
						if (prop.PropertyType == typeof(bool))
							prop.SetValue(EventSpecialConditions.c, package [key].boolValue, null);
						else if (prop.PropertyType == typeof(int))
							prop.SetValue(EventSpecialConditions.c, package [key].int32Value, null);
						else if (prop.PropertyType == typeof(float))
							prop.SetValue(EventSpecialConditions.c, package [key].floatValue, null);
						else if (prop.PropertyType == typeof(string))
							prop.SetValue(EventSpecialConditions.c, package [key].stringValue, null);
					}
					//Or set field
					else if (member.MemberType == MemberTypes.Field)
					{
						FieldInfo field = member as FieldInfo;
						if (field.FieldType == typeof(bool))
							field.SetValue(EventSpecialConditions.c, package [key].boolValue);
						else if (field.FieldType == typeof(int))
							field.SetValue(EventSpecialConditions.c, package [key].int32Value);
						else if (field.FieldType == typeof(float))
							field.SetValue(EventSpecialConditions.c, package [key].floatValue);
						else if (field.FieldType == typeof(string))
							field.SetValue(EventSpecialConditions.c, package [key].stringValue);

					}
				}
			}
			//TODO GameEventManager.gem.allEvents = availableEvents;

			//EventClockCheck
			var ecc = GameEventManager.gem.GetComponent<EventClockCheck>();
			ecc.minimumDay = dataToUnload ["events"] ["minimumDay"];
			ecc.minimumHour = dataToUnload ["events"] ["minimumHour"];
			ecc.minimumMinute = dataToUnload ["events"] ["minimumMinute"];

			//Create an event if none exists! (SAFETY FROM EARLY EXIT) (OBSOLETE)
//			if (dataToUnload ["events_count"] == 0 && !dataToUnload ["events"].ContainsKey("current"))
//				GameObject.FindObjectOfType<GameEventScheduler>().CreateEventByTime(dataToUnload ["day"], dataToUnload ["hour"], dataToUnload ["minute"]);
		}
	}

	/**Load in a specific custom ship from its save file.
	 * Need the objects tweaked somehow (i.e., customization screen prep)? Pass a processor that handles them. Otherwise, null is OK.
	 * ResourceHandler accepts the 6 (alpha order) resources, if ShipRes is null.
	 */
	public static void BuildShip(BSONObject dataToUnload, System.Action<GameObject> processor, System.Action<int[]> resourceHandler, bool spritesOnly)
	{
		//Destroy old stuff
		ClearScene();

		var items = new List<GameObject>();
			
		//Load in character datas
		for (int i = 0; i < dataToUnload ["characters_count"]; i++)
		{
			BSONObject dat = dataToUnload ["characters"] [i.ToString()] as BSONObject;
			//Create the new instance, add it to our list
			items.Add(CharacterData.Unload(dat, spritesOnly));
		}
			
		//Load in system datas
		for (int i = 0; i < dataToUnload ["systems_count"]; i++)
		{
			BSONObject dat = dataToUnload ["systems"] [i.ToString()] as BSONObject;
			//Create the new instance
			items.Add(ShipSystemData.Unload(dat, spritesOnly));
		}
			
		//Load in modules
		items.AddRange(UnloadShipStructure(dataToUnload));

		//Set and buy customizable objects
		if (processor != null)
		{
			//Do the processor action on our list
			foreach (var t in items)
			{
				processor.Invoke(t);
			}
		}

		//Finally, do res assignments after everything else is set
		if (ShipResources.res != null)
		{
			ShipResources.res.SetTotalAir(dataToUnload ["air"], updateStatTrack: false);
			ShipResources.res.SetUsableAir(dataToUnload ["air"], updateStatTrack: false);
			ShipResources.res.SetFood(dataToUnload ["food"], updateStatTrack: false);
			ShipResources.res.SetFuel(dataToUnload ["fuel"], updateStatTrack: false);
			ShipResources.res.SetMaterials(dataToUnload ["materials"], updateStatTrack: false);
			ShipResources.res.SetParts(dataToUnload ["parts"], updateStatTrack: false);
			ShipResources.res.SetWaste(dataToUnload ["waste"], updateStatTrack: false);
		}
		else if (resourceHandler != null)
		{
			resourceHandler.Invoke(new int[]
			{
				dataToUnload ["air"],
				dataToUnload ["food"],
				dataToUnload ["fuel"],
				dataToUnload ["materials"],
				dataToUnload ["parts"],
				dataToUnload ["waste"]
			});
		}
	}

	/**This is called when saving in some capacity.
	 */
	public static BSONObject ConvertStatTrackToBSON()
	{
		//Store Steam ish
		if (SteamStats.s != null)
			SteamStats.s.StoreStatsAndAchievements();

		//Reflect StatTrack values as new BSONValues
		BSONObject statObj = new BSONObject();	//StatTrack-holder BSONObject
		System.Type type = typeof(StatTrack);	//Type pointer

		//Iterate and create values
		foreach (PropertyInfo prop in type.GetProperties())
		{
			//Listed by property name, use the value from current stats tracker (StatTrack.stats)
			object obj = prop.GetValue(StatTrack.stats, null);

			//Find explicit cast (can't use generic cast...)
			if (obj is int)
				statObj [prop.Name] = (int)obj;
			else if (obj is bool)
				statObj [prop.Name] = (bool)obj;
			else if (prop.Name.ToLower() != "stats" && prop.Name.ToLower() != "memorialreversed" && prop.Name.ToLower() != "endgamestats")
				Debug.Log(prop.Name + " will not be saved, since it was not one of the types explicitly cast in this code.");
		}

		//StatTrack Memorial
		int count = 0;
		statObj ["memorial"] = new BSONObject();
		//Memorial consists of Vessels
		foreach (var t in StatTrack.stats.memorial)
		{
			//BSONObj
			statObj ["memorial"] [count.ToString()] = new BSONObject();
			//Vessel Name
			statObj ["memorial"] [count.ToString()] ["name"] = t.vesselName;
			//Survived?
			statObj ["memorial"] [count.ToString()] ["survived"] = t.survived;
			//How long?
			statObj ["memorial"] [count.ToString()] ["time"] = t.time;
			//Result?
			statObj ["memorial"] [count.ToString()] ["result"] = t.result;
			//Crew List
			statObj ["memorial"] [count.ToString()] ["crew"] = new BSONObject();
			int crewCount = 0;
			foreach (var u in t.crew)
			{
				//BSONObj
				statObj ["memorial"] [count.ToString()] ["crew"] [crewCount.ToString()] = new BSONObject();
				//Crew Name
				statObj ["memorial"] [count.ToString()] ["crew"] [crewCount.ToString()] ["name"] = u.crewName;
				//Survived?
				statObj ["memorial"] [count.ToString()] ["crew"] [crewCount.ToString()] ["survived"] = u.survived;
				//Result?
				statObj ["memorial"] [count.ToString()] ["crew"] [crewCount.ToString()] ["result"] = u.result;
				//Next!
				crewCount++;
			}
			statObj ["memorial"] [count.ToString()] ["crew_count"] = crewCount;
			//Next!
			count++;
		}
		statObj ["memorial_count"] = count;

		//Current lost crew
		statObj ["lostCrew"] = new BSONObject();
		count = 0;
		//Consists of Crew
		foreach (var t in StatTrack.stats.lostCrew)
		{
			//BSONObj
			statObj ["lostCrew"] [count.ToString()] = new BSONObject();
			//Crew Name
			statObj ["lostCrew"] [count.ToString()] ["name"] = t.crewName;
			//Survived?
			statObj ["lostCrew"] [count.ToString()] ["survived"] = t.survived;
			//Result?
			statObj ["lostCrew"] [count.ToString()] ["result"] = t.result;
			//Next!
			count++;
		}
		statObj ["lostCrew_count"] = count;

		//Return our BSONObject version of StatTrack's data
		return statObj;
	}


	public static BSONObject SaveMetagame()
	{
		BSONObject statObj = new BSONObject();
	
		//Unlock points
		statObj ["totalPoints"] = MetaGameManager.totalUnlockPoints;
		statObj ["currentPoints"] = MetaGameManager.currentUnlockPoints;

		int count = 0;

		//Achievements. We only care to save the ident int of unlocked ones
		statObj ["achievements"] = new BSONObject();
		foreach (var ach in AchievementTracker.unlockedAchievements)
		{
			//Skip nulls (we'll account for this in loading)
			if (ach != null)
			{
				//BSONObj
				statObj ["achievements"] [count.ToString()] = ach.achievementID;
				//Next!
				count++;
			}
		}
		statObj ["achievements_count"] = count;

		//Unlockables
		statObj ["unlockables"] = new BSONObject();
		count = 0;
		foreach (var unlock in MetaGameManager.unlockables)
		{
			if (unlock != null)
			{
				statObj ["unlockables"] [count.ToString()] = unlock.name;
				count++;
			}
		}
		statObj ["unlockables_count"] = count;

		//Keys
		statObj ["keys"] = new BSONObject();
		count = 0;
		foreach (var key in MetaGameManager.keys)
		{
			if (key != null)
			{
				statObj ["keys"] [count.ToString()] = key.name;
				count++;
			}
		}
		statObj ["keys_count"] = count;

		return statObj;
	}

	public static void UnloadStatTrack(BSONObject dataToUnload)
	{
		if (dataToUnload == null)
		{
			Debug.Log("No data! Can't update StatTrack.");
			return;
		}

		//Reflect StatTrack, fill property values with BSONValues by referencing (shared) property names
		System.Type type = typeof(StatTrack);	//Type pointer
		//Iterate and fill values
		foreach (PropertyInfo prop in type.GetProperties())
		{
			//Compare properties from StatTrack to see if they're in current data AND have a set function
			if (dataToUnload ["StatTrack"].ContainsKey(prop.Name) && prop.CanWrite)
			{
				//If so, input the BSONValue back into StatTrack.stats (using the property name as the pivot)
				//By type
				if (prop.PropertyType == typeof(int))
					prop.SetValue(StatTrack.stats, dataToUnload ["StatTrack"] [prop.Name].int32Value, null);
				else if (prop.PropertyType == typeof(bool))
					prop.SetValue(StatTrack.stats, dataToUnload ["StatTrack"] [prop.Name].boolValue, null);
			}
		}

		//StatTrack Memorial List
		StatTrack.stats.memorial = new List<StatTrack.Vessel>();
		for (int i = 0; i < dataToUnload ["StatTrack"] ["memorial_count"]; i++)
		{
			//New Vessel on the list!
			StatTrack.Vessel vessel;
			//Vessel data
			vessel.vesselName = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["name"];
			vessel.survived = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["survived"];
			vessel.time = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["time"];
			vessel.result = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["result"];
			vessel.crew = new List<StatTrack.Crew>();
			//Crew List
			for (int j = 0; j < dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["crew_count"]; j++)
			{
				//New crew on the list!
				StatTrack.Crew crew;
				//Crew data
				crew.crewName = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["crew"] [j.ToString()] ["name"];
				crew.survived = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["crew"] [j.ToString()] ["survived"];
				crew.result = dataToUnload ["StatTrack"] ["memorial"] [i.ToString()] ["crew"] [j.ToString()] ["result"];
				//Add this crew to the vessel's list!
				vessel.crew.Add(crew);
			}
			//Finally, put this in our memorial!
			StatTrack.stats.memorial.Add(vessel);
		}

		//StatTrack current lostCrew
		StatTrack.stats.lostCrew = new List<StatTrack.Crew>();
		if (dataToUnload ["StatTrack"].ContainsKey("lostCrew"))
		{
			for (int i = 0; i < dataToUnload ["StatTrack"] ["lostCrew_count"]; i++)
			{
				//New crew on the list!
				StatTrack.Crew crew;
				//Crew data
				crew.crewName = dataToUnload ["StatTrack"] ["lostCrew"] [i.ToString()] ["name"];
				crew.survived = dataToUnload ["StatTrack"] ["lostCrew"] [i.ToString()] ["survived"];
				crew.result = dataToUnload ["StatTrack"] ["lostCrew"] [i.ToString()] ["result"];
				//Add to lostCrew
				StatTrack.stats.lostCrew.Add(crew);
			}
		}

		//Piggyback: Achievements
		UnloadMetagame(dataToUnload);
	}

	public static void UnloadMetagame(BSONObject dataToUnload)
	{
		if (dataToUnload.ContainsKey("meta"))
		{
			//Unlock points, skip the properties and go straight to the real values (so we don't double add to total when changing current)
			MetaGameManager._tot = dataToUnload ["meta"] ["totalPoints"];
			MetaGameManager._cur = dataToUnload ["meta"] ["currentPoints"];

			//Achievements
			for (int i = 0; i < dataToUnload ["meta"] ["achievements_count"]; i++)
			{
				AchievementTracker.UnlockAchievement(dataToUnload ["meta"] ["achievements"] [i.ToString()], save: false);
			}

			//Unlockables
			for (int i = 0; i < dataToUnload ["meta"] ["unlockables_count"]; i++)
			{
				if (!MetaGameManager.unlockables.Exists(obj => obj.name == dataToUnload ["meta"] ["unlockables"] [i.ToString()]))
				{
					Unlockable unlock = Resources.Load<Unlockable>("Unlockables/" + dataToUnload ["meta"] ["unlockables"] [i.ToString()]);
					unlock.Unleash();
				}
			}

			//Keys
			for (int i = 0; i < dataToUnload ["meta"] ["keys_count"]; i++)
			{
				if (!MetaGameManager.keys.Exists(obj => obj.name == dataToUnload ["meta"] ["keys"] [i.ToString()]))
				{
					MetaGameManager.AddKey(dataToUnload ["meta"] ["keys"] [i.ToString()]);
				}
			}
		}
	}

	/**Loads the ship's structure. Note, also loads the doors, pins, and hull extensions!
	 */
	static List<GameObject> UnloadShipStructure(BSONObject dataToUnload)
	{

		if (dataToUnload == null)
		{
			Debug.Log("No data! Can't load ship structure.");
			return null;
		}

		//Get the Rooms obj
		GameObject rooms = GameObject.Find("Rooms");

		var items = new List<GameObject>();

		//Debug.Log(dataToUnload["modules_count"] + " modules to be loaded.");
		for (int i = 0; i < dataToUnload ["modules_count"]; i++)
		{
			//Get the size
			Module.Size size = (Module.Size)(int)dataToUnload ["modules"] [i.ToString()] ["size"];
			//Instantiate module based on size
			GameObject go = null;
			switch (size)
			{
			case Module.Size.Large:
					//Create
				go = GameObject.Instantiate<GameObject>(Resources.Load("Module_Lar") as GameObject);
				break;
			case Module.Size.Medium:
					//Create
				go = GameObject.Instantiate<GameObject>(Resources.Load("Module_Med") as GameObject);
				break;
			case Module.Size.Small:
					//Create
				go = GameObject.Instantiate<GameObject>(Resources.Load("Module_Sma") as GameObject);
				break;
			default :
					//UNDEFINED. Just break, don't need it.
				break;
			}
			
			//Fill data and art, if we've instantiated
			if (go != null)
			{
				//Position and rotation
				go.transform.position = new Vector3(dataToUnload ["modules"] [i.ToString()] ["x"], dataToUnload ["modules"] [i.ToString()] ["y"], dataToUnload ["modules"] [i.ToString()] ["z"]);
				go.transform.eulerAngles = new Vector3(go.transform.eulerAngles.x, go.transform.eulerAngles.y, dataToUnload ["modules"] [i.ToString()] ["rot"]);
				go.transform.SetParent(rooms.transform, true);
				
				//Module Art
				//First, get the array of module art components, which we'll iterate through while we iterate through the saved data
				ModuleArt[] art = go.GetComponentsInChildren<ModuleArt>();
				for (int j = 0; j < dataToUnload ["modules"] [i.ToString()] ["module_art_count"]; j++)
				{
					//Debug.Log("ModuleArts on " + go.name + ": " + art.Length + ". Trying to fill with " + (j + 1) + "/" + dataToUnload["modules"][i.ToString()]["module_art_count"] + " ModuleArt.");
					//Fill data
					art [j].spriteSignature = dataToUnload ["modules"] [i.ToString()] ["module_art"] [j.ToString()];
					art [j].lockToPredefined = true;
					//Assign sprite
					art [j].ChooseArt();
				}

				//Add it to items
				items.Add(go);
			}
			//Done. Iterate on to the next module.
		}

		//Get the Doors and Pins obj
		GameObject openings = GameObject.Find("Openings");
		
		for (int i = 0; i < dataToUnload ["openings_count"]; i++)
		{
			//Get the type
			Opening.Type type = (Opening.Type)(int)dataToUnload ["openings"] [i.ToString()] ["type"];
			//Instantiate module based on size
			GameObject go = null;
			switch (type)
			{
			case Opening.Type.Door:
					//Create
				go = GameObject.Instantiate<GameObject>(Resources.Load("Door") as GameObject);
				break;
			case Opening.Type.Pin:
					//Create
				go = GameObject.Instantiate<GameObject>(Resources.Load("Pin") as GameObject);
				break;
			}
			
			//Fill data, if we've instantiated
			if (go != null)
			{
				//Position and rotation
				go.transform.position = new Vector3(dataToUnload ["openings"] [i.ToString()] ["x"], dataToUnload ["openings"] [i.ToString()] ["y"], dataToUnload ["openings"] [i.ToString()] ["z"]);
				go.transform.eulerAngles = new Vector3(go.transform.eulerAngles.x, go.transform.eulerAngles.y, dataToUnload ["openings"] [i.ToString()] ["rot"]);
				if (openings != null)
					go.transform.SetParent(openings.transform, true);
				go.GetComponent<Placement>().isPlaced = true;

				//Add it to items
				items.Add(go);
			}
			//Done. Iterate on to the next opening.
		}

		//Time for hull extensions
		GameObject hullExtensions = GameObject.Find("HullExtensions");

		for (int i = 0; i < dataToUnload ["hullExtensions_count"]; i++)
		{
			var go = GameObject.Instantiate<GameObject>(Resources.Load("HullExtension") as GameObject);

			//Fill data, if we've instantiated
			if (go != null)
			{
				//Position and rotation
				go.transform.position = new Vector3(dataToUnload ["hullExtensions"] [i.ToString()] ["x"], dataToUnload ["hullExtensions"] [i.ToString()] ["y"], dataToUnload ["hullExtensions"] [i.ToString()] ["z"]);
				go.transform.eulerAngles = new Vector3(go.transform.eulerAngles.x, go.transform.eulerAngles.y, dataToUnload ["hullExtensions"] [i.ToString()] ["rot"]);
				if (hullExtensions != null)
					go.transform.SetParent(hullExtensions.transform, true);

				//Data
				var he = go.GetComponent<HullExtension>();
				he.artIndex = dataToUnload ["hullExtensions"] [i.ToString()] ["artIndex"];
				he.SetArt();

				//Add it to items
				items.Add(go);
			}
			//Done. Iterate on to the next hull extension.
		}

		return items;
	}

	public static void ClearScene()
	{
		if (GameReference.r != null)
		{
			//Destroy all chars in scene
			int oldCharCount = GameReference.r.allCharacters.Count;
			for (int i = 0; i < oldCharCount; i++)
			{
				var t = GameReference.r.allCharacters [0];
				GameReference.r.allCharacters.Remove(t);
				Object.Destroy(t.gameObject);
			}
			
			//Destroy all systems in scene
			int oldSysCount = GameReference.r.allSystems.Count;
			for (int i = 0; i < oldSysCount; i++)
			{
				var t = GameReference.r.allSystems [0];
				GameReference.r.allSystems.Remove(t);
				Object.Destroy(t.gameObject);
			}
			
			//Destroy all modules in scene
			int oldModCount = GameReference.r.allModules.Count;
			for (int i = 0; i < oldModCount; i++)
			{
				var t = GameReference.r.allModules [0];
				GameReference.r.allModules.Remove(t);
				Object.Destroy(t.gameObject);
			}
			
			//Destroy all openings in scene
			int oldOpeCount = GameReference.r.allOpenings.Count;
			for (int i = 0; i < oldOpeCount; i++)
			{
				var t = GameReference.r.allOpenings [0];
				GameReference.r.allOpenings.Remove(t);
				Object.Destroy(t.gameObject);
			}
			
			//Destroy all hull extensions in scene
			int oldHullCount = GameReference.r.allHullExtensions.Count;
			for (int i = 0; i < oldHullCount; i++)
			{
				var t = GameReference.r.allHullExtensions [0];
				GameReference.r.allHullExtensions.Remove(t);
				Object.Destroy(t.gameObject);
			}
		}
	}
}

static class CharacterData
{
	//VALUES
	//Transform, names, roles, skills, current stats, etc.
	//	public float x, y, z, rot;
	//	public string title, firstName, lastName;
	//	public Character.CharStatus status;
	//	public List<Character.CharSkill> skills = new List<Character.CharSkill>();
	//	public List<Character.CharRole> roles = new List<Character.CharRole>();
	//	public Character.Team team;
	//	public float stressCounter, sleepiness, hunger, waste;
	//	public int stressResilience, baseNeedsResilience;
	//	public bool injured;

	public static BSONObject NewData(Character ch)
	{
		BSONObject obj = new BSONObject();

		//Set the data points
		obj ["x"] = ch.transform.position.x;
		obj ["y"] = ch.transform.position.y;
		obj ["z"] = ch.transform.position.z;
		obj ["rot"] = ch.GetComponentInChildren<Rotation>().transform.eulerAngles.z;
		obj ["title"] = ch.title;
		obj ["firstName"] = ch.firstName;
		obj ["lastName"] = ch.lastName;
		obj ["status"] = (int)ch.status;	//Gotta cast enums to generic
		obj ["team"] = (int)ch.team;
		obj ["stressCounter"] = ch.stressCounter;
		obj ["stressResilience"] = ch.baseStressResilience;
		obj ["baseNeedsResilience"] = ch.baseNeedsResilience;
		obj ["sleepiness"] = ch.sleepiness;
		obj ["hunger"] = ch.hunger;
		obj ["waste"] = ch.waste;
		obj ["injured"] = ch.injured;
		obj ["result"] = ch.result;
		obj ["personality"] = (int)ch.GetComponent<CharacterSpeech>().personality;
		obj ["isRandomCrew"] = ch.isRandomCrew;

		//Alerts
		obj ["alerts"] = "";
		Alert alert = ch.GetComponentInChildren<Alert>();
		if (alert != null)
		{
			foreach (var t in alert.GetActivatedAlerts())
			{
				obj ["alerts"] += t.ToString() + " ";
			}
		}

		//Some Anims for the SpritesOnly load
		obj ["isMoving"] = ch.sPath.isMoving;
		obj ["isEating"] = ch.bHand.isEating;
		obj ["isExercising"] = ch.bHand.isExercising;
		obj ["isSleeping"] = ch.bHand.isSleeping;
		obj ["isWorking"] = ch.bHand.isWorking;

		//Convert lists (and cast enums to generic)
		int count = 0;
		obj ["skills"] = new BSONObject();
		foreach (Character.CharSkill sk in ch.skills)
		{
			obj ["skills"] [count.ToString()] = (int)sk;
			count++;
		}
		obj ["skills_count"] = count;

		//Reset count for next list
		count = 0;
		obj ["roles"] = new BSONObject();
		foreach (Character.CharRoles ro in ch.roles)
		{
			obj ["roles"] [count.ToString()] = (int)ro;
			count++;
		}
		obj ["roles_count"] = count;

		//Colors
		var colorScript = ch.GetComponent<CharacterColors>();
		obj ["eyeColor"] = new BSONObject();
		obj ["eyeColor"] ["a"] = colorScript.eyeColor.a;
		obj ["eyeColor"] ["r"] = colorScript.eyeColor.r;
		obj ["eyeColor"] ["g"] = colorScript.eyeColor.g;
		obj ["eyeColor"] ["b"] = colorScript.eyeColor.b;

		obj ["hairColor"] = new BSONObject();
		obj ["hairColor"] ["a"] = colorScript.hairColor.a;
		obj ["hairColor"] ["r"] = colorScript.hairColor.r;
		obj ["hairColor"] ["g"] = colorScript.hairColor.g;
		obj ["hairColor"] ["b"] = colorScript.hairColor.b;

		obj ["skinColor"] = new BSONObject();
		obj ["skinColor"] ["a"] = colorScript.skinColor.a;
		obj ["skinColor"] ["r"] = colorScript.skinColor.r;
		obj ["skinColor"] ["g"] = colorScript.skinColor.g;
		obj ["skinColor"] ["b"] = colorScript.skinColor.b;

		return obj;
	}

	public static GameObject Unload(BSONObject obj, bool spritesOnly = false)
	{
		//Load and create the prefab character
		GameObject charObj = null;

		if (spritesOnly)
		{
			charObj = GameObject.Instantiate<GameObject>(Resources.Load("CharSpriteOnly") as GameObject);

			//Set Animation values for the CharSprites
			CharacterAnim anim = charObj.GetComponent<CharacterAnim>();
			anim.isDead = (Character.CharStatus)(int)obj ["status"] == Character.CharStatus.Dead;
			anim.isPsychotic = (Character.CharStatus)(int)obj ["status"] == Character.CharStatus.Psychotic;
			anim.isUnconscious = (Character.CharStatus)(int)obj ["status"] == Character.CharStatus.Unconscious || obj ["isSleeping"];
			anim.isInjured = obj ["injured"];
			anim.isEating = obj ["isEating"];
			anim.isExercising = obj ["isExercising"];
			anim.isMoving = obj ["isMoving"];
			anim.isWorking = obj ["isWorking"];
		}
		else
		{
			charObj = GameObject.Instantiate<GameObject>(Resources.Load("Char") as GameObject);

			//Find the Character component
			Character ch = charObj.GetComponent<Character>();

			//Fill the data points
			ch.title = obj ["title"];
			ch.firstName = obj ["firstName"];
			ch.lastName = obj ["lastName"];
			ch.status = (Character.CharStatus)(int)obj ["status"];	//Don't forget to cast back to enum!
			ch.team = (Character.Team)(int)obj ["team"];
			ch.stressCounter = obj ["stressCounter"];
			ch.sleepiness = obj ["sleepiness"];
			ch.hunger = obj ["hunger"];
			ch.waste = obj ["waste"];
			ch.baseStressResilience = obj ["stressResilience"];
			ch.baseNeedsResilience = obj ["baseNeedsResilience"];
			ch.injured = obj ["injured"];
			ch.result = obj ["result"];
			ch.GetComponent<CharacterSpeech>().personality = (CharacterSpeech.Personality)(int)obj ["personality"];
			ch.isRandomCrew = obj ["isRandomCrew"];

			//Alerts
			if (obj.ContainsKey("alerts") && !obj ["alerts"].stringValue.Equals(""))
				ch.StartCoroutine(SaveLoad.s.RestoreAlerts(obj ["alerts"], ch.transform));

			//Restore lists, and cast back to enum
			for (int i = 0; i < obj ["skills_count"]; i++)
			{
				ch.skills.Add((Character.CharSkill)(int)obj ["skills"] [i.ToString()]);
			}
			for (int i = 0; i < obj ["roles_count"]; i++)
			{
				ch.roles.Add((Character.CharRoles)(int)obj ["roles"] [i.ToString()]);
			}

			ch.Rename();

			//Attach alert!
			if (UIGenerator.gen != null)
				UIGenerator.gen.AttachFollowCanvas(charObj);
		}

		//Child to "Characters"
		charObj.transform.parent = GameObject.Find("Characters").transform;
		charObj.transform.position = new Vector3(obj ["x"], obj ["y"], obj ["z"]);
		Rotation spriteRotation = charObj.GetComponentInChildren<Rotation>();
		spriteRotation.transform.eulerAngles = new Vector3(spriteRotation.transform.eulerAngles.x, spriteRotation.transform.eulerAngles.y, obj ["rot"]);

		//Set Colors
		var colorScript = charObj.GetComponent<CharacterColors>();
		colorScript.eyeColor = new Color(obj ["eyeColor"] ["r"], obj ["eyeColor"] ["g"], obj ["eyeColor"] ["b"], obj ["eyeColor"] ["a"]);
		colorScript.hairColor = new Color(obj ["hairColor"] ["r"], obj ["hairColor"] ["g"], obj ["hairColor"] ["b"], obj ["hairColor"] ["a"]);
		colorScript.skinColor = new Color(obj ["skinColor"] ["r"], obj ["skinColor"] ["g"], obj ["skinColor"] ["b"], obj ["skinColor"] ["a"]);
		colorScript.team = (Character.Team)(int)obj ["team"];
		colorScript.UpdateColors();

		//Created!
		return charObj;
	}
	
}

static class ShipSystemData
{
	//VALUES
	//Transform, name, attributes, status, etc.
	//	public float x, y, z, rot;
	//	public string sysName;
	//	public ShipSystem.SysStatus status;
	//	public ShipSystem.SysCondition condition;
	//	public ShipSystem.SysFunction function;
	//	public ShipSystem.SysQuality quality;
	//	public bool conditionHit, overdriven;
	//	public float boxCollSize;
	
	public static BSONObject NewData(ShipSystem sys)
	{
		BSONObject obj = new BSONObject();

		//Set the data points
		obj ["x"] = sys.transform.position.x;
		obj ["y"] = sys.transform.position.y;
		obj ["z"] = sys.transform.position.z;
		obj ["rot"] = sys.transform.eulerAngles.z;
		obj ["sysName"] = sys.sysName;
		obj ["status"] = (int)sys.status;
		obj ["condition"] = (int)sys.condition;
		obj ["function"] = (int)sys.function;
		obj ["quality"] = (int)sys.quality;
		obj ["conditionHit"] = sys.conditionHit;
		obj ["overdriven"] = sys.overdriven;
		obj ["timeLeftInTick"] = sys.timeLeftInTick;
		obj ["isPassive"] = sys.isPassive;

		//Alerts
		obj ["alerts"] = "";
		var alert = sys.GetComponentInChildren<Alert>();
		if (alert != null)
		{
			foreach (var t in alert.GetActivatedAlerts())
				obj ["alerts"] += t.ToString() + " ";
		}

		//Some anims for the SpritesOnly load
		obj ["inUse"] = sys.inUse;
		obj ["isAutomated"] = sys.isAutomated;

		//Convert array (and cast enums to generic)
		int count = 0;
		obj ["keywords"] = new BSONObject();
		foreach (ShipSystem.SysKeyword s in sys.keywords)
		{
			obj ["keywords"] [count.ToString()] = (int)s;
			count++;
		}
		obj ["keywords_count"] = count;

		return obj;
	}

	public static GameObject Unload(BSONObject obj, bool spritesOnly)
	{
		//Load and create the prefab system
		GameObject sysObj = null;

		if (spritesOnly)
		{
			sysObj = GameObject.Instantiate<GameObject>(Resources.Load("ShipSystemSpriteOnly") as GameObject);

			//Set anim values for shipsys sprite
			ShipSystemAnim anim = sysObj.GetComponent<ShipSystemAnim>();
			anim.SetSystemFunction((ShipSystem.SysFunction)(int)obj ["function"], obj ["isAutomated"]);

			anim.inUse = obj ["inUse"];
			anim.isBroken = (ShipSystem.SysCondition)(int)obj ["condition"] == ShipSystem.SysCondition.Broken;
			anim.isDestroyed = (ShipSystem.SysCondition)(int)obj ["condition"] == ShipSystem.SysCondition.Destroyed;
			anim.isStrained = (ShipSystem.SysCondition)(int)obj ["condition"] == ShipSystem.SysCondition.Strained;
			anim.isFunctional = (ShipSystem.SysCondition)(int)obj ["condition"] == ShipSystem.SysCondition.Functional;
			anim.isUnderConstruction = (ShipSystem.SysQuality)(int)obj ["quality"] == ShipSystem.SysQuality.UnderConstruction;
			anim.isOverdriven = obj ["overdriven"];
			anim.isActive = (ShipSystem.SysStatus)(int)obj ["status"] == ShipSystem.SysStatus.Active;
			anim.isInactive = (ShipSystem.SysStatus)(int)obj ["status"] == ShipSystem.SysStatus.Inactive;
			anim.isIntermittent = (ShipSystem.SysStatus)(int)obj ["status"] == ShipSystem.SysStatus.Intermittent;
			anim.isPassive = obj ["isPassive"];
		}
		else
		{
			sysObj = GameObject.Instantiate<GameObject>(Resources.Load("ShipSystem") as GameObject);
		
			//Find the ShipSystem component
			ShipSystem sys = sysObj.GetComponent<ShipSystem>();
		
			//Fill the data points
			sys.name = obj ["sysName"];
			sys.condition = (ShipSystem.SysCondition)(int)obj ["condition"];
			sys.function = (ShipSystem.SysFunction)(int)obj ["function"];
			sys.quality = (ShipSystem.SysQuality)(int)obj ["quality"];
			if (obj ["conditionHit"].valueType == BSONValue.ValueType.Int32)
				sys.conditionHit = obj ["conditionHit"];
			if (obj ["overdriven"])
			{
				//This will tip everything back
				sys.OverdriveOn();
			}
			//Status after overdrive, so we don't accidentally turn it on
			sys.status = (ShipSystem.SysStatus)(int)obj ["status"];

			//Ticking (this method will ignore anything that isn't automatic or enabled already)
			if (ShipResources.res != null)
				sys.StartTick(obj ["timeLeftInTick"]);

			//Alerts
			if (obj.ContainsKey("alerts") && !obj ["alerts"].stringValue.Equals(""))
				sys.StartCoroutine(SaveLoad.s.RestoreAlerts(obj ["alerts"], sys.transform));

			//Restore keyword array, and cast back to enum
			for (int i = 0; i < obj ["keywords_count"]; i++)
			{
				sys.keywords.Add((ShipSystem.SysKeyword)(int)obj ["keywords"] [i.ToString()]);
			}

			sys.Rename();

			//Attach alert!
			if (UIGenerator.gen != null)
				UIGenerator.gen.AttachFollowCanvas(sysObj);
		}

		//Child to "Systems"
		sysObj.transform.parent = GameObject.Find("ShipSystems").transform;
		sysObj.transform.position = new Vector3(obj ["x"], obj ["y"], obj ["z"]);
		sysObj.transform.eulerAngles = new Vector3(sysObj.transform.eulerAngles.x, sysObj.transform.eulerAngles.y, (float)obj ["rot"]);

		//Created!
		return sysObj;
	}
}