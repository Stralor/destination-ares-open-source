using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioController : MonoBehaviour
{
	/**Static ref. */
	public static AudioController aud;

	/**The main mixer board. */
	public AudioMixer mixer;

	public float snapshotTransitionTime;

	//The snapshots
	AudioMixerSnapshot standard, commsOff, paused;

	public bool commsEnabled { get; private set; }


	/*
	 * PUBLIC METHODS
	 */

	/// <summary>
	/// Sets the master volume.
	/// </summary>
	/// <param name="vol">Volume</param>
	public void SetMasterVolume(float vol)
	{
		mixer.SetFloat("vol_Master", vol);
		PlayerPrefs.SetFloat("vol_Master", vol);
	}

	/// <summary>
	/// Sets the music volume.
	/// </summary>
	/// <param name="vol">Volume</param>
	public void SetMusicVolume(float vol)
	{
		mixer.SetFloat("vol_Music", vol);
		PlayerPrefs.SetFloat("vol_Music", vol);
	}

	/// <summary>
	/// Sets the total SFX volume.
	/// </summary>
	/// <param name="vol">Volume</param>
	public void SetEffectsVolume(float vol)
	{
		mixer.SetFloat("vol_AllSFX", vol);
		PlayerPrefs.SetFloat("vol_AllSFX", vol);
	}

	/// <summary>
	/// Sets the game sounds volume.
	/// </summary>
	/// <param name="vol">Volume</param>
	public void SetGameSoundsVolume(float vol)
	{
		mixer.SetFloat("vol_GameSFX", vol);
		PlayerPrefs.SetFloat("vol_GameSFX", vol);
	}

	/// <summary>
	/// Sets the user interface volume.
	/// </summary>
	/// <param name="vol">Volume</param>
	public void SetUIVolume(float vol)
	{
		mixer.SetFloat("vol_UI", vol);
		PlayerPrefs.SetFloat("vol_UI", vol);
	}

	public void TransitionToStandard()
	{
		standard.TransitionTo(snapshotTransitionTime);
		commsEnabled = true;
	}

	public void TransitionToCommsOff()
	{
		commsOff.TransitionTo(snapshotTransitionTime);
		commsEnabled = false;
	}

	public void TransitionToPaused()
	{
		paused.TransitionTo(snapshotTransitionTime);
	}

	public void TransitionFromPaused()
	{
		if (commsEnabled)
			TransitionToStandard();
		else
			TransitionToCommsOff();
	}


	/*
	 * PRIVATE AND UTILITY METHODS
	 */

	void Update()
	{
		//Check for automatic commsOff transition
		bool noEnabledCommsFound = ShipResources.res != null && GameReference.r != null && !GameReference.r.allSystems.Exists(obj => obj.function == ShipSystem.SysFunction.Communications && obj.status != ShipSystem.SysStatus.Disabled);
		if (commsEnabled && noEnabledCommsFound)
			TransitionToCommsOff();
		//Otherwise, we'll want to check for the opposite
		else if (!commsEnabled && !noEnabledCommsFound)
			TransitionToStandard();
		
	}

	void Start()
	{
		commsEnabled = true;
		//Cache
		standard = AudioController.aud.mixer.FindSnapshot("Standard");
		commsOff = AudioController.aud.mixer.FindSnapshot("Comms Off");
		paused = AudioController.aud.mixer.FindSnapshot("Paused");


	}

	void Awake()
	{
		//Setup static ref
		if (aud == null)
		{
			aud = this;
			DontDestroyOnLoad(gameObject);
		}
		if (aud != this)
			Destroy(gameObject);
	}
}
