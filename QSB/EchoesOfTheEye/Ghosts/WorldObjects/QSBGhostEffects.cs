﻿using QSB.EchoesOfTheEye.Ghosts.Messages;
using QSB.Messaging;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.WorldObjects;

public class QSBGhostEffects : WorldObject<GhostEffects>, IGhostObject
{
	public override void SendInitialState(uint to)
	{
		// todo SendInitialState
	}

	public override bool ShouldDisplayDebug() => false;

	private QSBGhostData _data;

	public void Initialize(Transform nodeRoot, QSBGhostController controller, QSBGhostData data)
	{
		AttachedObject._animator = AttachedObject.GetComponent<Animator>();
		AttachedObject._controller = controller.AttachedObject;
		_data = data;
		if (AttachedObject._feetAudioSourceFar != null)
		{
			AttachedObject._stompyFootsteps = true;
		}

		AttachedObject.SetEyeGlow(AttachedObject._eyeGlow);
	}

	public bool AllowFootstepAudio(bool usingTimer)
	{
		var flag = AttachedObject._stompyFootsteps || _data.currentAction == GhostAction.Name.Chase || _data.currentAction == GhostAction.Name.Grab;
		if (usingTimer != flag)
		{
			return false;
		}

		var num = AttachedObject._stompyFootsteps ? AttachedObject._feetAudioSourceFar.maxDistance : AttachedObject._feetAudioSourceNear.maxDistance;
		return _data.localPlayer.playerLocation.distance < num + 5f;
	}

	public void PlayLanternAudio(AudioType audioType)
	{
		if (_data.localPlayer.playerLocation.distance < AttachedObject._lanternAudioSource.GetAudioSource().maxDistance + 5f)
		{
			AttachedObject._lanternAudioSource.PlayOneShot(audioType, 1f);
		}
	}

	public void Update_Effects()
	{
		if (_data == null)
		{
			return;
		}

		if (AttachedObject._waitToRespondToHelpCall && Time.time >= AttachedObject._respondToHelpCallTime)
		{
			AttachedObject._waitToRespondToHelpCall = false;
			PlayVoiceAudioFar(AudioType.Ghost_CallForHelpResponse, 1f);
		}

		if (AttachedObject._waitForGrabWindow && AttachedObject._animator.GetFloat(GhostEffects.AnimatorKeys.AnimCurve_GrabWindow) > 0.5f)
		{
			AttachedObject._waitForGrabWindow = false;
			AttachedObject.PlayGrabAudio(AudioType.Ghost_Grab_Swish);
		}

		AttachedObject.Update_Footsteps();

		var relativeVelocity = AttachedObject._controller.GetRelativeVelocity();
		var num = (AttachedObject._movementStyle == GhostEffects.MovementStyle.Chase) ? 8f : 2f;
		var targetValue = new Vector2(relativeVelocity.x / num, relativeVelocity.z / num);
		AttachedObject._smoothedMoveSpeed = AttachedObject._moveSpeedSpring.Update(AttachedObject._smoothedMoveSpeed, targetValue, Time.deltaTime);
		AttachedObject._animator.SetFloat(GhostEffects.AnimatorKeys.Float_MoveDirectionX, AttachedObject._smoothedMoveSpeed.x);
		AttachedObject._animator.SetFloat(GhostEffects.AnimatorKeys.Float_MoveDirectionY, AttachedObject._smoothedMoveSpeed.y);

		AttachedObject._smoothedTurnSpeed = AttachedObject._turnSpeedSpring.Update(AttachedObject._smoothedTurnSpeed, AttachedObject._controller.GetAngularVelocity() / 90f, Time.deltaTime);
		AttachedObject._animator.SetFloat(GhostEffects.AnimatorKeys.Float_TurnSpeed, AttachedObject._smoothedTurnSpeed);

		var target = _data.isIlluminated ? 1f : 0f;
		var num2 = _data.isIlluminated ? 8f : 0.8f;
		AttachedObject._eyeGlow = Mathf.MoveTowards(AttachedObject._eyeGlow, target, Time.deltaTime * num2);
		var closestPlayer = QSBPlayerManager.GetClosestPlayerToWorldPoint(AttachedObject.transform.position, true);
		var num3 = (closestPlayer?.AssignedSimulationLantern?.AttachedObject?.GetLanternController()?.GetLight()?.GetFlickerScale() - 1f + 0.07f) / 0.14f ?? 0;
		num3 = Mathf.Lerp(0.7f, 1f, num3);
		AttachedObject.SetEyeGlow(AttachedObject._eyeGlow * num3);

		if (AttachedObject._playingDeathSequence)
		{
			var @float = AttachedObject._animator.GetFloat(GhostEffects.AnimatorKeys.AnimCurve_DeathFade);
			for (var i = 0; i < AttachedObject._dissolveRenderers.Length; i++)
			{
				AttachedObject._dissolveRenderers[i].SetMaterialProperty(AttachedObject._propID_DissolveProgress, @float);
			}

			for (var j = 0; j < AttachedObject._ditherRenderers.Length; j++)
			{
				AttachedObject._ditherRenderers[j].SetDitherFade(@float);
			}

			if (AttachedObject._deathAnimComplete && (AttachedObject._deathParticleSystem == null || !AttachedObject._deathParticleSystem.isPlaying))
			{
				AttachedObject._playingDeathSequence = false;
				AttachedObject._controller.gameObject.SetActive(false);
				AttachedObject.OnGhostDeathComplete.Invoke();
				for (var k = 0; k < AttachedObject._dissolveRenderers.Length; k++)
				{
					AttachedObject._dissolveRenderers[k].SetMaterialProperty(AttachedObject._propID_DissolveProgress, 0f);
				}

				for (var l = 0; l < AttachedObject._ditherRenderers.Length; l++)
				{
					AttachedObject._ditherRenderers[l].SetDitherFade(0f);
				}
			}
		}
	}

	public void SetMovementStyle(GhostEffects.MovementStyle style, bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new SetMovementStyleMessage(style));
		}

		AttachedObject.SetMovementStyle(style);
	}

	public void PlayVoiceAudioNear(AudioType audioType, float volumeScale, bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new PlayVoiceAudioMessage(audioType, volumeScale, true));
		}

		AttachedObject.PlayVoiceAudioNear(audioType, volumeScale);
	}

	public void PlayVoiceAudioFar(AudioType audioType, float volumeScale, bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new PlayVoiceAudioMessage(audioType, volumeScale, false));
		}

		AttachedObject.PlayVoiceAudioFar(audioType, volumeScale);
	}

	public virtual void PlaySleepAnimation(bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new GhostAnimationTriggerMessage(GhostAnimationType.Sleep));
		}

		AttachedObject.PlaySleepAnimation();
	}

	public virtual void PlayDefaultAnimation(bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new GhostAnimationTriggerMessage(GhostAnimationType.Default));
		}

		AttachedObject.PlayDefaultAnimation();
	}

	public virtual void PlayGrabAnimation(bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new GhostAnimationTriggerMessage(GhostAnimationType.Grab));
		}

		AttachedObject.PlayGrabAnimation();
	}

	public virtual void PlayBlowOutLanternAnimation(bool fast = false, bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new GhostAnimationTriggerMessage(fast ? GhostAnimationType.BlowOutLanternFast : GhostAnimationType.BlowOutLanternNormal));
		}

		AttachedObject.PlayBlowOutLanternAnimation(fast);
	}

	public virtual void PlaySnapNeckAnimation(bool remote = false)
	{
		if (!remote)
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			this.SendMessage(new GhostAnimationTriggerMessage(GhostAnimationType.SnapNeck));
		}

		AttachedObject.PlaySnapNeckAnimation();
	}
}