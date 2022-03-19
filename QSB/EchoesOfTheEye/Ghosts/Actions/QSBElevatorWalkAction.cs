﻿using GhostEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.Actions;

public class QSBElevatorWalkAction : QSBGhostAction
{
	private bool _reachedEndOfPath;

	private bool _calledToElevator;

	private bool _hasUsedElevator;

	private GhostNode _elevatorNode;

	public bool reachedEndOfPath
	{
		get
		{
			return this._reachedEndOfPath;
		}
	}

	public override GhostAction.Name GetName()
	{
		return GhostAction.Name.ElevatorWalk;
	}

	public override float CalculateUtility()
	{
		if (this._calledToElevator && !this._hasUsedElevator && !this._data.isPlayerLocationKnown)
		{
			return 100f;
		}
		if (this._calledToElevator && !this._hasUsedElevator)
		{
			return 70f;
		}
		return -100f;
	}

	public void UseElevator()
	{
		this._hasUsedElevator = true;
	}

	public void CallToUseElevator()
	{
		this._calledToElevator = true;
		if (this._controller.GetNodeMap().GetPathNodes().Length > 1)
		{
			this._elevatorNode = this._controller.GetNodeMap().GetPathNodes()[1];
			this._controller.PathfindToNode(this._elevatorNode, MoveType.PATROL);
			return;
		}
		Debug.LogError("MissingElevatorNode");
	}

	protected override void OnEnterAction()
	{
		this._controller.SetLanternConcealed(true, true);
		this._controller.FaceVelocity();
		this._effects.AttachedObject.PlayDefaultAnimation();
		this._effects.AttachedObject.SetMovementStyle(GhostEffects.MovementStyle.Normal);
		if (this._elevatorNode != null)
		{
			this._controller.PathfindToNode(this._elevatorNode, MoveType.PATROL);
		}
	}

	protected override void OnExitAction()
	{
	}

	public override bool Update_Action()
	{
		return true;
	}

	public override void OnArriveAtPosition()
	{
		this._reachedEndOfPath = true;
	}
}
