﻿using QSB.Utility;
using UnityEngine;

namespace QSB.Player
{
	public class PlayerHUDMarker : HUDDistanceMarker
	{
		private PlayerInfo _player;
		private bool _needsInitializing;

		protected override void InitCanvasMarker()
		{
			_markerRadius = 2f;

			_markerTarget = new GameObject().transform;
			_markerTarget.parent = transform;

			_markerTarget.localPosition = Vector3.up * 2;
		}

		public void Init(PlayerInfo player)
		{
			DebugLog.DebugWrite($"Init {player.PlayerId} name:{player.Name}");
			_player = player;
			_player.HudMarker = this;
			_needsInitializing = true;
		}

		protected override void RefreshOwnVisibility()
		{
			if (_canvasMarker != null)
			{
				var isVisible = _canvasMarker.IsVisible();

				if (_player.Visible != isVisible)
				{
					_canvasMarker.SetVisibility(_player.Visible);
				}
			}
		}

		private void Update()
		{
			if (!_needsInitializing || !_player.PlayerStates.IsReady)
			{
				return;
			}

			Initialize();
		}

		private void Initialize()
		{
			_markerLabel = _player.Name.ToUpper();
			_needsInitializing = false;

			base.InitCanvasMarker();
		}

		public void Remove()
		{
			// do N O T destroy the parent - it completely breaks the ENTIRE GAME
			if (_canvasMarker != null)
			{
				_canvasMarker.DestroyMarker();
			}

			if (_markerTarget != null)
			{
				Destroy(_markerTarget.gameObject);
			}

			Destroy(this);
		}
	}
}