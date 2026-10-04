using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SmokeRunner
{
    const string Key = "HollowDemo.SmokeTest";
    static double deadline;
    static SmokeRunner()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (SessionState.GetBool(Key, false)) Arm();
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        RoomPreview.ShowAll();
        if (UnityEngine.SceneManagement.SceneManager.sceneCount != 6)
            throw new System.Exception("Editor room preview should load shell and all five rooms");
        SessionState.SetBool(Key, true);
        Arm();
        EditorApplication.isPlaying = true;
    }
    public static void RunMovedRooms()
    {
        var first = EditorSceneManager.OpenScene("Assets/Scenes/Rooms/Room1.unity");
        first.GetRootGameObjects()[0].transform.position += new Vector3(400, 30);
        EditorSceneManager.SaveScene(first);
        var second = EditorSceneManager.OpenScene("Assets/Scenes/Rooms/Room2.unity");
        second.GetRootGameObjects()[0].transform.position += new Vector3(-200, -40);
        EditorSceneManager.SaveScene(second);
        Debug.Log("DEMO_MOVED_ROOMS: translated room roots before entrance/checkpoint/camera/persistence regression.");
        Run();
    }
    public static void RunReusablePrefabs()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Rooms/Room3.unity");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/PickupPotion.prefab");
        var first = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        first.name = "Smoke Pickup";
        first.transform.position = new Vector2(132, 1);
        ScenePrefabIds.Assign();
        string firstId = first.GetComponent<HollowDemo.Pickup>().persistentId;
        var second = Object.Instantiate(first);
        second.name = "Smoke Pickup Copy";
        ScenePrefabIds.Assign();
        if (string.IsNullOrEmpty(firstId) || firstId == second.GetComponent<HollowDemo.Pickup>().persistentId || first.GetComponent<HollowDemo.Pickup>().persistentId != firstId)
            throw new System.Exception("Duplicated pickup should receive a new ID while original ID remains stable");
        var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Checkpoint.prefab"), scene);
        checkpoint.name = "Smoke Checkpoint";
        checkpoint.transform.position = new Vector2(134, 1);
        ScenePrefabIds.Assign();
        if (checkpoint.GetComponent<HollowDemo.Checkpoint>().roomId != 3 || checkpoint.transform.parent == null)
            throw new System.Exception("Dragged checkpoint should infer room and join its root");
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DEMO_PREFAB_DROP_OK: dragging and duplication produce unique IDs and automatic room binding.");
        Run();
    }
    static void Arm()
    {
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Watchdog;
        EditorApplication.update += Watchdog;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            new GameObject("Smoke Probe").AddComponent<HollowDemo.SmokeProbe>();
    }
    static void Watchdog()
    {
        if (SessionState.GetBool(Key, false) && EditorApplication.timeSinceStartup > deadline)
        {
            Debug.LogError("DEMO_SMOKE_TIMEOUT");
            Finish(1);
        }
    }
    static void Log(string message, string trace, LogType type)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Finish(1);
        else if (message.StartsWith("DEMO_SMOKE_OK")) Finish(0);
    }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.delayCall += () => EditorApplication.Exit(code);
    }
}
