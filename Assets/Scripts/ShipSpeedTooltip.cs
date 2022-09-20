using UnityEngine;
using System.Collections;
using System.Text;

public class ShipSpeedTooltip : MonoBehaviour
{

	GenericTooltip tooltip;
	UnityEngine.UI.Text speedUI;

	void Start()
	{
		tooltip = GetComponent<GenericTooltip>();
		speedUI = GetComponent<UnityEngine.UI.Text>();
	}

	void Update()
	{
		if (tooltip.activeTip)
		{
			var text = new StringBuilder();

			//Heading texts
			if (speedUI.text.Contains("Perfect"))
				text.Append("\n" + "Course and heading are ideal." +
				"\n\n" + "Speed is fully effective.");
			else if (speedUI.text.Contains("Good"))
				text.Append("\n" + "Deviating slightly from ideal course." +
				"\n\n" + "Speed is effective.");
			else if (speedUI.text.Contains("On Course"))
				text.Append("\n" + "Within tolerable deviation from ideal course." +
				"\n\n" + "Speed mostly effective.");
			else if (speedUI.text.Contains("Near"))
				text.Append("\n" + "Heading is non-ideal." +
				"\n\n" + "Speed is somewhat ineffective.");
			else if (speedUI.text.Contains("Poor"))
				text.Append("\n" + "Heading is closer to parallel than on course." +
				"\n\n" + "Speed is largely ineffective.");
			else if (speedUI.text.Contains("Off Course"))
				text.Append("\n" + "Negligible to no gains in progress." +
				"\n\n" + "Speed is ineffective.");
			else if (speedUI.text.Contains("Losing"))
				text.Append("\n" + "Moving away from intended course." +
				"\n\n" + "Speed is detrimental.");
			else if (speedUI.text.Contains("Wrong"))
				text.Append("\n" + "Severely off course." +
				"\n\n" + "Speed is very detrimental.");
			else if (speedUI.text.Contains("Negative"))
				text.Append("\n" + "Course largely corrected. Still moving backwards." +
				"\n\n" + "Apply speed to fix.");
			else if (speedUI.text.Contains("Stopped"))
				text.Append("\n" + "Not moving, heading irrelevant." +
				"\n\n" + "Apply speed to fix.");

			//Advanced tips
			if (PlayerPrefs.GetInt("Tooltips") == 1)
			{
				text.AppendLine();
				
				//Speed (in-game units per in-game hour, rather than engine units which is per real-time tick)
				int speed = (int)(ShipResources.res.speed * 60 / ShipMovement.sm.tickDelay / GameClock.clock.clockSpeed);
				text.Append("\n" + "Raw speed: " + ColorPalette.ColorText(ColorPalette.cp.yellow4, speed.ToString()));
				
				//Speed towards target. Uses current offcourse calc to determine
				//			int effectiveSpeed = ShipMovement.sm.CalculateEffectiveSpeed(speed); Not using this one because it's more accurate than the game and changes too rapidly
				int effectiveSpeed = (int)(ShipMovement.sm.CalculateEffectiveSpeed(ShipResources.res.speed) * 60 / ShipMovement.sm.tickDelay / GameClock.clock.clockSpeed);
				text.Append("\n" + "Effective speed: " + ColorPalette.ColorText(ColorPalette.cp.blue4, effectiveSpeed.ToString()));
			}

			tooltip.tooltipText = text.ToString();

			SendMessage("UpdateText");
		}
	}
}
