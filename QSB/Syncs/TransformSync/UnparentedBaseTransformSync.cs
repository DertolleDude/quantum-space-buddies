﻿using OWML.Common;
using QSB.Player.TransformSync;
using QSB.Utility;
using QuantumUNET.Transport;
using System.Linq;
using UnityEngine;

namespace QSB.Syncs.TransformSync
{
	public abstract class UnparentedBaseTransformSync : SyncBase
	{
		public virtual void Start()
		{
			var lowestBound = Resources.FindObjectsOfTypeAll<PlayerTransformSync>()
				.Where(x => x.NetId.Value <= NetId.Value).OrderBy(x => x.NetId.Value).Last();
			NetIdentity.SetRootIdentity(lowestBound.NetIdentity);

			DontDestroyOnLoad(gameObject);
			_intermediaryTransform = new IntermediaryTransform(transform);
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
		}

		protected virtual void OnDestroy()
		{
			if (!HasAuthority && AttachedObject != null)
			{
				Destroy(AttachedObject.gameObject);
			}

			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
		}

		protected void OnSceneLoaded(OWScene scene, bool isInUniverse) =>
			_isInitialized = false;

		protected override void Init()
		{
			AttachedObject = HasAuthority ? InitLocalTransform() : InitRemoteTransform();
			_isInitialized = true;
		}

		public override void SerializeTransform(QNetworkWriter writer)
		{
			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			var worldPos = _intermediaryTransform.GetPosition();
			var worldRot = _intermediaryTransform.GetRotation();
			writer.Write(worldPos);
			SerializeRotation(writer, worldRot);
			_prevPosition = worldPos;
			_prevRotation = worldRot;
		}

		public override void DeserializeTransform(QNetworkReader reader)
		{
			if (!QSBCore.WorldObjectsReady)
			{
				reader.ReadVector3();
				DeserializeRotation(reader);
				return;
			}

			var pos = reader.ReadVector3();
			var rot = DeserializeRotation(reader);

			if (HasAuthority)
			{
				return;
			}

			if (_intermediaryTransform == null)
			{
				_intermediaryTransform = new IntermediaryTransform(transform);
			}

			_intermediaryTransform.SetPosition(pos);
			_intermediaryTransform.SetRotation(rot);

			if (_intermediaryTransform.GetPosition() == Vector3.zero)
			{
				DebugLog.ToConsole($"Warning - {PlayerId}.{GetType().Name} at (0,0,0)! - Given position was {pos}", MessageType.Warning);
			}
		}

		protected override void UpdateTransform()
		{
			if (HasAuthority)
			{
				_intermediaryTransform.EncodePosition(AttachedObject.transform.position);
				_intermediaryTransform.EncodeRotation(AttachedObject.transform.rotation);
				return;
			}

			var targetPos = _intermediaryTransform.GetTargetPosition_Unparented();
			var targetRot = _intermediaryTransform.GetTargetRotation_Unparented();
			if (targetPos != Vector3.zero && _intermediaryTransform.GetTargetPosition_ParentedToReference() != Vector3.zero)
			{
				if (UseInterpolation)
				{
					AttachedObject.transform.position = SmartSmoothDamp(AttachedObject.transform.position, targetPos);
					AttachedObject.transform.rotation = QuaternionHelper.SmoothDamp(AttachedObject.transform.rotation, targetRot, ref _rotationSmoothVelocity, SmoothTime);
				}
				else
				{
					AttachedObject.transform.position = targetPos;
					AttachedObject.transform.rotation = targetRot;
				}
			}
		}

		public override bool HasMoved()
		{
			var displacementMagnitude = (_intermediaryTransform.GetPosition() - _prevPosition).magnitude;
			return displacementMagnitude > 1E-03f
				|| Quaternion.Angle(_intermediaryTransform.GetRotation(), _prevRotation) > 1E-03f;
		}

		public void SetReferenceTransform(Transform transform)
		{
			if (ReferenceTransform == transform)
			{
				return;
			}

			DebugLog.DebugWrite($"{PlayerId}.{GetType().Name} set reference transform to {transform.name}");
			ReferenceTransform = transform;
			_intermediaryTransform.SetReferenceTransform(transform);
		}
	}
}
