using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShipSystemAudio : MonoBehaviour
{

	ShipSystem sys;
	PlayerInteraction pI;

	ShipSystem.SysCondition lastCondition;

	AudioClip running;

	AudioSource aud_InUse;

	/**Should this audio be constantly running? */
	bool isConstant
	{
		get
		{
			//Quick and easy false
			if (sys.isPassive)
				return false;

			//Actual check
			switch (sys.function)
			{
			case ShipSystem.SysFunction.Engine:
				return true;
			case ShipSystem.SysFunction.Generator:
				return true;
			case ShipSystem.SysFunction.Reactor:
				return true;
			case ShipSystem.SysFunction.Scrubber:
				return true;
			default:
				return false;
			}
		}
	}




	void Start()
	{
		sys = GetComponent<ShipSystem>();
		pI = GetComponent<PlayerInteraction>();

		//Find the appropriate audio file for this system.
		running = AudioClipOrganizer.aco.GetClip(sys.function.ToString().ToLower());
	}

	void Update()
	{
		//Running audio, if there's a clip and it's time
		if (sys.inUse && running)
		{
			//Be sure we have a valid source (not null, active, child of this)
			if (aud_InUse == null || !aud_InUse.gameObject.activeSelf || !aud_InUse.transform.IsChildOf(transform))
			{
				//Claim the source
				aud_InUse = AudioClipOrganizer.aco.GetSourceForCustomPlay(running, parent: transform);
				//Custom settings
				if (isConstant)
					aud_InUse.loop = true;
			}

			//Start the sound, either constant or when toggled
			if (!aud_InUse.isPlaying && (sys.toggleAudio || isConstant))
			{
				//Doing the audio
				sys.toggleAudio = false;

				//Be sure AdjustPitch always gets played on new plays
				AdjustPitch(aud_InUse);
				//Play
				aud_InUse.Play();
				//Start coroutine to return
				StartCoroutine(AudioClipOrganizer.aco.ReturnToPoolWhenNotPlaying(aud_InUse));
			}

			if (!pI.highlighted && !pI.selected)
				aud_InUse.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Unfocused") [0];
			else
				aud_InUse.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Important") [0];
		}
		//End running audio
		else if (aud_InUse != null && aud_InUse.isPlaying)
		{
			//Revert custom settings
			aud_InUse.loop = false;
			//Stop
			aud_InUse.Stop();
			//Clear
			aud_InUse = null;
		}

		//TODO breakdown sounds, etc.
	}

	void AdjustPitch(AudioSource src)
	{
		if (sys.condition == ShipSystem.SysCondition.Strained)
				//Give strained systems some whine
				src.pitch = Random.Range(1.15f, 1.3f);
		else
				//Otherwise make it normal
				src.pitch = Random.Range(0.95f, 1.05f);
	}
}
