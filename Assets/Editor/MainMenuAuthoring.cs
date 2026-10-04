using HollowDemo;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuAuthoring
{
    public static void Configure()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/HollowGeometry.unity");
        Create(Object.FindObjectOfType<DemoGame>());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("DEMO_CANVAS_MENU_OK: editable Canvas, scaler, event system and four persistent UI button actions created.");
    }

    public static void Create(DemoGame game)
    {
        var existing = Object.FindObjectOfType<MainMenuCanvas>();
        if (existing != null) { SplitHierarchy(existing); return; }
        var root = new GameObject("主界面 Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(game.transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        var controller = root.AddComponent<MainMenuCanvas>();
        controller.game = game;
        var panel = Rect(root.transform, "主菜单", Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = new Color(.01f, .025f, .05f);
        controller.menuPanel = panel.gameObject;
        var card = Rect(panel, "菜单背景", Vector2.zero, new Vector2(980, 550));
        card.gameObject.AddComponent<Image>().color = new Color(.05f, .09f, .14f);
        Label(card, "标题", "几何空洞", new Vector2(0, 185), new Vector2(560, 65), 46, Color.white);
        Label(card, "副标题", "探索 · 战斗 · 钩爪穿行", new Vector2(0, 130), new Vector2(560, 40), 20, new Color(.6f, .84f, .9f));
        controller.newGameButton = MenuButton(card, "新游戏", 54, controller.NewGame);
        controller.continueButton = MenuButton(card, "继续游戏", -14, controller.ContinueGame);
        controller.settingsButton = MenuButton(card, "设置", -82, controller.Settings);
        controller.quitButton = MenuButton(card, "退出游戏", -150, controller.Quit);
        controller.saveStatus = Label(card, "存档状态", "尚无存档，请选择新游戏。", new Vector2(0, -220), new Vector2(730, 35), 16, new Color(.65f, .75f, .8f));
        Layout(controller);
        SplitHierarchy(controller);
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(game.transform, false);
        }
    }

    static void SplitHierarchy(MainMenuCanvas menu)
    {
        menu.name = "主菜单 Canvas";
        menu.menuPanel.name = "背景";
        if (menu.menuButtons == null)
        {
            var group = Rect(menu.transform, "菜单按钮", Vector2.zero, Vector2.zero);
            group.anchorMin = Vector2.zero; group.anchorMax = Vector2.one;
            group.offsetMin = group.offsetMax = Vector2.zero;
            menu.menuButtons = group.gameObject.AddComponent<CanvasGroup>();
            foreach (var button in new[] { menu.newGameButton, menu.continueButton, menu.settingsButton, menu.quitButton })
                button.transform.SetParent(group, true);
        }
        if (menu.confirmationPanel == null)
        {
            var panel = Rect(menu.transform, "新游戏确认框", Vector2.zero, new Vector2(560, 300));
            panel.gameObject.AddComponent<Image>().color = new Color(.05f, .09f, .14f);
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(.4f, .7f, .8f);
            border.effectDistance = new Vector2(2, -2);
            Label(panel, "标题", "开始新游戏？", new Vector2(0, 100), new Vector2(500, 45), 30, Color.white);
            Label(panel, "说明", "开始后会替换现有进度，包括地图、背包和已拾取物品。", new Vector2(0, 30), new Vector2(480, 90), 20, Color.white);
            menu.confirmButton = MenuButton(panel, "确认", -95, menu.ConfirmNew);
            menu.cancelButton = MenuButton(panel, "取消", -95, menu.CancelNew);
            menu.confirmButton.GetComponent<RectTransform>().sizeDelta = new Vector2(230, 44);
            menu.cancelButton.GetComponent<RectTransform>().sizeDelta = new Vector2(230, 44);
            menu.confirmButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-125, -95);
            menu.cancelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(125, -95);
            foreach (var button in new[] { menu.confirmButton, menu.cancelButton })
            {
                var label = button.GetComponentInChildren<Text>().rectTransform;
                label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
                label.offsetMin = label.offsetMax = Vector2.zero;
            }
            menu.confirmationPanel = panel.gameObject;
            panel.gameObject.SetActive(false);
        }
        menu.confirmationPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(menu);
        const string path = "Assets/Prefabs/UI/GameInterface.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ui = root.GetComponent<DemoUI>();
            var panels = new System.Collections.Generic.List<DemoUI.ScreenPanel>(ui.screens);
            foreach (var entry in ui.screens)
                if (entry.screen == GameScreen.ConfirmNew)
                {
                    panels.Remove(entry);
                    Object.DestroyImmediate(entry.panel);
                }
            ui.screens = panels.ToArray();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Layout(MainMenuCanvas menu)
    {
        var card = (RectTransform)menu.newGameButton.transform.parent;
        var canvas = menu.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = null;
        menu.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
        menu.transform.localScale = Vector3.one * .02f;
        canvas.planeDistance = 1;
        card.sizeDelta = new Vector2(460, 440);
        var title = card.Find("标题").GetComponent<Text>();
        title.rectTransform.anchoredPosition = new Vector2(0, 166);
        title.rectTransform.sizeDelta = new Vector2(420, 52);
        title.fontSize = 36;
        var subtitle = card.Find("副标题").GetComponent<Text>();
        subtitle.rectTransform.anchoredPosition = new Vector2(0, 120);
        subtitle.rectTransform.sizeDelta = new Vector2(420, 32);
        var buttons = new[] { menu.newGameButton, menu.continueButton, menu.settingsButton, menu.quitButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            var rect = buttons[i].GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 44);
            rect.anchoredPosition = new Vector2(0, 30 - i * 58);
            var label = buttons[i].GetComponentInChildren<Text>();
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
        menu.saveStatus.rectTransform.anchoredPosition = new Vector2(0, -194);
        menu.saveStatus.rectTransform.sizeDelta = new Vector2(420, 32);
        menu.saveStatus.fontSize = 14;
        EditorUtility.SetDirty(menu);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
        text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    static Button MenuButton(Transform parent, string title, float y, UnityAction action)
    {
        var rect = Rect(parent, title, new Vector2(0, y), new Vector2(400, 48));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.12f, .25f, .31f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(.65f, .95f, 1);
        colors.pressedColor = new Color(.35f, .65f, .75f);
        colors.selectedColor = new Color(.65f, .95f, 1);
        button.colors = colors;
        Label(rect, "文字", title, Vector2.zero, rect.sizeDelta, 20, Color.white);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
}
