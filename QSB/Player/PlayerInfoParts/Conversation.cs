﻿using UnityEngine;

namespace QSB.Player;

public partial class PlayerInfo
{
	public int CurrentCharacterDialogueTreeId { get; set; } = -1;
	public GameObject CurrentDialogueBox { get; set; }
}
