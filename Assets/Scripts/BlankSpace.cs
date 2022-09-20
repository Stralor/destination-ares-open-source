using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlankSpace : MonoBehaviour
{
	Animator anim;
	bool _visible = false;
	public bool preventAnim;

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.tag == "Obstacle")
		{
			//Destroy(gameObject);
		}
		else if (other.tag == "Player" && !preventAnim && other.attachedRigidbody != null)
		{
			anim.SetBool("Appear", true);
			anim.SetTrigger("Throb");
			preventAnim = _visible = true;

			AllowAnimAfterDelay();
		}
		else if (other.tag == "Space" && !preventAnim && _visible)
		{
			anim.SetTrigger("Glint");
			preventAnim = true;

			AllowAnimAfterDelay();
		}
	}

	void AllowAnimAfterDelay()
	{
		StartCoroutine(CoroutineUtil.DoAfter(() => preventAnim = false, 1));
	}

	void Start()
	{
		anim = GetComponent<Animator>();
	}
}
