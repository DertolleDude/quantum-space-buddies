﻿using OWML.Common;
using QSB.Messaging;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;

namespace QSB.ConversationSync.Messages;

public class ConversationStartEndMessage : QSBMessage<(int TreeId, bool Start)>
{
	public ConversationStartEndMessage(int treeId, bool start)
	{
		Data.TreeId = treeId;
		Data.Start = start;
	}

	public override bool ShouldReceive => QSBWorldSync.AllObjectsReady;

	public override void OnReceiveRemote()
	{
		if (Data.TreeId == -1)
		{
			DebugLog.ToConsole("Warning - Received conv. start/end event with char id -1.", MessageType.Warning);
			return;
		}

		var dialogueTree = QSBWorldSync.OldDialogueTrees[Data.TreeId];

		if (Data.Start)
		{
			StartConversation(From, Data.TreeId, dialogueTree);
		}
		else
		{
			EndConversation(From, dialogueTree);
		}
	}

	private static void StartConversation(
		uint playerId,
		int treeId,
		CharacterDialogueTree tree)
	{
		QSBPlayerManager.GetPlayer(playerId).CurrentCharacterDialogueTreeId = treeId;
		tree.GetInteractVolume().DisableInteraction();
	}

	private static void EndConversation(
		uint playerId,
		CharacterDialogueTree tree)
	{
		QSBPlayerManager.GetPlayer(playerId).CurrentCharacterDialogueTreeId = -1;
		tree.GetInteractVolume().EnableInteraction();
	}
}