using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CharacterNames
{
	//Non sci-fi / sci-fantasy refs commented

	public static List<string> firstNames = new List<string>()
	{
		"Tommy",	//Salami
		"Jean-Luc",
		"Kaylee",
		"Mace",
		"Benjamin",
		"Mal",
		"Kara",
		"Luke",
		"Leia",
		"Kathryn",
		"James",
		"Julian",
		"Maya",
		"River",
		"Zoe",
		"Rose",
		"Clara",
		"David",
		"Arkady",
		"Hiroko",
		"Nadia",
		"Rory",
		"Alex",
		"Padme",
		"Aeryn",
		"Daniel",
		"Chloe",
		"Tamara",
		"Eli",
		"Nicholas",
		"Philip",
		"Petra",
		"Korben",
		"Gillian",
		"Valentine",
		"Kris",
		"Jessica",
		"Paul",
		"Chani",
		"Marvin",
		"Theo",
		"Jasper",
		"Kojo",		//The man, the legend
		"Helen"		//of Troy
	};

	public static List<string> lastNames = new List<string>()
	{
		"Dax",
		"Salami",	//Tommy
		"Baltar",
		"Adama",
		"Boone",
		"O'Brien",
		"Fontaine",
		"Cobb",
		"Harkness",
		"Pond",
		"Chalmers",
		"Clayborne",
		"Taneev",
		"Hawkins",
		"McCoy",
		"Shepard",
		"Sing",
		"Tano",
		"Crichton",
		"O'Neill",
		"Carter",
		"Deckard",
		"Anderson",
		"Wiggin",
		"Robinson",
		"Rico",
		"Stendahl",
		"Hathaway",
		"Dent",
		"Prefect"
	};


	/**Sets the given strings (usually character name fields) to random names. Needs to be followed up by Character.Rename() to update gameObject name */
	public static void AssignRandomName(out string firstName, out string lastName)
	{
		firstName = firstNames [Random.Range(0, firstNames.Count)];
		lastName = lastNames [Random.Range(0, lastNames.Count)];
	}
}
