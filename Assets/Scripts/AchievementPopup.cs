using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPopup : MonoBehaviour
{
	public Image achievementImage;
	public Text achievementText;

	public List<Achievement> queue = new List<Achievement>();

	private Animator anim;
	private bool animating;

	void Update()
	{
		//Animate if we have something in the queue
		if (queue.Count > 0 && !animating)//&& anim.GetCurrentAnimatorStateInfo(0).IsName("Offscreen") && !anim.GetAnimatorTransitionInfo(0).IsName("FlyIn") && anim.)
		{
			//Set values for the animation
			achievementImage.sprite = queue [0].achievedArt;
			achievementText.text = queue [0].name + "\n" + queue [0].description;
			if (queue [0].unlockPoints > 0)
				achievementText.text += ColorPalette.ColorText(ColorPalette.cp.yellow4, "\n+" + queue [0].unlockPoints.ToString() + " AP");

			//Go
			anim.SetTrigger("Begin");
			anim.ResetTrigger("Exit");
			animating = true;

			//Audio
			AudioClipOrganizer.aco.PlayAudioClip("Press", transform);

			//Remove the first from the queue
			queue.RemoveAt(0);
		}
	}

	public void DoneAnimating()
	{
		animating = false;
	}

	void Awake()
	{
		anim = GetComponent<Animator>();

		DontDestroyOnLoad(gameObject);
	}
}
