using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**Requires GenericTooltipPool and a prefab in Resources called "Tooltip" with an Image and a Text as children.*/
public class GenericTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

	public enum Direction
	{
		Above,
		Below,
		Left,
		Right
	}

	public Direction direction = Direction.Left;
	public float offset = 0.5f;
	public string tooltipTitle;
	[TextArea]
	public string
		tooltipText;

	[Tooltip("If true (default), will child the tooltip to this gameObject. Doesn't work well with layout groups, so turn it off for those.")]
	public bool
		setParent = true;
	[Tooltip("If true, will set the local scale to 1 on spawn. Use when having natural scaling issues.")]
	public bool resetScale = false;
	[Tooltip("Is part of an overlay screen: should disappear when overlay is gone. If not, shouldn't appear when overlay is present.")]
	public bool
		overlayTip;
	public bool useIPointerCalls = true;
	public bool useOnMouseEnter = false;
	/**Please only give one prerequisite. Only the last delegate gets checked in Funcs. */
	public System.Func<bool> prerequisiteToOpen;
	public System.Action onFadedIn, onFadedOut;

	[Tooltip("Ignore the GenericTooltipPool's set values.")]
	public bool
		overridePoolValues;
	public float waitDelay, fadeIn, fadeOut;

	public bool activeTip { get; private set; }

	[HideInInspector]
	public bool
		lockedFromOpenClose;

	private GameObject currentTooltip;
	private Image panel;
	private Text text;
	private List<GenericTooltip> parents = new List<GenericTooltip>();

	/*
	 * PUBLIC CALLS
	 */

	/**Call the tooltip! Puts it adjacent to the the object (in 'direction'), as a child.
     */
	public void OpenTooltip()
	{
		//Make sure we can see it (Tooltips allowed, not locked, correct layer)
		if (PlayerPrefs.GetInt("Tooltips") > 0 && !lockedFromOpenClose && (GameReference.r == null || overlayTip || !GameReference.r.overlayActive))
		{
			if (currentTooltip == null)
				currentTooltip = GenericTooltipPool.GetFreshTooltip();
			else
				currentTooltip.SetActive(true);

			panel = currentTooltip.GetComponentsInChildren<Image>().ToList().Find(obj => obj.name == "Panel");
			text = currentTooltip.GetComponentInChildren<Text>();

			//Position
			currentTooltip.transform.SetParent(transform);
			if (resetScale)
			{
				currentTooltip.transform.localScale = Vector3.one;
			}

			SetPosition(Vector3.zero);

			//Override parent and rotation for GUI Main tips
			if (!setParent)
			{
				var GUIMain = GameObject.Find("GUI Main");
				if (GUIMain)
					currentTooltip.transform.SetParent(GUIMain.transform);
				else
					currentTooltip.transform.SetParent(transform.root);
				currentTooltip.transform.localEulerAngles = new Vector3(0, Camera.main.transform.eulerAngles.y, 0);
			}

			//Content
			UpdateText();

			//Alpha
			panel.CrossFadeAlpha(0, 0, true);	//Reset
			text.CrossFadeAlpha(0, 0, true);	//Reset
			StopCoroutine("FadeOut");
			StopCoroutine("ReturnTooltip");
			StartCoroutine("FadeIn", waitDelay);
		}
	}

	/**Return the tooltip to the pool.
	 */
	public void CloseTooltip()
	{
		if (!lockedFromOpenClose)
		{
			StopCoroutine("FadeIn");
			StartCoroutine("FadeOut", waitDelay);
			StartCoroutine("ReturnTooltip", fadeOut + waitDelay);
		}
	}

	/**Used when you've got a border you want on/off (and probably flashing)
	 */
	public void ToggleBorder(bool enabled)
	{
		currentTooltip.GetComponent<Image>().enabled = enabled;
	}


	/*
	 * FUNCTIONAL
	 */

	void SetPosition(Vector3 startingLocal)
	{
		if (currentTooltip != null)
		{
			RectTransform rect = currentTooltip.GetComponent<RectTransform>();

			switch (direction)
			{
			case Direction.Above:
				rect.pivot = new Vector2(0.5f, 0);
				currentTooltip.transform.localPosition = new Vector3(startingLocal.x, startingLocal.y + offset, 0);
				break;
			case Direction.Below:
				rect.pivot = new Vector2(0.5f, 1);
				currentTooltip.transform.localPosition = new Vector3(startingLocal.x, startingLocal.y - offset, 0);
				break;
			case Direction.Left:
				rect.pivot = new Vector2(1, 0.5f);
				currentTooltip.transform.localPosition = new Vector3(startingLocal.x - offset, startingLocal.y, 0);
				break;
			case Direction.Right:
				rect.pivot = new Vector2(0, 0.5f);
				currentTooltip.transform.localPosition = new Vector3(startingLocal.x + offset, startingLocal.y, 0);
				break;
			}
		}
	}

	void UpdateText()
	{
		if (currentTooltip != null)
		{
			//Clear
			text.text = "";
			//Title
			if (tooltipTitle.Trim() != "")
			{
				text.text += "<size=16>" + tooltipTitle + "</size>";

				//Space for body
				if (tooltipText != "")
					text.text += "\n";
			}
			
			//Body
			if (tooltipText != "")
				text.text += tooltipText;

			//Color
			text.text = ColorText(text.text);
		}
	}

	string ColorText(string colorMe)
	{
		//Patterns (Regex...)
		string bracketPattern = @".\[.+\]";
		string plusPattern = @"[^\w]\+\w+\b( \(\d+\))*";
		string minusPattern = @"[^\w]\-\w+\b( \(\d+\))*";
				
		//Color Changes
		IterateColor(ref colorMe, bracketPattern, ColorPalette.cp.yellow4);
		IterateColor(ref colorMe, plusPattern, ColorPalette.cp.blue4);
		IterateColor(ref colorMe, minusPattern, ColorPalette.cp.red4);

		return colorMe;
	}

	void IterateColor(ref string colorMe, string pattern, Color color)
	{
		//Do it for each match found
		int offset = 0;
		foreach (Match match in Regex.Matches(colorMe, pattern))
		{
			//Start color change at beginning of match. Skip the first character since we use it to sort
			colorMe = colorMe.Insert(match.Index + offset + 1, "<color=#" + ColorPalette.ColorToHex(color) + ">");
			//Finish color change at end of match first
			colorMe = colorMe.Insert(match.Index + match.Length + 15 + offset, "</color>");
			//Total offset for the added characters
			offset += 23;
		}
	}

	IEnumerator FadeIn(float time)
	{
		//Earliest point the tip is active and needs text updates
		activeTip = true;

		//Audio
		if (AudioClipOrganizer.aco)
		{
			AudioClipOrganizer.aco.PlayAudioClip("hover", null);
		}

		yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(time));

		if (panel && text)
		{
			text.CrossFadeAlpha(1, fadeIn, true);       //Fade
			panel.CrossFadeAlpha(0.75f, fadeIn, true);  //Fade
		}

		yield return new WaitForSecondsRealtime(fadeIn);

		if (onFadedIn != null)
			onFadedIn.Invoke();
	}

	IEnumerator FadeOut(float time)
	{
		yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(time));

		if (panel && text)
		{
			text.CrossFadeAlpha(0, fadeOut, true);   //Fade
			panel.CrossFadeAlpha(0, fadeOut, true);  //Fade
		}

		yield return new WaitForSecondsRealtime(fadeOut);

		if (onFadedOut != null)
			onFadedOut.Invoke();
	}

	IEnumerator ReturnTooltip(float time)
	{
		yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(time));

		DirectReturnTooltip();
	}

	void DirectReturnTooltip()
	{
		GenericTooltipPool.ReturnTooltip(currentTooltip);
		currentTooltip = null;

		//Latest point the tip is active and can cease text updates
		activeTip = false;
	}


	/*
	 * AUTOMATIC CALLS
	 */


	public void OnPointerEnter(PointerEventData data)
	{
		if (useIPointerCalls)
		{
			if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
				OpenTooltip();
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		if (useIPointerCalls)
		{
			CloseTooltip();
		}
	}

	//Will call the tooltip, if appropriate
	void OnMouseEnter()
	{
		if (useOnMouseEnter)
		{
			if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
				OpenTooltip();
		}
	}

	//Will update the tooltip text while still active. Also does a safety open call.
	void OnMouseOver()
	{
		if (useOnMouseEnter)
		{
			if (currentTooltip == null)
			{
				if (prerequisiteToOpen == null || prerequisiteToOpen.Invoke())
					OpenTooltip();
			}

			//Sync to mouse now for mouse control NEEDS WORK
			//SetPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			//Keep updated
			UpdateText();
		}
	}

	//Will return the tooltip, if appropriate
	void OnMouseExit()
	{
		if (useOnMouseEnter)
		{
			CloseTooltip();
		}
	}

	void Update()
	{
		if (activeTip)
		{
			parents.ForEach((tt) => tt.CloseTooltip());
		}
	}

	void Start()
	{
		if (!overridePoolValues)
		{
			waitDelay = GenericTooltipPool.waitDelay;
			fadeIn = GenericTooltipPool.fadeIn;
			fadeOut = GenericTooltipPool.fadeOut;
		}

		foreach (var t in GetComponentsInParent<GenericTooltip>())
		{
			if (t != this)
			{
				parents.Add(t);
			}
		}
	}

	void OnDisable()
	{
		if (activeTip)
			DirectReturnTooltip();
	}
}
