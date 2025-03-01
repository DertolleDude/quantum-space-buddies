﻿using Cysharp.Threading.Tasks;
using QSB.ItemSync.Messages;
using QSB.ItemSync.WorldObjects.Sockets;
using QSB.Messaging;
using QSB.Patches;
using QSB.Player;
using QSB.SectorSync.WorldObjects;
using QSB.WorldSync;
using System.Threading;
using UnityEngine;

namespace QSB.ItemSync.WorldObjects.Items;

public class QSBItem<T> : WorldObject<T>, IQSBItem
	where T : OWItem
{
	public ItemState ItemState { get; } = new();

	private Transform _lastParent;
	private Vector3 _lastPosition;
	private Quaternion _lastRotation;
	private QSBSector _lastSector;
	private QSBItemSocket _lastSocket;

	public override string ReturnLabel()
	{
		return $"{ToString()}" +
			$"\r\nState:{ItemState.State}" +
			$"\r\nParent:{ItemState.Parent?.name}" +
			$"\r\nLocalPosition:{ItemState.LocalPosition}" +
			$"\r\nLocalNormal:{ItemState.LocalNormal}" +
			$"\r\nHoldingPlayer:{ItemState.HoldingPlayer?.PlayerId}";
	}

	public override async UniTask Init(CancellationToken ct)
	{
		await UniTask.WaitUntil(() => QSBWorldSync.AllObjectsAdded, cancellationToken: ct);

		StoreLocation();

		QSBPlayerManager.OnRemovePlayer += OnPlayerLeave;
	}

	public override void OnRemoval() => QSBPlayerManager.OnRemovePlayer -= OnPlayerLeave;

	public void StoreLocation()
	{
		_lastParent = AttachedObject.transform.parent;
		_lastPosition = AttachedObject.transform.localPosition;
		_lastRotation = AttachedObject.transform.localRotation;

		var sector = AttachedObject.GetSector();
		if (sector != null)
		{
			_lastSector = sector.GetWorldObject<QSBSector>();
		}

		var socket = _lastParent.GetComponent<OWItemSocket>();
		if (socket != null)
		{
			_lastSocket = socket.GetWorldObject<QSBItemSocket>();
		}
	}

	private void OnPlayerLeave(PlayerInfo player)
	{
		if (player.HeldItem != this)
		{
			return;
		}

		if (_lastSocket != null)
		{
			QSBPatch.RemoteCall(() => _lastSocket.PlaceIntoSocket(this));
		}
		else
		{
			// TODO at some point we should probably call the proper drop item code to account for funny overrides
			AttachedObject.transform.parent = _lastParent;
			AttachedObject.transform.localPosition = _lastPosition;
			AttachedObject.transform.localRotation = _lastRotation;
			AttachedObject.transform.localScale = Vector3.one;
			AttachedObject.SetSector(_lastSector?.AttachedObject);
			AttachedObject.SetColliderActivation(true);
		}
	}

	public override void SendInitialState(uint to)
	{
		if (!ItemState.HasBeenInteractedWith)
		{
			return;
		}

		switch (ItemState.State)
		{
			case ItemStateType.Held:
				((IQSBItem)this).SendMessage(new MoveToCarryMessage(ItemState.HoldingPlayer.PlayerId));
				break;
			case ItemStateType.Socketed:
				new SocketItemMessage(SocketMessageType.Socket, ItemState.Socket, AttachedObject).Send();
				break;
			case ItemStateType.OnGround:
				((IQSBItem)this).SendMessage(
					new DropItemMessage(
						ItemState.WorldPosition,
						ItemState.WorldNormal,
						ItemState.Parent,
						ItemState.Sector,
						ItemState.CustomDropTarget,
						ItemState.Rigidbody));
				break;
		}
	}

	public ItemType GetItemType() => AttachedObject.GetItemType();

	public void PickUpItem(Transform holdTransform) =>
		QSBPatch.RemoteCall(() => AttachedObject.PickUpItem(holdTransform));

	public void DropItem(Vector3 worldPosition, Vector3 worldNormal, Transform parent, Sector sector, IItemDropTarget customDropTarget) =>
		QSBPatch.RemoteCall(() => AttachedObject.DropItem(worldPosition, worldNormal, parent, sector, customDropTarget));

	public void OnCompleteUnsocket() => AttachedObject.OnCompleteUnsocket();
}
