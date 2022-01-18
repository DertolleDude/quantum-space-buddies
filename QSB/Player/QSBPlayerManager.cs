﻿using OWML.Common;
using QSB.ItemSync.WorldObjects.Items;
using QSB.Player.TransformSync;
using QSB.Tools.FlashlightTool;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.Player
{
	public static class QSBPlayerManager
	{
		public static PlayerInfo LocalPlayer
		{
			get
			{
				var localInstance = PlayerTransformSync.LocalInstance;
				if (localInstance == null)
				{
					DebugLog.ToConsole("Error - Trying to get LocalPlayer when the local PlayerTransformSync instance is null." +
						$"{Environment.NewLine} Stacktrace : {Environment.StackTrace} ", MessageType.Error);
					return null;
				}

				return localInstance.Player;
			}
		}
		public static uint LocalPlayerId => LocalPlayer?.PlayerId ?? uint.MaxValue;

		/// <summary>
		/// called right after player is added
		/// </summary>
		public static Action<PlayerInfo> OnAddPlayer;
		/// <summary>
		/// called right before player is removed
		/// </summary>
		public static Action<PlayerInfo> OnRemovePlayer;

		public static readonly List<PlayerInfo> PlayerList = new();

		public static PlayerInfo GetPlayer(uint id)
		{
			if (id is uint.MaxValue or 0)
			{
				return default;
			}

			var player = PlayerList.FirstOrDefault(x => x.PlayerId == id);
			if (player == null)
			{
				DebugLog.ToConsole($"Error - Player with id {id} does not exist! Stacktrace : {Environment.StackTrace}", MessageType.Error);
				return default;
			}

			return player;
		}

		public static bool PlayerExists(uint id) =>
			id is not (uint.MaxValue or 0) && PlayerList.Any(x => x.PlayerId == id);

		public static List<PlayerInfo> GetPlayersWithCameras(bool includeLocalCamera = true)
		{
			var cameraList = PlayerList.Where(x => x.Camera != null && x.PlayerId != LocalPlayerId).ToList();
			if (includeLocalCamera
			    && LocalPlayer != default
			    && LocalPlayer.Camera != null)
			{
				cameraList.Add(LocalPlayer);
			}
			else if (includeLocalCamera && (LocalPlayer == default || LocalPlayer.Camera == null))
			{
				if (LocalPlayer == default)
				{
					DebugLog.ToConsole($"Error - LocalPlayer is null.", MessageType.Error);
					return cameraList;
				}

				DebugLog.ToConsole($"Error - LocalPlayer.Camera is null.", MessageType.Error);
				LocalPlayer.Camera = Locator.GetPlayerCamera();
			}

			return cameraList;
		}

		public static Tuple<Flashlight, IEnumerable<QSBFlashlight>> GetPlayerFlashlights()
			=> new(Locator.GetFlashlight(), PlayerList.Where(x => x.FlashLight != null).Select(x => x.FlashLight));

		public static void ShowAllPlayers()
			=> PlayerList.Where(x => x != LocalPlayer && x.DitheringAnimator != null).ForEach(x => x.DitheringAnimator.SetVisible(true, 0.5f));

		public static void HideAllPlayers()
			=> PlayerList.Where(x => x != LocalPlayer && x.DitheringAnimator != null).ForEach(x => x.DitheringAnimator.SetVisible(false, 0.5f));

		public static PlayerInfo GetClosestPlayerToWorldPoint(Vector3 worldPoint, bool includeLocalPlayer) => includeLocalPlayer
				? GetClosestPlayerToWorldPoint(PlayerList, worldPoint)
				: GetClosestPlayerToWorldPoint(PlayerList.Where(x => x != LocalPlayer).ToList(), worldPoint);

		public static PlayerInfo GetClosestPlayerToWorldPoint(List<PlayerInfo> playerList, Vector3 worldPoint)
		{
			if (playerList == null)
			{
				DebugLog.ToConsole($"Error - Cannot get closest player from null player list.", MessageType.Error);
				return null;
			}

			if (playerList.Count == 0)
			{
				DebugLog.ToConsole($"Error - Cannot get closest player from empty player list.", MessageType.Error);
				return null;
			}

			return playerList.Where(x => x.IsReady && x.Body != null).OrderBy(x => Vector3.Distance(x.Body.transform.position, worldPoint)).FirstOrDefault();
		}

		public static IEnumerable<Tuple<PlayerInfo, IQSBOWItem>> GetPlayerCarryItems()
			=> PlayerList.Select(x => new Tuple<PlayerInfo, IQSBOWItem>(x, x.HeldItem));
	}
}