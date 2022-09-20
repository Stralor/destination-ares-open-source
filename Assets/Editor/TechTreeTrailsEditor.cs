using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TechTreeTrails))]
public class TechTreeTrailsEditor : Editor
{
	public override void OnInspectorGUI()
	{
		//base.OnInspectorGUI();
		DrawDefaultInspector();

		if (GUILayout.Button("Recompute All Lines"))
		{
			foreach (var t in FindObjectsOfType<TechTreeTrails>())
			{
				t.ComputeLines();
			}
		}
	}

}
