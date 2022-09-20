using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{

	[Tooltip("Rigidbody for movement. Required.")]
	public Rigidbody2D rigid;
	[Tooltip("Rotation script for rotating in direction of movement. Optional.")]
	public Rotation rotation;

	public float minimumSpeed;
	[FormerlySerializedAs("speed")] public float acceleration;
	public float turnSpeed, maskBaseScale = 1;
	public SpriteRenderer mask;

	private float slowRatio = 1f;

	AudioSource src;
	const float BASE_AUDIO_VOLUME = 0.02f;


	public void Slow(bool slow)
	{
		if (slow)
			slowRatio *= 0.9f;
		else
			slowRatio /= 0.9f;

		if (slowRatio > 1)
			slowRatio = 1;

		UpdateAudioSettings();
	}

	public void ResetSlow()
	{
		slowRatio = 1f;

		UpdateAudioSettings();
	}

	public void ReduceSpeed()
	{
		rigid.velocity /= 4;
	}

	void FixedUpdate()
	{
		//Keyboard input
		Vector2 inputDir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		//Alt keyboard input
		if (Input.GetKey(KeyCode.A))
			inputDir += new Vector2(-1, 0);
		if (Input.GetKey(KeyCode.D))
			inputDir += new Vector2(1, 0);
		if (Input.GetKey(KeyCode.S))
			inputDir += new Vector2(0, -1);
		if (Input.GetKey(KeyCode.W))
			inputDir += new Vector2(0, 1);

		//Normalized
		inputDir.Normalize();

		//Mouse override
		if (Input.GetMouseButton(0))
		{
			var mousePos = Input.mousePosition;
			mousePos.z = rigid.transform.position.z - Camera.main.transform.position.z;

			//Get each position
			float x = Camera.main.ScreenToWorldPoint(mousePos).x - rigid.transform.position.x;
			float y = Camera.main.ScreenToWorldPoint(mousePos).y - rigid.transform.position.y;
			//Set the direction vector, if the mouse isn't too close
			if (Mathf.Abs(x + y) > 0.1f)
				inputDir = new Vector2(x, y).normalized;
		}

		//Output to velocity
//		rigid.velocity = new Vector2(inputDir.x * speed * slowRatio, inputDir.y * speed * slowRatio);
		var isBelowMinimumSpeed = rigid.velocity.magnitude < minimumSpeed;
		var hasInput = Mathf.Abs(inputDir.x) + Mathf.Abs(inputDir.y) > 0.1f;
		if (isBelowMinimumSpeed && hasInput)
		{
			rigid.velocity = new Vector2(inputDir.x * minimumSpeed * slowRatio, inputDir.y * minimumSpeed * slowRatio);
		}
		rigid.AddForce(new Vector2(inputDir.x * acceleration * slowRatio, inputDir.y * acceleration * slowRatio));

		//Rotate to dir (or up at rest)
		if (rotation != null)
		{
			if (inputDir != Vector2.zero)
				rotation.RotateTo(inputDir, turnSpeed * slowRatio);
			else if (Mathf.Abs(rotation.GetFacing() - rotation.GetAngle(Vector2.up)) > 5)
				rotation.RotateTo(Vector2.up, turnSpeed / 10);
		}

		//Scale the mask
		if (mask != null)
		{
			float targetMaskScaleValue = Mathf.Sqrt(slowRatio) * maskBaseScale;
			Vector3 targetMaskScale = new Vector3(targetMaskScaleValue, targetMaskScaleValue, targetMaskScaleValue);
			mask.transform.localScale = Vector3.Lerp(mask.transform.localScale, targetMaskScale, 5 * Time.fixedDeltaTime);
		}

		//Audio
		if (rigid.velocity.sqrMagnitude > 0.0025f)
		{
			if (!src.isPlaying)
			{
				UpdateAudioSettings();
				src.Play();
			}
			else
			{
				//In the else so we don't do it twice (UpdateAudio calls this)
				UpdateAudioVolume();
			}
		}
		else
		{
			src.Stop();
		}
	}

	void UpdateAudioSettings()
	{
		if (src != null)
		{
			src.pitch = slowRatio;//Random.Range(slowRatio - 0.5f, slowRatio + 0.5f);

			UpdateAudioVolume();
		}
	}

	void UpdateAudioVolume()
	{
		if (src != null)
		{
			src.volume = BASE_AUDIO_VOLUME * rigid.velocity.magnitude / (Mathf.Pow(slowRatio, 2));
		}
	}

	void Start()
	{
		if (rigid == null)
		{
			if ((rigid = GetComponent<Rigidbody2D>()) == null)
			{
				Debug.LogWarning("Rigidbody could not be found on this gameObject (" + this.gameObject + ")! Destroying this movement controller.");
				Destroy(this);
			}
		}

		if (src == null)
		{
			src = AudioClipOrganizer.aco.GetSourceForCustomPlay(clipName: "transmission", parent: transform);
			src.spatialBlend = 0.05f;
			src.volume = BASE_AUDIO_VOLUME;
		}
	}
}
