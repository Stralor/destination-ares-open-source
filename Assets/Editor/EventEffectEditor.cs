using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(EventEffectData))]
[CanEditMultipleObjects]
public class EventEffectEditor : Editor
{

	//Declare all properties we're gonna use.
	SerializedProperty typeProp, tarsProp, filtProp, adoiProp, abouProp, adodProp, lexcProp, laveProp, lshoProp, lmakProp, chanceProp, giveProp, resuProp;



	void OnEnable()
	{
		//Find the properties
		typeProp = serializedObject.FindProperty("type");
		tarsProp = serializedObject.FindProperty("targets");
		filtProp = serializedObject.FindProperty("filtersOnly");
		adoiProp = serializedObject.FindProperty("allowDamagedOrInjured");
		abouProp = serializedObject.FindProperty("allowBrokenOrUncontrollable");
		adodProp = serializedObject.FindProperty("allowDestroyedOrDead");
		lexcProp = serializedObject.FindProperty("limitToExceptional");
		laveProp = serializedObject.FindProperty("limitToAverage");
		lshoProp = serializedObject.FindProperty("limitToShoddy");
		lmakProp = serializedObject.FindProperty("limitToMakeshift");
		chanceProp = serializedObject.FindProperty("chance");
		giveProp = serializedObject.FindProperty("giveResources");
		resuProp = serializedObject.FindProperty("resultString");
	}

	//This updates the Inspector. Duh.
	public override void OnInspectorGUI()
	{

		//Call to keep values up to date
		serializedObject.Update();

		//First UI field! Just the effect type
		EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));

		//Is it a resource-based event?
		bool resourceEvent = typeProp.enumDisplayNames [typeProp.enumValueIndex].Contains("Give") || typeProp.enumDisplayNames [typeProp.enumValueIndex].Contains("Course");

		//If not, we're gonna target characters/systems!
		if (!resourceEvent)
		{
			//Be sure to check if the list count changes!
			EditorGUI.BeginChangeCheck();
			//The targets list
			EditorGUILayout.PropertyField(tarsProp, new GUIContent("Potential Targets"), true);
			//If anything changed, update it
			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();

			//Don't forget regular old chance
			EditorGUILayout.PropertyField(chanceProp, new GUIContent("Chance"));
		}
		else
		{
			//In resource events, chance affects how random the resources to give are
			EditorGUILayout.PropertyField(chanceProp, new GUIContent("Invariance"));
			//Only need this value for resource events
			EditorGUILayout.PropertyField(giveProp, new GUIContent("Resources to Give"));
		}

		//If it's still not a resource event and we have a list active, we need to open up filters
		if (!resourceEvent && tarsProp.arraySize > 0)
		{
			//Filters only is the easy one
			EditorGUILayout.PropertyField(filtProp, new GUIContent("Filters Only"));

			//Now we need to see if the list contains potential systems and/or characters
			EventEffectData effect = (EventEffectData)target;
			bool isSys = false;
			bool isChar = false;
			//Iterate through the array to find sys targets and char targets
			for (int i = 0; i < tarsProp.arraySize; i++)
			{
				//Get the element at this index of the targets property. Convert the element into it's enumValueIndex. Cast as the enum type. Check target type.
				if (effect.IsSystemTarget((EventTarget)tarsProp.GetArrayElementAtIndex(i).enumValueIndex))
					isSys = true;
				if (effect.IsCharacterTarget((EventTarget)tarsProp.GetArrayElementAtIndex(i).enumValueIndex))
					isChar = true;
			}

			//Set up filter fields accordingly
			if (isSys && isChar)
			{
				EditorGUILayout.PropertyField(adoiProp, new GUIContent("Allow Strained or Injured"));
				EditorGUILayout.PropertyField(abouProp, new GUIContent("Allow Broken or Uncontrollable"));
				EditorGUILayout.PropertyField(adodProp, new GUIContent("Allow Destroyed or Dead"));
				EditorGUILayout.PropertyField(lexcProp, new GUIContent("Limit to Exceptional"));
				EditorGUILayout.PropertyField(laveProp, new GUIContent("Limit to Average"));
				EditorGUILayout.PropertyField(lshoProp, new GUIContent("Limit to Shoddy"));
				EditorGUILayout.PropertyField(lmakProp, new GUIContent("Limit to Makeshift"));
			}
			else if (isSys)
			{
				EditorGUILayout.PropertyField(adoiProp, new GUIContent("Allow Strained"));
				EditorGUILayout.PropertyField(abouProp, new GUIContent("Allow Broken"));
				EditorGUILayout.PropertyField(adodProp, new GUIContent("Allow Destroyed"));
				EditorGUILayout.PropertyField(lexcProp, new GUIContent("Limit to Exceptional"));
				EditorGUILayout.PropertyField(laveProp, new GUIContent("Limit to Average"));
				EditorGUILayout.PropertyField(lshoProp, new GUIContent("Limit to Shoddy"));
				EditorGUILayout.PropertyField(lmakProp, new GUIContent("Limit to Makeshift"));
			}
			else if (isChar)
			{
				EditorGUILayout.PropertyField(adoiProp, new GUIContent("Allow Injured"));
				EditorGUILayout.PropertyField(abouProp, new GUIContent("Allow Uncontrollable"));
				EditorGUILayout.PropertyField(adodProp, new GUIContent("Allow Dead"));
			}

			EditorGUILayout.PropertyField(resuProp, new GUIContent("Result String"));
		}
		
		//Apply any changes
		serializedObject.ApplyModifiedProperties();
	}
	
}
