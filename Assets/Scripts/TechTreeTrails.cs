using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TechTreeTrails : MonoBehaviour
{
	public float heightAtCutOver;
	public List<LineRenderer> lines = new List<LineRenderer>();
	public bool updateInRuntime;

	int lastChildrenCount = 0;


	void Start()
	{
		GetLines();
	}

	void Update()
	{
		bool removedALine = false;

		//Clear nulls
		lines.RemoveAll(obj => obj == null);

		//Remove lines to defunct nodes
		for (int i = 0; i < lines.Count; i++)
		{
			if (lines [i] != null && !lines [i].transform.IsChildOf(transform))
			{
				TurnOffLine(lines [i]);

				removedALine = true;
			}
		}

		//Any change? Let's redraw
		if (updateInRuntime && (removedALine || lastChildrenCount != transform.childCount))
		{
			ComputeLines();

			//Update this
			lastChildrenCount = transform.childCount;
		}
	}

	public void ComputeLines()
	{
		GetLines();

		foreach (var line in lines)
		{
			SetLine(line);
		}
	}

	public void GetLines()
	{
		//Find all the LineRenderer on all children, where available
		foreach (Transform child in transform)
		{
			var line = child.GetComponentInChildren<LineRenderer>(includeInactive: true);

			//If it exists, track it
			if (line != null && !lines.Contains(line))
			{
				lines.Add(line);
			}
		}
	}

	void SetLine(LineRenderer line)
	{
		//Some info caches
		var lineLayout = line.GetComponent<LayoutElement>();
		var layoutGroup = GetComponent<LayoutGroup>();

		//Settings
		var length = layoutGroup.padding.top + lineLayout.preferredHeight; //-line.transform.parent.localPosition.y + lineLayout.preferredHeight;
		var xOffset = -line.transform.parent.localPosition.x;

		//Set points
		line.SetPosition(2, new Vector3(0, length - heightAtCutOver));
		line.SetPosition(3, new Vector3(xOffset, length - heightAtCutOver));
		line.SetPosition(4, new Vector3(xOffset, length - 3));
		line.SetPosition(5, new Vector3(xOffset, length));

		//Turn it on
		line.gameObject.SetActive(true);
	}

	void TurnOffLine(LineRenderer line)
	{
		line.gameObject.SetActive(false);

		lines.Remove(line);
	}
}