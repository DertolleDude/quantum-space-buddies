﻿using OWML.Utils;
using QSB.Events;
using QSB.Patches;
using QSB.Player;
using QSB.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.DeathSync
{
	internal class RespawnManager : MonoBehaviour
	{
		public static RespawnManager Instance;

		public bool RespawnNeeded => _playersPendingRespawn.Count != 0;

		private List<PlayerInfo> _playersPendingRespawn = new List<PlayerInfo>();
		private NotificationData _previousNotification;
		private GameObject _owRecoveryPoint;
		private GameObject _qsbRecoveryPoint;

		private void Start()
		{
			Instance = this;
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
			QSBNetworkManager.Instance.OnClientConnected += OnConnected;
			QSBNetworkManager.Instance.OnClientDisconnected += OnDisconnected;
		}

		private void OnConnected()
		{
			if (QSBSceneManager.IsInUniverse)
			{
				OnSceneLoaded(OWScene.None, QSBSceneManager.CurrentScene, true);
			}
		}

		private void OnDisconnected(NetworkError error)
		{
			_owRecoveryPoint?.SetActive(true);
			_qsbRecoveryPoint?.SetActive(false);
		}

		private void OnSceneLoaded(OWScene oldScene, OWScene newScene, bool inUniverse)
		{
			QSBPlayerManager.ShowAllPlayers();
			QSBPlayerManager.PlayerList.ForEach(x => x.IsDead = false);
			_playersPendingRespawn.Clear();

			if (newScene != OWScene.SolarSystem)
			{
				return;
			}

			if (_owRecoveryPoint == null)
			{
				_owRecoveryPoint = GameObject.Find("Systems_Supplies/PlayerRecoveryPoint");
			}

			if (_owRecoveryPoint == null)
			{
				DebugLog.ToConsole($"Error - Couldn't find the ship's PlayerRecoveryPoint!", OWML.Common.MessageType.Error);
				return;
			}
			
			_owRecoveryPoint.SetActive(false);

			var Systems_Supplies = _owRecoveryPoint.gameObject.transform.parent;

			if (_qsbRecoveryPoint == null)
			{
				_qsbRecoveryPoint = new GameObject("QSBPlayerRecoveryPoint");
				_qsbRecoveryPoint.SetActive(false);
				_qsbRecoveryPoint.transform.parent = Systems_Supplies;
				_qsbRecoveryPoint.transform.localPosition = new Vector3(2.46f, 1.957f, 1.156f);
				_qsbRecoveryPoint.transform.localRotation = Quaternion.Euler(0, 6.460001f, 0f);
				_qsbRecoveryPoint.layer = 21;

				var boxCollider = _qsbRecoveryPoint.AddComponent<BoxCollider>();
				boxCollider.isTrigger = true;
				boxCollider.size = new Vector3(1.3f, 1.01f, 0.47f);

				var multiInteract = _qsbRecoveryPoint.AddComponent<MultiInteractReceiver>();
				multiInteract._usableInShip = true;
				multiInteract._interactRange = 1.5f;

				_qsbRecoveryPoint.AddComponent<ShipRecoveryPoint>();
			}

			_qsbRecoveryPoint.SetActive(true);
		}

		public void TriggerRespawnMap()
		{
			DebugLog.DebugWrite($"TRIGGER RESPAWN MAP");
			QSBPatchManager.DoPatchType(QSBPatchTypes.RespawnTime);
			QSBCore.UnityEvents.FireOnNextUpdate(() => GlobalMessenger.FireEvent("TriggerObservatoryMap"));
		}

		public void Respawn()
		{
			var mapController = FindObjectOfType<MapController>();
			QSBPatchManager.DoUnpatchType(QSBPatchTypes.RespawnTime);

			var playerSpawner = FindObjectOfType<PlayerSpawner>();
			playerSpawner.DebugWarp(playerSpawner.GetSpawnPoint(SpawnLocation.Ship));

			mapController.GetType().GetAnyMethod("ExitMapView").Invoke(mapController, null);

			var cameraEffectController = Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>();
			cameraEffectController.OpenEyes(1f, false);
		}

		public void OnPlayerDeath(PlayerInfo player)
		{
			DebugLog.DebugWrite($"ON PLAYER DEATH");

			if (_playersPendingRespawn.Contains(player))
			{
				DebugLog.ToConsole($"Warning - Received death message for player who is already in _playersPendingRespawn!", OWML.Common.MessageType.Warning);
				return;
			}

			DebugLog.DebugWrite($"set player to be dead");
			player.IsDead = true;

			_playersPendingRespawn.Add(player);
			UpdateRespawnNotification();

			QSBPlayerManager.ChangePlayerVisibility(player.PlayerId, false);
		}

		public void OnPlayerRespawn(PlayerInfo player)
		{
			if (!_playersPendingRespawn.Contains(player))
			{
				DebugLog.ToConsole($"Warning - Received respawn message for player who is not in _playersPendingRespawn!", OWML.Common.MessageType.Warning);
				return;
			}

			player.IsDead = false;

			_playersPendingRespawn.Remove(player);
			UpdateRespawnNotification();

			QSBPlayerManager.ChangePlayerVisibility(player.PlayerId, true);
		}

		public void RespawnSomePlayer()
		{
			var playerToRespawn = _playersPendingRespawn.First();
			QSBEventManager.FireEvent(EventNames.QSBPlayerRespawn, playerToRespawn.PlayerId);
		}

		private void UpdateRespawnNotification()
		{
			NotificationManager.SharedInstance.UnpinNotification(_previousNotification);

			if (_playersPendingRespawn.Count == 0)
			{
				return;
			}

			var data = new NotificationData(NotificationTarget.Player, $"[{_playersPendingRespawn.Count}] PLAYER(S) AWAITING RESPAWN");
			NotificationManager.SharedInstance.PostNotification(data, true);
			_previousNotification = data;
		}
	}
}
