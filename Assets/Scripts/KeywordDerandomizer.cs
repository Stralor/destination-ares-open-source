using UnityEngine;
using System.Collections;
using System.Linq;

public class KeywordDerandomizer : MonoBehaviour
{
	void Update()
	{
		//Characters that are random should be assigned
		if (GameReference.r.allCharacters.Exists(obj => obj != null && obj.isRandomCrew))
		{
			//Set all of them
			foreach (var t in GameReference.r.allCharacters.FindAll(obj => obj != null && obj.isRandomCrew))
			{
				//Only some will have new stats
				if (Random.Range(0, 2) == 0)
				{
					//A shitty role
					t.roles.Add(Character.unlockedShittyRoles [Random.Range(0, Character.unlockedShittyRoles.Count)]);
					//And something to offset it
					t.GiveRandomRoleOrSkill();
				}

				//No more random!
				t.isRandomCrew = false;
			}
		}

		//Same with systems
		if (GameReference.r.allSystems.Exists(obj => obj != null && obj.keywords.Contains(ShipSystem.SysKeyword.Random)))
		{
			//Give it to em
			foreach (var t in GameReference.r.allSystems.FindAll(obj => obj != null && obj.keywords.Contains(ShipSystem.SysKeyword.Random)))
			{
				//Let it do its thing
				t.SetKeywords(false);
			}
		}
	}
}
