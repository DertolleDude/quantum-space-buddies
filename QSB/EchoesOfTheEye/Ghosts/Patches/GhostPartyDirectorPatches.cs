﻿using GhostEnums;
using HarmonyLib;
using QSB.EchoesOfTheEye.Ghosts.WorldObjects;
using QSB.Patches;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.Patches;

[HarmonyPatch(typeof(GhostPartyDirector))]
internal class GhostPartyDirectorPatches : QSBPatch
{
	public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

	[HarmonyPrefix]
	[HarmonyPatch(nameof(GhostPartyDirector.UnlockGhostForAmbush))]
	public static bool UnlockGhostForAmbush(GhostPartyDirector __instance, bool firstAmbush)
	{
		if (__instance._ghostsWaitingToAmbush.Count == 0)
		{
			return false;
		}

		var index = Random.Range(0, __instance._ghostsWaitingToAmbush.Count);
		var ghost = __instance._ghostsWaitingToAmbush[index].GetWorldObject<QSBGhostBrain>();
		(ghost.GetAction(GhostAction.Name.PartyHouse) as QSBPartyHouseAction).AllowChasePlayer();
		ghost.HintPlayerLocation();
		if (firstAmbush)
		{
			ghost.GetEffects().PlayVoiceAudioNear(global::AudioType.Ghost_Stalk, 1f);
		}

		__instance._ghostsWaitingToAmbush.QuickRemoveAt(index);

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(GhostPartyDirector.OnEnterAmbushTrigger))]
	public static bool OnEnterAmbushTrigger(GhostPartyDirector __instance, GameObject hitObj)
	{
		if (__instance._ambushTriggered)
		{
			return false;
		}
		if (hitObj.CompareTag("PlayerDetector"))
		{
			__instance._ambushTriggeredThisLoop = true;
			__instance._ambushTriggered = true;
			__instance._waitingToAmbushInitial = true;
			__instance._ambushTriggerTime = Time.time + (__instance._ambushTriggeredThisLoop ? __instance._secondaryAmbushDelay : __instance._initialAmbushDelay);
			(__instance._fireplaceGhost.GetWorldObject<QSBGhostBrain>().GetAction(GhostAction.Name.PartyHouse) as QSBPartyHouseAction).LookAtPlayer(0f, TurnSpeed.MEDIUM);
			for (int i = 0; i < __instance._ambushGhosts.Length; i++)
			{
				float delay = (float)i;
				(__instance._ambushGhosts[i].GetWorldObject<QSBGhostBrain>().GetAction(GhostAction.Name.PartyHouse) as QSBPartyHouseAction).LookAtPlayer(delay, TurnSpeed.SLOWEST);
			}
		}

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(GhostPartyDirector.OnEnterSector))]
	public static bool OnEnterSector(GhostPartyDirector __instance, SectorDetector detector)
	{
		if (__instance._connectedDreamCampfire != null && __instance._connectedDreamCampfire.GetState() == Campfire.State.UNLIT)
		{
			return false;
		}

		if (detector.GetOccupantType() == DynamicOccupant.Player)
		{
			__instance._partyMusicController.FadeIn(3f);
			__instance._ghostsWaitingToAmbush.Clear();
			__instance._ghostsWaitingToAmbush.AddRange(__instance._ambushGhosts);
			for (int i = 0; i < __instance._directedGhosts.Length; i++)
			{
				(__instance._directedGhosts[i].GetWorldObject<QSBGhostBrain>().GetAction(GhostAction.Name.PartyHouse) as QSBPartyHouseAction).ResetAllowChasePlayer();
			}
		}

		return false;
	}
}
