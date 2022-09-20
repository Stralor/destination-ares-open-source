using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum AlertType
{
	//Except 'Danger', only one active on any object at a time.
	
	Warning, //Low level alert. Get chars to pay attention to stuff they should have already been dealing with (maintenance, etc.).
	Alert, //Medium priority. Used for non-trivial issues with system (broken, etc.) or chars.
	Emergency, //High priority. Get characters to drop what they're doing and handle this situation NOW. May call multiple chars.
	Danger, //High priority negative. Keep crew away. Mark threats (hull breach, etc.).
	Use, //Minimum level request to take action with a system, such as do research or process resources.
	Ignore, //Alert to not repair/ help/ use this target
	Salvage, //Break down a system for resources
}

public class Alert : MonoBehaviour
{

	/* Get characters' attention and get them to do what you want, indirectly
	 * Activated by player on systems, characters, etc. to denote priorities to char AI
	 */


	public Sprite emergencySprite, useSprite, warningSprite, ignoreSprite, salvageSprite;

	//Declarations
	private List<AlertType> activeAlerts = new List<AlertType>();
	//The alerts that the player has signalled
	private List<AlertType> visibleAlerts = new List<AlertType>();
	//The alerts that the crew can see. On a slight delay from activeAlerts, as determined by various time parameters
	private Animation anim;
	//The alert's animation script
	private Image image;
	//The image object
	private Text txt;
	//The text object, child of image
	private bool fadingOut = true;
	//Used to check if the alert is in the process of shutting down (or already has)
	private int currentAlert = 0;
	//The index of the current alert being animated

	AudioSource src;
	float audioDelay = 0;
	float audioDelayBase = 8;
	//private PlayerInteraction pI;	//The parent object's Player Interaction


	/**Add an alert, with optional delay 'time'
	 */
	public void SetAlert(AlertType a, float time = 0)
	{
		//Make sure it isn't already added
		if (!activeAlerts.Contains(a))
		{
			//Add it
			activeAlerts.Add(a);
			//And to visibleAlerts!
			StartCoroutine(AddToVisibleAlerts(a, time));

			//Let it make noise
			audioDelay = 0;
		}
		//Clear any other alerts (if not adding Danger)
		if (a != AlertType.Danger && activeAlerts.Count > 0)
		{
			//Make a list of what to remove
			List<AlertType> deleteThese = new List<AlertType>();
			//Find what to remove
			foreach (AlertType al in activeAlerts)
			{
				//Must only clear alerts other than Danger and the added alert
				if (al != AlertType.Danger && al != a)
				{
					//Add it to the list... to remove
					deleteThese.Add(al);
				}
			}
			//Remove them
			foreach (AlertType al in deleteThese)
			{
				//Remove from active
				activeAlerts.Remove(al);
				//And from visible!
				StartCoroutine(RemoveFromVisibleAlerts(al, time));
			}
		}
	}

	/**Add an alert.
	 */
	public void SetAlert(string s)
	{
		//Gotta compare the string to available Alerts
		foreach (AlertType a in ((AlertType[]) AlertType.GetValues(typeof(AlertType))))
		{
			if (a.ToString().ToLower() == s.ToLower())
			{
				//Found it. Send it. We're done here.
				SetAlert(a);
				break;
			}
		}
	}

	//End an alert
	public void EndAlert(AlertType a, float time = 0)
	{
		//Be sure that alerts contains a
		if (activeAlerts.Contains(a))
		{
			activeAlerts.Remove(a);
		}
		//Also remove from visibleAlerts!
		StartCoroutine(RemoveFromVisibleAlerts(a, time));
	}

	//Alternate, helper method. For inspector use.
	public void EndAlert(string s)
	{
		//Gotta compare the string to available Alerts
		foreach (AlertType a in ((AlertType[]) AlertType.GetValues(typeof(AlertType))))
		{
			if (a.ToString().ToLower() == s.ToLower())
			{
				//Found it. Send it. We're done here.
				EndAlert(a);
				break;
			}
		}
	}

	//Toggle the given alert.
	public void ToggleAlert(AlertType a, float time = 0)
	{
		//Remove it or add it, if it's there or not
		if (activeAlerts.Contains(a))
		{
			EndAlert(a, time);
		}
		else
		{
			SetAlert(a, time);
		}
	}

	//Alternate, helper method. For inspector use.
	public void ToggleAlert(string s, float time = 0)
	{
		foreach (AlertType a in ((AlertType[]) AlertType.GetValues(typeof(AlertType))))
		{
			if (a.ToString().ToLower() == s.ToLower())
			{
				//Found it. Send it. We're done here.
				ToggleAlert(a, time);
				break;
			}
		}
	}

	public List<AlertType> GetVisibleAlerts()
	{
		return visibleAlerts;
	}

	public List<AlertType> GetActivatedAlerts()
	{
		return activeAlerts;
	}

	//Used to clear any alerts
	public void Clear()
	{
		activeAlerts.Clear();
		visibleAlerts.Clear();
		//Also need to reset the UI. Find the buttons that might need clearing.
		List<Animator> anims = new List<Animator>();
		anims.AddRange(transform.parent.GetComponentsInChildren<Animator>());
//		//Turn off "Selected" where present (in buttons)
//		//TODO target only the right buttons, or suppress the fucking warnings from Animator
//		foreach (Animator s in anims){
//			if (s.GetFloat("Selected") != 0)
//				s.SetFloat("Selected", 0);
//		}
	}




	private IEnumerator AddToVisibleAlerts(AlertType alertType, float time)
	{
		yield return new WaitForSeconds(time);
		
		if (!visibleAlerts.Contains(alertType))
			visibleAlerts.Add(alertType);
	}

	private IEnumerator RemoveFromVisibleAlerts(AlertType alertType, float time)
	{
		yield return new WaitForSeconds(time);
		
		if (visibleAlerts.Contains(alertType))
			visibleAlerts.Remove(alertType);
	}


	void Start()
	{
		anim = GetComponentInChildren<Animation>();	//Establish the animation
		image = GetComponentInChildren<Image>();
		txt = GetComponentInChildren<Text>();
		//pI = GetComponentInParent<PlayerInteraction>();

	}



	void Update()
	{
		//Graphical changes based on active alerts!
		//TODO Anims for visibleAlerts vs. active alerts:
		/* Compare active and visible.
			 * If an alert is only present on active, power up (anim for beginning signal).
			 * If an alert is only present on visible, power down (anim for ending signal).
			 * If on both, do full, normal anim.
			 */

//		//Don't do regular animations when this object is clicked on
//		if (pI != null && pI.selected){
//			//Do clear the current queue, if any
//			if (activeAlerts.Count >= 1 && !fadingOut){
//				//Clear the alert
//				anim.CrossFade("AlertFadeOut");
//				txt.color = Color.clear;
//				fadingOut = true;
//			}
//		}
		//Regular alert anims
		if (activeAlerts.Count > 0)
		{
			//Allow this to fade out when alerts drops below 1 again
			fadingOut = false;

			//Keep audioDelay relevant
			audioDelay -= Time.deltaTime;

			//Is it time for something to play?
			if (!anim.isPlaying)
			{
				//Great, do the animation for the next alert in the stack
				//Check to make sure there is a valid alert at the next point in the stack
				if (currentAlert >= activeAlerts.Count)
				{
					//If not, reset the index
					currentAlert = 0;
				}
				//Change the color and text to be used for the alert
				switch (activeAlerts [currentAlert])
				{
				case AlertType.Warning:
					image.sprite = warningSprite;
						//image.color = ColorPalette.cp.yellow3;
					txt.text = "Warning";
					PrepAudioSource("WarningAlert");
					break;
				case AlertType.Alert:
						//image.color = ColorPalette.cp.yellow3;
					txt.text = "Alert";
					break;
				case AlertType.Emergency:
					image.sprite = emergencySprite;
						//image.color = ColorPalette.cp.red3;
					txt.text = "Emergency";
					PrepAudioSource("EmergencyAlert");
					break;
				case AlertType.Danger:
						//image.color = ColorPalette.cp.blue3;
					txt.text = "Danger";
					break;
				case AlertType.Ignore:
					image.sprite = ignoreSprite;
					txt.text = "Ignore";
					//Set src to null so we don't play other alert's SFX
					src = null;
					break;
				case AlertType.Use:
					image.sprite = useSprite;
						//image.color = ColorPalette.cp.blue3;
					txt.text = "Use";
					PrepAudioSource("UseAlert");
					break;
				case AlertType.Salvage:
					image.sprite = salvageSprite;
					txt.text = "Salvage";
					break;
				}
				//Play the audio
				if (src != null && src.gameObject.activeSelf && src.transform.IsChildOf(transform) && audioDelay <= 0)
				{
					audioDelay = audioDelayBase;
					src.priority = 100;
					src.Play();
				}

				//Set the image color, the alert text, and the text's visibility
				txt.color = ColorPalette.cp.wht;
				GetComponentInChildren<CanvasGroup>().alpha = 1;
				//Play the fading animation
				anim.Play("AlertFadeIn");
				currentAlert++;
			}
		}
		//Shut down, if not doing so already.
		else if (!fadingOut)
		{
			//Clear the alert
			anim.CrossFade("AlertFadeOut");
			txt.color = Color.clear;
			fadingOut = true;

			//Kill the audio, if it's here
			if (src != null && src.gameObject.activeSelf && src.transform.IsChildOf(transform))
			{
				src.Stop();
			}
		}
	}

	void PrepAudioSource(string clipName)
	{
		//If we don't have a valid one, get it
		if (src == null || !src.gameObject.activeSelf || !src.transform.IsChildOf(transform))
			src = AudioClipOrganizer.aco.GetSourceForCustomPlay(clipName: clipName, parent: transform);
		//Otherwise, prep the one we got
		else
			src.clip = AudioClipOrganizer.aco.GetClip(clipName);
	}

	void OnEnable()
	{
		if (JobAssignment.ja != null && JobAssignment.ja.allPossibleAlerts != null && !JobAssignment.ja.allPossibleAlerts.Contains(this))
			JobAssignment.ja.allPossibleAlerts.Add(this);
	}

	void OnDisable()
	{
		if (JobAssignment.ja != null && JobAssignment.ja.allPossibleAlerts != null && JobAssignment.ja.allPossibleAlerts.Contains(this))
			JobAssignment.ja.allPossibleAlerts.Remove(this);
	}

}
