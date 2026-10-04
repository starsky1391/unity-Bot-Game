using System.IO;
using System.Linq;
using HollowDemo;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ReusablePrefabAuthoring
{
    [MenuItem("洞穴 Demo/生成可复用预制体")]
    public static void Configure()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Prefabs/World");
        Directory.CreateDirectory("Assets/Prefabs/UI");
        AssetDatabase.Refresh();
        var shell = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        CreateWorldPrefabs();
        CreateDialogue(Object.FindObjectOfType<DemoGame>());
        var dialogueAsset = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/DialogueBox.prefab");
        var dialogue = dialogueAsset.GetComponent<DialogueCanvas>();
        var bodyFitter = dialogue.body.GetComponent<ContentSizeFitter>();
        if (bodyFitter != null && dialogue.body.transform.parent.GetComponent<VerticalLayoutGroup>() != null)
            Object.DestroyImmediate(bodyFitter);
        dialogue.bodyScroll.scrollSensitivity = dialogue.choicesScroll.scrollSensitivity = 30;
        PrefabUtility.SaveAsPrefabAsset(dialogueAsset, "Assets/Prefabs/UI/DialogueBox.prefab");
        PrefabUtility.UnloadPrefabContents(dialogueAsset);
        EditorSceneManager.SaveScene(shell);
        foreach (var definition in Object.FindObjectsOfType<RoomVolume>().OrderBy(r => r.id))
        {
            var scene = EditorSceneManager.OpenScene(definition.scenePath, OpenSceneMode.Additive);
            var root = scene.GetRootGameObjects().Single();
            foreach (var collider in root.GetComponentsInChildren<BoxCollider2D>(true))
                if (collider.gameObject.layer == 8 && collider.GetComponent<GroundSurface>() == null)
                    collider.gameObject.AddComponent<GroundSurface>();
            var point = root.GetComponent<RoomContent>().checkpoint;
            if (string.IsNullOrEmpty(point.persistentId)) point.persistentId = "checkpoint_" + definition.id;
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_REUSABLE_PREFABS_OK: world prefabs and Canvas dialogue prefab created; original pickup IDs retained.");
    }

    public static void CreateWorldPrefabs()
    {
        Directory.CreateDirectory("Assets/Prefabs/World");
        foreach (string name in new[] { "Ground", "Platform", "PickupPotion", "PickupMaterial", "GrapplePoint", "Checkpoint", "RoomExit" })
        {
            string path = "Assets/Prefabs/World/" + name + ".prefab";
            if (File.Exists(path)) continue;
            bool ground = name == "Ground" || name == "Platform";
            bool hook = name == "GrapplePoint";
            var go = new GameObject(name);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/" + (hook ? "Circle" : "Square") + ".png");
            sprite.sortingOrder = 2;
            if (ground)
            {
                go.layer = 8;
                go.transform.localScale = name == "Ground" ? new Vector3(8, 2, 1) : new Vector3(6, .45f, 1);
                go.AddComponent<BoxCollider2D>().sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Frictionless.physicsMaterial2D");
                go.AddComponent<GroundSurface>();
                sprite.color = new Color(.19f, .28f, .34f);
            }
            else if (name.StartsWith("Pickup"))
            {
                go.transform.localScale = Vector3.one * .65f;
                var pickup = go.AddComponent<Pickup>();
                pickup.item = Resources.Load<ItemDefinition>(name == "PickupPotion" ? "Items/potion" : "Items/crystal");
                pickup.count = name == "PickupPotion" ? 1 : 5;
                pickup.persistentId = "";
                sprite.color = pickup.item.color;
            }
            else if (hook)
            {
                go.transform.localScale = Vector3.one * .55f;
                go.AddComponent<GrapplePoint>();
                sprite.color = new Color(.2f, 1, .85f);
            }
            else if (name == "Checkpoint")
            {
                go.transform.localScale = new Vector3(.45f, 1.8f, 1);
                var point = go.AddComponent<Checkpoint>();
                point.persistentId = "";
                point.spawnPoint = new GameObject("重生点").transform;
                point.spawnPoint.SetParent(go.transform, false);
                point.spawnPoint.localPosition = new Vector2(1.5f / .45f, 0);
                sprite.color = new Color(.25f, .4f, .5f);
            }
            else
            {
                go.transform.localScale = new Vector3(.5f, 3, 1);
                go.AddComponent<BoxCollider2D>().isTrigger = true;
                var exit = go.AddComponent<RoomExit>();
                exit.targetRoom = 1; exit.targetEntrance = "LeftEntrance";
                sprite.color = new Color(.4f, .68f, .65f, .3f);
            }
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }

    public static void CreateDialogue(DemoGame game)
    {
        Directory.CreateDirectory("Assets/Prefabs/UI");
        const string path = "Assets/Prefabs/UI/DialogueBox.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            var root = new GameObject("对话框 Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DialogueCanvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 80;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
            root.transform.localScale = Vector3.one * .02f;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var dialogue = root.GetComponent<DialogueCanvas>();
            var panel = Rect(root.transform, "对话面板", Vector2.zero, Vector2.zero);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = new Color(.01f, .025f, .05f, .94f);
            dialogue.panel = panel.gameObject;
            var frame = Rect(panel, "对话框", Vector2.zero, new Vector2(980, 550));
            frame.gameObject.AddComponent<Image>().color = new Color(.05f, .09f, .14f);
            dialogue.speaker = Text(frame, "说话者", new Vector2(-15, 205), new Vector2(850, 48), 30);
            dialogue.speaker.text = "说话者";
            dialogue.bodyScroll = Scroll(frame, "正文滚动", new Vector2(0, 65), new Vector2(870, 210), false);
            dialogue.body = Text(dialogue.bodyScroll.content, "正文", Vector2.zero, Vector2.zero, 23);
            var bodyRect = dialogue.body.rectTransform;
            bodyRect.anchorMin = new Vector2(0, 1); bodyRect.anchorMax = new Vector2(1, 1);
            bodyRect.pivot = new Vector2(.5f, 1); bodyRect.sizeDelta = new Vector2(-30, 0);
            bodyRect.anchoredPosition = new Vector2(0, -12);
            dialogue.body.text = "对话内容";
            dialogue.body.alignment = TextAnchor.UpperLeft;
            var bodyLayout = dialogue.bodyScroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(15, 15, 12, 12);
            bodyLayout.childControlWidth = bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandHeight = false;
            dialogue.choicesScroll = Scroll(frame, "选项滚动", new Vector2(0, -140), new Vector2(870, 165), true);
            dialogue.choices = dialogue.choicesScroll.content;
            dialogue.choiceTemplate = Button(dialogue.choices, "选项模板", "选项", Vector2.zero, new Vector2(820, 46), null);
            dialogue.choiceTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
            dialogue.choiceTemplate.gameObject.SetActive(false);
            Button(frame, "关闭", "结束对话", new Vector2(355, -250), new Vector2(170, 35), dialogue.Close);
            panel.gameObject.SetActive(false);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
        var contents = PrefabUtility.LoadPrefabContents(path);
        var ui = contents.GetComponent<DialogueCanvas>();
        contents.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        contents.GetComponent<Canvas>().planeDistance = 1;
        var backdrop = ui.panel.GetComponent<Image>();
        backdrop.color = Color.clear;
        backdrop.raycastTarget = false;
        var dialogueFrame = ui.speaker.transform.parent.GetComponent<RectTransform>();
        dialogueFrame.anchorMin = dialogueFrame.anchorMax = dialogueFrame.pivot = new Vector2(.5f, 0);
        dialogueFrame.anchoredPosition = new Vector2(0, 20);
        dialogueFrame.sizeDelta = new Vector2(1120, 240);
        ui.speaker.rectTransform.anchoredPosition = new Vector2(-170, 80);
        ui.speaker.rectTransform.sizeDelta = new Vector2(730, 35);
        ui.speaker.alignment = TextAnchor.MiddleLeft;
        ui.speaker.fontSize = 24;
        ui.bodyScroll.GetComponent<RectTransform>().anchoredPosition = new Vector2(-170, -15);
        ui.bodyScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(730, 140);
        ui.body.fontSize = 20;
        ui.choicesScroll.GetComponent<RectTransform>().anchoredPosition = new Vector2(390, -15);
        ui.choicesScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 140);
        var close = dialogueFrame.Find("关闭").GetComponent<RectTransform>();
        close.anchoredPosition = new Vector2(465, 82);
        close.sizeDelta = new Vector2(150, 32);
        prefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        if (Object.FindObjectOfType<DialogueCanvas>() == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, game.gameObject.scene);
            instance.transform.SetParent(game.transform, false);
        }
        var sceneCanvas = Object.FindObjectOfType<DialogueCanvas>().GetComponent<Canvas>();
        if (sceneCanvas.renderMode == RenderMode.ScreenSpaceCamera) sceneCanvas.worldCamera = Camera.main;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneCanvas);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static Text Text(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        var text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize;
        text.color = new Color(.84f, .94f, 1); text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
    static Button Button(Transform parent, string name, string title, Vector2 position, Vector2 size, UnityAction action)
    {
        var rect = Rect(parent, name, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .25f, .31f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Text(rect, "文字", Vector2.zero, size, 18); label.text = title; Stretch(label.rectTransform);
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
    static ScrollRect Scroll(Transform parent, string name, Vector2 position, Vector2 size, bool choices)
    {
        var rect = Rect(parent, name, position, size);
        rect.gameObject.AddComponent<Image>().color = new Color(.025f, .055f, .09f);
        var scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        scroll.scrollSensitivity = 30;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = Rect(rect, "裁剪", Vector2.zero, Vector2.zero); Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "内容", Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (choices)
        {
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }
        scroll.viewport = viewport; scroll.content = content;
        return scroll;
    }
}
