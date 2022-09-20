using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DisplayScore : MonoBehaviour {

	 Text target;

	void Start(){
		target = GetComponent<Text>();
	}

	// Update is called once per frame
	void Update () {
		target.text = StatTrack.stats.score.ToString();
	}
}
