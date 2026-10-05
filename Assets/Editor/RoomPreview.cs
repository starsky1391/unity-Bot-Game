using UnityEditor;
using UnityEditor.SceneManagement;

public static class RoomPreview
{
    const string Path = "Assets/Scenes/HollowGeometry.unity";
    [MenuItem("洞穴 Demo/打开整体地图")]
    public static void ShowAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Path);
    }
}
