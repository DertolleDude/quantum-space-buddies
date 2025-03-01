﻿using HarmonyLib;
using QSB.EchoesOfTheEye.EclipseElevators.VariableSync;
using System.Collections.Generic;
using UnityEngine;

namespace QSB.EchoesOfTheEye.EclipseElevators.WorldObjects;

internal class QSBEclipseElevatorController : QSBRotatingElements<EclipseElevatorController, EclipseElevatorVariableSyncer>
{
	protected override IEnumerable<SingleLightSensor> LightSensors => AttachedObject._lightSensors;

	public override string ReturnLabel()
		=> $"{base.ReturnLabel()}\r\n- SyncerValue:{NetworkBehaviour.Value?.Join()}\r\n- HasAuth:{NetworkBehaviour.hasAuthority}";

	protected override GameObject NetworkObjectPrefab => QSBNetworkManager.singleton.ElevatorPrefab;
}
