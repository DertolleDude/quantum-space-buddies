﻿using Cysharp.Threading.Tasks;
using QSB.GeyserSync.WorldObjects;
using QSB.WorldSync;
using System.Threading;

namespace QSB.GeyserSync
{
	public class GeyserManager : WorldObjectManager
	{
		public override WorldObjectType WorldObjectType => WorldObjectType.SolarSystem;

		public override async UniTask BuildWorldObjects(QSBScene scene, CancellationToken ct)
			=> QSBWorldSync.Init<QSBGeyser, GeyserController>();
	}
}