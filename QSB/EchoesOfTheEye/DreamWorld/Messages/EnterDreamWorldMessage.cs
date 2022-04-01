﻿using QSB.ItemSync.WorldObjects.Items;
using QSB.Messaging;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.PlayerBodySetup.Remote;
using QSB.WorldSync;

namespace QSB.EchoesOfTheEye.DreamWorld.Messages;

/// <summary>
/// todo SendInitialState
/// </summary>
internal class EnterDreamWorldMessage : QSBWorldObjectMessage<QSBDreamLanternItem>
{
	static EnterDreamWorldMessage()
	{
		GlobalMessenger.AddListener(OWEvents.EnterDreamWorld, () =>
		{
			if (!PlayerTransformSync.LocalInstance)
			{
				return;
			}

			Locator.GetDreamWorldController()
				.GetPlayerLantern()
				.GetWorldObject<QSBDreamLanternItem>()
				.SendMessage(new EnterDreamWorldMessage());
		});
	}

	public override void OnReceiveLocal()
	{
		var player = QSBPlayerManager.LocalPlayer;
		player.InDreamWorld = true;
		player.AssignedSimulationLantern = WorldObject;
	}

	public override void OnReceiveRemote()
	{
		var player = QSBPlayerManager.GetPlayer(From);
		player.InDreamWorld = true;
		player.AssignedSimulationLantern = WorldObject;

		// do the spawn shader
		player.SetVisible(false);
		player.SetVisible(true, DreamWorldSpawnAnimator.DREAMWORLD_SPAWN_TIME);
		player.DreamWorldSpawnAnimator.StartSpawnEffect();
	}
}
