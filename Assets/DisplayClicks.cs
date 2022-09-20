using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DisplayClicks : MonoBehaviour {

	Text button;

	// Use this for initialization
	void Start () {
		button = GetComponent<Button>().GetComponentInChildren<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		button.text = StatTrack.stats.clicks.ToString() + " / " + StatTrack.stats.clicks_total.ToString();
	}
}
