using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(PanObjects))]
public class PanObjectsEditor : Editor
{

	public override void OnInspectorGUI()
	{
		//Update!
		serializedObject.Update();

		//Draw default
		DrawDefaultInspector();

		EditorGUILayout.Space();

		//Buttons
		if (Application.isPlaying)
		{
			if (GUILayout.Button("Pan"))
			{
				(target as PanObjects).SendMessage("Pan");
			}
			
			if (GUILayout.Button("Reset"))
			{
				(target as PanObjects).SendMessage("Reset");
			}
		}
		else
		{
			EditorGUILayout.LabelField("Pan and Reset disabled while game inactive.");
		}

	}

}
