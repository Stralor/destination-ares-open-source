using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class CameraEffectsController : MonoBehaviour
{

	/**Singleton reference */
	public static CameraEffectsController cec;

	//Declarations

	//How fast does the camera move when taking input?
	public float cameraMoveSpeed;
	//Is the camera currently rotating?
	private bool rotating = false;
	//Values used by rotation code
	private float currentAngle, targetAngle, currentTime;
	//How far can the camera turn?
	public float angleLimit;
	//How far the camera can move
	public float scrollWidth, scrollHeight;
	public bool canMove;
	//How far can the camera zoom in and out?
	public float zMax, zMin;
	//Mouse drag origin for camera
	private Vector3 dragOrigin;

	private float invertLerp = 0;

	//Cache

	//Parent's RectTransform, for boundary size controls.
	private RectTransform parent;
	//The pin on which the parent's RectTransform must stay connected to. Move this, move the boundary!
	private RectTransform scrollRect;
	//Contrast/ Saturation Controls
	private ColorCorrectionCurves ccc;
	//Blur effect
	private VignetteAndChromaticAberration vca;
	private ColorLerpByProgress clerp;
	//FlashRed effect
	private ScreenOverlay overlay;
	//Damage Text
	public GameObject damageText;

	//Constants
	const float CONTRAST_RATIO = 0.1f;


	//METHODS

	/**Add 'value' of blur the the screen. Blur will slowly dissipate.
	 */
	public void BlurScreen(float value)
	{
		damageText.SetActive(true);
		vca.blur = vca.blur + value < 0 ? 0 : vca.blur + value;
		//Begin reducing blur!
		StartCoroutine(ReduceBlur(1));
		//Invoke("ReduceBlur", 1);
	}

	IEnumerator ReduceBlur(float time)
	{
		yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(time));
		vca.blur = vca.blur - 0.01f < 0 ? 0 : vca.blur - 0.01f;
		//Continue reducing blur
		if (vca.blur > 0)
			StartCoroutine(ReduceBlur(0.075f));
		else
			damageText.SetActive(false);
	}

	/**Set the screen overlay, turn it on, then call to turn it off.
	 */
	public void FlashRed()
	{
		overlay.blendMode = ScreenOverlay.OverlayBlendMode.ScreenBlend;
		overlay.intensity = 0.3f;
		overlay.enabled = true;
		Invoke("FadeOverlay", 0.02f);
	}

	void FadeOverlay()
	{

		overlay.intensity -= 0.01f;

		if (overlay.intensity <= 0)
		{
			overlay.intensity = 0;
			overlay.enabled = false;
		}
		else
			Invoke("FadeOverlay", 0.02f);
	}

	/**Adjust camera angle to match current speed!
	 * (The parent object is rotated, so that the camera stays focused on the same point)
	 */
	void RotateCamera()
	{
		//Only do this when we can access ship res
		if (ShipResources.res != null)
		{
			//Add time
			currentTime += Time.deltaTime;
			
			//Reset values (find new current angle, new target angle, reset time)
			if (!rotating)
			{
				//Set target angle value
				DetermineAnglesForRotation(ShipResources.res.speed);
			
				currentTime = 0;
				rotating = true;
			}
			
			//Find rotation value!
			float rotation = Mathf.LerpAngle(currentAngle, targetAngle, currentTime);
			//Rotate! Rotate in opposite direction, to move into the bow.
			transform.parent.eulerAngles = new Vector3(0, rotation, 0);
			
			//Finished rotating? Reset it!
			if (currentTime >= 1)
				rotating = false;
		}
	}

	/**Sets current and target angles */
	void DetermineAnglesForRotation(float speed)
	{
		currentAngle = transform.parent.eulerAngles.y;
	
		//Get the raw angle we want from the speed (unvectorized)
		float rawAngle = Mathf.Sqrt(Mathf.Abs(speed));
		//Set direction based on speed's direction (inverted, so to rotate TOWARDS bow when moving forward)
		if (ShipResources.res.speed < 0)
			targetAngle = rawAngle;
		else
			targetAngle = -rawAngle;

		//Clamp to within bounds of angle (positive and negative)
		targetAngle = targetAngle > angleLimit ? angleLimit : targetAngle = targetAngle < -angleLimit ? -angleLimit : targetAngle;
	}

	public void InstantCameraRotation()
	{
		rotating = false;

		DetermineAnglesForRotation(ShipResources.res.speed);

		//Go straight to target angle in this one
		transform.parent.eulerAngles = new Vector3(0, targetAngle, 0);
	}

	public void SetCameraPosition(Vector2 newPos)
	{
		parent.position = newPos;
	}

	void Update()
	{
		if (canMove)
		{
			//First, adjust the bounds based on current zoom!
			parent.sizeDelta = new Vector2((scrollWidth + parent.position.z / 2) * 4, (scrollHeight + parent.position.z / 2) * 2);
			
			//We also need to adjust the center of the bounds, based on the angle of the camera
			float camAngle = parent.eulerAngles.y;
			if (camAngle >= 180)
				camAngle -= 360;
			
			scrollRect.position = new Vector2(camAngle / 4.5f, 0);	//Fudged, because otherwise trig
			
			//Get values
			float horizontal = Input.GetAxisRaw("Horizontal") * cameraMoveSpeed * Time.unscaledDeltaTime;
			float vertical = Input.GetAxisRaw("Vertical") * cameraMoveSpeed * Time.unscaledDeltaTime;
			float zoom = Input.GetAxisRaw("Zoom/ Scroll") * cameraMoveSpeed * Time.unscaledDeltaTime;
			
			//Do it!
			parent.Translate(new Vector3(horizontal, vertical, zoom), Space.World);
			
			//Drag camera controls
			//Set origin for new drag
			if (Input.GetMouseButtonDown(0))
			{
				dragOrigin = Input.mousePosition;
			}
			//Do movement
			if (Input.GetMouseButton(0))
			{
				Vector3 mousePos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
				parent.Translate(new Vector3(-mousePos.x * cameraMoveSpeed / 2, -mousePos.y * cameraMoveSpeed / 2, 0), Space.World);
				dragOrigin = Input.mousePosition;
			}

			//Now find the maximum zoom out, such that the whole ship can be seen
			zMin = -(scrollWidth > scrollHeight * 2 ? scrollWidth : scrollHeight * 2) - 5;

			//Limit zoom level
			if (parent.position.z > zMax)
				parent.position = new Vector3(parent.position.x, parent.position.y, zMax);
			else if (parent.position.z < zMin)
				parent.position = new Vector3(parent.position.x, parent.position.y, zMin);
			
			//Rotate the camera!
			RotateCamera();
		}

		//Automated Adjustments (aka, Saturation and Contrast)
		AutomatedAdjustments();
	}

	void Start()
	{
		parent = transform.parent.GetComponent<RectTransform>();
		scrollRect = GameObject.Find("Camera Scroll Rect").GetComponent<RectTransform>();
		ccc = GetComponent<ColorCorrectionCurves>();
		vca = GetComponent<VignetteAndChromaticAberration>();
		clerp = GetComponent<ColorLerpByProgress>();
		overlay = GetComponent<ScreenOverlay>();

		//Until told otherwise:
		canMove = true;

		//Initial invertLerp state
		invertLerp = EventSpecialConditions.c.dark_truthRealized ? 1 : 0;

		AutomatedAdjustments();
	}

	/**Adjust Saturation and Contrast by in-game Progress
	 */
	void AutomatedAdjustments()
	{
		//Color lerp only if we're going far
		if (clerp != null)
			clerp.enabled = ShipResources.res != null && ShipResources.res.startingDistance > 50000;

		//Only do it for certain minimum distances
		if (ShipResources.res != null && ShipResources.res.startingDistance > 50000)
		{
			//Check progress, as a unit value
			float prog = (float)ShipResources.res.progress / 100;

			float baseSaturation = 1,
			lowChannel = 0.15f - (prog * CONTRAST_RATIO * 3),
			highChannel = 0.95f + (prog * CONTRAST_RATIO);

			//Change saturation!
			if (prog <= 0.7f)
			{
				ccc.saturation = baseSaturation = (-prog * 10f / 7f) + 1f;
			}
			else
			{
				ccc.saturation = baseSaturation = (prog - 0.7f) * 4f;
			}

			//Change contrast!
			if (prog <= 0.5f)
			{
				ccc.blueChannel.MoveKey(0, new Keyframe(0, lowChannel));
				ccc.blueChannel.MoveKey(1, new Keyframe(1, highChannel));
				ccc.greenChannel.MoveKey(0, new Keyframe(0, lowChannel));
				ccc.greenChannel.MoveKey(1, new Keyframe(1, highChannel));
				ccc.redChannel.MoveKey(0, new Keyframe(0, lowChannel));
				ccc.redChannel.MoveKey(1, new Keyframe(1, highChannel));
				ccc.blueChannel.SmoothTangents(0, 0);
				ccc.blueChannel.SmoothTangents(1, 0);
				ccc.greenChannel.SmoothTangents(0, 0);
				ccc.greenChannel.SmoothTangents(1, 0);
				ccc.redChannel.SmoothTangents(0, 0);
				ccc.redChannel.SmoothTangents(1, 0);
				ccc.UpdateParameters();
			}

			//Special saturation
			if (EventSpecialConditions.c.dark_truthRealized)
			{
				ccc.saturation = Mathf.Lerp(baseSaturation, -5, invertLerp);

				float curveVal = -0.7f,
				invertLowChannel = 0.15f - curveVal,
				invertHighChannel = 0.95f + curveVal,

				lowChannelLerp = Mathf.Lerp(lowChannel, invertLowChannel, invertLerp),
				highChannelLerp = Mathf.Lerp(highChannel, invertHighChannel, invertLerp);

				ccc.blueChannel.MoveKey(0, new Keyframe(0, lowChannelLerp));
				ccc.blueChannel.MoveKey(1, new Keyframe(1, highChannelLerp));
				ccc.greenChannel.MoveKey(0, new Keyframe(0, lowChannelLerp));
				ccc.greenChannel.MoveKey(1, new Keyframe(1, highChannelLerp));
				ccc.redChannel.MoveKey(0, new Keyframe(0, lowChannelLerp));
				ccc.redChannel.MoveKey(1, new Keyframe(1, highChannelLerp));
				ccc.blueChannel.SmoothTangents(0, 0);
				ccc.blueChannel.SmoothTangents(1, 0);
				ccc.greenChannel.SmoothTangents(0, 0);
				ccc.greenChannel.SmoothTangents(1, 0);
				ccc.redChannel.SmoothTangents(0, 0);
				ccc.redChannel.SmoothTangents(1, 0);
				ccc.UpdateParameters();

				invertLerp = invertLerp > 1 ? 1 : invertLerp + Time.unscaledDeltaTime / 10;
			}

		}

		//Aberration on time dilation
		vca.chromaticAberration = Time.timeScale <= GameClock.PAUSE_SPEED ? -3 : Mathf.Pow(Time.timeScale - 1, 2);
	}


	void Awake()
	{
		cec = this;
	}

}
