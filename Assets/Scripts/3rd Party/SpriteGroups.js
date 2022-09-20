// SpriteGroups.js was created by Not Quite Black and White for the upcoming point and click adventure This is No Time for Games
// but the script can be used in your Unity projects, license free.
// If you want to find out more about This is No Time for Games please visit ThisisNoTimeforGames.com

// This script is for editing the colour, sorting layer and sorting order of a group of sprite renderers simultaneously.
// Multiple groups of sprite renderers can be contained within the variable "spriteGroups" and edited separately in the editor.
// To allow the sprite groups to be edited at runtime too, just delete the if (!(Application.isPlaying)) on lines 38, 25 and 24.

// For instructions on use please attach this to a game object and see the tooltips in the inspector.

#pragma strict

import System.Collections.Generic;

@script ExecuteInEditMode()
public class SpriteGroups extends MonoBehaviour
{
	@Tooltip ("To create a new group of sprites that can be edited simultaneously, click the arrow and type the number of groups of sprites you want to create into the field.")
	public var spriteGroups : List.<SpriteGroupInfo>;

	#if UNITY_EDITOR
	public function Update()
	{
		if (!(Application.isPlaying))
		{
			if (spriteGroups != null)
			{
				for (var i=0; i < spriteGroups.Count; i++)
			    {
			     	for (var j=0; j < spriteGroups[i].spriteRendererList.Count; j++)
			     	{
			     		if (spriteGroups[i].updateColour) spriteGroups[i].spriteRendererList[j].color = spriteGroups[i].colour;
			     		if (spriteGroups[i].updateSortingLayer) spriteGroups[i].spriteRendererList[j].sortingLayerName = spriteGroups[i].sortingLayer;
			     		if (spriteGroups[i].updateSortingOrder) spriteGroups[i].spriteRendererList[j].sortingOrder = spriteGroups[i].sortingOrder;
			     	}
			    }
		 	}
		}
	}
	#endif

	// Set colour at runtime e.g. SetColour(0, Color.black);
	public function SetColour(spriteGroupsIndex : int, colour : Color)
	{
		if (spriteGroups != null)
		{
			if (spriteGroups.Count >= spriteGroupsIndex + 1)
		    {
		     	for (var i=0; i < spriteGroups[spriteGroupsIndex].spriteRendererList.Count; i++)
		     	{
		     		spriteGroups[spriteGroupsIndex].spriteRendererList[i].color = colour;
		     	}
		    }
		}
	}

	// Set sorting layer at runtime e.g SetSortingLayer(0, "Default");
	public function SetSortingLayer(spriteGroupsIndex : int, sortingLayer : String)
	{
		if (spriteGroups != null)
		{
			if (spriteGroups.Count >= spriteGroupsIndex + 1)
		    {
		     	for (var i=0; i < spriteGroups[spriteGroupsIndex].spriteRendererList.Count; i++)
		     	{
		     		spriteGroups[spriteGroupsIndex].spriteRendererList[i].sortingLayerName = sortingLayer;
		     	}
		    }
		}
	}

	// Set sorting order at runtime e.g SetSortingOrder(0, 10);
	public function SetSortingOrder(spriteGroupsIndex : int, sortingOrder : int)
	{
		if (spriteGroups != null)
		{
			if (spriteGroups.Count >= spriteGroupsIndex + 1)
		    {
		     	for (var i=0; i < spriteGroups[spriteGroupsIndex].spriteRendererList.Count; i++)
		     	{
		     		spriteGroups[spriteGroupsIndex].spriteRendererList[i].sortingOrder = sortingOrder;
		     	}
		    }
		}
	}
}

public class SpriteGroupInfo
{
	@Tooltip ("Set the colour for this sprite group.")
	public var colour : Color = Color.white;
	@Tooltip ("Set the sorting layer for this sprite group.")
	public var sortingLayer : String;
	@Tooltip ("Set the sorting order for this sprite group.")
	public var sortingOrder : int;
	@Tooltip ("The sprite group: Drag game objects that have sprite renderers attached from the hierarchy into this list.")
	public var spriteRendererList : List.<SpriteRenderer>;
	@Tooltip ("Do you want to update the colour of this sprite group?")
	public var updateColour : boolean = false;
	@Tooltip ("Do you want to update the sorting layer of this sprite group?")
	public var updateSortingLayer : boolean = false;
	@Tooltip ("Do you want to update the sorting order of this sprite group?")
	public var updateSortingOrder : boolean = false;
}