using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(EventRequirementData))]
[CanEditMultipleObjects]
public class EventRequirementEditor : Editor {

	public override void OnInspectorGUI(){
		
		EditorGUILayout.HelpBox ("Each List only needs one of it's contents to be true, if there are any contents. Thus a list of all options is equivalent to an empty list.", MessageType.Info);

		DrawDefaultInspector ();
	}
}
