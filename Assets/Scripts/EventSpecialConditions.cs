using UnityEngine;
using System.Collections;
using System.Reflection;

public class EventSpecialConditions
{
	private static EventSpecialConditions _c;

	public static EventSpecialConditions c
	{
		get
		{
			if (_c == null)
			{
				_c = new EventSpecialConditions();
			}
			return _c;
		}
		set
		{
			_c = value;
		}
	}

	//Refugee Ship Conditions
	public bool refugee_scientist, refugee_doctor, refugee_engineer, refugee_retrofits, refugee_contactByAI, refugee_sabotaged, refugee_spy;
	public string refugee_specialCrew;

	//A Dark Truth
	public bool dark_truthAvailable, dark_truthRealized, dark_rogueMutiny;

	/**Hard reset all the fields in this class. Useful when starting a new run!
	 * bools = false, ints and float = 0, strings = "".
	 */
	public void ResetFields()
	{
		foreach (var t in typeof(EventSpecialConditions).GetFields())
		{
			System.Type type = t.FieldType;
			if (type == typeof(bool))
				t.SetValue(this, false);
			if (type == typeof(int) || type == typeof(float))
				t.SetValue(this, 0);
			if (type == typeof(string))
				t.SetValue(this, "");
		}
	}
}
