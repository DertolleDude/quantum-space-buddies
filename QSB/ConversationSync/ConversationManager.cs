﻿using Cysharp.Threading.Tasks;
using OWML.Common;
using QSB.ConversationSync.Messages;
using QSB.ConversationSync.WorldObjects;
using QSB.Messaging;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace QSB.ConversationSync
{
	public class ConversationManager : WorldObjectManager
	{
		public override WorldObjectType WorldObjectType => WorldObjectType.Both;

		public static ConversationManager Instance { get; private set; }
		public Dictionary<CharacterDialogueTree, GameObject> BoxMappings { get; } = new Dictionary<CharacterDialogueTree, GameObject>();

		private GameObject _boxPrefab;

		public void Start()
		{
			Instance = this;

			_boxPrefab = QSBCore.ConversationAssetBundle.LoadAsset<GameObject>("assets/Prefabs/dialoguebubble.prefab");

			var font = (Font)Resources.Load(@"fonts\english - latin\HVD Fonts - BrandonGrotesque-Bold_Dynamic");
			if (font == null)
			{
				DebugLog.ToConsole("Error - Font is null!", MessageType.Error);
			}

			_boxPrefab.GetComponent<Text>().font = font;
			_boxPrefab.GetComponent<Text>().color = Color.white;
		}

		public override async UniTask BuildWorldObjects(QSBScene scene, CancellationToken ct)
			=> QSBWorldSync.Init<QSBRemoteDialogueTrigger, RemoteDialogueTrigger>();

		public uint GetPlayerTalkingToTree(CharacterDialogueTree tree)
		{
			var treeIndex = QSBWorldSync.OldDialogueTrees.IndexOf(tree);
			return QSBPlayerManager.PlayerList.All(x => x.CurrentCharacterDialogueTreeId != treeIndex)
				? uint.MaxValue
				: QSBPlayerManager.PlayerList.First(x => x.CurrentCharacterDialogueTreeId == treeIndex).PlayerId;
		}

		public void SendPlayerOption(string text) =>
			new ConversationMessage(ConversationType.Player, (int)QSBPlayerManager.LocalPlayerId, text).Send();

		public void SendCharacterDialogue(int id, string text)
		{
			if (id == -1)
			{
				DebugLog.ToConsole("Warning - Tried to send conv. event with char id -1.", MessageType.Warning);
				return;
			}

			new ConversationMessage(ConversationType.Character, id, text).Send();
		}

		public void CloseBoxPlayer() =>
			new ConversationMessage(ConversationType.ClosePlayer, (int)QSBPlayerManager.LocalPlayerId).Send();

		public void CloseBoxCharacter(int id) =>
			new ConversationMessage(ConversationType.CloseCharacter, id).Send();

		public void SendConvState(int charId, bool state)
		{
			if (charId == -1)
			{
				DebugLog.ToConsole("Warning - Tried to send conv. start/end event with char id -1.", MessageType.Warning);
				return;
			}

			new ConversationStartEndMessage(charId, state).Send();
		}

		public void DisplayPlayerConversationBox(uint playerId, string text)
		{
			if (playerId == QSBPlayerManager.LocalPlayerId)
			{
				DebugLog.ToConsole("Error - Cannot display conversation box for local player!", MessageType.Error);
				return;
			}

			var player = QSBPlayerManager.GetPlayer(playerId);

			// Destroy old box if it exists
			var playerBox = player.CurrentDialogueBox;
			if (playerBox != null)
			{
				Destroy(playerBox);
			}

			QSBPlayerManager.GetPlayer(playerId).CurrentDialogueBox = CreateBox(player.Body.transform, 2, text);
		}

		public void DisplayCharacterConversationBox(int index, string text)
		{
			if (QSBWorldSync.OldDialogueTrees.ElementAtOrDefault(index) == null)
			{
				DebugLog.ToConsole($"Error - Tried to display character conversation box for id {index}! (Doesn't exist!)", MessageType.Error);
				return;
			}

			// Remove old box if it exists
			var oldDialogueTree = QSBWorldSync.OldDialogueTrees[index];
			if (BoxMappings.ContainsKey(oldDialogueTree))
			{
				Destroy(BoxMappings[oldDialogueTree]);
				BoxMappings.Remove(oldDialogueTree);
			}

			BoxMappings.Add(oldDialogueTree, CreateBox(oldDialogueTree.gameObject.transform, 2, text));
		}

		private GameObject CreateBox(Transform parent, float vertOffset, string text)
		{
			var newBox = Instantiate(_boxPrefab);
			newBox.SetActive(false);
			newBox.transform.SetParent(parent);
			newBox.transform.localPosition = new Vector3(0, vertOffset, 0);
			newBox.transform.rotation = parent.rotation;
			newBox.GetComponent<Text>().text = text;
			newBox.SetActive(true);
			return newBox;
		}
	}
}