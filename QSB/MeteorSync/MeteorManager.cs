﻿using Cysharp.Threading.Tasks;
using QSB.MeteorSync.WorldObjects;
using QSB.WorldSync;
using System.Linq;
using System.Threading;

namespace QSB.MeteorSync
{
	public class MeteorManager : WorldObjectManager
	{
		public override WorldObjectType WorldObjectType => WorldObjectType.SolarSystem;

		public static WhiteHoleVolume WhiteHoleVolume;

		public override async UniTask BuildWorldObjects(QSBScene scene, CancellationToken ct)
		{
			// wait for all late initializers (which includes meteor launchers) to finish
			await UniTask.WaitUntil(() => LateInitializerManager.isDoneInitializing, cancellationToken: ct);

			WhiteHoleVolume = QSBWorldSync.GetUnityObjects<WhiteHoleVolume>().First();
			QSBWorldSync.Init<QSBMeteorLauncher, MeteorLauncher>();
			QSBWorldSync.Init<QSBMeteor, MeteorController>();
			QSBWorldSync.Init<QSBFragment, FragmentIntegrity>();
		}
	}
}
