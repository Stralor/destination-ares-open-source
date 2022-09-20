using UnityEngine;
using System.Collections;

public static class CoroutineUtil
{
	public static IEnumerator WaitForRealSeconds(float time)
	{
		float start = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup < start + time)
		{
			yield return null;
		}
	}

	/**Do 'action' after time 'seconds'. Defaults to realtime.
	 */
	public static IEnumerator DoAfter(System.Action action, float seconds, bool realtime = true)
	{
		if (realtime)
			yield return new WaitForSecondsRealtime(seconds);
		else
			yield return new WaitForSeconds(seconds);

		UnityEngine.Assertions.Assert.IsNotNull(action);

		action.Invoke();
	}

	/**Do 'action' once 'condition' is true.
	 */
	public static IEnumerator DoAfter(System.Action action, System.Func<bool> condition)
	{
		yield return new WaitUntil(condition);
			
		UnityEngine.Assertions.Assert.IsNotNull(action);

		action.Invoke();
	}
}

