﻿using HarmonyLib;
using QSB.EchoesOfTheEye.LightSensorSync.WorldObjects;
using QSB.Patches;
using QSB.Player;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.EchoesOfTheEye.LightSensorSync.Patches;

[HarmonyPatch(typeof(SingleLightSensor))]
internal class LightSensorPatches : QSBPatch
{
	public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

	[HarmonyPrefix]
	[HarmonyPatch(nameof(SingleLightSensor.ManagedFixedUpdate))]
	private static bool ManagedFixedUpdate(SingleLightSensor __instance)
	{
		if (!QSBWorldSync.AllObjectsReady)
		{
			return true;
		}

		if (!__instance.TryGetWorldObject(out QSBLightSensor qsbLightSensor))
		{
			return true;
		}

		if (__instance._fixedUpdateFrameDelayCount > 0)
		{
			__instance._fixedUpdateFrameDelayCount--;
			return false;
		}

		var locallyIlluminated = qsbLightSensor.LocallyIlluminated;
		var illuminated = qsbLightSensor._illuminated;
		__instance.UpdateIllumination();
		if (!locallyIlluminated && qsbLightSensor.LocallyIlluminated)
		{
			qsbLightSensor.OnDetectLocalLight?.Invoke();
		}
		else if (locallyIlluminated && !__instance._illuminated)
		{
			qsbLightSensor.OnDetectLocalDarkness?.Invoke();
		}

		__instance._illuminated = qsbLightSensor._illuminated;
		if (!illuminated && qsbLightSensor._illuminated)
		{
			__instance.OnDetectLight.Invoke();
		}
		else if (illuminated && !qsbLightSensor._illuminated)
		{
			__instance.OnDetectDarkness.Invoke();
		}

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(SingleLightSensor.UpdateIllumination))]
	private static bool UpdateIllumination(SingleLightSensor __instance)
	{
		if (!QSBWorldSync.AllObjectsReady)
		{
			return true;
		}

		if (!__instance.TryGetWorldObject(out QSBLightSensor qsbLightSensor))
		{
			return true;
		}

		qsbLightSensor.LocallyIlluminated = false;
		qsbLightSensor._illuminated = false;
		__instance._illuminatingDreamLanternList?.Clear();
		if (__instance._lightSources == null || __instance._lightSources.Count == 0)
		{
			return false;
		}

		var vector = __instance.transform.TransformPoint(__instance._localSensorOffset);
		var sensorWorldDir = Vector3.zero;
		if (__instance._directionalSensor)
		{
			sensorWorldDir = __instance.transform.TransformDirection(__instance._localDirection).normalized;
		}

		foreach (var lightSource in __instance._lightSources)
		{
			if ((__instance._lightSourceMask & lightSource.GetLightSourceType()) == lightSource.GetLightSourceType()
				&& lightSource.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance))
			{
				switch (lightSource.GetLightSourceType())
				{
					case LightSourceType.UNDEFINED:
						{
							var owlight = lightSource as OWLight2;
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								qsbLightSensor._illuminated = true;
							}

							break;
						}
					case LightSourceType.FLASHLIGHT:
						{
							var position = Locator.GetPlayerCamera().transform.position;
							var to = __instance.transform.position - position;
							if (Vector3.Angle(Locator.GetPlayerCamera().transform.forward, to) <= __instance._maxSpotHalfAngle
								&& !__instance.CheckOcclusion(position, vector, sensorWorldDir))
							{
								qsbLightSensor.LocallyIlluminated = true;
								qsbLightSensor._illuminated = true;
							}

							break;
						}
					case LightSourceType.PROBE:
						{
							var probe = Locator.GetProbe();
							if (probe != null
								&& probe.IsLaunched()
								&& !probe.IsRetrieving()
								&& probe.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(probe.GetLightSourcePosition(), vector, sensorWorldDir))
							{
								qsbLightSensor.LocallyIlluminated = true;
								qsbLightSensor._illuminated = true;
							}

							break;
						}
					case LightSourceType.DREAM_LANTERN:
						{
							var dreamLanternController = lightSource as DreamLanternController;
							if (dreamLanternController.IsLit()
								&& dreamLanternController.IsFocused(__instance._lanternFocusThreshold)
								&& dreamLanternController.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(dreamLanternController.GetLightPosition(), vector, sensorWorldDir))
							{
								__instance._illuminatingDreamLanternList.Add(dreamLanternController);
								var dreamLanternItem = dreamLanternController.GetComponent<DreamLanternItem>();
								qsbLightSensor.LocallyIlluminated |= QSBPlayerManager.LocalPlayer.HeldItem?.AttachedObject == dreamLanternItem;
								qsbLightSensor._illuminated = true;
							}

							break;
						}
					case LightSourceType.SIMPLE_LANTERN:
						foreach (var owlight in lightSource.GetLights())
						{
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							var maxDistance = Mathf.Min(__instance._maxSimpleLanternDistance, __instance._maxDistance);
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								var simpleLanternItem = (SimpleLanternItem)lightSource;
								qsbLightSensor.LocallyIlluminated |= QSBPlayerManager.LocalPlayer.HeldItem?.AttachedObject == simpleLanternItem;
								qsbLightSensor._illuminated = true;
								break;
							}
						}

						break;
					case LightSourceType.VOLUME_ONLY:
						qsbLightSensor._illuminated = true;
						break;
				}
			}
		}

		return false;
	}
}
