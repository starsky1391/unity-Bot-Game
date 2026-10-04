using System;
using System.IO;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoBuilder
{
    const string ScenePath = "Assets/Scenes/HollowGeometry.unity";
    static Sprite square, circle;
    static PhysicsMaterial2D frictionless;
    static ItemDefinition potion, crystal, largePotion;

    [MenuItem("Demo/Generate Demo Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath) && !Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Regenerate demo", "This replaces the generated scene and prefabs. Continue?", "Regenerate", "Cancel")) return;
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Resources/Sprites");
        Directory.CreateDirectory("Assets/Prefabs/Enemies");
        Directory.CreateDirectory("Assets/Prefabs/Player");
        Directory.CreateDirectory("Assets/Prefabs/NPCs");
        Directory.CreateDirectory("Assets/Settings");
        Directory.CreateDirectory("Assets/Resources/Items");
        Directory.CreateDirectory("Assets/Data");
        Directory.CreateDirectory("Assets/Data/Dialogues");
        Directory.CreateDirectory("Assets/Data/Shops");
        potion = CreateItem("potion", "回复药", "恢复 2 点生命，满血时不消耗。", 2, new Color(.35f, 1, .6f));
        crystal = CreateItem("crystal", "洞穴晶石", "洞穴中采集的普通材料，也可用于与商人交易。", 0, new Color(.8f, .55f, 1));
        largePotion = CreateItem("large_potion", "大回复药", "恢复 4 点生命，满血时不消耗。", 4, new Color(.2f, .85f, .9f));
        MakeSprite("Square", false);
        MakeSprite("Circle", true);
        AssetDatabase.Refresh();
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Square.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Circle.png");
        frictionless = new PhysicsMaterial2D("Frictionless") { friction = 0, bounciness = 0 };
        AssetDatabase.CreateAsset(frictionless, "Assets/Settings/Frictionless.physicsMaterial2D");
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        layers.GetArrayElementAtIndex(8).stringValue = "Terrain";
        layers.GetArrayElementAtIndex(9).stringValue = "Player";
        layers.GetArrayElementAtIndex(10).stringValue = "Enemy";
        tags.ApplyModifiedProperties();
        PlayerSettings.productName = "几何空洞";
        PlayerSettings.companyName = "Geometry Lab";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedProperties(); }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("Demo Game").AddComponent<DemoGame>();
        var playerObject = Shape("Player", new Vector2(0, 1), new Vector2(.75f, 1.35f), new Color(.6f, .95f, 1), true, false);
        playerObject.layer = 9;
        var rb = playerObject.AddComponent<Rigidbody2D>();
        ConfigureBody(rb, 3.2f);
        var slash = Shape("Slash", Vector2.zero, new Vector2(1.6f / .75f, 1.35f / 1.35f), new Color(.65f, 1, 1, .45f));
        slash.transform.SetParent(playerObject.transform, false);
        slash.GetComponent<SpriteRenderer>().sortingOrder = 7;
        slash.GetComponent<SpriteRenderer>().enabled = false;
        var player = playerObject.AddComponent<PlayerMotor>();
        PrefabUtility.SaveAsPrefabAsset(playerObject, "Assets/Prefabs/Player/Player.prefab");
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.5f;
        camera.backgroundColor = new Color(.035f, .055f, .085f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(7, 3.5f, -10);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CameraFollow>().player = player;

        MakeRoom(0, "01 / 移动教学", "二段跳、冲刺缺口与攀爬竖井。青色圆环为固定钩点。", -5, 42);
        MakeRoom(1, "02 / 地面战斗一", "巡逻虫、追逐虫、冲撞虫、跳跃虫。留意黄色预警。", 42, 74);
        MakeRoom(2, "03 / 地面战斗二", "射击虫、盾甲虫、重锤虫、钻地虫。绕到盾甲虫背后攻击。", 74, 112);
        MakeRoom(3, "04 / 空中混合", "追踪飞虫与俯冲飞虫。俯冲锁定后及时离开原位置。", 112, 145);
        MakeRoom(4, "05 / 综合挑战", "组合使用移动能力与钩爪，抵达绿色终点。", 145, 188);
        Ground(4.5f, -1, 19, 2);
        Ground(24, -1, 8, 2);
        Ground(32.5f, -1, 9, 2);
        Ground(39.5f, -1, 5, 2);
        Ground(58, -1, 32, 2);
        Ground(93, -1, 38, 2);
        Ground(128.5f, -1, 33, 2);
        Ground(166.5f, -1, 43, 2);
        Platform(7, 1.65f, 3.5f);
        Platform(11.5f, 3.7f, 3);
        Platform(24, 2.3f, 3);
        Ground(25.5f, 4.4f, 1.2f, 5.2f);
        Ground(30, 3.5f, 1.2f, 7);
        Platform(34.5f, 6.8f, 5);
        Platform(38.5f, 3.7f, 3);
        Platform(59, 2.3f, 5);
        Platform(63, 4.5f, 4);
        Platform(94, 2.5f, 4);
        Platform(98, 4.6f, 4);
        Platform(117, 2, 5);
        Platform(124, 4, 5);
        Platform(132, 2.4f, 4);
        Platform(139, 4.6f, 4);
        Platform(150, 2.5f, 4);
        Platform(157, 4.3f, 5);
        Platform(165, 2.2f, 4);
        Ground(173, 2.5f, 1.2f, 5);
        Platform(178, 4.8f, 5);
        Ground(-5.5f, 7, 1, 18);
        Ground(188.5f, 7, 1, 18);
        Check(0, 1, 0);
        Check(43, 1, 1);
        Check(75, 1, 2);
        Check(113, 1, 3);
        Check(146, 1, 4);
        var gate = Shape("Finish Gate", new Vector2(183, 1.5f), new Vector2(1, 3), new Color(.2f, 1, .7f, .5f));
        Hook(17.5f, 5.5f);
        Hook(32.5f, 9.5f);
        Hook(60, 7);
        Hook(96, 7);
        Hook(122, 7.5f);
        Hook(135, 7.5f);
        Hook(156, 7.5f);
        Hook(176, 7.5f);
        for (int i = 0; i < 32; i++)
        {
            var decor = Shape("Distant pillar", new Vector2(i * 6, 4), new Vector2(.3f, 12), new Color(.07f, .11f, .15f));
            decor.GetComponent<SpriteRenderer>().sortingOrder = -10;
        }
        var prefabs = new GameObject[10];
        for (int i = 0; i < 10; i++) prefabs[i] = MakeEnemyPrefab((EnemyKind)i);
        Enemy(prefabs, EnemyKind.Patrol, 49, .7f, 1);
        Enemy(prefabs, EnemyKind.Chaser, 56, .7f, 1);
        Enemy(prefabs, EnemyKind.Charger, 64, .7f, 1);
        Enemy(prefabs, EnemyKind.Hopper, 70, .7f, 1);
        Enemy(prefabs, EnemyKind.Shooter, 82, .7f, 2);
        Enemy(prefabs, EnemyKind.Shield, 90, .7f, 2);
        Enemy(prefabs, EnemyKind.Hammer, 100, .9f, 2);
        Enemy(prefabs, EnemyKind.Burrower, 107, .7f, 2);
        Enemy(prefabs, EnemyKind.Pursuer, 121, 5.8f, 3);
        Enemy(prefabs, EnemyKind.Diver, 136, 7.5f, 3);
        Enemy(prefabs, EnemyKind.Patrol, 130, .7f, 3);
        Enemy(prefabs, EnemyKind.Charger, 153, .7f, 4);
        Enemy(prefabs, EnemyKind.Pursuer, 161, 6.5f, 4);
        Enemy(prefabs, EnemyKind.Hammer, 168, .9f, 4);
        Enemy(prefabs, EnemyKind.Diver, 179, 8, 4);
        Item(5, .6f, crystal, 15);
        Item(11.5f, 4.5f, potion, 2);
        Item(24, 3.1f, crystal, 8);
        Item(63, 5.3f, crystal, 12);
        Item(98, 5.4f, potion, 3);
        Item(124, 4.8f, crystal, 90);
        Item(139, 5.4f, potion, 2);
        Item(157, 5.1f, crystal, 15);
        Item(178, 5.6f, potion, 1);
        CreateNpcs();
        SplitRoomScenes();
        MapAuthoring.Assign(UnityEngine.Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray());
        MainMenuAuthoring.Create(game);
        ReusablePrefabAuthoring.CreateDialogue(game);
        ProjectOrganizer.GroupScene(SceneManager.GetActiveScene(), true);
        GameInterfaceAuthoring.Create(game);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(Enumerable.Range(0, 5).Select(i => new EditorBuildSettingsScene("Assets/Scenes/Rooms/Room" + i + ".unity", true))).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_BUILD_OK: scene, player and 10 independent enemy prefabs generated.");
        WorldMapAuthoring.Convert();
        BossAuthoring.Configure();
        Validate();
    }

    static void MakeSprite(string name, bool round)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            texture.SetPixel(x, y, !round || new Vector2(x - 15.5f, y - 15.5f).sqrMagnitude < 240 ? Color.white : Color.clear);
        texture.Apply();
        string path = "Assets/Resources/Sprites/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    static GameObject Shape(string name, Vector2 position, Vector2 size, Color color, bool collider = false, bool round = false)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = round ? circle : square;
        renderer.color = color;
        renderer.sortingOrder = 2;
        if (collider) go.AddComponent<BoxCollider2D>().sharedMaterial = frictionless;
        return go;
    }

    static void ConfigureBody(Rigidbody2D body, float gravity)
    {
        body.gravityScale = gravity;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    static void Ground(float x, float y, float width, float height)
    {
        var go = Shape("Terrain", new Vector2(x, y), new Vector2(width, height), new Color(.19f, .28f, .34f), true);
        go.layer = 8;
        go.AddComponent<GroundSurface>();
        var edge = Shape("Platform rim", new Vector2(x, y + height / 2 - .06f), new Vector2(width, .12f), new Color(.3f, .5f, .55f));
        edge.GetComponent<SpriteRenderer>().sortingOrder = 3;
    }

    static void Platform(float x, float y, float width) => Ground(x, y, width, .45f);

    static void MakeRoom(int id, string title, string hint, float left, float right)
    {
        var room = new GameObject(title).AddComponent<RoomVolume>();
        room.id = id;
        room.title = title;
        room.hint = hint;
        room.bounds = new Rect(left, -3, right - left, 17);
        room.scenePath = "Assets/Scenes/Rooms/Room" + id + ".unity";
        room.neighbors = new[] { id - 1, id + 1 }.Where(i => i >= 0 && i < 5).ToArray();
        float riseLeft = id == 0 ? 27 : left + 5;
        float riseRight = id == 0 ? 32 : right - 5;
        float height = id == 0 ? 11 : id >= 3 ? 12 : 10;
        room.ceilingOutline = id == 0 ? new[]
        {
            new Vector2(-5, 0), new Vector2(42, 0), new Vector2(42, 6), new Vector2(37, 6),
            new Vector2(37, 11), new Vector2(27, 11), new Vector2(27, 6), new Vector2(14, 6),
            new Vector2(14, 8), new Vector2(8, 8), new Vector2(8, 6), new Vector2(-5, 6)
        } : new[] { new Vector2(left, 0), new Vector2(right, 0), new Vector2(right, 6),
            new Vector2(riseRight, 6), new Vector2(riseRight, height), new Vector2(riseLeft, height),
            new Vector2(riseLeft, 6), new Vector2(left, 6) };
        room.bounds = Rect.MinMaxRect(left, -3, right, height);
    }

    static void Check(float x, float y, int room)
    {
        var go = Shape("Checkpoint " + room, new Vector2(x, y), new Vector2(.45f, 1.8f), new Color(.25f, .4f, .5f));
        var point = go.AddComponent<Checkpoint>();
        point.roomId = room;
        point.persistentId = "checkpoint_" + room;
        point.spawnPoint = new GameObject("重生点").transform;
        point.spawnPoint.SetParent(go.transform, false);
        point.spawnPoint.localPosition = new Vector2(1.5f, 0);
        WorldLabelAuthoring.Attach(go.transform, "检查点");
    }

    static GameObject MakeEnemyPrefab(EnemyKind kind)
    {
        int index = (int)kind;
        bool flying = index >= 8;
        var color = Color.HSVToRGB(index / 11f, .65f, .9f);
        Vector2 size = kind == EnemyKind.Hammer ? new Vector2(1.4f, 1.7f) : flying ? Vector2.one * .9f : new Vector2(1, 1.2f);
        var go = Shape(kind.ToString(), Vector2.zero, size, color, true, flying || kind == EnemyKind.Hopper);
        go.layer = 10;
        ConfigureBody(go.AddComponent<Rigidbody2D>(), flying ? 0 : 3.2f);
        var warning = Shape("Warning", Vector2.zero, new Vector2(1.4f, .15f), Color.yellow);
        warning.transform.SetParent(go.transform, false);
        warning.GetComponent<SpriteRenderer>().enabled = false;
        var shield = Shape("Shield", Vector2.zero, new Vector2(.16f, 1.3f), new Color(.65f, .75f, .8f));
        shield.transform.SetParent(go.transform, false);
        shield.GetComponent<SpriteRenderer>().enabled = kind == EnemyKind.Shield;
        var enemy = go.AddComponent<EnemyBrain>();
        enemy.kind = kind;
        enemy.baseColor = color;
        enemy.maxHealth = kind == EnemyKind.Hammer ? 5 : kind == EnemyKind.Shield ? 4 : flying ? 2 : 3;
        enemy.moveSpeed = kind == EnemyKind.Chaser ? 3 : 2;
        enemy.attackTime = kind == EnemyKind.Hopper ? .75f : kind == EnemyKind.Diver ? .8f : .45f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/" + kind + ".prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static void Enemy(GameObject[] prefabs, EnemyKind kind, float x, float y, int room)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[(int)kind]);
        go.transform.position = new Vector2(x, y);
        go.GetComponent<EnemyBrain>().roomId = room;
        go.GetComponent<EnemyBrain>().persistentId = room + ":" + kind + ":" + x;
    }

    static void Item(float x, float y, ItemDefinition item, int count)
    {
        var go = Shape(item.displayName, new Vector2(x, y), Vector2.one * .5f, item.color, false, true);
        var pickup = go.AddComponent<Pickup>();
        pickup.item = item;
        pickup.persistentId = item.id + "_" + x.ToString(System.Globalization.CultureInfo.InvariantCulture);
        pickup.count = count;
    }

    [MenuItem("Demo/Validate Demo")]
    public static void Validate()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        var world = UnityEngine.Object.FindObjectOfType<WorldMap>();
        Require(world != null && world.startPoint.IsChildOf(world.transform) && world.fallBoundary.IsChildOf(world.transform), "map-relative spawn and death line");
        Require(world.TryGetSpawn(world.startPoint, out _), "safe spawn over terrain");
        Require(UnityEngine.Object.FindObjectsOfType<RoomContent>().Length == 0 && UnityEngine.Object.FindObjectsOfType<RoomExit>().Length == 0, "single continuous map");
        foreach (var arena in world.GetComponentsInChildren<BossArena>())
        {
            Require(arena.boss != null && arena.spawnPoint.IsChildOf(arena.transform) && arena.leftBarrier.transform.IsChildOf(arena.transform) && arena.rightBarrier.transform.IsChildOf(arena.transform), "boss arena uses local object references");
            Require(arena.GetComponent<BoxCollider2D>().isTrigger && arena.leftBarrier.layer == 8 && arena.rightBarrier.layer == 8, "boss entry trigger and blocking terrain walls");
            Require(!string.IsNullOrEmpty(arena.boss.persistentId) && arena.boss.maxHealth > 0, "boss persistence ID and health configured");
            var bossUI = UnityEngine.Object.FindObjectOfType<BossCanvas>();
            Require(bossUI != null && bossUI.introTitle != null && bossUI.healthFill != null && bossUI.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "editable Canvas boss name and health bar");
        }
        Require(UnityEngine.Object.FindObjectsOfType<EnemyBrain>().Select(e => e.kind).Distinct().Count() == 10, "10 enemy types");
        Require(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }).Length == 10, "10 enemy prefabs");

        Require(UnityEngine.Object.FindObjectsOfType<Checkpoint>().Length >= 5, "demo checkpoints present; extra instances allowed");
        Require(UnityEngine.Object.FindObjectsOfType<Pickup>().All(p => p.item != null && p.count > 0), "pickup definitions and quantities assigned");
        var enemyIds = UnityEngine.Object.FindObjectsOfType<EnemyBrain>().Select(e => e.persistentId).ToArray();
        Require(enemyIds.All(id => !string.IsNullOrWhiteSpace(id)) && enemyIds.Distinct().Count() == enemyIds.Length, "unique enemy persistence IDs");
        var pickupIds = UnityEngine.Object.FindObjectsOfType<Pickup>().Select(p => p.persistentId).ToArray();
        Require(pickupIds.All(id => !string.IsNullOrWhiteSpace(id)) && pickupIds.Distinct().Count() == pickupIds.Length, "unique pickup persistence IDs");
        for (int s = 0; s < SceneManager.sceneCount; s++)
            foreach (var root in SceneManager.GetSceneAt(s).GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "no missing scripts after asset moves");
        Require(UnityEngine.Object.FindObjectsOfType<Npc>().Length >= 2, "guide and merchant; extra NPCs allowed");
        Require(UnityEngine.Object.FindObjectsOfType<GrapplePoint>().Length >= 8, "demo grapple anchors present; extra instances allowed");
        Require(UnityEngine.Object.FindObjectsOfType<PlayerMotor>().Length == 1, "one player");
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuCanvas>();
        Require(menu != null && menu.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace && menu.GetComponent<Canvas>().worldCamera == null, "independent edit-mode Canvas main menu");
        Require(menu.menuButtons.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length == 4 && menu.confirmButton != null && menu.cancelButton != null && menu.confirmationPanel.transform.parent == menu.transform, "four standard UI menu buttons");
        Require(UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length == 1, "one UI event system");
        Require(UnityEngine.Object.FindObjectsOfType<DialogueCanvas>().Length == 1, "one reusable Canvas dialogue instance");
        var interfaceUI = UnityEngine.Object.FindObjectOfType<DemoUI>();
        Require(interfaceUI != null && interfaceUI.GetComponent<Canvas>() != null && interfaceUI.screens.Length == 5 &&
            interfaceUI.inventoryButtons.Length == 16 && interfaceUI.map.GetComponent<CanvasRenderer>() != null &&
            interfaceUI.fade != null && interfaceUI.offerTemplate != null, "complete editable Canvas game interface");
        var checkpointIds = UnityEngine.Object.FindObjectsOfType<Checkpoint>().Select(c => c.persistentId).ToArray();
        Require(checkpointIds.All(id => !string.IsNullOrEmpty(id)) && checkpointIds.Distinct().Count() == checkpointIds.Length, "unique checkpoint IDs");
        Require(new[] { "Ground", "Platform", "PickupPotion", "PickupMaterial", "GrapplePoint", "Checkpoint" }
            .All(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/" + name + ".prefab") != null), "ready-to-use world prefabs available");
        var bag = new Inventory();
        var material = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Resources/Items/crystal.asset");
        var healing = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Resources/Items/potion.asset");
        Require(bag.TryAdd(material, 100), "stack overflow to next slot");
        Require(bag.slots[0].count == 99 && bag.slots[1].count == 1, "99 stack limit");
        Require(bag.TryAdd(healing, 14 * 99), "fill remaining slots");
        Require(!bag.TryAdd(material, 99), "reject insufficient capacity atomically");
        Require(bag.slots[1].count == 1, "failed pickup keeps bag unchanged");
        Require(bag.TryAdd(material, 98), "fill existing partial stack");
        Require(!bag.TryAdd(healing, 1), "reject full bag");
        Require(!bag.TryBuy(healing, 100, material, 1) && bag.Count(material) == 198, "failed purchase does not charge currency");
        Require(bag.TryBuy(healing, 1, material, 99), "purchase can use a slot freed by payment");
        var definitions = Resources.LoadAll<ItemDefinition>("Items");
        Require(definitions.All(i => !string.IsNullOrWhiteSpace(i.id)) && definitions.Select(i => i.id).Distinct().Count() == definitions.Length, "unique nonempty item IDs");
        foreach (var npc in UnityEngine.Object.FindObjectsOfType<Npc>())
        {
            Require(npc.dialogue != null && npc.dialogue.nodes.Length > 0, "NPC dialogue assigned");
            foreach (var node in npc.dialogue.nodes) foreach (var choice in node.choices)
                Require(choice.nextNode < npc.dialogue.nodes.Length && (!choice.openShop || npc.shop != null), "valid dialogue links");
        }
        Debug.Log("DEMO_VALIDATE_OK: scene composition and inventory capacity/stacking checks passed.");

    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Demo validation failed: " + message);
    }

    [MenuItem("洞穴 Demo/构建 Windows Demo（更新固定目录）")]
    public static void BuildWindows()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Validate();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), "Builds/Windows/HollowGeometry.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("Windows player build failed: " + report.summary.result);
        Debug.Log("DEMO_PLAYER_BUILD_OK: Builds/Windows/HollowGeometry.exe");
    }

    static ItemDefinition CreateItem(string id, string name, string description, int healing, Color color)
    {
        string path = "Assets/Resources/Items/" + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.id = id;
        item.displayName = name;
        item.description = description;
        item.healing = healing;
        item.color = color;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    static void Hook(float x, float y)
    {
        var go = Shape("固定钩点", new Vector2(x, y), Vector2.one * .55f, new Color(.2f, 1, .85f), false, true);
        go.AddComponent<GrapplePoint>();
        var center = Shape("钩点内环", Vector2.zero, Vector2.one * .6f, new Color(.035f, .055f, .085f), false, true);
        center.transform.SetParent(go.transform, false);
        center.GetComponent<SpriteRenderer>().sortingOrder = 3;
    }

    static void CreateNpcs()
    {
        var shop = AssetDatabase.LoadAssetAtPath<ShopDefinition>("Assets/Data/Shops/MerchantShop.asset");
        if (shop == null)
        {
            shop = ScriptableObject.CreateInstance<ShopDefinition>();
            shop.displayName = "阿砾的小铺";
            shop.currency = crystal;
            shop.offers = new[]
            {
                new ShopDefinition.Offer { item = potion, quantity = 1, price = 5 },
                new ShopDefinition.Offer { item = largePotion, quantity = 1, price = 12 }
            };
            AssetDatabase.CreateAsset(shop, "Assets/Data/Shops/MerchantShop.asset");
        }
        var merchant = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Dialogues/MerchantDialogue.asset");
        if (merchant == null)
        {
            merchant = ScriptableObject.CreateInstance<DialogueDefinition>();
            merchant.nodes = new[]
            {
                new DialogueDefinition.Node { speaker = "游商阿砾", text = "旅行者，欢迎！带来洞穴晶石了吗？我这里有恢复生命的药品。", choices = new[]
                {
                    new DialogueDefinition.Choice { text = "看看商品", openShop = true },
                    new DialogueDefinition.Choice { text = "晶石在哪里？", nextNode = 1 },
                    new DialogueDefinition.Choice { text = "告辞" }
                } },
                new DialogueDefinition.Node { speaker = "游商阿砾", text = "高台支路上常有晶石。背包里每格最多堆叠 99 个。先确认背包有空间再买东西吧。", choices = new[]
                {
                    new DialogueDefinition.Choice { text = "回到刚才的话题", nextNode = 0 },
                    new DialogueDefinition.Choice { text = "谢谢，告辞" }
                } }
            };
            AssetDatabase.CreateAsset(merchant, "Assets/Data/Dialogues/MerchantDialogue.asset");
        }
        var guide = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Dialogues/GuideDialogue.asset");
        if (guide == null)
        {
            guide = ScriptableObject.CreateInstance<DialogueDefinition>();
            guide.nodes = new[]
            {
                new DialogueDefinition.Node { speaker = "向导微光", text = "前方是洞穴试炼。你想了解什么？", choices = new[]
                {
                    new DialogueDefinition.Choice { text = "检查点有什么用？", nextNode = 1 },
                    new DialogueDefinition.Choice { text = "钩爪怎么使用？", nextNode = 2 },
                    new DialogueDefinition.Choice { text = "我准备好了" }
                } },
                new DialogueDefinition.Node { speaker = "向导微光", text = "靠近检查点后主动互动，可恢复生命并记录重生位置。退出后选择继续游戏，也会从最近记录的检查点出发。" },
                new DialogueDefinition.Node { speaker = "向导微光", text = "面对青色圆环，按 K 连接前方最近且无遮挡的钩点。再按 K 松开，按空格跳离。钩爪不会恢复空中跳跃或冲刺次数。" }
            };
            AssetDatabase.CreateAsset(guide, "Assets/Data/Dialogues/GuideDialogue.asset");
        }
        MakeNpc("向导微光", new Vector2(3, .8f), guide, null, new Color(.95f, .8f, .45f), "Guide");
        MakeNpc("游商阿砾", new Vector2(10, .8f), merchant, shop, new Color(.5f, .8f, 1), "Merchant");
    }

    static void MakeNpc(string name, Vector2 position, DialogueDefinition dialogue, ShopDefinition shop, Color color, string prefabName)
    {
        var go = Shape(name, position, new Vector2(.8f, 1.4f), color, false, true);
        var npc = go.AddComponent<Npc>();
        npc.displayName = name;
        npc.dialogue = dialogue;
        npc.shop = shop;
        npc.interactionRange = 2;
        WorldLabelAuthoring.Attach(go.transform, name);
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/NPCs/" + prefabName + ".prefab");
    }

    static void SplitRoomScenes()
    {
        Directory.CreateDirectory("Assets/Scenes/Rooms");
        var shell = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(shell, ScenePath);
        var rooms = UnityEngine.Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray();
        var content = shell.GetRootGameObjects().Where(go => go.GetComponent<DemoGame>() == null &&
            go.GetComponent<PlayerMotor>() == null && go.GetComponent<Camera>() == null && go.GetComponent<RoomVolume>() == null).ToArray();
        for (int i = 0; i < rooms.Length; i++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach (var go in content)
            {
                float x = go.transform.position.x;
                int owner = x < rooms[0].bounds.xMin ? 0 : x >= rooms[4].bounds.xMax ? 4 :
                    Array.FindIndex(rooms, r => x >= r.bounds.xMin && x < r.bounds.xMax);
                if (owner == i) SceneManager.MoveGameObjectToScene(go, scene);
            }
            var room = rooms[i];
            if (i > 0) CreateExit(room.bounds.xMin + .25f, i - 1, "RightEntrance");
            if (i < 4) CreateExit(room.bounds.xMax - .25f, i + 1, "LeftEntrance");
            for (int edge = 0; edge < room.ceilingOutline.Length; edge++)
            {
                Vector2 a = room.ceilingOutline[edge], b = room.ceilingOutline[(edge + 1) % room.ceilingOutline.Length];
                if (a.y != b.y || a.y <= 0) continue;
                Ground((a.x + b.x) / 2, (a.y + 15) / 2, Mathf.Abs(a.x - b.x), 15 - a.y);
            }
            if (i > 0) Ground(room.bounds.xMin, 8.5f, .5f, 11);
            if (i < 4) Ground(room.bounds.xMax, 8.5f, .5f, 11);
            ProjectOrganizer.GroupScene(scene, false);
            RoomAuthoring.Wrap(scene, room);
            EditorSceneManager.SaveScene(scene, room.scenePath);
        }
        foreach (var room in rooms) EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(room.scenePath), true);
        SceneManager.SetActiveScene(shell);
    }

    static void CreateExit(float x, int target, string targetEntrance)
    {
        var go = new GameObject("通往房间 " + target);
        go.transform.position = new Vector2(x, 1.5f);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(.5f, 3);
        collider.isTrigger = true;
        var exit = go.AddComponent<RoomExit>();
        exit.targetRoom = target;
        exit.targetEntrance = targetEntrance;
        Shape("出口门楣", new Vector2(x, 3), new Vector2(1, .15f), new Color(.4f, .68f, .65f));
    }

}



