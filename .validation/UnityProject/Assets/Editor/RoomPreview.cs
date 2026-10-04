using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RoomPreview
{
    const string ShellPath = "Assets/Scenes/HollowGeometry.unity";

    static RoomPreview()
    {
        EditorSceneManager.sceneOpened += (_, __) => Schedule();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Schedule();
        };
        Schedule();
    }

    static void Schedule()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall -= ShowIfEditing;
        EditorApplication.delayCall += ShowIfEditing;
    }

    static void ShowIfEditing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !SceneManager.GetSceneByPath(ShellPath).isLoaded) return;
        ShowAll();
    }

    [MenuItem("洞穴 Demo/显示全部房间（编辑）")]
    public static void ShowAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var shell = SceneManager.GetSceneByPath(ShellPath);
        if (!shell.isLoaded) shell = EditorSceneManager.OpenScene(ShellPath, OpenSceneMode.Additive);
        foreach (var root in shell.GetRootGameObjects())
            foreach (var room in root.GetComponentsInChildren<RoomVolume>(true))
                if (!SceneManager.GetSceneByPath(room.scenePath).isLoaded)
                    EditorSceneManager.OpenScene(room.scenePath, OpenSceneMode.Additive);
    }
}
