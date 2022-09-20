using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CreateAssetMenu(fileName = "Event Req CheckScript - ", menuName = "Events/Event Requirement Data (CheckScript)")]
public class EventRequirementData_CheckScriptBool : EventRequirementData
{
	public ScriptableObject targetScriptInstance;

	[Tooltip("Finds instance member(s) with these names (limited to field, property, or method) in Target Script to check as the requirement. If any are true, success. " +
	"\nMembers need to return/ convert to bool. Uses reflection. \n\nCase insensitive and removes inner spaces for readability.")]
	public List<string> boolsToCheck = new List<string>();
	[Tooltip("If multiple matches, chooses member of this type. Else, uses first found members.")]
	public MemberTypes preferredMemberType = MemberTypes.All;
	[Tooltip("What the requirement is called in game, if it's not hidden. If left empty, will use Bools To Check values.")]
	public string inGameReqText;

	public override string RequirementText
	{
		get
		{
			//StringBuilder ftw
			var stringBuilder = new System.Text.StringBuilder();

			//Inverts *yawn*
			if (invertRequirements)
				stringBuilder.Append("Not: ");

			//Do we have an explicit string to use?
			if (inGameReqText.Trim() != "")
				stringBuilder.Append(inGameReqText);
			//Nah
			else
				stringBuilder.Append(BuildStringSectionFromList(boolsToCheck));

			//Remove the last character, if it's a semicolon
			stringBuilder.Replace(";", "", stringBuilder.Length - 1, 1);

			//Return (trim off that leading space from BuildStringSectionFromList)
			return stringBuilder.ToString().Trim();
		}
	}


	/**Check the given requirement. We'll reflect the script, dig out the one by name, and get the value.
	 * Don't you dare pass anything other than a bool or bool-returning method, you'll have a bad time.
	 */
	public override bool CheckRequirements()
	{
		if (!isUsable)
			return false;

		//Time for some reflection
		System.Type type = targetScriptInstance.GetType();

		//Safety
		if (preferredMemberType != MemberTypes.All && preferredMemberType != MemberTypes.Field && preferredMemberType != MemberTypes.Property && preferredMemberType != MemberTypes.Method)
		{
			Debug.LogWarning("Hey, you, designer: stick to using All, Field, Property, or Method as the Preferred Member Type in your EventRequirement. The rest aren't currently supported.");
			return false;
		}

		bool foundMatch = false, itemsToSearch = false;
		foreach (var item in boolsToCheck)
		{
			//Need a version of the string that for sure has no spaces inside it
			var spacelessItem = item.Replace(" ", "");

			//Find the member(s) with that name.
			List<MemberInfo> members = new List<MemberInfo>();
			members.AddRange(type.GetMember(spacelessItem, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase));
			
			//Get the one we actually want
			MemberInfo preferredMember = members.Find(member => member.MemberType == preferredMemberType);
			
			//Or just grab the first one. Fuck it.
			if (preferredMember == null)
				preferredMember = members [0];
			
			//If it's a bool field, get that value
			if (preferredMember.MemberType == MemberTypes.Field && ((FieldInfo)preferredMember).FieldType == typeof(bool))
			{
				//We have a search match
				itemsToSearch = true;
				//Get the condition
				foundMatch = (bool)((FieldInfo)preferredMember).GetValue(targetScriptInstance);
			}
			//If it's a bool property, get that value instead
			else if (preferredMember.MemberType == MemberTypes.Property && ((PropertyInfo)preferredMember).PropertyType == typeof(bool))
			{
				//We have a search match
				itemsToSearch = true;
				//Get the condition
				foundMatch = (bool)((PropertyInfo)preferredMember).GetValue(targetScriptInstance, null);
			}
			//If it's a bool method, we'll use a delegate and Invoke
			else if (preferredMember.MemberType == MemberTypes.Method && ((MethodInfo)preferredMember).ReturnType == typeof(bool))
			{
				//We have a search match
				itemsToSearch = true;
				//The condition
				var requiredCondition = (System.Func<bool>)System.Delegate.CreateDelegate(typeof(System.Func<bool>), targetScriptInstance, (MethodInfo)preferredMember);
				//Get this
				foundMatch = requiredCondition.Invoke();
			}

			//If we found a match on this iteration, we're done.
			if (foundMatch)
				return !invertRequirements;
		}

		//If we searched valid items, but never found matches, return 'false'/ invert
		if (itemsToSearch)
			return invertRequirements;

		//No found search matches means no requirement of that name. Maybe event designer error. Return success.
		Debug.LogWarning("(CheckRequirements) No valid match found for the requirement \"" + name + "\" using type " + preferredMemberType.ToString() + " on " + targetScriptInstance.ToString() + ". Returning success.");
		return true;
	}
}