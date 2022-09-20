using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Customization_CurrencyController : MonoBehaviour
{
	public static Customization_CurrencyController c;


	[SerializeField] private int baseStartingCurrency;

	int _currency = 0, _resValue = 0;

	public int resValue
	{
		get
		{
			return _resValue;
		}
		set
		{
			if (currencyText != null)
				currencyText.text =	(_currency - (_resValue = value)).ToString();
		}
	}

	public int preResCurrency
	{
		get
		{
			return _currency;
		}
		set
		{
			if (currencyText != null)
				currencyText.text =	((_currency = value) - _resValue).ToString();
		}
	}

	public int effectiveCurrency
	{
		get
		{
			return preResCurrency - resValue;
		}
	}

	public Text currencyText;



	public static Dictionary<int, int> ModuleValueDictionary = new Dictionary<int, int>
	{
		{ (int)Module.Size.Small, 10 },
		{ (int)Module.Size.Medium, 20 },
		{ (int)Module.Size.Large, 30 }
	};

	public static Dictionary<int, float> SysFunctionMultiplierDictionary = new Dictionary<int, float>
	{
		{ (int)ShipSystem.SysFunction.Electrolyser, 2 },
		{ (int)ShipSystem.SysFunction.Engine, 2 },
		{ (int)ShipSystem.SysFunction.Processor, 2 },
		{ (int)ShipSystem.SysFunction.Reactor, 2 },
		{ (int)ShipSystem.SysFunction.WasteCannon, 2 },
		{ (int)ShipSystem.SysFunction.Storage, 0.4f }
	};

	public static Dictionary<int, int> SysQualityValueDictionary = new Dictionary<int, int>
	{
		{ (int)ShipSystem.SysQuality.Exceptional, 25 },
		{ (int)ShipSystem.SysQuality.Average, 15 },
		{ (int)ShipSystem.SysQuality.Shoddy, 10 },
		{ (int)ShipSystem.SysQuality.Makeshift, 5 },
		{ (int)ShipSystem.SysQuality.UnderConstruction, 5 }
	};

	public static Dictionary<int, int> SysKeywordValueDictionary = new Dictionary<int, int>
	{
		{ (int)ShipSystem.SysKeyword.NOTDEFINED, 0 },
		{ (int)ShipSystem.SysKeyword.Random, 5 }
	};

	//Shitty roles will be -10, reg are 10. Shitty roles are defined in Character.shittyRoles
	public static Dictionary<int, int> CharRolesValueDictionary = new Dictionary<int, int>{	};



	/**Purchase given system. Returns success */
	public bool Buy(ShipSystem sys)
	{
		return CostCheck(GetAssetsCost(sys));
	}

	/**Purchase given module. Returns success */
	public bool Buy(Module module)
	{
		return CostCheck(GetAssetsCost(module));
	}

	/**Purchase given crew. Returns success */
	public bool Buy(Character ch)
	{
		return CostCheck(GetAssetsCost(ch));
	}

	public bool Rebate(ShipSystem sys)
	{
		return CostCheck(-GetAssetsCost(sys));
	}

	public bool Rebate(Module module)
	{
		return CostCheck(-GetAssetsCost(module));
	}

	public bool Rebate(Character ch)
	{
		return CostCheck(-GetAssetsCost(ch));
	}

	/**Get cost of a given system */
	public static int GetAssetsCost(ShipSystem sys)
	{
		return GetAssetsCost(sys.function, sys.quality, sys.keywords);
	}

	/**Get cost of a given system, as it would cost w/o tampering (and without know anything else about it) */
	public static int GetAssetsCostOfBase(ShipSystem.SysFunction func)
	{
		//Random starter on non-storage
		List<ShipSystem.SysKeyword> keys = new List<ShipSystem.SysKeyword>();
		if (func != ShipSystem.SysFunction.Storage && PlayerPrefs.GetInt("RandomKeywords") == 1)
		{
			keys.Add(ShipSystem.SysKeyword.Random);
		}

		return GetAssetsCost(func, ShipSystem.SysQuality.Average, keys);
	}

	/**Get cost of a given system, based on it's component parts */
	public static int GetAssetsCost(ShipSystem.SysFunction function, ShipSystem.SysQuality quality, List<ShipSystem.SysKeyword> keywords)
	{
		//Cost of quality
		var val = SysQualityValueDictionary [(int)quality];

		//Plus keywords
		foreach (var t in keywords)
		{
			val += SysKeywordValueDictionary [(int)t];
		}

		//Function multiplier
		val = (int)(val * SysFunctionMultiplierDictionary [(int)function]);

		return (int)val;
	}

	public static int GetMaterialsCost(ShipSystem sys) => GetMaterialsCost(sys.function, sys.quality, sys.keywords);

	public static int GetMaterialsCost(ShipSystem.SysFunction function, ShipSystem.SysQuality quality, List<ShipSystem.SysKeyword> keywords)
	{
		int assetsValue = GetAssetsCost(function, quality, keywords);

		return assetsValue;
	}

	public static int GetPartsCost(ShipSystem sys) => GetPartsCost(sys.function, sys.quality, sys.keywords);
	
	public static int GetPartsCost(ShipSystem.SysFunction function, ShipSystem.SysQuality quality, List<ShipSystem.SysKeyword> keywords)
	{
		int assetsValue = GetAssetsCost(function, quality, keywords);

		return assetsValue / 2;
	}
	
	/**Get cost of a given module */
	public static int GetAssetsCost(Module module)
	{
		return ModuleValueDictionary [(int)module.size];
	}

	/**Get cost of a given crew */
	public static int GetAssetsCost(Character ch)
	{
		return GetAssetsCost(ch.isRandomCrew, ch.roles, ch.skills);
	}

	public static int GetAssetsCost(bool isRandomCrew, List<Character.CharRoles> roles, List<Character.CharSkill> skills)
	{
		//Base value of a human life
		var val = 10;

		if (isRandomCrew)
			val -= 5;

		//Roles
		foreach (var t in roles)
		{
			val += CharRolesValueDictionary [(int)t];
		}

		//Skills
		val += skills.Count * 10;

		return val;
	}

	public void ResetCurrency()
	{
		int advancementBonus = 0;

		foreach (var key in MetaGameManager.keys)
		{
			switch (key.name)
			{
			case "Finance":
			case "Fuels":
			case "Elegance":
				advancementBonus += 100;
				break;
			}
		}

		preResCurrency = baseStartingCurrency + advancementBonus;
	}

	/**Just the basic "can I afford this" logic */
	bool CostCheck(int cost)
	{
		if (cost <= preResCurrency)
		{
			preResCurrency -= cost;
			return true;
		}
		else
			return false;
	}

	/**Get cost of a given array of the six basic resources*/
	public static int GetResCost(float air, float food, float fuel, float materials, float parts, float waste)
	{
		float value = 0;
		value += materials * 0.25f;
		value += air * 0.5f;
		value += food * 0.5f;
		value += fuel * 0.5f;
		value += parts * 1.0f;
		value += waste * 0.125f;

		return Mathf.RoundToInt(value);
	}

	void Awake()
	{
		if (c == null)
		{
			c = this;
		}
		else if (c != this)
		{
			Destroy(this);
		}

		//Fill Function Values dictionary
		foreach (ShipSystem.SysFunction t in System.Enum.GetValues(typeof(ShipSystem.SysFunction)))
		{
			if (!SysFunctionMultiplierDictionary.ContainsKey((int)t))
				SysFunctionMultiplierDictionary.Add((int)t, 1);
		}

		//Fill Keyword Values dictionary
		foreach (ShipSystem.SysKeyword t in System.Enum.GetValues(typeof(ShipSystem.SysKeyword)))
		{
			if (!SysKeywordValueDictionary.ContainsKey((int)t))
				SysKeywordValueDictionary.Add((int)t, 10);
		}

		//Fill Roles Values dictionary
		foreach (Character.CharRoles t in System.Enum.GetValues(typeof(Character.CharRoles)))
		{
			if (!CharRolesValueDictionary.ContainsKey((int)t))
			{
				if (Character.shittyRoles.Contains(t))
					CharRolesValueDictionary.Add((int)t, -10);
				else
					CharRolesValueDictionary.Add((int)t, 10);
			}
		}

		ResetCurrency();
	}
}
