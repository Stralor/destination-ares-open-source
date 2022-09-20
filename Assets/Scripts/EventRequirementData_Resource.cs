using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Event Req Res - ", menuName = "Events/Event Requirement Data (Resource)")]
public class EventRequirementData_Resource : EventRequirementData
{

	/* Just a bunch of possible requirements for an option to be available.
	 * Filled out as needed.
	 * 
	 * Each List only needs one of it's contents to be true, if there are any contents. Thus a list of all options is equivalent to an empty list.
	 */

	//Resources-to-check
	public int minDistanceLeft, minSpeed, minProgress, maxProgress = 100, minStorageTotal, minStorageRemaining,
		minCapacityTotal, minCapacityRemaining, minUsableAir, minTotalAir,
		minEnergy, minFood, minFuel, minMaterials, minParts, minWaste;


	/**The generated text of the of requirement
	 */
	public override string RequirementText
	{
		get
		{
			var stringBuilder = new System.Text.StringBuilder();

			string qualifierTag = "";
			if (invertRequirements)
				qualifierTag = "less than ";

			//Resource requirements
			if (minDistanceLeft > 0)
				stringBuilder.Append("Distance (" + qualifierTag + minDistanceLeft + ")");
			else if (minSpeed > 0)
				stringBuilder.Append("Speed (" + qualifierTag + (int)(minSpeed * 60 / ShipMovement.sm.tickDelay / GameClock.clock.clockSpeed) + ")");
			else if (minProgress > 0 || maxProgress < 100)
			{
				if (!invertRequirements)
					stringBuilder.Append("Progress (" + minProgress + " to " + maxProgress + ")");
				else
					stringBuilder.Append("Progress (not between " + minProgress + " and " + maxProgress + ")");
			}
			else if (minStorageTotal > 0)
				stringBuilder.Append("Total Storage (" + qualifierTag + minStorageTotal + ")");
			else if (minStorageRemaining > 0)
				stringBuilder.Append("Remaining Storage (" + qualifierTag + minStorageRemaining + ")");
			else if (minCapacityTotal > 0)
				stringBuilder.Append("Total Energy Capacity (" + qualifierTag + minCapacityTotal + ")");
			else if (minCapacityRemaining > 0)
				stringBuilder.Append("Remaining Energy Capacity (" + qualifierTag + minCapacityRemaining + ")");
			else if (minUsableAir > 0)
				stringBuilder.Append("Usable Air (" + qualifierTag + minUsableAir + ")");
			else if (minTotalAir > 0)
				stringBuilder.Append("Total Air (" + qualifierTag + minTotalAir + ")");
			else if (minEnergy > 0)
				stringBuilder.Append("Energy (" + qualifierTag + minEnergy + ")");
			else if (minFood > 0)
				stringBuilder.Append("Food (" + qualifierTag + minFood + ")");
			else if (minFuel > 0)
				stringBuilder.Append("Fuel (" + qualifierTag + minFuel + ")");
			else if (minMaterials > 0)
				stringBuilder.Append("Materials (" + qualifierTag + minMaterials + ")");
			else if (minParts > 0)
				stringBuilder.Append("Parts (" + qualifierTag + minParts + ")");
			else if (minWaste > 0)
				stringBuilder.Append("Waste (" + qualifierTag + minWaste + ")");

			//Done
			return stringBuilder.ToString();
		}
	}


	/**Searches through game resources.*/
	public override bool CheckRequirements()
	{
		if (!isUsable)
			return false;

		//Regular
		if (!invertRequirements)
		{
			if (minDistanceLeft > 0 && minDistanceLeft > ShipResources.res.distance)
				return false;
			if (minSpeed > 0 && minSpeed > ShipResources.res.speed)
				return false;
			if (minProgress > 0 && minProgress > ShipResources.res.progress)
				return false;
			if (maxProgress < 100 && maxProgress < ShipResources.res.progress)
				return false;
			if (minStorageTotal > 0 && minStorageTotal > ShipResources.res.storageTotal)
				return false;
			if (minStorageRemaining > 0 && minStorageRemaining > ShipResources.res.storageRemaining)
				return false;
			if (minCapacityTotal > 0 && minCapacityTotal > ShipResources.res.capacityTotal)
				return false;
			if (minCapacityRemaining > 0 && minCapacityRemaining > ShipResources.res.capacityRemaining)
				return false;
			if (minUsableAir > 0 && minUsableAir > ShipResources.res.usableAir)
				return false;
			if (minTotalAir > 0 && minTotalAir > ShipResources.res.totalAir)
				return false;
			if (minEnergy > 0 && minEnergy > ShipResources.res.energy)
				return false;
			if (minFood > 0 && minFood > ShipResources.res.food)
				return false;
			if (minFuel > 0 && minFuel > ShipResources.res.fuel)
				return false;
			if (minMaterials > 0 && minMaterials > ShipResources.res.materials)
				return false;
			if (minParts > 0 && minParts > ShipResources.res.parts)
				return false;
			if (minWaste > 0 && minWaste > ShipResources.res.waste)
				return false;
		}
		//Inverted
		else
		{
			if (minDistanceLeft > 0 && minDistanceLeft < ShipResources.res.distance)
				return false;
			if (minSpeed > 0 && minSpeed < ShipResources.res.speed)
				return false;
			if (minProgress > 0 && minProgress < ShipResources.res.progress)
				return false;
			if (maxProgress < 100 && maxProgress > ShipResources.res.progress)
				return false;
			if (minStorageTotal > 0 && minStorageTotal < ShipResources.res.storageTotal)
				return false;
			if (minStorageRemaining > 0 && minStorageRemaining < ShipResources.res.storageRemaining)
				return false;
			if (minCapacityTotal > 0 && minCapacityTotal < ShipResources.res.capacityTotal)
				return false;
			if (minCapacityRemaining > 0 && minCapacityRemaining < ShipResources.res.capacityRemaining)
				return false;
			if (minUsableAir > 0 && minUsableAir < ShipResources.res.usableAir)
				return false;
			if (minTotalAir > 0 && minTotalAir < ShipResources.res.totalAir)
				return false;
			if (minEnergy > 0 && minEnergy < ShipResources.res.energy)
				return false;
			if (minFood > 0 && minFood < ShipResources.res.food)
				return false;
			if (minFuel > 0 && minFuel < ShipResources.res.fuel)
				return false;
			if (minMaterials > 0 && minMaterials < ShipResources.res.materials)
				return false;
			if (minParts > 0 && minParts < ShipResources.res.parts)
				return false;
			if (minWaste > 0 && minWaste < ShipResources.res.waste)
				return false;
		}

		//Success, no falses tripped!
		return true;
	}
}