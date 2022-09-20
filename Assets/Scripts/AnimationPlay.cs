using UnityEngine;
using System.Collections;

public class AnimationPlay : MonoBehaviour {

	//This class exists just to play the default animation of the object the script is attached to. Plus related functionality.
	//(Unity 4.6 Beta UI can't access Animation's Play() function from the inspector, thus workaround)


	private Animation comp;	//The Animation component
	private bool clicked;	//Was is just hovered, or clicked?

	public void AnimPlay(){	//Can be called by UI triggers from inspector
		//Do it
		comp.Play();
	}

	//Add more than default functionality
	public void AnimPlay(string s){
		comp.Play(s);
	}

	//Check if clicked first
	public void AnimPlayUnclicked(string s){
		if (clicked){
			comp.Play (s);
		}
	}

	//Like AnimPlayUnclicked, but calls rewind function
	public void AnimRewind(){
		if (!clicked) {
			comp.Rewind();
		}
	}

	//Click dat
	public void Click(){
		if (clicked) {
						clicked = false;
				} else {
						clicked = true;
				}
	}

	// Use this for initialization
	void Start () {
		comp = gameObject.GetComponent<Animation> ();
		clicked = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
