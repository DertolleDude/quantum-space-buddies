﻿using QSB.AuthoritySync;
using QSB.Messaging;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.ShipSync.TransformSync;
using QSB.WorldSync;

namespace QSB.ShipSync.Messages;

/// <summary>
/// TODO: initial state for the current flyer
/// </summary>
internal class FlyShipMessage : QSBMessage<bool>
{
	static FlyShipMessage()
	{
		GlobalMessenger<OWRigidbody>.AddListener(OWEvents.EnterFlightConsole, _ => Handler(true));
		GlobalMessenger.AddListener(OWEvents.ExitFlightConsole, () => Handler(false));
	}

	private static void Handler(bool flying)
	{
		if (PlayerTransformSync.LocalInstance)
		{
			new FlyShipMessage(flying).Send();
		}
	}

	public FlyShipMessage(bool flying) : base(flying) { }

	public override bool ShouldReceive => QSBWorldSync.AllObjectsReady;

	public override void OnReceiveLocal() => OnReceiveRemote();

	public override void OnReceiveRemote()
	{
		SetCurrentFlyer(From, Data);
		var shipCockpitController = ShipManager.Instance.CockpitController;
		if (Data)
		{
			shipCockpitController._interactVolume.DisableInteraction();
		}
		else
		{
			shipCockpitController._interactVolume.EnableInteraction();
		}
	}

	private static void SetCurrentFlyer(uint id, bool isFlying)
	{
		ShipManager.Instance.CurrentFlyer = isFlying
			? id
			: uint.MaxValue;

		if (QSBCore.IsHost)
		{
			ShipTransformSync.LocalInstance.netIdentity.SetAuthority(isFlying
				? id
				: QSBPlayerManager.LocalPlayerId);
		}
	}
}