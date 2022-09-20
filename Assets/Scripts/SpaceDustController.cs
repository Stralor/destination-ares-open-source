using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceDustController : MonoBehaviour
{
	public ParticleSystem dust, sparks;




	void Update()
	{
		int dustSpeedMultiplier = ShipResources.res.speed / 10;

		//They're get-only, so store as temp and set there
		var dustMain = dust.main;
		var dustEmit = dust.emission;
		var sparksEmit = sparks.emission;

		//Values
		dustMain.startLifetime = dustSpeedMultiplier == 0 ? 100 : 100 / dustSpeedMultiplier;
		dustMain.startSpeedMultiplier = dustSpeedMultiplier;
		dustEmit.rateOverTimeMultiplier = dustSpeedMultiplier * 2;

		sparksEmit.rateOverTimeMultiplier = dustSpeedMultiplier / 10;
	}
}
