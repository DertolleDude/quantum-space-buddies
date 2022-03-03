﻿using QSB.Messaging;
using QSB.QuantumSync.WorldObjects;

namespace QSB.QuantumSync.Messages;

internal class MoveSkeletonMessage : QSBWorldObjectMessage<QSBQuantumSkeletonTower, int>
{
	public MoveSkeletonMessage(int index) => Data = index;

	public override void OnReceiveRemote() => WorldObject.MoveSkeleton(Data);
}