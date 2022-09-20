using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Connection : MonoBehaviour
{

	[Tooltip("Placement or other similar script to send \"Connect\" and \"Disconnect\" messages to.")]
	public GameObject targetForConnection;

	[SerializeField] bool _isConnected;

	List<Collider2D> _others = new List<Collider2D>();

	/**Update _isConnected based on any current links, then send the (dis)connect message to targetForConnection, if it exists.
	 */
	public void UpdateConnection()
	{
		CleanOthers();

		_isConnected = _others.Count > 0;

		if (targetForConnection)
		{
			if (_isConnected)
				targetForConnection.SendMessage("Connect", SendMessageOptions.DontRequireReceiver);
			else
				targetForConnection.SendMessage("Disconnect", SendMessageOptions.DontRequireReceiver);
		}
	}

	/** Returns a list of the connection scripts this is overlapping triggers with */
	public List<Connection> GetOthers()
	{
		List<Connection> otherConnections = new List<Connection>();

		foreach (var t in _others)
		{
			otherConnections.Add(t.GetComponent<Connection>());
		}

		return otherConnections;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (!enabled) return;
		
		//New connection!
		_others.Add(other);

		UpdateConnection();
	}

	void OnTriggerStay2D(Collider2D other)
	{
		if (!enabled) return;
		
		//Still connected? If not (and we should be, because of teleportation), connect
		if (!_others.Contains(other))
		{
			_others.Add(other);
		}

		UpdateConnection();
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (!enabled) return;
		
		//Connection lost!
		_others.Remove(other);

		UpdateConnection();
	}

	/**Clear the nulls from _others (disabled connections that didn't do OnTriggerExit)
	 */
	void CleanOthers()
	{
		while (_others.Contains(null))
		{
			_others.Remove(null);
		}
	}

	private void OnEnable()
	{
		SnapNodeList.connectionNodes.Add(transform);
	}

	private void OnDisable()
	{
		SnapNodeList.connectionNodes.Remove(transform);
	}
}
