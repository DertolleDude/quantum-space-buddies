﻿using QSB.EchoesOfTheEye.DreamObjectProjectors.WorldObject;
using QSB.Messaging;
using QSB.Patches;

namespace QSB.EchoesOfTheEye.DreamRafts.Messages;

public class RespawnRaftMessage : QSBWorldObjectMessage<QSBDreamObjectProjector>
{
	public override void OnReceiveRemote()
	{
		var attachedObject = (DreamRaftProjector)WorldObject.AttachedObject;
		QSBPatch.RemoteCall(attachedObject.RespawnRaft);
	}
}
