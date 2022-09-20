using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Customization_SelectionMenu : MonoBehaviour
{

	public List<GameObject> flightSys, energySys, propulsionSys, crewSys, producersSys, recyclersSys, miscSys;

	private List<GameObject> everything = new List<GameObject>();

	public void SelectList(string list)
	{
		//Just a wee bit o' self reflection
		List<GameObject> actualList = (List<GameObject>)typeof(Customization_SelectionMenu).GetField(list).GetValue(this);

		//Turn off everything we don't want
		foreach (var t in everything.Where(obj => !actualList.Contains(obj)))
		{
			if (t != null)
				t.SetActive(false);
		}

		//Turn on everything we do want!
		foreach (var t in actualList)
		{
			if (t != null)
				t.SetActive(true);
		}
	}

	void Awake()
	{
		//All our lists into everything. In Awake because Start doesn't do these changes in time on the first time this opens.
		everything.AddRange(flightSys);
		everything.AddRange(energySys);
		everything.AddRange(propulsionSys);
		everything.AddRange(crewSys);
		everything.AddRange(producersSys);
		everything.AddRange(recyclersSys);
		everything.AddRange(miscSys);
	}
}
