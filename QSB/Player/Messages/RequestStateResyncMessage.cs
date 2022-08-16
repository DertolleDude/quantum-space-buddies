﻿using OWML.Common;
using QSB.ClientServerStateSync;
using QSB.ClientServerStateSync.Messages;
using QSB.Messaging;
using QSB.Utility;

namespace QSB.Player.Messages;

// Can be sent by any client (including host) to signal they want latest player and server information
public class RequestStateResyncMessage : QSBMessage
{
	/// <summary>
	/// set to true when we send this, and false when we receive a player info message back. <br/>
	/// this prevents message spam a bit.
	/// </summary>
	internal static bool _waitingForEvent;

	/// <summary>
	/// used instead of QSBMessageManager.Send to do the extra check
	/// </summary>
	public void Send()
	{
		if (_waitingForEvent)
		{
			return;
		}

		_waitingForEvent = true;
		QSBMessageManager.Send(this);
	}

	public override void OnReceiveLocal() => Delay.RunFramesLater(60,
		() =>
		{
			if (_waitingForEvent)
			{
				if (QSBPlayerManager.PlayerList.Count > 1)
				{
					DebugLog.ToConsole($"Did not receive PlayerInformationEvent in time. Setting _waitingForEvent to false.", MessageType.Info);
				}

				_waitingForEvent = false;
			}
		});

	public override void OnReceiveRemote()
	{
		if (QSBCore.IsHost)
		{
			new ServerStateMessage(ServerStateManager.Instance.GetServerState()) { To = From }.Send();
		}

		new PlayerInformationMessage { To = From }.Send();
	}
}