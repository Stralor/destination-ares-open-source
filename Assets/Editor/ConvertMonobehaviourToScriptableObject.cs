using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class ConvertMonobehaviourToScriptableObject : EditorWindow
{

	public List<MonoScript> inputScripts = new List<MonoScript>(), scriptables = new List<MonoScript>();

	Vector2 renameScrollPos, convertWarningScrollPos;

	bool convertAll = true;
	bool convertInactive = true;
	string isActiveAndEnabledAnalogue = "";
	List<bool> renames = new List<bool>();
	List<List<bool>> renameLists = new List<List<bool>>();

	/**Used if interdependencies**/
	List<LinkedScripts> scriptList = new List<LinkedScripts>();

	struct LinkedScripts
	{
		public MonoBehaviour original;
		public ScriptableObject result;
		public int indexOfT;

		public LinkedScripts(MonoBehaviour p1, ScriptableObject p2, int p3)
		{
			original = p1;
			result = p2;
			indexOfT = p3;
		}
	};



	// Add menu item named "My Window" to the Window menu
	[MenuItem("Window/Convert to ScriptableObject")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow(typeof(ConvertMonobehaviourToScriptableObject));
	}

	void OnGUI()
	{
		//Label
		GUILayout.Label("Convert MonoBehaviours to Equivalent ScriptableObjects");

		bool ready = true;

		SerializedObject so = new SerializedObject(this);

		//Input
		SerializedProperty inputClasses = so.FindProperty("inputScripts");
		EditorGUILayout.PropertyField(inputClasses, new GUIContent("Scripts to Convert"), true);

		//Clear nulls
		inputScripts.RemoveAll(obj => obj == null);

		//Input Warnings
		if (inputScripts.Count == 0)
		{
			EditorGUILayout.HelpBox("No MonoBehaviuor type selected for the input", MessageType.Warning);
			EditorGUILayout.Space();

			ready = false;
		}
		else if (inputScripts.Exists(obj => !obj.GetClass().IsSubclassOf(typeof(MonoBehaviour))))
		{
			EditorGUILayout.HelpBox("Incorrect filetype: " + inputScripts.Find(obj => !obj.GetClass().IsSubclassOf(typeof(MonoBehaviour))).name + ". Requires MonoBehaviour", MessageType.Error);
			EditorGUILayout.Space();

			ready = false;
		}

		//Output Style
		SerializedProperty scriptableObjects = so.FindProperty("scriptables");
		EditorGUILayout.PropertyField(scriptableObjects, new GUIContent("Style as"), true);

		//Clear nulls
		scriptables.RemoveAll(obj => obj == null);

		//Output Style Warnings
		if (scriptables.Count == 0)
		{
			EditorGUILayout.HelpBox("No ScriptableObject type selected for the output style", MessageType.Warning);
			EditorGUILayout.Space();

			ready = false;
		}
		else if (scriptables.Exists(obj => !obj.GetClass().IsSubclassOf(typeof(ScriptableObject))))
		{
			EditorGUILayout.HelpBox("Incorrect filetype: " + scriptables.Find(obj => !obj.GetClass().IsSubclassOf(typeof(ScriptableObject))).name + ". Requires ScriptableObject", MessageType.Error);
			EditorGUILayout.Space();

			ready = false;
		}

		//Count mismatch error
		if (inputScripts.Count != scriptables.Count)
		{
			EditorGUILayout.HelpBox("Input scripts to output styles ratio is not 1:1", MessageType.Error);
			EditorGUILayout.Space();

			ready = false;
		}


		//Options
		var convertStyleString = convertAll ? "Convert: all in scene" : "Convert: selected";
		convertAll = EditorGUILayout.Toggle(convertStyleString, convertAll);

		convertInactive = EditorGUILayout.Toggle("Include inactive", convertInactive);

		isActiveAndEnabledAnalogue = EditorGUILayout.TextField("isActiveAndEnabled style", isActiveAndEnabledAnalogue).Trim();

		renameScrollPos = EditorGUILayout.BeginScrollView(renameScrollPos);

		//Rename controls
		for (int i = 0; i < inputScripts.Count; i++)
		{
			//Add toggles per input script
			if (renames.Count <= i)
				renames.Add(false);

			//Get the lists
			for (int j = 0; j < inputScripts.Count; j++)
			{
				if (renameLists.Count <= j)
					renameLists.Add(new List<bool>());
			}

			renames[i] = EditorGUILayout.Toggle("Rename " + inputScripts[i].name, renames[i]);

			//Populate desired rename lists
			if (renames[i])
			{
				EditorGUI.indentLevel++;

				//Label
				//				EditorGUILayout.LabelField(t.name);

				//Get fields
				var fields = new List<FieldInfo>();
				fields.AddRange(inputScripts[i].GetClass().GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public));

				//Have that many bools
				for (int j = 0; j < fields.Count; j++)
				{
					if (renameLists[i].Count <= j)
						renameLists[i].Add(false);
				}

				//Indent
				EditorGUI.indentLevel++;

				//Set bools
				foreach (var u in fields)
				{
					renameLists[i][fields.IndexOf(u)] = EditorGUILayout.Toggle("Use " + u.Name, renameLists[i][fields.IndexOf(u)]);
				}

				//Clean up spacing
				EditorGUILayout.Space();
				EditorGUI.indentLevel--;
				EditorGUI.indentLevel--;
			}
		}

		EditorGUILayout.EndScrollView();

		//Action
		if (ready)
		{
			EditorGUILayout.Space();
			if (GUILayout.Button("Convert"))
			{
				//First clear any old entries in scriptList
				scriptList.Clear();
				//Take the extra step to link everything now, in case of interdependencies
				//To do so, create all of the new scripts (without filling data yet) and store a reference to the original
				for (int i = 0; i < inputScripts.Count; i++)
				{
					//Skip abstracts (they're only there for reference of inheritance)
					if (inputScripts[i].GetClass().IsAbstract)
						continue;

					//We're gonna reflect to get a strongly-typed version of CreateNewScript that matches our specific use case
					MethodInfo createMethod = typeof(ConvertMonobehaviourToScriptableObject).GetMethod("CreateNewScripts", BindingFlags.Instance | BindingFlags.NonPublic);
					MethodInfo genericCreate = createMethod.MakeGenericMethod(inputScripts[i].GetClass());
					//Do it, with the scriptable ref
					genericCreate.Invoke(this, new object[] { scriptables[i], i });
				}

				//Then transfer over all of the data
				scriptList.ForEach(obj => Convert(obj));

				AssetDatabase.SaveAssets();
				EditorUtility.FocusProjectWindow();
			}

			//convertWarningScrollPos = EditorGUILayout.BeginScrollView(convertWarningScrollPos);

			foreach (var t in inputScripts)
			{
				//Warnings
				var inputMembers = new List<MemberInfo>();
				inputMembers.AddRange(t.GetClass().GetMembers(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static));

				var outputMembers = new List<MemberInfo>();
				outputMembers.AddRange(scriptables[inputScripts.IndexOf(t)].GetClass().GetMembers(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static));

				var missingMembers = GetMissingMembers(inputMembers, outputMembers);

				var sharedMembers = inputMembers.Except(missingMembers);

				var changingMembers = GetChangingMembers(inputMembers.Except(sharedMembers).ToList(), outputMembers.Except(sharedMembers).ToList());

				//Compare members of the files to see if the new one is missing members
				foreach (var u in missingMembers.Except(changingMembers))
				{
					if (u.MemberType != MemberTypes.Constructor && u.MemberType != MemberTypes.Method)
					{
						string objectTypeName = "";

						if (u.MemberType == MemberTypes.Field)
							objectTypeName = ((FieldInfo)u).FieldType.ToString();

						if (u.MemberType == MemberTypes.Property)
							objectTypeName = ((PropertyInfo)u).PropertyType.ToString();

						//Display warning
						EditorGUILayout.HelpBox("Output class \"" + scriptables[inputScripts.IndexOf(t)].name + "\" is missing a name- and type-matched member: \"" + u.Name + "\" (" + objectTypeName + ")" +
						"\nThis value will not transfer on convert.", MessageType.Warning);
					}
				}

				foreach (var u in inputMembers.Where(field => field.MemberType == MemberTypes.Field && (field as FieldInfo).FieldType.IsSubclassOf(typeof(MonoBehaviour))))
				{
					EditorGUILayout.HelpBox("MonoBehaviour reference \"" + u.Name + "\" in \"" + t.name + "\" will not transfer value on convert.", MessageType.Warning);
				}

				//EditorGUILayout.EndScrollView();
			}
		}

		//Keep errythang updated
		so.ApplyModifiedProperties();
	}

	/**Finds and returns any members in list0 for which list1 doesn't have a member that matches in name and type.
	 * If the member is a pointer to a script that is changing type right now, puts it in changingMembersList.
	 */
	List<MemberInfo> GetMissingMembers(List<MemberInfo> list0, List<MemberInfo> list1)
	{
		var missing = new List<MemberInfo>();

		foreach (var t in list0)
		{
			MemberInfo match = list1.Find(obj => obj.Name == t.Name);

			//Add everything that straight up doesn't have a name match
			if (match == null)
				goto NoMatch;

			//Also add everything that doesn't line up in type
			if (t.MemberType != match.MemberType)
			{
				goto NoMatch;
			}

			//Get specific with fields
			if (t.MemberType == MemberTypes.Field)
			{
				if (((FieldInfo)t).FieldType != ((FieldInfo)match).FieldType)
					goto NoMatch;
			}

			//Also with properties
			if (t.MemberType == MemberTypes.Property)
			{
				if (((PropertyInfo)t).PropertyType != ((PropertyInfo)match).PropertyType)
					goto NoMatch;
			}

			//If we get here, we're good
			continue;

			//Add mismatches and no matches to missing
			NoMatch:
			missing.Add(t);
			continue;
		}

		return missing;
	}

	/**Finds and returns any members (assumed to be fields only) that are pointers to scripts (or collections thereof) that are changing type right now.**/
	List<MemberInfo> GetChangingMembers(List<MemberInfo> list0, List<MemberInfo> list1)
	{
		List<MemberInfo> changing = new List<MemberInfo>();

		//Search through any members that match by name and are field type
		foreach (var t in list0.Where(m => m.MemberType == MemberTypes.Field && list1.Exists(obj => obj.Name == m.Name)))
		{
			//Get the match
			//			MemberInfo match = list1.Find(obj => obj.Name == t.Name);
			var type = (t as FieldInfo).FieldType; //Needs to be the specific script type

			//Is a collection?
			if (type.GetInterface("IEnumerable") != null)
			{
				if (type.GetInterface("Dictionary") != null)
					continue;

				//Change our listed "type" to be the base type
				type = type.GetGenericArguments().FirstOrDefault();
			}

			//Is it an enum (or collection of enums)?
			if (type.IsEnum)
			{
				changing.Add(t);
			}

			//Is core match type a script we're converting?
			if (inputScripts.Exists(mb => mb.GetClass() == type))
			{
				changing.Add(t);
			}
		}

		return changing;
	}

	void CreateNewScripts<T>(MonoScript target, int indexOfT) where T : MonoBehaviour
	{
		//Get out list of real objects we're going to convert
		List<T> inputs = new List<T>();
		if (convertAll)
		{
			if (!convertInactive)
				inputs.AddRange(FindObjectsOfType<T>());
			else
				inputs.AddRange(FindObjectsOfTypeInclInactive<T>());
		}
		else
			inputs.AddRange(Selection.GetFiltered<T>(SelectionMode.Deep));

		foreach (var t in inputs)
		{
			//Create a new object of the target type
			var obj = ScriptableObject.CreateInstance(target.GetClass());

			//Keep all the related information together; we haven't done the data transfer yet!
			scriptList.Add(new LinkedScripts(t, obj, indexOfT));
		}
	}

	//List<T> CreateNewListFromOldValues<T>() where T : IEnumerable
	//{
	//	var listType = typeof(List<>);
	//	var genericArgs = (m as FieldInfo).FieldType.GetGenericArguments();
	//	var concreteType = listType.MakeGenericType(genericArgs);
	//	object newList = System.Activator.CreateInstance(concreteType);

	//	return (List<T>)newList;
	//}

	void Convert(LinkedScripts linkObj)
	{
		//Reflect lists first to cut down on processing, since it'll always be the same classes
		var inputMembers = new List<MemberInfo>();
		var outputMembers = new List<MemberInfo>();
		var sharedMembers = new List<MemberInfo>();
		var changingMembers = new List<MemberInfo>();

		//Do the member sorting
		inputMembers.AddRange(linkObj.original.GetType().GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
			.Where(obj => obj.MemberType == MemberTypes.Field || obj.MemberType == MemberTypes.Property));
		outputMembers.AddRange(linkObj.result.GetType().GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
			.Where(obj => obj.MemberType == MemberTypes.Field || obj.MemberType == MemberTypes.Property));
		sharedMembers.AddRange(inputMembers.Except(GetMissingMembers(inputMembers, outputMembers)));
		changingMembers.AddRange(GetChangingMembers(inputMembers.Except(sharedMembers).ToList(), outputMembers.Except(sharedMembers).ToList()));

		//Let's begin conversion

		//Set each of the fields and properties
		foreach (var m in sharedMembers)
		{
			//Fields
			if (m.MemberType == MemberTypes.Field)
				linkObj.result.GetType().GetField(m.Name, BindingFlags.Instance |  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
					.SetValue(linkObj.result, ((FieldInfo)m).GetValue(linkObj.original));

			//Properties
			if (m.MemberType == MemberTypes.Property && (m as PropertyInfo).CanWrite)
				linkObj.result.GetType().GetProperty(m.Name, BindingFlags.Instance |  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
					.SetValue(linkObj.result, ((PropertyInfo)m).GetValue(linkObj.original, null), null);
		}

		//Write isActiveAndEnabled to a bool, if given
		if (!string.IsNullOrEmpty(isActiveAndEnabledAnalogue))
		{
			var field = linkObj.result.GetType().GetField(isActiveAndEnabledAnalogue);
			var property = linkObj.result.GetType().GetProperty(isActiveAndEnabledAnalogue);
			var value = linkObj.original.GetType().GetProperty("isActiveAndEnabled").GetValue(linkObj.original, null);

			if (value != null)
			{
				if (field != null)
					field.SetValue(linkObj.result, value);
				if (property != null)
					property.SetValue(linkObj.result, value, null);
			}
		}

		//Change the script pointers from the old to the new
		foreach (var m in changingMembers)
		{
			if (m == null)
				continue;

			FieldInfo newField = linkObj.result.GetType().GetField(m.Name, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

			//First, it's a quick fix if it's an enum (we'll just transfer the int value)
			if (newField.FieldType.IsEnum)
			{
				newField.SetValue(linkObj.result, (int)(m as FieldInfo).GetValue(linkObj.original));
			}

			//Otherwise, check for ienumerable interface (like a list would have)
			else if ((m as FieldInfo).FieldType.GetInterface("IEnumerable") != null)
			{
				object newFieldObj = newField.GetValue(linkObj.result);
				System.Type type = newFieldObj.GetType();

				bool listIsEnums = type.GetGenericArguments().FirstOrDefault().IsEnum;

				bool skipDictionary = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

				if (skipDictionary)
					continue;

				var oldList = (m as FieldInfo).GetValue(linkObj.original) as IEnumerable;

				//Add each corresponding ScriptableObject
				foreach (var t in oldList)
				{
					if (t == null)
						continue;

					//SIKE IT'S ENUMS
					if (listIsEnums)
					{
						object enumInt = (int)t;

						var addMethod = type.GetMethod("Add");
						addMethod.Invoke(newFieldObj, new[] { enumInt });

						continue;
					}

					//Not enums?

					if (scriptList.Exists(obj => obj.original.name == (t as MonoBehaviour).name))
					{
						var content = scriptList.Find(obj => obj.original.name == (t as MonoBehaviour).name && obj.original.GetType() == t.GetType()).result;

						if (content != null)
						{
							//var addMethod = type.GetMethod("Add", encasedTypes);
							var addMethod = type.GetMethod("Add");
							addMethod.Invoke(newFieldObj, new[] { content });
						}
						else
							Debug.Log("Couldn't find \"" + (t as MonoBehaviour).name + "\" when looking for a matching script.");
					}
				}

				//newList.RemoveAll(obj => obj == null);
			}

			//And if it's safely a single script conversion, find the field with the matching name and transfer it to the new one!
			else
			{
				try
				{
					newField.SetValue(linkObj.result, scriptList.Find(obj => obj.original.name == ((m as FieldInfo).GetValue(linkObj.original) as MonoBehaviour).name).result);
				}
				catch (System.Exception exc)
				{
					Debug.Log(linkObj.result + " | " + m.Name);
					Debug.Log(exc.StackTrace);
				}
			}
		}

		//Naming
		var outputName = linkObj.original.name;
		if (renames[linkObj.indexOfT] && renameLists[linkObj.indexOfT].Exists(boo => boo))
		{
			outputName = "";
			//Rename over each "true" to rename by
			for (int i = 0; i < renameLists[linkObj.indexOfT].Count; i++)
			{
				if (!renameLists[linkObj.indexOfT][i])
					continue;

				//Divider
				outputName += outputName == "" ? "" : "-";

				//The info
				var fieldInfo = inputMembers.Where(mem => mem.MemberType == MemberTypes.Field).ElementAt(i) as FieldInfo;
				var val = fieldInfo.GetValue(linkObj.original);

				//Iterate out lists arrays
				if (typeof(IList).IsAssignableFrom(fieldInfo.FieldType))
				{
					foreach (var item in val as IList)
					{
						outputName += item.ToString();
					}
				}
				//Dump non-lists
				else
					outputName += val.ToString();
			}
		}

		//Create dump folders if they don't exist
		if (!AssetDatabase.IsValidFolder("Assets/ConvertedScriptables"))
			AssetDatabase.CreateFolder("Assets", "ConvertedScriptables");
		if (!AssetDatabase.IsValidFolder("Assets/ConvertedScriptables/" + linkObj.result.GetType().Name))
			AssetDatabase.CreateFolder("Assets/ConvertedScriptables", linkObj.result.GetType().Name);

		//Save the object
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath("Assets/ConvertedScriptables/" + linkObj.result.GetType().Name + "/" + outputName + ".asset");
		AssetDatabase.CreateAsset(linkObj.result, assetPathAndName);
		Selection.activeObject = linkObj.result;
	}

	public static List<T> FindObjectsOfTypeInclInactive<T>()
	{
		List<T> results = new List<T>();
		for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
		{
			var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
			if (s.isLoaded)
			{
				var allGameObjects = s.GetRootGameObjects();
				for (int j = 0; j < allGameObjects.Length; j++)
				{
					var go = allGameObjects[j];
					results.AddRange(go.GetComponentsInChildren<T>(true));
				}
			}
		}
		return results;
	}
}
