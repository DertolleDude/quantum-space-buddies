﻿using OWML.Common;
using QSB.ClientServerStateSync;
using QSB.ClientServerStateSync.Messages;
using QSB.Messaging;
using QSB.Utility;

namespace QSB.DeathSync.Messages
{
	internal class StartLoopMessage : QSBMessage
	{
		public override void OnReceiveLocal() => OnReceiveRemote();

		public override void OnReceiveRemote()
		{
			DebugLog.DebugWrite($" ~~~ LOOP START ~~~");
			if (QSBSceneManager.CurrentScene == QSBScene.SolarSystem)
			{
				new ClientStateMessage(ClientState.AliveInSolarSystem).Send();
			}
			else if (QSBSceneManager.CurrentScene == QSBScene.EyeOfTheUniverse)
			{
				new ClientStateMessage(ClientState.AliveInEye).Send();
			}
			else
			{
				DebugLog.ToConsole($"Error - Got StartLoop event when not in universe!", MessageType.Error);
				new ClientStateMessage(ClientState.NotLoaded).Send();
			}
		}
	}
}