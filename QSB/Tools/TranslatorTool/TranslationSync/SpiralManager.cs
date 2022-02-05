﻿using Cysharp.Threading.Tasks;
using QSB.Tools.TranslatorTool.TranslationSync.WorldObjects;
using QSB.WorldSync;
using System.Threading;

namespace QSB.Tools.TranslatorTool.TranslationSync
{
	internal class SpiralManager : WorldObjectManager
	{
		public override WorldObjectType WorldObjectType => WorldObjectType.Both;

		public override async UniTask BuildWorldObjects(QSBScene scene, CancellationToken ct)
		{
			// wait for all late initializers (which includes nomai text) to finish
			await UniTask.WaitUntil(() => LateInitializerManager.isDoneInitializing, cancellationToken: ct);

			QSBWorldSync.Init<QSBNomaiText, NomaiText>(typeof(GhostWallText));
		}
	}
}
