using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoInitializationSmokeRunner
{
    const string Key = "HollowDemo.AutoInitializationSmoke";
    static double deadline;

    static AutoInitializationSmokeRunner()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                new GameObject("自动初始化测试").AddComponent<AutoInitializationSmokeProbe>();
        };
        if (SessionState.GetBool(Key, false)) Arm();
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new System.InvalidOperationException("请在隔离项目的批处理模式运行此测试。");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("自建关卡（没有 WorldMap）");
        var ground = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Ground.prefab"));
        ground.transform.SetParent(map.transform, false);
        ground.transform.position = new Vector3(220, 50);
        ground.transform.localScale = new Vector3(50, 1, 1);
        for (int i = 0; i < 2; i++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Checkpoint.prefab"));
            instance.transform.SetParent(map.transform, false);
            instance.transform.position = new Vector3(210 + i * 15, 51.3f);
            var point = instance.GetComponent<Checkpoint>();
            point.isInitialSpawn = i == 0;
            point.roomId = i;
        }
        var pickup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/PickupMaterial.prefab"));
        pickup.transform.SetParent(map.transform, false);
        pickup.transform.position = new Vector3(218, 51.3f);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
        player.transform.position = new Vector3(-400, 300);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(CameraFollow));
        cameraObject.tag = "MainCamera";
        cameraObject.GetComponent<Camera>().orthographic = true;
        var extraCamera = new GameObject("bot map 默认相机", typeof(Camera));
        extraCamera.tag = "MainCamera";
        var game = new GameObject("系统").AddComponent<DemoGame>();
        game.flaskItem = Resources.Load<ItemDefinition>("Items/flask");
        game.manaFlaskItem = Resources.LoadAll<ItemDefinition>("Items")[0];
        var hiddenMap = new GameObject("旧地图（隐藏）", typeof(WorldMap));
        hiddenMap.SetActive(false);
        pickup.transform.SetParent(null, true);
        ScenePrefabIds.Assign();
        if (pickup.transform.parent != null || pickup.scene != SceneManager.GetActiveScene())
            throw new System.Exception("隐藏的旧地图不应接管新对象的父节点或场景。");
        foreach (string path in new[] { "Assets/Prefabs/Enemies/Patrol.prefab", "Assets/Prefabs/World/Checkpoint.prefab", "Assets/Prefabs/World/PickupMaterial.prefab", "Assets/Prefabs/Bosses/BossArena.prefab" })
        {
            var hiddenObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            hiddenObject.transform.SetParent(hiddenMap.transform, false);
        }
        hiddenMap.GetComponentInChildren<Checkpoint>(true).isInitialSpawn = true;
        ScenePrefabIds.Assign();
        Debug.Log("DEMO_AUTO_INIT_CHECK: ID assignment preserves manually placed objects even with a hidden old map.");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Tests/AutoInitializationFixture.unity");
        SessionState.SetBool(Key, true);
        Arm();
        EditorApplication.isPlaying = true;
    }

    static void Arm()
    {
        deadline = EditorApplication.timeSinceStartup + 60;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
        EditorApplication.update -= Watchdog;
        EditorApplication.update += Watchdog;
    }

    static void Watchdog()
    {
        if (SessionState.GetBool(Key, false) && EditorApplication.timeSinceStartup > deadline) Finish(1);
    }

    static void Log(string message, string trace, LogType type)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(1);
        else if (message.StartsWith("DEMO_AUTO_INIT_OK")) Finish(0);
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.delayCall += () => EditorApplication.Exit(code);
    }
}
