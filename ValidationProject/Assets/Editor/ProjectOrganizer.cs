using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HollowDemo;

public static class ProjectOrganizer
{
    public static void Organize()
    {
        MoveScripts("Core", "DemoGame", "SaveData");
        MoveScripts("Player", "PlayerMotor");
        MoveScripts("Enemies", "EnemyBrain", "Projectile");
        MoveScripts("World", "RoomVolume", "RoomExit", "GrapplePoint", "Checkpoint", "Pickup", "Interactable", "CameraFollow");
        MoveScripts("Items", "Inventory", "ItemDefinition");
        MoveScripts("NPC", "Npc", "DialogueDefinition", "ShopDefinition");
        MoveScripts("UI", "DemoUI", "MapGeometry", "ChineseFont", "WorldLabel");
        Move("Assets/Resources/Square.png", "Assets/Resources/Sprites/Square.png");
        Move("Assets/Resources/Circle.png", "Assets/Resources/Sprites/Circle.png");
        Move("Assets/Data/GuideDialogue.asset", "Assets/Data/Dialogues/GuideDialogue.asset");
        Move("Assets/Data/MerchantDialogue.asset", "Assets/Data/Dialogues/MerchantDialogue.asset");
        Move("Assets/Data/MerchantShop.asset", "Assets/Data/Shops/MerchantShop.asset");
        Move("Assets/Prefabs/Player.prefab", "Assets/Prefabs/Player/Player.prefab");
        Move("Assets/Prefabs/Guide.prefab", "Assets/Prefabs/NPCs/Guide.prefab");
        Move("Assets/Prefabs/Merchant.prefab", "Assets/Prefabs/NPCs/Merchant.prefab");
        foreach (var entry in EditorBuildSettings.scenes)
        {
            var scene = EditorSceneManager.OpenScene(entry.path);
            GroupScene(scene, entry.path.EndsWith("HollowGeometry.unity"));
            EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_ORGANIZE_OK: GUID-preserving asset moves and scene hierarchy grouping complete.");
    }

    static void MoveScripts(string folder, params string[] names)
    {
        foreach (string name in names) Move("Assets/Scripts/" + name + ".cs", "Assets/Scripts/" + folder + "/" + name + ".cs");
    }

    static void Move(string source, string destination)
    {
        if (!File.Exists(source)) return;
        string folder = Path.GetDirectoryName(destination).Replace('\\', '/');
        EnsureFolder(folder);
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    public static void GroupScene(Scene scene, bool shell)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            string group;
            if (shell)
            {
                if (root.GetComponent<DemoGame>()) group = "系统";
                else if (root.GetComponent<PlayerMotor>()) group = "主角";
                else if (root.GetComponent<Camera>()) group = "相机";
                else if (root.GetComponent<RoomVolume>()) group = "房间信息";
                else continue;
            }
            else
            {
                if (root.GetComponent<EnemyBrain>()) group = "怪物";
                else if (root.GetComponent<Npc>()) group = "NPC";
                else if (root.GetComponent<Pickup>()) group = "拾取物";
                else if (root.GetComponent<Checkpoint>()) group = "检查点";
                else if (root.GetComponent<GrapplePoint>()) group = "钩点";
                else if (root.GetComponent<RoomExit>()) group = "房间出口";
                else if (root.GetComponent<WorldLabel>()) group = "文字标记";
                else if (root.GetComponent<Collider2D>()) group = "地形";
                else if (root.GetComponent<SpriteRenderer>()) group = "装饰";
                else continue;
            }
            Transform parent = null;
            foreach (var candidate in scene.GetRootGameObjects())
                if (candidate.name == group) { parent = candidate.transform; break; }
            if (parent == null)
            {
                var container = new GameObject(group);
                SceneManager.MoveGameObjectToScene(container, scene);
                parent = container.transform;
            }
            root.transform.SetParent(parent, true);
        }
    }
}
