using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicController : MonoBehaviour
{

	/**Singleton ref!
	 */
	public static MusicController mc;

	//The songs
	public AudioClip opening, theme, setup, journey0, journey0amb, journey1, journey1amb, journey2, journey2amb, journey3, journey3amb, victory, defeat, eventStandard, eventStory, eventHelpful;
	public List<AudioClip> extras = new List<AudioClip>();

	/**The currently playing music source object. It plays the clips. */
	public AudioSource musicPlayer;

	public float musicSourceVolume = 0.1f;

	/**The current playlist! Changing this will change what songs are played next.*/
	public List<AudioClip> playlist = new List<AudioClip>(), interruptedPlaylist = new List<AudioClip>();

	public List<AudioSource> allMusicPlayers = new List<AudioSource>();
	AudioSource queuedPlayer, interruptedPlayer;

	int autoPlayFrameWait = 0;

	public float realTimeSinceLastSong = 0;

	/**Change the music directly! (Use a clip from this class) This plays this clip ASAP, without affecting the playlist.
	 */
	public void SetSong(AudioClip clip)
	{
		//Check that it isn't just the same song currently playing, and clip isn't null
		if (!VetClip(clip))
			return;

		if (musicPlayer == null)
			musicPlayer = GetMusicPlayer();

		//Play immediately if no music is playing
		if (!musicPlayer.isPlaying)
		{
			musicPlayer.clip = clip;
			musicPlayer.Play();
		}
		//Cool, let's fade between the two tracks.
		else
		{
			//Get another player
			var newPlayer = GetMusicPlayer();

			//Add clip
			newPlayer.clip = clip;

			//Crossfade
			Crossfade(newPlayer);
		}
	}

	/**Queue a song to play after the current one. Doesn't affect the playlist.
	 */
	public void QueueSong(AudioClip clip)
	{
		//Check that it isn't just the same song currently playing, and clip isn't null
		if (!VetClip(clip))
			return;
		
		if (musicPlayer == null)
			musicPlayer = GetMusicPlayer();

		//QueueSong is only applicable when music player is already playing
		if (!musicPlayer.isPlaying)
		{
			SetSong(clip);
			return;
		}

		//Get new player
		var newPlayer = GetMusicPlayer();

		//Set clip
		newPlayer.clip = clip;

		//Use Crossfade to play
		Crossfade(newPlayer, 0, true);
	}

	public void InterruptCurrentSong(AudioClip clip)
	{
		//Just do SetSong if there's nothing to interrupt
		if (musicPlayer == null || !musicPlayer.isPlaying)
		{
			SetSong(clip);
			return;
		}

		//Vet the clip
		if (!VetClip(clip))
			return;

		//Clear any old interruptedPlayer, just to be sure
		if (interruptedPlayer)
			interruptedPlayer.Stop();

		//Mark the interrupted player
		interruptedPlayer = musicPlayer;

		//Get and set new player
		var newPlayer = GetMusicPlayer();
		newPlayer.clip = clip;

		//Update the playlist
		interruptedPlaylist = playlist;
		playlist = new List<AudioClip>(){ clip };

		//Crossfade!
		Crossfade(newPlayer);
	}

	public void ResumeInterruptedSong()
	{
		if (interruptedPlayer && !interruptedPlayer.isPlaying)
		{
			Crossfade(interruptedPlayer);
			playlist = interruptedPlaylist;
		}
	}

	void Crossfade(AudioSource newPlayer, float fadeTime = 1.0f, bool waitUntilEnd = false)
	{
		StartCoroutine(CrossfadeIterator(newPlayer, fadeTime, waitUntilEnd));
	}

	IEnumerator CrossfadeIterator(AudioSource newPlayer, float fadeTime = 1.0f, bool waitUntilEnd = false)
	{
		//New song into the queue (even if it's immediate)
		queuedPlayer = newPlayer;

		//Delay
		if (waitUntilEnd)
			yield return new WaitUntil(() => !musicPlayer.isPlaying || musicPlayer.time >= musicPlayer.clip.length - fadeTime || queuedPlayer != newPlayer);

		//Get out if this isn't the queued player anymore (maybe something else came along while it was waiting)
		if (newPlayer != queuedPlayer)
			yield break;

		//Start playing
		newPlayer.Play();

		//Lerp value
		float time = 0;

		//The lerp
		do
		{
			//More time
			time += Time.unscaledDeltaTime;

			//Lerp vol
			if (fadeTime != 0)
			{
				musicPlayer.volume = Mathf.Lerp(musicSourceVolume, 0, time / fadeTime);
				newPlayer.volume = musicSourceVolume - musicPlayer.volume;

				//Iterate
				yield return null;
			}
			//No lerp vol
			else
				newPlayer.volume = musicSourceVolume;
		}
		while (time < fadeTime);

		//New current player
		musicPlayer = newPlayer;

		//Reset framewait
		autoPlayFrameWait = 0;

		//Wrap up by shutting off any other audios
		foreach (var t in allMusicPlayers)
		{
			if (t != musicPlayer && t.isPlaying)
			{
				if (t == interruptedPlayer)
					t.Pause();
				else
					t.Stop();
			}
		}
	}

	AudioSource GetMusicPlayer()
	{
		AudioSource player;

		//Find one
		if (allMusicPlayers.Exists(obj => !obj.isPlaying && obj != interruptedPlayer))
			player = allMusicPlayers.Find(obj => !obj.isPlaying && obj != interruptedPlayer);
		//Or make one
		else
		{
			//Instantiate
			var go = Instantiate(Resources.Load("AudioSrc")) as GameObject;
			player = go.GetComponent<AudioSource>();

			//Settings
			go.transform.SetParent(transform);
			go.name = "Music Player";
			player.outputAudioMixerGroup = AudioController.aud.mixer.FindMatchingGroups("Music") [0];
			player.loop = false;
			player.spatialBlend = 0;
			player.volume = musicSourceVolume;
			player.priority = 0;

			//Add to allMusicPlayers
			allMusicPlayers.Add(player);
		}

		return player;
	}

	/**Check if this is a valid clip to play and a valid time to play it */
	bool VetClip(AudioClip clip)
	{
		//Check that it isn't just the same song currently playing, and clip isn't null
		return clip && !(musicPlayer && musicPlayer.isPlaying && !musicPlayer.mute && musicPlayer.clip == clip);
	}


	void Update()
	{
		//Play music if it stops for awhile
		if (musicPlayer != null && !musicPlayer.isPlaying)
		{
			//Waiting
			realTimeSinceLastSong += Time.unscaledDeltaTime;

			//Be sure we've haven't just been waiting for the player to start -- OLD, IDGAF, LEAVING IT. No one is hurt by a few extra frames after a couple minutes already waiting
			autoPlayFrameWait++;

			if (autoPlayFrameWait > 5 && realTimeSinceLastSong > 120)
			{
				autoPlayFrameWait = 0;
				realTimeSinceLastSong = 0;

				//Find an appropriate song
				AudioClip newClip;

				//The theme is fine if we don't have context
				if (musicPlayer.clip == null)
					newClip = theme;
				//A non-repetitive song!
				else if (playlist.Exists(song => song != null && song != musicPlayer.clip))
				{
					var candidates = playlist.FindAll(song => song != null && song != musicPlayer.clip);
					newClip = candidates [Random.Range(0, candidates.Count)];
				}
				//Ah, or just repeat
				else
					newClip = musicPlayer.clip;

				//Set and gooo
				musicPlayer.clip = newClip;
				musicPlayer.Play();

				//Cool. Not quite done. Throw some other tracks on the playlist if it's completely empty.
				if (playlist.Count == 0)
					playlist.AddRange(extras);
			}
		}

		//Shut off other players
		foreach (var t in allMusicPlayers.FindAll(obj => obj.isPlaying))
		{
			if (t != musicPlayer)
				t.Stop();
		}
	}

	void Awake()
	{
		//Set the music controller
		if (mc == null)
		{
			mc = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (mc != this)
		{
			Destroy(gameObject);
		}
	}
}
