using HollowDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerHudAuthoring
{
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var game = Object.FindObjectOfType<DemoGame>();
        const string path = "Assets/Prefabs/UI/PlayerHUD.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            var root = new GameObject("角色 HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerHudCanvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.planeDistance = 1; canvas.sortingOrder = 20;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
            root.transform.localScale = Vector3.one * .02f;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var hud = root.GetComponent<PlayerHudCanvas>();
            hud.panel = Rect(root.transform, "血量与道具栏", Vector2.zero, new Vector2(300, 160)).gameObject;
            hud.itemIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Rect(hud.panel.transform, i == 1 ? "当前道具" : i == 0 ? "左侧道具" : "右侧道具", new Vector2((i - 1) * 92, -20), Vector2.one * (i == 1 ? 86 : 60));
                hud.itemIcons[i] = Image(Rect(slot, "物品图标", Vector2.zero, Vector2.one * (i == 1 ? 42 : 26)), Color.clear);
            }
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
        var contents = PrefabUtility.LoadPrefabContents(path);
        var controller = contents.GetComponent<PlayerHudCanvas>();
        foreach (var text in contents.GetComponentsInChildren<Text>(true)) Object.DestroyImmediate(text.gameObject);
        var panel = controller.panel.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(20, -20); panel.sizeDelta = new Vector2(300, 160);
        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null) Object.DestroyImmediate(panelImage);
        var oldTrack = panel.Find("血条底色");
        if (oldTrack != null) Object.DestroyImmediate(oldTrack.gameObject);
        var healthRow = panel.Find("血量方块");
        if (healthRow == null)
        {
            var row = Rect(panel, "血量方块", new Vector2(15, -10), new Vector2(270, 20));
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0, 1);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 9; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            int count = Object.FindObjectOfType<PlayerMotor>().maxHealth;
            controller.healthSquares = new Image[count];
            for (int i = 0; i < count; i++)
                controller.healthSquares[i] = Image(Rect(row, "血量 " + (i + 1), Vector2.zero, Vector2.one * 20), controller.filledHealthColor);
        }
        for (int i = 0; i < 3; i++)
        {
            var icon = controller.itemIcons[i];
            var slot = icon.transform.parent.GetComponent<RectTransform>();
            slot.anchoredPosition = new Vector2((i - 1) * 92, -20);
            var background = slot.GetComponent<Image>();
            if (background != null) Object.DestroyImmediate(background);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.color = Color.clear;
            if (slot.Find("上边框") == null)
            {
                Color color = i == 1 ? new Color(.45f, .85f, .9f) : new Color(.35f, .5f, .55f);
                float size = slot.sizeDelta.x;
                Image(Rect(slot, "上边框", new Vector2(0, size / 2 - 1), new Vector2(size, 2)), color);
                Image(Rect(slot, "下边框", new Vector2(0, -size / 2 + 1), new Vector2(size, 2)), color);
                Image(Rect(slot, "左边框", new Vector2(-size / 2 + 1, 0), new Vector2(2, size)), color);
                Image(Rect(slot, "右边框", new Vector2(size / 2 - 1, 0), new Vector2(2, size)), color);
            }
        }
        controller.EnsureItemIndicators();
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (Object.FindObjectOfType<PlayerHudCanvas>() == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(game.transform, false);
        }
        var hudCanvas = Object.FindObjectOfType<PlayerHudCanvas>().GetComponent<Canvas>();
        if (hudCanvas.renderMode == RenderMode.ScreenSpaceCamera) hudCanvas.worldCamera = Camera.main;
        PrefabUtility.RecordPrefabInstancePropertyModifications(hudCanvas);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DEMO_HUD_CONFIGURED_OK: square health, transparent background, equipment icons only.");
    }
    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static Image Image(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
}
