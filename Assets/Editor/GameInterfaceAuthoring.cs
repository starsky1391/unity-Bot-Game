using System.Collections.Generic;
using HollowDemo;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class GameInterfaceAuthoring
{
    [MenuItem("洞穴 Demo/生成 Canvas 游戏界面")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        Create(Object.FindObjectOfType<DemoGame>());
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_CANVAS_INTERFACE_OK: menus, map, inventory, shop, prompts and transition use editable Canvas prefab.");
    }

    [MenuItem("洞穴 Demo/界面使用独立编辑布局")]
    public static void ConfigureEditingLayout()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        var game = Object.FindObjectOfType<DemoGame>();
        foreach (string path in new[] { "Assets/Prefabs/UI/PlayerHUD.prefab", "Assets/Prefabs/UI/DialogueBox.prefab", "Assets/Prefabs/UI/GameInterface.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1280, 720);
                rect.localPosition = Vector3.zero;
                rect.localScale = Vector3.one * .02f;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var canvas in game.GetComponentsInChildren<Canvas>(true))
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280, 720);
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one * .02f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
        game.transform.position = new Vector3(-30, 25, 0);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DEMO_EDITING_UI_OK: independent world-space editing canvases; runtime controllers bind to gameplay camera.");
    }

    public static void Create(DemoGame game)
    {
        var old = game.GetComponent<DemoUI>();
        if (old != null) Object.DestroyImmediate(old);
        const string path = "Assets/Prefabs/UI/GameInterface.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            var root = new GameObject("游戏界面 Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DemoUI));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.planeDistance = 1; canvas.sortingOrder = 120;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
            root.transform.localScale = Vector3.one * .02f;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var ui = root.GetComponent<DemoUI>();
            var panels = new List<DemoUI.ScreenPanel>();

            var pause = Panel(root.transform, "暂停与按键说明", new Vector2(940, 540));
            panels.Add(new DemoUI.ScreenPanel { screen = GameScreen.Pause, panel = pause.gameObject });
            Text(pause, "标题", "游戏已暂停", new Vector2(0, 220), new Vector2(840, 45), 30);
            Button(pause, "继续游戏", "继续游戏", new Vector2(-275, 115), new Vector2(280, 46), ui.Resume);
            Button(pause, "设置", "设置", new Vector2(-275, 40), new Vector2(280, 46), ui.Settings);
            Button(pause, "回到主界面", "回到主界面", new Vector2(-275, -35), new Vector2(280, 46), ui.ReturnToMenu);
            Button(pause, "退出游戏", "退出游戏", new Vector2(-275, -110), new Vector2(280, 46), ui.Quit);
            Text(pause, "操作说明", "A / D　移动\n空格　跳跃、二段跳、钩爪跳离\nShift　冲刺　　W / S　爬墙\nJ　近战攻击\nK　连接前方或头顶钩点 / 松开\nQ　向右切换道具　F　使用道具\nE　拾取、对话、激活检查点\nM　地图　　B　背包\nESC　暂停 / 关闭界面", new Vector2(140, 20), new Vector2(540, 335), 20, TextAnchor.MiddleLeft);
            Text(pause, "提示", "黄色：预警　红色：攻击　暗色：恢复。返回主界面或退出时自动保存。", new Vector2(0, -225), new Vector2(860, 50), 16);

            var settings = Panel(root.transform, "设置", new Vector2(640, 430));
            panels.Add(new DemoUI.ScreenPanel { screen = GameScreen.Settings, panel = settings.gameObject });
            Text(settings, "标题", "设置", new Vector2(0, 160), new Vector2(570, 45), 30);
            ui.volumeLabel = Text(settings, "音量数值", "主音量", new Vector2(0, 85), new Vector2(540, 30), 20);
            var sliderRect = Rect(settings, "主音量滑条", new Vector2(0, 35), new Vector2(500, 24));
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            Image(sliderRect, new Color(.12f, .18f, .23f)).raycastTarget = true;
            var fill = Rect(sliderRect, "音量填充", Vector2.zero, Vector2.zero); Stretch(fill);
            Image(fill, new Color(.3f, .75f, .8f)); slider.fillRect = fill;
            var handle = Rect(sliderRect, "滑块", Vector2.zero, new Vector2(18, 30));
            slider.targetGraphic = Image(handle, Color.white); slider.handleRect = handle;
            slider.targetGraphic.raycastTarget = true;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, ui.SetVolume); ui.volume = slider;
            var toggleRect = Rect(settings, "全屏显示", new Vector2(0, -35), new Vector2(500, 35));
            var toggle = toggleRect.gameObject.AddComponent<Toggle>();
            var checkbox = Rect(toggleRect, "勾选框", new Vector2(-215, 0), new Vector2(28, 28));
            toggle.targetGraphic = Image(checkbox, new Color(.15f, .3f, .36f));
            toggle.targetGraphic.raycastTarget = true;
            toggle.graphic = Image(Rect(checkbox, "勾选", Vector2.zero, new Vector2(18, 18)), Color.white);
            Text(toggleRect, "文字", "全屏显示", new Vector2(0, 0), new Vector2(360, 30), 20, TextAnchor.MiddleLeft);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, ui.SetFullscreen); ui.fullscreen = toggle;
            Text(settings, "说明", "设置自动保存。当前原型尚未添加音效资源。", new Vector2(0, -105), new Vector2(560, 50), 18);
            Button(settings, "返回", "返回", new Vector2(0, -165), new Vector2(240, 44), ui.CloseSettings);

            var map = Panel(root.transform, "地图", new Vector2(1040, 570));
            panels.Add(new DemoUI.ScreenPanel { screen = GameScreen.Map, panel = map.gameObject });
            Text(map, "标题", "洞穴手绘图", new Vector2(0, 235), new Vector2(950, 45), 30);
            Text(map, "操作提示", "鼠标拖动平移 · 滚轮缩放 · 未探索区域隐藏", new Vector2(0, 185), new Vector2(950, 30), 16);
            var viewport = Rect(map, "地图裁剪窗口", new Vector2(0, -5), new Vector2(960, 325));
            Image(viewport, new Color(.025f, .045f, .07f)); viewport.gameObject.AddComponent<RectMask2D>();
            var graphic = Rect(viewport, "地图轮廓与图标", Vector2.zero, Vector2.zero); Stretch(graphic);
            ui.map = graphic.gameObject.AddComponent<MapCanvasGraphic>();
            Text(map, "图例", "白点：当前位置　绿菱形：激活检查点　金方块：发现的商人", new Vector2(0, -190), new Vector2(950, 30), 16);
            Button(map, "回到当前位置", "回到当前位置", new Vector2(210, -240), new Vector2(200, 40), ui.map.Recenter);
            Button(map, "关闭地图", "收起地图", new Vector2(385, -240), new Vector2(130, 40), ui.Resume);

            var bag = Panel(root.transform, "背包", new Vector2(960, 540));
            panels.Add(new DemoUI.ScreenPanel { screen = GameScreen.Bag, panel = bag.gameObject });
            Text(bag, "标题", "背包 · 16 格", new Vector2(0, 220), new Vector2(870, 45), 30);
            ui.inventoryButtons = new Button[16]; ui.inventoryLabels = new Text[16];
            for (int i = 0; i < 16; i++)
            {
                var button = Button(bag, "背包格 " + (i + 1), "空", new Vector2(-355 + (i % 4) * 125, 125 - (i / 4) * 78), new Vector2(112, 68), null);
                ui.inventoryButtons[i] = button; ui.inventoryLabels[i] = button.GetComponentInChildren<Text>();
            }
            ui.itemTitle = Text(bag, "物品名称", "选择物品", new Vector2(280, 130), new Vector2(310, 45), 26);
            ui.itemDescription = Text(bag, "物品说明", "", new Vector2(280, 35), new Vector2(310, 135), 20, TextAnchor.UpperLeft);
            ui.useItem = Button(bag, "使用物品", "使用物品", new Vector2(280, -90), new Vector2(270, 44), ui.UseSelected);
            ui.equipItem = Button(bag, "装备道具", "装备到道具栏", new Vector2(280, -145), new Vector2(270, 44), ui.EquipSelected);
            Button(bag, "关闭背包", "关闭背包", new Vector2(-295, -220), new Vector2(240, 40), ui.Resume);

            var shop = Panel(root.transform, "商店", new Vector2(960, 540));
            panels.Add(new DemoUI.ScreenPanel { screen = GameScreen.Shop, panel = shop.gameObject });
            ui.shopTitle = Text(shop, "商店标题", "商店", new Vector2(0, 220), new Vector2(870, 45), 30);
            ui.currency = Text(shop, "货币数量", "", new Vector2(0, 165), new Vector2(870, 30), 20);
            var scroll = Rect(shop, "商品滚动列表", new Vector2(0, -10), new Vector2(880, 300));
            Image(scroll, new Color(.025f, .045f, .07f));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.scrollSensitivity = 30; scrollRect.movementType = ScrollRect.MovementType.Clamped;
            var clip = Rect(scroll, "裁剪", Vector2.zero, Vector2.zero); Stretch(clip); clip.gameObject.AddComponent<RectMask2D>();
            var content = Rect(clip, "商品内容", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 10; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = content; scrollRect.viewport = clip; ui.shopContent = content;
            var row = Rect(content, "商品行模板", Vector2.zero, new Vector2(850, 96));
            Image(row, new Color(.06f, .12f, .16f)); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            ui.offerTemplate = row.gameObject.AddComponent<ShopOfferView>();
            ui.offerTemplate.title = Text(row, "商品名称", "商品名称", new Vector2(-140, 22), new Vector2(530, 30), 20, TextAnchor.MiddleLeft);
            ui.offerTemplate.description = Text(row, "商品说明", "商品说明", new Vector2(-140, -17), new Vector2(530, 48), 16, TextAnchor.MiddleLeft);
            ui.offerTemplate.buy = Button(row, "购买", "购买", new Vector2(290, 0), new Vector2(225, 45), null);
            ui.offerTemplate.price = ui.offerTemplate.buy.GetComponentInChildren<Text>(); row.gameObject.SetActive(false);
            Button(shop, "返回对话", "返回对话", new Vector2(-275, -220), new Vector2(290, 40), ui.ReturnToDialogue);
            ui.screens = panels.ToArray();

            var interaction = Panel(root.transform, "互动提示", new Vector2(490, 45));
            interaction.anchorMin = interaction.anchorMax = interaction.pivot = new Vector2(.5f, 0); interaction.anchoredPosition = new Vector2(0, 65);
            ui.interactionPanel = interaction.gameObject;
            ui.interaction = Text(interaction, "提示文字", "E 互动", Vector2.zero, new Vector2(460, 36), 20);
            var notice = Panel(root.transform, "通知", new Vector2(640, 45));
            notice.anchorMin = notice.anchorMax = notice.pivot = new Vector2(.5f, 1); notice.anchoredPosition = new Vector2(0, -25);
            ui.notificationPanel = notice.gameObject;
            ui.notification = Text(notice, "通知文字", "获得物品", Vector2.zero, new Vector2(610, 36), 20);
            ui.enemyLayer = Rect(root.transform, "怪物标签层", Vector2.zero, Vector2.zero); Stretch(ui.enemyLayer);
            ui.enemyTemplate = Text(ui.enemyLayer, "怪物名称血量模板", "怪物 生命", Vector2.zero, new Vector2(160, 30), 16);
            ui.enemyTemplate.gameObject.SetActive(false);
            var fade = Rect(root.transform, "切房间黑屏", Vector2.zero, Vector2.zero); Stretch(fade);
            ui.fade = Image(fade, Color.black); ui.fade.raycastTarget = true; fade.gameObject.SetActive(false);
            foreach (var panel in panels) panel.panel.SetActive(false);
            interaction.gameObject.SetActive(false); notice.gameObject.SetActive(false);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        }
        var contents = PrefabUtility.LoadPrefabContents(path);
        var mapObject = contents.GetComponent<DemoUI>().map.gameObject;
        if (mapObject.GetComponent<CanvasRenderer>() == null) mapObject.AddComponent<CanvasRenderer>();
        prefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        if (Object.FindObjectOfType<DemoUI>() == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, game.gameObject.scene);
            instance.transform.SetParent(game.transform, false);
        }
        var sceneCanvas = Object.FindObjectOfType<DemoUI>().GetComponent<Canvas>();
        if (sceneCanvas.renderMode == RenderMode.ScreenSpaceCamera) sceneCanvas.worldCamera = Camera.main;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneCanvas);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static Image Image(RectTransform rect, Color color)
    { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
    static RectTransform Panel(Transform parent, string name, Vector2 size)
    {
        var rect = Rect(parent, name, Vector2.zero, size);
        Image(rect, new Color(.05f, .09f, .14f, .97f));
        var border = rect.gameObject.AddComponent<Outline>(); border.effectColor = new Color(.35f, .6f, .65f); border.effectDistance = new Vector2(2, -2);
        return rect;
    }
    static Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
        text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize;
        text.color = new Color(.84f, .94f, 1); text.alignment = alignment; text.raycastTarget = false; return text;
    }
    static Button Button(Transform parent, string name, string title, Vector2 position, Vector2 size, UnityAction action)
    {
        var rect = Rect(parent, name, position, size); var image = Image(rect, new Color(.12f, .25f, .31f)); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Text(rect, "文字", title, Vector2.zero, size, 20); Stretch(label.rectTransform);
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action); return button;
    }
}
