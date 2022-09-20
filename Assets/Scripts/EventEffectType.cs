public enum EventEffectType
{
	//Break a system or send a character to Psychotic.
	Break,
	//Hurt a system or character
	Damage,
	//Turn off a system. Be sure to target non-inerts!
	Disable,
	//Give (or take, if negative) player random resources (Energy, Fuel, Materials, Parts, or Waste)
	GiveRandom,
	GiveAir,
	GiveUsableAir,
	GiveDistance,
	GiveEnergy,
	GiveFuel,
	GiveFood,
	GiveMaterials,
	GiveParts,
	GiveSpeed,
	GiveThrust,
	GiveWaste,
	//Just returns a character name
	GetCharacter,
	//Returns a system name
	GetSystem,
	KnockOffCourse,
	//Crit break a system
	CritBreak,
	//Repair a system or heal/improve a character!
	Repair,
	//Crit repair a system! (works on destroyed)
	CritRepair,
	//Use a system or force a stress check on a char.
	Use,
	//Force a dura check on a sys, or induce stress on a char.
	Stress,
	//Remove a Character or ShipSystem from the ship!
	Remove,
	//Spawn an object on a specific spot (TODO)
	Spawn,
	//Spawn an object to a random valid location (TODO)
	SpawnRandomly,
	//Reduce OffCourse
	ImproveCourse,
	//Remove conditionHit on sys, or destress check on a char
	Destress,
	//Literally feed a crew member, or get a free use on a system
	Feed,
	//Give Keyword / Role / Skill improvement
	Improve,
	//Destroy, Kill, Ruin, THE DARK THE DARK THE DARK
	Destroy
}