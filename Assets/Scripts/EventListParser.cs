//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//
//
//public class EventListParser : MonoBehaviour {
//
//	//Declarations
//	private int itemCount; 		//Number of top-level nodes (e.g. events)
//	private string fileName;	//Name of file
//	private string fileLoc;		//File path
//	TextReader reader;  		//TextReader, superclass of StreamReader and StringReader
//	private string fileText;	//The raw text of the file
//	private List<EventStore> eventList;	//EventList.txt, parsed and itemized for event selection and further parsing
//	private EventTargetEvent;		//The event being created
//	private int untitledIncrement = 0;	//A value appended to UNTITLED event names
//	private List<string> uniqueNames = new List<string>();	//A list of event names generated from the source, so we don't have repeats
//
//
//
//	//List of possible event types
//	public enum condition {
//		NEVER,
//		SUBEVENT,
//		Standard
//	}
//	//List of possible option effects
//	public enum effect {
//		Destroy,
//		Damage,
//		Give
//	}
//	//List of possible option targets
//	public enum target {
//		System,
//		Engine,
//		Oxygen
//	}
//
//
//
//	private void parse(){ //Check and process the passed text
//		//Establish the reader
//		try {
//			reader = File.OpenText (fileLoc);
//		} catch (IOException e) {
//			Debug.Log (e);
//		}
//		//Can read?
//		if ( reader == null )
//		{
//			Debug.Log(fileName + " is not readable.");
//		}
//		else
//		{
//			string s; //The currently read string
//			string q = ""; //The event-in-construction
//			int nestCount = 0; //Used to count how many event texts are nested
//			int totalNestCount = 0; //Used to count how many layers were in the text
//			//Read the text. Divide events into items on the list.
//			while ( (s = reader.ReadLine()) != null ) {
//				fileText += "\n" + s; //Update raw text. Useful mostly for debugging parser.
//				//Is it an event?
//				if (s.Trim().StartsWith("<")){
//					//Is it a new event?
//					if (nestCount == 0){
//						if (q != ""){ //Don't add the first item
//							addEvent(q, totalNestCount); //Add other previous items
//							totalNestCount = 0; //Reset totalNestCount for future items
//						}
//						q = s; //New events need to reset the construction string
//					}
//					//It's a nested event!
//					else {
//						q += "\n" + s;
//					}
//					//Now add a layer to this onion!
//					nestCount++;
//					//Also mark how many layers there are
//					totalNestCount++;
//				}
//				else {
//					q += "\n" + s; //This is an old event. Keep adding to it!
//				}
//				//Is the event over?
//				if (s.Contains(">")){
//					//Search for event enders from the first to last instance of them
//					for (int i = s.IndexOf(">"); i <= s.LastIndexOf(">"); i++){
//						//Be sure we're counting the correct character
//						if (s.ToCharArray()[i].ToString() == ">"){
//							//Reduce the nesting level!
//							nestCount--;
//						}
//						//Otherwise, keep looking.
//					}
//				}
//
//			}
//			addEvent(q, totalNestCount); //Need to add the last item in text
//			//eventList.ForEach(Debug.Log);
//		}
//	}
//
//
//
//
//	private void addEvent(string q, int nest){
//		//Add the event to the list in the hierarchy
//		GameObject newEvent = new GameObject("NewEvent");
//		//Establish parent
//		newEvent.transform.parent = GameObject.Find ("Events").transform;
//		//Add the script
//		targetEvent = newEvent.AddComponent<EventStore>();
//		//Parse the event
//		build(targetEvent, q, nest);
//		//Put it in the List
//		eventList.Add (targetEvent);
//	}
//
//
//
//
//	public EventStore getEvent(){
//		return getEvent(condition.Standard);
//	}
//
//	public EventStore getEvent(condition c){
//		return null;
//	}
//
//
//
//	//Called when this is constructed in script, for top-level events (or just the initial event)
//	public void build(EventTarget, string q, int n){
//		build (target, q, n, false, "");
//	}
//
//
//
//	//The main parser function, called when this is constructed in script. Extra params are for subevents.
//	public void build(EventStore targ, string q, int n, bool nested, string parent){
//		//The counter for build layers
//		int currentNest = 0;
//		//Initialize some values
//		targ.eventText = "";
//		//Let's read it!
//		string s = q; //The whole event text, for processing and adulteration
//
//		//Begin Main Process
//
//		//Set the name and find the event text
//		if ((s = s.TrimStart()).StartsWith("<")){ //It's an event starter!
//			currentNest++; //Add a layer each time we encounter an event starter
//			//Is it a top-level event?
//			if (currentNest == 1){
//				//What's the name?
//				s = s.TrimStart("<".ToCharArray()); //Remove front clutter
//				int uniqueIncrement = 1;	//A value appended to non-unique names
//				//Make name
//				if (!nested){
//					targ.gameObject.name = s.Remove(s.IndexOf("\"")).Trim(); //Listed name
//					//Create something, if unnamed
//					if (targ.gameObject.name == ""){
//						untitledIncrement++;
//						targ.gameObject.name = "UNTITLED_" + untitledIncrement.ToString("D3");
//					}
//					//If there is SOMEHOW has a duplicate name, concat another ident
//					while (uniqueNames.Contains(targ.gameObject.name)){
//						targ.gameObject.name += "(" + uniqueIncrement + ")";
//						uniqueIncrement++;
//					}
//					//Set the parent for any subevents
//					parent = targ.gameObject.name;
//				}
//				else { //Add a subname to denote the subevent, if it is nested.
//					//If parent is an UNTITLED, just use the number
//					string par = parent;
//					if (parent.Contains("UNTITLED")){
//						par = parent.Substring(parent.IndexOf("_") + 1);
//					}
//					targ.gameObject.name = par + "_" + s.Remove(s.IndexOf("\"")).Trim();
//					if (s.Remove(s.IndexOf("\"")).Trim().Equals("")){
//						//Concat a number if no name was provided
//						targ.gameObject.name += n;
//					}
//					//If there is SOMEHOW has a duplicate name, concat another ident
//					while (uniqueNames.Contains(targ.gameObject.name)){
//						targ.gameObject.name += "(" + uniqueIncrement + ")";
//						uniqueIncrement++;
//					}
//				}
//				//Great, it has a name. Add it to the list of names.
//				uniqueNames.Add(targ.gameObject.name);
//			}
//			//Find the event text
//			targ.eventText = quoteHunt(s);
//		}
//
//		
//		
//		//Let's establish the conditions
//		//If it's a subevent, cancel all others
//		if (nested){
//			targ.conditions.Add(condition.SUBEVENT);
//		}
//		//Everything else
//		else {
//			//Like SUBEVENT, cancel further search if it's NEVER
//			if (conditionHunt(s, "never")){
//				targ.conditions.Add(condition.NEVER);
//			}
//			//Actually everything else
//			else {
//				//Yikes, this foreach was a pain. But here it is, in all it's glory. It will cycle through all the possible conditions
//				foreach (condition c in ((condition[]) condition.GetValues(typeof(condition)))){
//					if (conditionHunt(s, c.ToString())){
//						targ.conditions.Add(c);
//					}
//				}
//			}
//		}
//		//If no conditions, condition = Standard!
//		if (targ.conditions.Count == 0){
//			targ.conditions.Add(condition.Standard);
//		}
//		
//		
//		//Time to parse the options and pass them to their own objects
//		//First, parse out how many options there are
//		if (s.Contains ("+")) {
//			//Number used to check event openers
//			int startCount = 0;
//			//Number used to check event enders
//			int endCount = 0;
//			//The string, divided whereever an option is found
//			string[] split = s.Split("+".ToCharArray());
//			//Boolean to see if we've found a valid option
//			//We need to parse the NEXT substring
//			bool valid = false;
//			//Let's math it out
//			foreach (string t in split){
//				//Is it valid? Process it!
//				if (valid){
//					//Add new choice to the hierarchy
//					EventOption eo = GameObject.Find(targ.gameObject.name).AddComponent<EventOption>();
//					//Build up the option
//					eo.masterEvent = targ; //Controlling event, listed on option
//					targ.options.Add(eo); //Also list the slaved option on the event
//					eo.optionText = quoteHunt(t); //Option text
//					//We're done. Let the loop check for the next valid section.
//					valid = false;
//				}
//				//The substring, divided whereever an opener is found
//				string[] starts = t.Split("<".ToCharArray());
//				//The substring, divided whereever an ender is found
//				string[] ends = t.Split(">".ToCharArray());
//				//Count starts
//				for (int i = 0; i < starts.Length; i ++){
//					startCount++;
//				}
//				//Count ends
//				for (int i = 0; i < ends.Length; i ++){
//					endCount++;
//				}
//				/*Check for valid options
//				 *This returns positive on options found at this level, but not sublevels
//				 *This works because of the processing already done on the string (i.e. the loss of the original opener)
//				 *(Don't worry about figuring it out, I landed on it on accident)
//				*/
//				if (startCount == endCount){
//					valid = true;
//				}
//				
//				//Debug.Log("Starts: " + startCount + ", Ends: " + endCount);
//			}
//			//Debug.Log("Number of options: " + optionCount);
//		}
//		//If no options, provide a generic "continue"
//		if (targ.gameObject.GetComponents<EventOption>().Length == 0) {
//			//Add new choice to the hierarchy
//			EventOption eo = GameObject.Find(targ.gameObject.name).AddComponent<EventOption>();
//			//Build up the option
//			eo.masterEvent = targ; //Controlling event, listed on option
//			targ.options.Add(eo); //Also list the slaved options on the event
//			eo.optionText = "Continue"; //Default option text
//		}
//
//
//
//		//Do it again on the sub-event, if any
//		if (n > 1){
//			//Cut out the previous layer of the event, using the unadultered text
//			string t = q.Substring(q.IndexOf("<") + 1);
//			t = t.Substring(t.IndexOf("<"));//So nice, we did it twice (almost)
//			//Add a new event to the hierarchy
//			GameObject newEvent = new GameObject("NewEvent");
//			//Establish the parent
//			newEvent.transform.parent = GameObject.Find(parent).transform;
//			//Add the script
//			targetEvent = newEvent.AddComponent<EventStore>();
//			//Run the sub-event's internal parsing
//			build(targetEvent, t, n - currentNest, true, parent);
//		}
//		effectHunt (q, targ.gameObject);
//		//Finally, establish pointers from options to sub-events
//		linkEvents(targ.gameObject, q);
//	}
//
//
//	
//	//This finds the conditions attached to an event
//	protected bool conditionHunt(string s, string condition){
//		//Let's clear the junk up front
//		s = s.Substring(s.IndexOf("\"") + 1);
//		//Again
//		s = s.Substring(s.IndexOf("\"") + 1);
//		//And the stuff behind it, if any
//		if (s.Contains("+")){
//			s = s.Remove (s.IndexOf ("+"));
//		}
//		//Great, now check if our condition is there
//		if (s.ToLower().Contains (condition.ToLower ())) {
//			return true;
//		}
//		//Got this far? Guess it wasn't there.
//		return false;
//	}
//
//
//
//	//This finds the effects [type, target] attached to an event, and then builds the list
//	protected void effectHunt(string s, GameObject o){
//		//Let's clear the junk up front
//		s = s.Substring(s.IndexOf("\"") + 1);
//		//Again
//		s = s.Substring(s.IndexOf("\"") + 1);
//		//And the stuff behind it, if any
//		if (s.Contains("+")){
//			s = s.Remove (s.IndexOf ("+"));
//		}
//		if (s.Contains("<")){
//			s = s.Remove (s.IndexOf ("<"));
//		}
//		if (s.Contains(">")){
//			s = s.Remove (s.IndexOf (">"));
//		}
//		//Parse it
//		s.ToLower(); //Make it uniform
//		string[] split = s.Split(";".ToCharArray()); //
//		foreach (string t in split) {
//			foreach (effect eff in (effect[])effect.GetValues(typeof(effect))) {
//				//Does it have the effect we're looking for?
//				if (t.Contains(eff.ToString().ToLower())){
//					//Great! Make a new EventEffect
//					EventEffect ee = o.AddComponent<EventEffect>();
//					//Check to see if the chance to trigger is non-default
//					if (t.Contains("0")){ //Default is 1.0f, anything else starts with 0 (i.e., 0.5f)
//						//Isolate the number
//						string x = t.Substring(t.IndexOf("0"));
//						int end = x.LastIndexOfAny(new char[]{'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'});
//						if (end + 1 < x.Length){
//							x = x.Remove(end + 1);
//						}
//						//Check that it's a valid entry
//						if (x.Split (".".ToCharArray()).Length == 2){ //Should be one and only one decimal
//							//Assign the value
//							ee.chance = float.Parse(x);
//						}
//						else {
//							Debug.Log("PARSE ERROR: INVALID CHANCE VALUE");
//						}
//					}
//					//Assign the type
//					ee.type = eff;
//					//Check for targets and assign them
//					foreach (target tar in (target[])target.GetValues(typeof(target))){
//						//Does it have this target?
//						if (t.Contains(tar.ToString().ToLower())){
//							ee.targets.Add(tar);
//						}
//					}
//				}
//			}
//		}
//	}
//
//
//
//	//This parses out the text inside quotes.
//	protected string quoteHunt(string s){
//		if (s.Contains("\"")){ //Redundancy and exception protection
//			//Search for in-game text
//			//Find the opening quotes
//			int firstLocation = s.IndexOf("\"");
//			//Find the closing quotes
//			int lastLocation = s.IndexOf("\"", firstLocation + 1);
//			//Do the closing quotes exist?
//			if (lastLocation < 0){
//				//No! Error!
//				return "PARSE ERROR. CLOSING QUOTATION MARKS NOT FOUND.";
//			}
//			else{
//				//Great! Fix all the misshapen white space. Return the text.
//				s = s.Substring(firstLocation + 1, lastLocation - firstLocation - 1).Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
//				while (s.Contains("  ")){
//					s = s.Replace("  ", " ");
//				}
//				return s;
//			}
//		}
//		else {
//			return "PARSE ERROR. NO IN-GAME TEXT DETECTED.";
//		}
//	}
//
//
//
//	//Point options to the next event
//	protected void linkEvents(GameObject go, string q){ //'go' is the parent event, 'q' is the text of the parent event
//		//Find the available options
//		EventOption[] optionList = go.GetComponentsInChildren<EventOption>();
//		//Find the available subevents
//		EventStore[] subevents = go.GetComponentsInChildren<EventStore> ();
//		//Let's divide and store the string
//		List<string> split = new List<string>();
//		split.AddRange(q.Split("+".ToCharArray()));
//		//Ignore parent's EventStore
//		int se = 1;
//		//Only continue if there's something to do
//		if (optionList.Length > 0){
//			foreach (EventOption ev in optionList){
//				//Only continue if this EventOption doesn't have a reference yet
//				if (ev.nextEvent == null){
//					//We're gonna do a bit more parsing to confirm the links
//
//					//Search split
//					if (split.Count == 1){ //True only when there are no explicit, "+", options
//						if (se >= subevents.Length){
//							//Do nothing, there are no more subevents to process
//							break;
//						}
//						else if (split[0].IndexOf("<") < split[0].IndexOf(">") || (split[0].Contains("<") && !split[0].Contains(">"))){
//							//Assign pointer, that's our nextEvent
//							ev.nextEvent = subevents[se];
//							//Don't assign that event again
//							se++;
//							break;
//						}
//					}
//					else{
//						int currentNest = 0;
//						for (int i = 0; i < split.Count; i++){
//							//Boolean to mark if THIS string has a starter
//							bool starter = false;
//							//Check for starters. Add nesting as appropriate.
//							foreach (char c in split[i].ToCharArray()){
//								if (c.ToString() == "<"){
//									currentNest++;
//									starter = true;
//								}
//							}
//							//Debug.Log("Nest: " + currentNest);
//							//Attempt to assign nextEvent
//							if (se < subevents.Length && ev.nextEvent == null){ //Make sure there are remaining subevents and nextEvent isn't assigned
//								Debug.Log("Allowed to assign.");
//								if (currentNest > 2){
//									Debug.Log ("Too buried.");
//									//There's a subevent here, but it's buried. Best skip it and move to the next one.
//									se++;
//								}
//								//Is this our candidate?
//								if (currentNest == 2 && starter){
//									if (split[i].Contains("sure")){
//										Debug.Log("Incorrect assignment.");
//									}
//									Debug.Log("Next Event assigned: " + subevents[se].eventText + " TO " + ev.optionText);
//									//There's a subevent here! Assign it.
//									ev.nextEvent = subevents[se];
//									//Don't assign that event again
//									se++;
//								}
//							}
//							//Check for enders. Remove nesting as appropriate.
//							foreach (char c in split[i].ToCharArray()){
//								if (c.ToString() == ">"){
//									currentNest--;
//								}
//							}
//						}
//
//						/*
//						//for (int i = EventOption[].IndexOf(optionList, ev); i < string[].IndexOf(split, ************); i++){
//						//foreach (string s in split){
//						int i = EventOption[].IndexOf(optionList, ev) + 1;
//						if (i == 
//						Debug.Log("TRUTH.");
//							if (se >= subevents.Length){
//								//Do nothing, there are no more subevents to process
//								break;
//							}
//							//else if ((s.IndexOf("<") < s.IndexOf(">")) || (s.Contains("<") && !s.Contains(">"))){
//							else if ((split[i].IndexOf("<") < split[i].IndexOf(">")) || (split[i].Contains("<") && !split[i].Contains(">"))){
//								Debug.Log("POINTED.");
//								//There's a subevent here! Assign it.
//								ev.nextEvent = subevents[se];
//								//Don't assign that event again
//								se++;
//								break;
//							}
//						}
//						*/
//					}
//				}
//			}
//		}
//	}
//
//
//
//	// Use this for initialization, make sure stuff is non-null, etc.
//	void Start () {
//		fileText = "";
//		eventList = new List<EventStore>();
//		fileName = "EventList.txt";
//		fileLoc = Application.dataPath + "/" + fileName;
//		//fileLoc = Application.persistentDataPath + "/" + fileName;
//		parse ();
//	}
//
//}
