using System;
using System.Collections.Generic;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public sealed class ScenePrefabIds : AssetModificationProcessor
{
    static ScenePrefabIds()
    {
        EditorApplication.hierarchyChanged += Schedule;
        EditorSceneManager.sceneOpened += (_, __) => Schedule();
    }

    static void Schedule()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.delayCall -= Assign;
        EditorApplication.delayCall += Assign;
    }

    static string[] OnWillSaveAssets(string[] paths)
    {
        if (paths.Any(p => p.EndsWith(".unity"))) Assign();
        return paths;
    }

    public static void Assign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var pickups = new HashSet<string>();
        var checkpoints = new HashSet<string>();
        var waveBossIds = new HashSet<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || (!string.IsNullOrEmpty(scene.path) && !scene.path.EndsWith(".unity"))) continue;
            var room = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoomContent>(true)).SingleOrDefault();
            foreach (var pickup in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Pickup>(true)))
                if (string.IsNullOrEmpty(pickup.persistentId) || !pickups.Add(pickup.persistentId))
                {
                    pickup.persistentId = Guid.NewGuid().ToString("N");
                    pickups.Add(pickup.persistentId);
                    EditorUtility.SetDirty(pickup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            foreach (var unlock in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AbilityUnlock>(true)))
                if (string.IsNullOrEmpty(unlock.persistentId) || !pickups.Add(unlock.persistentId))
                {
                    unlock.persistentId = Guid.NewGuid().ToString("N");
                    pickups.Add(unlock.persistentId);
                    EditorUtility.SetDirty(unlock);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(unlock);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            foreach (var wave in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveBoss>(true)))
            {
                var boss = wave.GetComponent<BossArena>().boss;
                if (string.IsNullOrEmpty(boss.persistentId) || !waveBossIds.Add(boss.persistentId))
                {
                    boss.persistentId = Guid.NewGuid().ToString("N");
                    waveBossIds.Add(boss.persistentId);
                    EditorUtility.SetDirty(boss);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(boss);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
            foreach (var point in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Checkpoint>(true)))
            {
                bool changed = room != null && point.roomId != room.roomId;
                if (room != null) point.roomId = room.roomId;
                if (string.IsNullOrEmpty(point.persistentId) || !checkpoints.Add(point.persistentId))
                {
                    point.persistentId = Guid.NewGuid().ToString("N");
                    checkpoints.Add(point.persistentId);
                    changed = true;
                }
                if (!changed) continue;
                EditorUtility.SetDirty(point);
                PrefabUtility.RecordPrefabInstancePropertyModifications(point);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
