﻿using QSB.EchoesOfTheEye.DreamObjectProjectors.WorldObject;
using QSB.Messaging;
using QSB.Patches;

namespace QSB.EchoesOfTheEye.DreamObjectProjectors.Messages;

internal class ProjectorLitMessage : QSBWorldObjectMessage<QSBDreamObjectProjector, bool>
{
	public ProjectorLitMessage(bool lit) : base(lit) { }

	public override void OnReceiveRemote()
		=> QSBPatch.RemoteCall(() => WorldObject.AttachedObject.SetLit(Data));
}
