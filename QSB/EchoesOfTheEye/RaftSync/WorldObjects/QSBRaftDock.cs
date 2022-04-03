﻿using QSB.ItemSync.WorldObjects;
using QSB.Patches;
using QSB.WorldSync;

namespace QSB.EchoesOfTheEye.RaftSync.WorldObjects;

public class QSBRaftDock : WorldObject<RaftDock>, IQSBDropTarget
{
	IItemDropTarget IQSBDropTarget.AttachedObject => AttachedObject;

	public override void SendInitialState(uint to) { }

	public void OnPressInteract() =>
		QSBPatch.RemoteCall(AttachedObject.OnPressInteract);
}
