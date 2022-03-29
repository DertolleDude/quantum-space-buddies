﻿using OWML.Common;
using QSB.Animation.Player;
using QSB.Audio;
using QSB.ClientServerStateSync;
using QSB.Messaging;
using QSB.Player.Messages;
using QSB.Player.TransformSync;
using QSB.QuantumSync.WorldObjects;
using QSB.Tools;
using QSB.Utility;
using System.Linq;

namespace QSB.Player;

public partial class PlayerInfo
{
	/// <summary>
	/// the player transform sync's net id
	/// </summary>
	public uint PlayerId { get; }
	public string Name { get; set; }
	public PlayerHUDMarker HudMarker { get; set; }
	public PlayerTransformSync TransformSync { get; }
	public ClientState State { get; set; }
	public EyeState EyeState { get; set; }
	public bool IsDead { get; set; }
	public bool IsReady { get; set; }
	public bool IsInMoon { get; set; }
	public bool IsInShrine { get; set; }
	public bool IsInEyeShuttle { get; set; }
	public IQSBQuantumObject EntangledObject { get; set; }
	public QSBPlayerAudioController AudioController { get; set; }
	public bool IsLocalPlayer => TransformSync.isLocalPlayer;
	public ThrusterLightTracker ThrusterLightTracker;

	public PlayerInfo(PlayerTransformSync transformSync)
	{
		PlayerId = transformSync.netId;
		TransformSync = transformSync;
		AnimationSync = transformSync.GetComponent<AnimationSync>();
	}

	/// <summary>
	/// called on player transform sync uninit.
	/// (BOTH local and non-local)
	/// </summary>
	public void Reset()
	{
		EyeState = default;
		IsDead = default;
		IsReady = default;
		IsInMoon = default;
		IsInShrine = default;
		IsInEyeShuttle = default;
		EntangledObject = default;

		CurrentCharacterDialogueTreeId = -1;

		InDreamWorld = default;
		AssignedSimulationLantern = default;

		Campfire = default;
		HeldItem = default;
		FlashlightActive = default;
		SuitedUp = default;
		ProbeLauncherEquipped = default;
		SignalscopeEquipped = default;
		TranslatorEquipped = default;
		ProbeActive = default;
	}

	public void UpdateObjectsFromStates()
	{
		if (OWInput.GetInputMode() == InputMode.None)
		{
			// ? why is this here lmao
			return;
		}

		if (CameraBody == null)
		{
			return;
		}

		FlashLight?.UpdateState(FlashlightActive);
		Translator?.ChangeEquipState(TranslatorEquipped);
		ProbeLauncher?.ChangeEquipState(ProbeLauncherEquipped);
		Signalscope?.ChangeEquipState(SignalscopeEquipped);
		AnimationSync.SetSuitState(SuitedUp);
	}

	public void UpdateStatesFromObjects()
	{
		if (Locator.GetFlashlight() == null || Locator.GetPlayerBody() == null)
		{
			FlashlightActive = false;
			SuitedUp = false;
		}
		else
		{
			FlashlightActive = Locator.GetFlashlight()._flashlightOn;
			SuitedUp = Locator.GetPlayerBody().GetComponent<PlayerSpacesuit>().IsWearingSuit();
		}

		new PlayerInformationMessage().Send();
	}

	private QSBTool GetToolByType(ToolType type)
	{
		if (CameraBody == null)
		{
			DebugLog.ToConsole($"Warning - Tried to GetToolByType({type}) on player {PlayerId}, but CameraBody was null.", MessageType.Warning);
			return null;
		}

		var tools = CameraBody.GetComponentsInChildren<QSBTool>();

		if (tools == null || tools.Length == 0)
		{
			DebugLog.ToConsole($"Warning - Couldn't find any QSBTools for player {PlayerId}.", MessageType.Warning);
			return null;
		}

		var tool = tools.FirstOrDefault(x => x.Type == type);

		if (tool == null)
		{
			DebugLog.ToConsole($"Warning - No tool found on player {PlayerId} matching ToolType {type}.", MessageType.Warning);
		}

		return tool;
	}

	public void SetVisible(bool visible, float seconds = 0)
	{
		if (IsLocalPlayer)
		{
			return;
		}

		if (!_ditheringAnimator)
		{
			DebugLog.ToConsole($"Warning - {PlayerId}.DitheringAnimator is null!", MessageType.Warning);
			return;
		}

		_ditheringAnimator.SetVisible(visible, seconds);
	}

	public override string ToString() => $"{PlayerId}:{GetType().Name} ({Name})";
}