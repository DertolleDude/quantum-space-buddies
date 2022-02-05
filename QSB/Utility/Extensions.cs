﻿using Cysharp.Threading.Tasks;
using Mirror;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace QSB.Utility
{
	public static class Extensions
	{
		#region UNITY

		public static Quaternion TransformRotation(this Transform transform, Quaternion localRotation)
			=> transform.rotation * localRotation;

		public static GameObject InstantiateInactive(this GameObject original)
		{
			original.SetActive(false);
			var copy = Object.Instantiate(original);
			original.SetActive(true);
			return copy;
		}

		public static Transform InstantiateInactive(this Transform original) =>
			original.gameObject.InstantiateInactive().transform;

		#endregion

		#region MIRROR

		public static uint GetPlayerId(this NetworkConnection conn)
		{
			if (!conn.identity)
			{
				// wtf
				DebugLog.ToConsole($"Error - GetPlayerId on {conn.address} has no identity\n{Environment.StackTrace}", MessageType.Error);
				return uint.MaxValue;
			}

			return conn.identity.netId;
		}

		public static NetworkConnection GetNetworkConnection(this uint playerId)
		{
			var conn = NetworkServer.connections.Values.FirstOrDefault(x => playerId == x.GetPlayerId());
			if (conn == default)
			{
				DebugLog.ToConsole($"Error - GetNetworkConnection on {playerId} found no connection\n{Environment.StackTrace}", MessageType.Error);
			}

			return conn;
		}

		public static void SpawnWithServerAuthority(this GameObject go) =>
			NetworkServer.Spawn(go, NetworkServer.localConnection);

		#endregion

		#region C#

		public static void SafeInvoke(this MulticastDelegate multicast, params object[] args)
		{
			foreach (var del in multicast.GetInvocationList())
			{
				try
				{
					del.DynamicInvoke(args);
				}
				catch (TargetInvocationException ex)
				{
					DebugLog.ToConsole($"Error invoking delegate! {ex.InnerException}", MessageType.Error);
				}
			}
		}

		public static float Map(this float value, float inputFrom, float inputTo, float outputFrom, float outputTo, bool clamp)
		{
			var mappedValue = (value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom) + outputFrom;

			return clamp
				? Mathf.Clamp(mappedValue, outputTo, outputFrom)
				: mappedValue;
		}

		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (var item in enumerable)
			{
				action(item);
			}
		}

		public static int IndexOf<T>(this T[] array, T value) => Array.IndexOf(array, value);

		public static bool IsInRange<T>(this IList<T> list, int index) => index >= 0 && index < list.Count;

		public static void RaiseEvent<T>(this T instance, string eventName, params object[] args)
		{
			const BindingFlags flags = BindingFlags.Instance
				| BindingFlags.Static
				| BindingFlags.Public
				| BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly;
			if (typeof(T)
					.GetField(eventName, flags)?
					.GetValue(instance) is not MulticastDelegate multiDelegate)
			{
				return;
			}

			multiDelegate.GetInvocationList().ForEach(dl => dl.DynamicInvoke(args));
		}

		public static IEnumerable<Type> GetDerivedTypes(this Type type) => type.Assembly.GetTypes()
			.Where(x => !x.IsInterface && !x.IsAbstract && type.IsAssignableFrom(x));

		public static Guid ToGuid(this int value)
		{
			var bytes = new byte[16];
			BitConverter.GetBytes(value).CopyTo(bytes, 0);
			return new Guid(bytes);
		}

		public static void Try(this object self, string description, Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				DebugLog.ToConsole($"{self} - error {description} : {e}", MessageType.Error);
			}
		}

		public static async UniTask Try(this object self, string description, Func<UniTask> func)
		{
			try
			{
				await func();
			}
			catch (Exception e)
			{
				DebugLog.ToConsole($"{self} - error {description} : {e}", MessageType.Error);
			}
		}

		#endregion

		#region CUSTOM

		public static QSBScene ToQSBScene(this Scene scene)
		{
			switch (scene.name)
			{
				case "TitleScreen":
					return QSBScene.TitleScreen;
				case "SolarSystem":
					return QSBScene.SolarSystem;
				case "EyeOfTheUniverse":
					return QSBScene.EyeOfTheUniverse;
				case "Credits_Fast":
					return QSBScene.Credits_Fast;
				case "Credits_Final":
					return QSBScene.Credits_Final;
				case "PostCreditsScene":
					return QSBScene.PostCreditsScene;
				case "DebugScene":
					return QSBScene.DebugScene;
				default:
					return QSBScene.Undefined;
			}
		}

		#endregion
	}
}
