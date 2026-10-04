using System.IO;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RoomPreview
{
    const string Path = "Assets/Scenes/HollowGeometry.unity";
    static RoomPreview()
    {
        EditorApplication.delayCall += RefreshOldScenes;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RefreshOldScenes;
        };
    }
    static void RefreshOldScenes()
    {
        if (Object.FindObjectOfType<DemoGame>() == null || Object.FindObjectOfType<WorldMap>() != null ||
            !File.Exists(Path) || !File.ReadAllText(Path).Contains("legacyFogRegions:")) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.LogWarning("编辑器仍打开旧房间场景，且存在未保存改动。请先另存改动，再通过 洞穴 Demo/打开整体地图 重新打开场景。");
                return;
            }
        EditorSceneManager.OpenScene(Path);
        Debug.Log("DEMO_EDITOR_SCENE_REFRESH_OK: reopened saved continuous map instead of cached room scenes.");
    }
    [MenuItem("洞穴 Demo/打开整体地图")]
    public static void ShowAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Path);
    }
}
