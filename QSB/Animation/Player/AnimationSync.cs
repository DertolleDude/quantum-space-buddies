﻿using OWML.Common;
using OWML.Utils;
using QSB.Animation.Player.Thrusters;
using QSB.Events;
using QSB.Player;
using QSB.Utility;
using QuantumUNET.Components;
using System.Linq;
using UnityEngine;

namespace QSB.Animation.Player
{
	public class AnimationSync : PlayerSyncObject
	{
		private RuntimeAnimatorController _suitedAnimController;
		private AnimatorOverrideController _unsuitedAnimController;
		private GameObject _suitedGraphics;
		private GameObject _unsuitedGraphics;
		private PlayerCharacterController _playerController;
		private CrouchSync _crouchSync;

		private RuntimeAnimatorController _chertController;
		private readonly RuntimeAnimatorController _eskerController;
		private readonly RuntimeAnimatorController _feldsparController;
		private readonly RuntimeAnimatorController _gabbroController;
		private RuntimeAnimatorController _riebeckController;

		public AnimatorMirror Mirror { get; private set; }
		public AnimationType CurrentType { get; set; }
		public Animator VisibleAnimator { get; private set; }
		public Animator InvisibleAnimator { get; private set; }
		public QNetworkAnimator NetworkAnimator { get; private set; }

		protected void Awake()
		{
			InvisibleAnimator = gameObject.AddComponent<Animator>();
			NetworkAnimator = gameObject.AddComponent<QNetworkAnimator>();
			NetworkAnimator.enabled = false;
			NetworkAnimator.animator = InvisibleAnimator;

			QSBSceneManager.OnUniverseSceneLoaded += OnUniverseSceneLoaded;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Destroy(InvisibleAnimator);
			Destroy(NetworkAnimator);
			QSBSceneManager.OnUniverseSceneLoaded -= OnUniverseSceneLoaded;
		}

		private void OnUniverseSceneLoaded(OWScene oldScene, OWScene newScene) => LoadControllers();

		private void LoadControllers()
		{
			var bundle = QSBCore.InstrumentAssetBundle;
			_chertController = bundle.LoadAsset("Assets/GameAssets/AnimatorController/Traveller_Chert.controller") as RuntimeAnimatorController;
			_riebeckController = bundle.LoadAsset("Assets/GameAssets/AnimatorController/Traveller_Riebeck.controller") as RuntimeAnimatorController;
		}

		private void InitCommon(Transform body)
		{
			if (QSBSceneManager.IsInUniverse)
			{
				LoadControllers();
			}

			NetworkAnimator.enabled = true;
			VisibleAnimator = body.GetComponent<Animator>();
			Mirror = body.gameObject.AddComponent<AnimatorMirror>();
			if (IsLocalPlayer)
			{
				Mirror.Init(VisibleAnimator, InvisibleAnimator);
			}
			else
			{
				Mirror.Init(InvisibleAnimator, VisibleAnimator);
			}

			for (var i = 0; i < InvisibleAnimator.parameterCount; i++)
			{
				NetworkAnimator.SetParameterAutoSend(i, true);
			}

			var playerAnimController = body.GetComponent<PlayerAnimController>();
			_suitedAnimController = AnimControllerPatch.SuitedAnimController;
			_unsuitedAnimController = playerAnimController.GetValue<AnimatorOverrideController>("_unsuitedAnimOverride");
			_suitedGraphics = playerAnimController.GetValue<GameObject>("_suitedGroup");
			_unsuitedGraphics = playerAnimController.GetValue<GameObject>("_unsuitedGroup");
		}

		public void InitLocal(Transform body)
		{
			InitCommon(body);

			_playerController = body.parent.GetComponent<PlayerCharacterController>();

			InitCrouchSync();
			InitAccelerationSync();
		}

		public void InitRemote(Transform body)
		{
			InitCommon(body);

			var playerAnimController = body.GetComponent<PlayerAnimController>();
			playerAnimController.enabled = false;

			playerAnimController.SetValue("_suitedGroup", new GameObject());
			playerAnimController.SetValue("_unsuitedGroup", new GameObject());
			playerAnimController.SetValue("_baseAnimController", null);
			playerAnimController.SetValue("_unsuitedAnimOverride", null);
			playerAnimController.SetValue("_rightArmHidden", false);

			var rightArmObjects = playerAnimController.GetValue<GameObject[]>("_rightArmObjects").ToList();
			rightArmObjects.ForEach(rightArmObject => rightArmObject.layer = LayerMask.NameToLayer("Default"));

			body.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_Head").gameObject.layer = 0;
			body.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_Helmet").gameObject.layer = 0;

			SetAnimationType(AnimationType.PlayerUnsuited);

			InitCrouchSync();
			InitAccelerationSync();
			ThrusterManager.CreateRemotePlayerVFX(Player);

			var ikSync = body.gameObject.AddComponent<PlayerHeadRotationSync>();
			QSBCore.UnityEvents.RunWhen(() => Player.CameraBody != null, () => ikSync.Init(Player.CameraBody.transform));
		}

		private void InitAccelerationSync()
		{
			Player.JetpackAcceleration = GetComponent<JetpackAccelerationSync>();
			var thrusterModel = HasAuthority ? Locator.GetPlayerBody().GetComponent<ThrusterModel>() : null;
			Player.JetpackAcceleration.Init(thrusterModel);
		}

		private void InitCrouchSync()
		{
			_crouchSync = GetComponent<CrouchSync>();
			_crouchSync.Init(_playerController, VisibleAnimator);
		}

		private void SuitUp()
		{
			QSBEventManager.FireEvent(EventNames.QSBChangeAnimType, PlayerId, AnimationType.PlayerSuited);
			SetAnimationType(AnimationType.PlayerSuited);
		}

		private void SuitDown()
		{
			QSBEventManager.FireEvent(EventNames.QSBChangeAnimType, PlayerId, AnimationType.PlayerUnsuited);
			SetAnimationType(AnimationType.PlayerUnsuited);
		}

		public void SetSuitState(bool state)
		{
			if (!Player.IsReady)
			{
				return;
			}

			if (state)
			{
				SuitUp();
				return;
			}

			SuitDown();
		}

		public void SetAnimationType(AnimationType type)
		{
			if (CurrentType == type)
			{
				return;
			}

			CurrentType = type;
			if (_unsuitedAnimController == null)
			{
				DebugLog.ToConsole($"Error - Unsuited controller is null. ({PlayerId})", MessageType.Error);
			}

			if (_suitedAnimController == null)
			{
				DebugLog.ToConsole($"Error - Suited controller is null. ({PlayerId})", MessageType.Error);
			}

			RuntimeAnimatorController controller = default;
			switch (type)
			{
				case AnimationType.PlayerSuited:
					controller = _suitedAnimController;
					_unsuitedGraphics?.SetActive(false);
					_suitedGraphics?.SetActive(true);
					break;

				case AnimationType.PlayerUnsuited:
					controller = _unsuitedAnimController;
					_unsuitedGraphics?.SetActive(true);
					_suitedGraphics?.SetActive(false);
					break;

				case AnimationType.Chert:
					controller = _chertController;
					break;

				case AnimationType.Esker:
					controller = _eskerController;
					break;

				case AnimationType.Feldspar:
					controller = _feldsparController;
					break;

				case AnimationType.Gabbro:
					controller = _gabbroController;
					break;

				case AnimationType.Riebeck:
					controller = _riebeckController;
					break;
			}

			InvisibleAnimator.runtimeAnimatorController = controller;
			VisibleAnimator.runtimeAnimatorController = controller;
			if (type is not AnimationType.PlayerSuited and not AnimationType.PlayerUnsuited)
			{
				VisibleAnimator.SetTrigger("Playing");
				InvisibleAnimator.SetTrigger("Playing");
			}
			else
			{
				// Avoids "jumping" when exiting instrument and putting on suit
				VisibleAnimator.SetTrigger("Grounded");
				InvisibleAnimator.SetTrigger("Grounded");
			}

			NetworkAnimator.animator = InvisibleAnimator; // Probably not needed.
			Mirror.RebuildFloatParams();
			for (var i = 0; i < InvisibleAnimator.parameterCount; i++)
			{
				NetworkAnimator.SetParameterAutoSend(i, true);
			}
		}
	}
}