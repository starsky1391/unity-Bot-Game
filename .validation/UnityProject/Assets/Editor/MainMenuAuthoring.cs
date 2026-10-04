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
        if (Object.FindObjectOfType<MainMenuCanvas>() != null) return;
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
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(game.transform, false);
        }
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
