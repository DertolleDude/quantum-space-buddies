﻿using Mirror;
using QSB.Messaging;
using QSB.QuantumSync.WorldObjects;
using QSB.WorldSync;

namespace QSB.Player.Messages;

// almost a world object message, but supports null (-1) as well
internal class PlayerEntangledMessage : QSBMessage<int>
{
	public PlayerEntangledMessage(int objectId) => Data = objectId;

	public override bool ShouldReceive => QSBWorldSync.AllObjectsReady;

	public override void OnReceiveLocal()
	{
		var player = QSBPlayerManager.LocalPlayer;
		if (Data == -1)
		{
			player.EntangledObject = null;
			return;
		}

		var quantumObject = Data.GetWorldObject<IQSBQuantumObject>();
		player.EntangledObject = quantumObject;
	}

	public override void OnReceiveRemote()
	{
		var player = QSBPlayerManager.GetPlayer(From);
		if (Data == -1)
		{
			player.EntangledObject = null;
			return;
		}

		var quantumObject = Data.GetWorldObject<IQSBQuantumObject>();
		player.EntangledObject = quantumObject;
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Data);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Data = reader.Read<int>();
	}
}