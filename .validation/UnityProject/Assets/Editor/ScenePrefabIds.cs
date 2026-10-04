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
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !scene.path.EndsWith(".unity")) continue;
            var room = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoomContent>(true)).SingleOrDefault();
            if (room == null) continue;
            foreach (var root in scene.GetRootGameObjects())
                if (root != room.gameObject && (root.GetComponent<GroundSurface>() || root.GetComponent<Pickup>() || root.GetComponent<GrapplePoint>() || root.GetComponent<Checkpoint>() || root.GetComponent<RoomExit>()))
                {
                    root.transform.SetParent(room.transform, true);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            foreach (var pickup in room.GetComponentsInChildren<Pickup>(true))
                if (string.IsNullOrEmpty(pickup.persistentId) || !pickups.Add(pickup.persistentId))
                {
                    pickup.persistentId = Guid.NewGuid().ToString("N");
                    pickups.Add(pickup.persistentId);
                    EditorUtility.SetDirty(pickup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            foreach (var point in room.GetComponentsInChildren<Checkpoint>(true))
            {
                bool changed = point.roomId != room.roomId;
                point.roomId = room.roomId;
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
