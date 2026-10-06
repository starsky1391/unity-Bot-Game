using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HollowDemo;

public static class ProjectOrganizer
{
    [MenuItem("洞穴 Demo/迁移主场景关卡到 bot map")]
    public static void MoveLevelToBotMap()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var shell=EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var map=EditorSceneManager.OpenScene("Assets/Scenes/Rooms/bot map.unity",OpenSceneMode.Additive);
        var wrapper=map.GetRootGameObjects().FirstOrDefault(r=>r.name=="bot map");
        if(wrapper!=null){foreach(var child in wrapper.transform.Cast<Transform>().ToArray())child.SetParent(null,true);UnityEngine.Object.DestroyImmediate(wrapper);}
        var groups=map.GetRootGameObjects().ToDictionary(r=>r.name,r=>r.transform);
        int moved=0;
        foreach(var root in shell.GetRootGameObjects())
        {
            if(root.GetComponentInChildren<DemoGame>(true)!=null || root.GetComponentInChildren<PlayerMotor>(true)!=null || root.GetComponentInChildren<Camera>(true)!=null || root.GetComponent<Canvas>()!=null)continue;
            var objects=root.GetComponentsInChildren<Transform>(true);
            var positions=objects.Select(t=>t.position).ToArray();
            string category="背景装饰";
            if(!root.activeSelf)category="停用关卡";
            else if(root.GetComponent<BossArena>()!=null || root.GetComponent<EnemyBrain>()!=null)category="怪物与 Boss";
            else if(root.GetComponent<Npc>()!=null)category="NPC";
            else if(root.GetComponent<Checkpoint>()!=null)category="检查点";
            else if(root.GetComponent<Pickup>()!=null || root.GetComponent<AbilityUnlock>()!=null)category="拾取物";
            else if(root.GetComponent<DamageWater>()!=null || root.GetComponent<InstantDeathZone>()!=null)category="危险区域";
            else if(root.GetComponent<DropPlatform>()!=null)category="单向平台";
            else if(root.GetComponent<GroundSurface>()!=null)category="地形";
            else if(root.GetComponent<GrapplePoint>()!=null)category="钩点";
            else if(root.GetComponentInChildren<RoomVolume>(true)!=null)category="场景信息";
            if(!groups.TryGetValue(category,out var parent)){var group=new GameObject(category);SceneManager.MoveGameObjectToScene(group,map);parent=group.transform;groups.Add(category,parent);}
            SceneManager.MoveGameObjectToScene(root,map);root.transform.SetParent(parent,true);
            for(int n=0;n<objects.Length;n++)if(Vector3.Distance(objects[n].position,positions[n])>.001f)throw new InvalidOperationException("迁移改变坐标: "+objects[n].name);
            moved++;
        }
        EditorSceneManager.SaveScene(shell);EditorSceneManager.SaveScene(map);
        Debug.Log("DEMO_LEVEL_MIGRATE_OK: "+moved+" roots moved; map categories are scene roots");
    }

    [MenuItem("洞穴 Demo/整理 bot map 层级")]
    public static void OrganizeBotMap()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Rooms/bot map.unity");
        var originals=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var positions=originals.ToDictionary(t=>t,t=>t.position);
        var rotations=originals.ToDictionary(t=>t,t=>t.rotation);
        var scales=originals.ToDictionary(t=>t,t=>t.lossyScale);
        var checkpoints=originals.Select(t=>t.GetComponent<Checkpoint>()).Where(c=>c!=null).ToDictionary(c=>c,c=>c.persistentId);
        var pickups=originals.Select(t=>t.GetComponent<Pickup>()).Where(c=>c!=null).ToDictionary(c=>c,c=>c.persistentId);
        var prefabRoots=originals.Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).Select(t=>t.gameObject).ToArray();
        var map=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="bot map");
        if(map==null){map=new GameObject("bot map");SceneManager.MoveGameObjectToScene(map,scene);}
        var groups=new Dictionary<string,Transform>();
        foreach(var name in new[]{"地形","单向平台","钩点","怪物与 Boss","NPC","拾取物","检查点","危险区域","背景装饰"})
        {
            var group=map.transform.Find(name);
            if(group==null){group=new GameObject(name).transform;group.SetParent(map.transform,false);}
            groups[name]=group;
        }
        var targets=new Dictionary<GameObject,string>();
        foreach(var t in originals)
        {
            if(t.GetComponent<Camera>()!=null || t.GetComponent<Light>()!=null)continue;
            string group=null;
            if(t.GetComponent<BossArena>()!=null || t.GetComponent<EnemyBrain>()!=null)group="怪物与 Boss";
            else if(t.GetComponent<Npc>()!=null)group="NPC";
            else if(t.GetComponent<Checkpoint>()!=null)group="检查点";
            else if(t.GetComponent<Pickup>()!=null || t.GetComponent<AbilityUnlock>()!=null)group="拾取物";
            else if(t.GetComponent<GrapplePoint>()!=null)group="钩点";
            else if(t.GetComponent<DamageWater>()!=null || t.GetComponent<InstantDeathZone>()!=null)group="危险区域";
            else if(t.GetComponent<DropPlatform>()!=null)group="单向平台";
            else if(t.GetComponent<GroundSurface>()!=null)group="地形";
            if(group==null)continue;
            var owner=PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) ?? t.gameObject;
            var arena=t.GetComponentInParent<BossArena>();
            if(arena!=null){owner=PrefabUtility.GetOutermostPrefabInstanceRoot(arena.gameObject) ?? arena.gameObject;group="怪物与 Boss";}
            targets[owner]=group;
        }
        foreach(var entry in targets)entry.Key.transform.SetParent(groups[entry.Value],true);
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root==map || root.GetComponent<Camera>()!=null || root.GetComponent<Light>()!=null)continue;
            if(root.GetComponentsInChildren<SpriteRenderer>(true).Length>0)root.transform.SetParent(groups["背景装饰"],true);
            else if(root.GetComponents<Component>().Length==1 && root.transform.childCount==0)UnityEngine.Object.DestroyImmediate(root);
            else root.transform.SetParent(map.transform,true);
        }
        Physics2D.SyncTransforms();
        foreach(var t in originals)
            if(t!=null && (Vector3.Distance(t.position,positions[t])>.001f || Quaternion.Angle(t.rotation,rotations[t])>.01f || Vector3.Distance(t.lossyScale,scales[t])>.001f))throw new InvalidOperationException("整理改变了对象变换: "+t.name);
        if(checkpoints.Any(c=>c.Key.persistentId!=c.Value) || pickups.Any(c=>c.Key.persistentId!=c.Value))throw new InvalidOperationException("整理改变了持久化 ID");
        if(prefabRoots.Any(r=>r!=null && !PrefabUtility.IsAnyPrefabInstanceRoot(r)))throw new InvalidOperationException("整理破坏了预制体关联");
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DEMO_BOT_MAP_ORGANIZE_OK: "+targets.Count+" objects; world transforms, IDs and prefab links preserved");
    }

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

