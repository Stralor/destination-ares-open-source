using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventControlData_TheDarkness : EventControlData
{
	bool theHint { get { return EventSpecialConditions.c.dark_truthAvailable || (StatTrack.stats.crewDied > 0 && StatTrack.stats.eventsSurvived > 5); } set { EventSpecialConditions.c.dark_truthAvailable = value; } }

	bool theTruth { get { return EventSpecialConditions.c.dark_truthRealized; } set { EventSpecialConditions.c.dark_truthRealized = value; } }

	bool mutiny { get { return EventSpecialConditions.c.dark_rogueMutiny && GameReference.r.allCharacters.Exists(obj => obj.isControllable); } set { EventSpecialConditions.c.dark_rogueMutiny = value; } }


	public void BeginTheNewPath()
	{
		ShipResources.res.SetStartingDistance(ShipResources.res.startingDistance / 2);

		//Clear the old story mission(s). It's only this now.
		GameEventManager.gem.scheduledEvents.RemoveAll(obj => obj.progressType);
	}

	public void TheNewPath()
	{
		theTruth = true;

		ShipMovement.sm.offCourse = 1.5f;

		OverrideLossEvents(primaryCustomLossEvents);
	}

	public void PrepareForMutiny()
	{
		mutiny = true;
	}

	void Awake()
	{
		if (theTruth)
			OverrideLossEvents(primaryCustomLossEvents);
	}
}
