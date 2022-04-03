﻿using Cysharp.Threading.Tasks;
using MonoMod.Utils;
using OWML.Common;
using QSB.LogSync;
using QSB.Messaging;
using QSB.Player.TransformSync;
using QSB.TriggerSync.WorldObjects;
using QSB.Utility;
using QSB.Utility.LinkedWorldObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace QSB.WorldSync;

public static class QSBWorldSync
{
	public static WorldObjectManager[] Managers;

	/// <summary>
	/// Set when all WorldObjectManagers have called Init() on all their objects (AKA all the objects are created)
	/// </summary>
	public static bool AllObjectsAdded { get; private set; }
	/// <summary>
	/// Set when all WorldObjects have finished running Init()
	/// </summary>
	public static bool AllObjectsReady { get; private set; }

	private static CancellationTokenSource _cts;
	private static readonly Dictionary<WorldObjectManager, UniTask> _managersBuilding = new();
	private static readonly Dictionary<IWorldObject, UniTask> _objectsIniting = new();

	public static async UniTaskVoid BuildWorldObjects(OWScene scene)
	{
		if (_cts != null)
		{
			return;
		}

		_cts = new CancellationTokenSource();

		if (!PlayerTransformSync.LocalInstance)
		{
			DebugLog.ToConsole("Warning - Tried to build WorldObjects when LocalPlayer is not ready! Building when ready...", MessageType.Warning);
			await UniTask.WaitUntil(() => PlayerTransformSync.LocalInstance, cancellationToken: _cts.Token);
		}

		GameInit();

		foreach (var manager in Managers)
		{
			if (manager.DlcOnly && !QSBCore.DLCInstalled)
			{
				continue;
			}

			switch (manager.WorldObjectScene)
			{
				case WorldObjectScene.SolarSystem when QSBSceneManager.CurrentScene != OWScene.SolarSystem:
				case WorldObjectScene.Eye when QSBSceneManager.CurrentScene != OWScene.EyeOfTheUniverse:
					continue;
			}

			var task = UniTask.Create(async () =>
			{
				await manager.Try("building world objects", async () =>
				{
					await manager.BuildWorldObjects(scene, _cts.Token);
					DebugLog.DebugWrite($"Built {manager}", MessageType.Info);
				});
				_managersBuilding.Remove(manager);
			});
			if (!task.Status.IsCompleted())
			{
				_managersBuilding.Add(manager, task);
			}
		}

		await _managersBuilding.Values;
		if (_cts == null)
		{
			return;
		}

		AllObjectsAdded = true;
		DebugLog.DebugWrite("World Objects added.", MessageType.Success);

		if (!QSBCore.IsHost)
		{
			new RequestLinksMessage().Send();
		}

		await _objectsIniting.Values;
		if (_cts == null)
		{
			return;
		}

		AllObjectsReady = true;
		DebugLog.DebugWrite("World Objects ready.", MessageType.Success);

		DeterministicManager.WorldObjectsReady();

		if (!QSBCore.IsHost)
		{
			new RequestInitialStatesMessage().Send();
		}
	}

	public static void RemoveWorldObjects()
	{
		if (_cts == null)
		{
			return;
		}

		if (_managersBuilding.Count > 0)
		{
			DebugLog.DebugWrite($"{_managersBuilding.Count} managers still building", MessageType.Warning);
		}

		if (_objectsIniting.Count > 0)
		{
			DebugLog.DebugWrite($"{_objectsIniting.Count} objects still initing", MessageType.Warning);
		}

		_cts.Cancel();
		_cts.Dispose();
		_cts = null;

		_managersBuilding.Clear();
		_objectsIniting.Clear();
		AllObjectsAdded = false;
		AllObjectsReady = false;

		GameReset();

		foreach (var item in WorldObjects)
		{
			item.Try("removing", item.OnRemoval);
		}

		WorldObjects.Clear();
		UnityObjectsToWorldObjects.Clear();

		foreach (var manager in Managers)
		{
			manager.Try("unbuilding world objects", manager.UnbuildWorldObjects);
		}
	}

	// =======================================================================================================

	public static readonly List<CharacterDialogueTree> OldDialogueTrees = new();
	public static readonly Dictionary<string, bool> DialogueConditions = new();
	private static readonly Dictionary<string, bool> PersistentConditions = new();
	public static readonly List<FactReveal> ShipLogFacts = new();

	private static readonly List<IWorldObject> WorldObjects = new();
	private static readonly Dictionary<MonoBehaviour, IWorldObject> UnityObjectsToWorldObjects = new();

	private static void GameInit()
	{
		DebugLog.DebugWrite($"GameInit QSBWorldSync", MessageType.Info);

		OldDialogueTrees.Clear();
		OldDialogueTrees.AddRange(GetUnityObjects<CharacterDialogueTree>().SortDeterministic());

		if (!QSBCore.IsHost)
		{
			return;
		}

		DialogueConditions.Clear();
		DialogueConditions.AddRange(DialogueConditionManager.SharedInstance._dictConditions);

		PersistentConditions.Clear();
		PersistentConditions.AddRange(PlayerData._currentGameSave.dictConditions);
	}

	private static void GameReset()
	{
		DebugLog.DebugWrite($"GameReset QSBWorldSync", MessageType.Info);

		OldDialogueTrees.Clear();
		DialogueConditions.Clear();
		PersistentConditions.Clear();
		ShipLogFacts.Clear();
	}

	public static IEnumerable<IWorldObject> GetWorldObjects() => WorldObjects;

	public static IEnumerable<TWorldObject> GetWorldObjects<TWorldObject>()
		where TWorldObject : IWorldObject
		=> WorldObjects.OfType<TWorldObject>();

	public static TWorldObject GetWorldObject<TWorldObject>(this int objectId)
		where TWorldObject : IWorldObject
	{
		if (!WorldObjects.IsInRange(objectId))
		{
			DebugLog.ToConsole($"Warning - Tried to find {typeof(TWorldObject).Name} id {objectId}. Count is {WorldObjects.Count}.", MessageType.Warning);
			return default;
		}

		if (WorldObjects[objectId] is not TWorldObject worldObject)
		{
			DebugLog.ToConsole($"Error - {typeof(TWorldObject).Name} id {objectId} is actually {WorldObjects[objectId].GetType().Name}.", MessageType.Error);
			return default;
		}

		return worldObject;
	}

	public static TWorldObject GetWorldObject<TWorldObject>(this MonoBehaviour unityObject)
		where TWorldObject : IWorldObject
	{
		if (!unityObject)
		{
			DebugLog.ToConsole($"Error - Trying to run GetWorldFromUnity with a null unity object! TWorldObject:{typeof(TWorldObject).Name}, TUnityObject:NULL, Stacktrace:\r\n{Environment.StackTrace}", MessageType.Error);
			return default;
		}

		if (!UnityObjectsToWorldObjects.TryGetValue(unityObject, out var worldObject))
		{
			DebugLog.ToConsole($"Error - UnityObjectsToWorldObjects does not contain \"{unityObject.name}\"! TWorldObject:{typeof(TWorldObject).Name}, TUnityObject:{unityObject.GetType().Name}, Stacktrace:\r\n{Environment.StackTrace}", MessageType.Error);
			return default;
		}

		return (TWorldObject)worldObject;
	}

	/// <summary>
	/// not deterministic across platforms
	/// </summary>
	public static IEnumerable<TUnityObject> GetUnityObjects<TUnityObject>()
		where TUnityObject : MonoBehaviour
		=> Resources.FindObjectsOfTypeAll<TUnityObject>()
			.Where(x => x.gameObject.scene.name != null);

	public static void Init<TWorldObject, TUnityObject>()
		where TWorldObject : WorldObject<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		var list = GetUnityObjects<TUnityObject>().SortDeterministic();
		Init<TWorldObject, TUnityObject>(list);
	}

	public static void Init<TWorldObject, TUnityObject>(params Type[] typesToExclude)
		where TWorldObject : WorldObject<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		var list = GetUnityObjects<TUnityObject>()
			.Where(x => !typesToExclude.Contains(x.GetType()))
			.SortDeterministic();
		Init<TWorldObject, TUnityObject>(list);
	}

	/// <summary>
	/// make sure to sort the list!
	/// </summary>
	public static void Init<TWorldObject, TUnityObject>(IEnumerable<TUnityObject> listToInitFrom)
		where TWorldObject : WorldObject<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		foreach (var item in listToInitFrom)
		{
			var obj = new TWorldObject
			{
				AttachedObject = item,
				ObjectId = WorldObjects.Count
			};
			AddAndInit(obj, item);
		}
	}

	public static void Init<TWorldObject, TUnityObject>(Func<TUnityObject, OWTriggerVolume> triggerSelector)
		where TWorldObject : QSBTrigger<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		var list = GetUnityObjects<TUnityObject>().SortDeterministic();
		foreach (var owner in list)
		{
			var item = triggerSelector(owner);
			if (!item)
			{
				continue;
			}

			var obj = new TWorldObject
			{
				AttachedObject = item,
				ObjectId = WorldObjects.Count,
				TriggerOwner = owner
			};
			AddAndInit(obj, item);
		}
	}

	private static void AddAndInit<TWorldObject, TUnityObject>(TWorldObject worldObject, TUnityObject unityObject)
		where TWorldObject : WorldObject<TUnityObject>
		where TUnityObject : MonoBehaviour
	{
		WorldObjects.Add(worldObject);
		if (!UnityObjectsToWorldObjects.TryAdd(unityObject, worldObject))
		{
			DebugLog.ToConsole($"Error - UnityObjectsToWorldObjects already contains \"{unityObject.name}\"! TWorldObject:{typeof(TWorldObject).Name}, TUnityObject:{unityObject.GetType().Name}, Stacktrace:\r\n{Environment.StackTrace}", MessageType.Error);
			return;
		}

		var task = UniTask.Create(async () =>
		{
			await worldObject.Try("initing", () => worldObject.Init(_cts.Token));
			_objectsIniting.Remove(worldObject);
		});
		if (!task.Status.IsCompleted())
		{
			_objectsIniting.Add(worldObject, task);
		}
	}

	public static void SetDialogueCondition(string name, bool state)
	{
		if (!QSBCore.IsHost)
		{
			DebugLog.ToConsole("Warning - Cannot write to dialogue condition dict when not server!", MessageType.Warning);
			return;
		}

		DialogueConditions[name] = state;
	}

	public static void SetPersistentCondition(string name, bool state)
	{
		if (!QSBCore.IsHost)
		{
			DebugLog.ToConsole("Warning - Cannot write to persistent condition dict when not server!", MessageType.Warning);
			return;
		}

		PersistentConditions[name] = state;
	}

	public static void AddFactReveal(string id, bool saveGame)
	{
		if (!QSBCore.IsHost)
		{
			DebugLog.ToConsole("Warning - Cannot write to fact list when not server!", MessageType.Warning);
			return;
		}

		if (ShipLogFacts.Any(x => x.Id == id))
		{
			return;
		}

		ShipLogFacts.Add(new FactReveal
		{
			Id = id,
			SaveGame = saveGame
		});
	}
}
