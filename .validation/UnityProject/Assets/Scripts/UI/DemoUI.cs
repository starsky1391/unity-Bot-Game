using System.Linq;
using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(DemoGame))]
    public sealed class DemoUI : MonoBehaviour
    {
        DemoGame game;
        GUIStyle label, title, small, button;
        int selectedSlot;
        readonly MapViewport mapView = new MapViewport();
        bool mapWasOpen, draggingMap;
        Vector2 lastMapMouse;
        Vector2 shopScroll;
        void Awake() => game = GetComponent<DemoGame>();

        void OnGUI()
        {
            if (game.Player == null || game.Rooms == null || (game.Screen == GameScreen.MainMenu || game.Screen == GameScreen.Dialogue)) return;
            if (game.Screen != GameScreen.Map) { mapWasOpen = false; draggingMap = false; }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font = ChineseFont.Shared, fontSize = 18, wordWrap = true,
                    normal = { textColor = new Color(.84f, .94f, 1) } };
                title = new GUIStyle(label) { fontSize = 30, fontStyle = FontStyle.Bold };
                small = new GUIStyle(label) { fontSize = 15 };
                button = new GUIStyle(GUI.skin.button) { font = ChineseFont.Shared, fontSize = 18, wordWrap = true };
            }
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.enabled = !game.Transitioning;
            if (game.Screen != GameScreen.MainMenu && game.Screen != GameScreen.ConfirmNew && game.Screen != GameScreen.Map) DrawHud();
            if (game.Paused && !game.Transitioning)
            {
                Fill(new Rect(0, 0, 1280, 720), new Color(.01f, .025f, .05f, .9f));
                Fill(new Rect(150, 90, 980, 550), new Color(.05f, .09f, .14f));
                switch (game.Screen)
                {
                    case GameScreen.ConfirmNew: ConfirmNew(); break;
                    case GameScreen.Pause: PauseMenu(); break;
                    case GameScreen.Settings: Settings(); break;
                    case GameScreen.Map: Map(); break;
                    case GameScreen.Bag: Bag(); break;
                    case GameScreen.Shop: Shop(); break;
                }
            }
            if (game.NoticeUntil > Time.unscaledTime)
            {
                Fill(new Rect(320, 30, 640, 45), new Color(.04f, .12f, .16f, .96f));
                GUI.Label(new Rect(335, 39, 615, 30), game.Notice, label);
            }
            GUI.enabled = true;
            if (game.Fade > 0) Fill(new Rect(0, 0, 1280, 720), new Color(0, 0, 0, game.Fade));
        }

        void DrawHud()
        {
            var player = game.Player;
            Fill(new Rect(20, 20, 340, 102), new Color(.03f, .06f, .1f, .92f));
            GUI.Label(new Rect(36, 26, 310, 35), "几何空洞", title);
            for (int i = 0; i < player.maxHealth; i++) Fill(new Rect(38 + i * 29, 68, 20, 20),
                i < player.Health ? new Color(.45f, .92f, 1) : new Color(.19f, .24f, .3f));
            GUI.Label(new Rect(38, 95, 320, 23), "二段跳：" + (!player.enableDoubleJump ? "关闭" : player.CanDoubleJump ? "可用" : "已用") +
                "　冲刺：" + (!player.enableDash ? "关闭" : player.CanAirDash ? "可用" : "已用"), small);
            var room = game.Rooms.FirstOrDefault(r => r.id == game.CurrentRoom);
            if (room != null)
            {
                GUI.Label(new Rect(900, 24, 355, 38), room.title, title);
                GUI.Label(new Rect(900, 66, 350, 85), room.hint, small);
            }
            if (game.Nearby != null && !game.Paused)
            {
                Fill(new Rect(395, 602, 490, 45), new Color(.03f, .06f, .1f, .92f));
                GUI.Label(new Rect(413, 612, 460, 30), game.Nearby.Prompt, label);
            }
            if (game.Paused) return;
            foreach (var enemy in game.Enemies)
            {
                if (!enemy.gameObject.activeInHierarchy || Vector2.Distance(enemy.transform.position, player.transform.position) > 15) continue;
                var p = Camera.main.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.1f);
                GUI.Label(new Rect(p.x / Screen.width * 1280 - 52, (1 - p.y / Screen.height) * 720 - 10, 150, 30),
                    ChineseFont.EnemyNames[(int)enemy.kind] + " " + enemy.Health, small);
            }
        }

        void ConfirmNew()
        {
            GUI.Label(new Rect(220, 200, 800, 55), "开始新游戏？", title);
            GUI.Label(new Rect(220, 275, 800, 75), "现有存档会被新进度替换，包括地图、背包和已拾取物品。", label);
            if (GUI.Button(new Rect(220, 420, 300, 50), "确认开始新游戏", button)) game.NewGame();
            if (GUI.Button(new Rect(560, 420, 300, 50), "取消", button)) game.SetScreen(GameScreen.MainMenu);
        }

        void PauseMenu()
        {
            GUI.Label(new Rect(200, 125, 650, 45), "游戏已暂停", title);
            if (GUI.Button(new Rect(200, 218, 310, 48), "继续游戏", button)) game.SetScreen(GameScreen.None);
            if (GUI.Button(new Rect(200, 290, 310, 48), "设置", button)) game.OpenSettings();
            if (GUI.Button(new Rect(200, 362, 310, 48), "回到主界面", button)) game.ReturnToMenu();
            if (GUI.Button(new Rect(200, 434, 310, 48), "退出游戏", button)) game.QuitGame();
            GUI.Label(new Rect(590, 200, 420, 40), "操作说明", title);
            GUI.Label(new Rect(590, 253, 445, 340), "A / D　移动\n空格　跳跃、二段跳、钩爪跳离\nShift　冲刺\nW / S　爬墙\nJ　近战攻击\nK　连接前方最近钩点 / 松开\nE　拾取、对话、激活检查点\nM　地图　　B　背包\nESC　暂停 / 关闭界面", label);
            GUI.Label(new Rect(200, 552, 830, 48), "黄色为预警，红色为攻击，暗色为恢复。返回主界面或退出时自动保存。", small);
        }

        void Settings()
        {
            GUI.Label(new Rect(220, 135, 850, 45), "设置", title);
            GUI.Label(new Rect(220, 235, 400, 40), "主音量　" + Mathf.RoundToInt(game.Volume * 100) + "%", label);
            float volume = GUI.HorizontalSlider(new Rect(220, 288, 680, 30), game.Volume, 0, 1);
            if (!Mathf.Approximately(volume, game.Volume)) game.SetVolume(volume);
            bool fullscreen = GUI.Toggle(new Rect(220, 345, 680, 40), Screen.fullScreen, "全屏显示", button);
            if (fullscreen != Screen.fullScreen) game.SetFullscreen(fullscreen);
            GUI.Label(new Rect(220, 405, 780, 65), "设置自动保存。当前原型尚未添加音效资源。", small);
            if (GUI.Button(new Rect(220, 525, 280, 48), "返回", button)) game.CloseSettings();
        }

        void Map()
        {
            GUI.Label(new Rect(195, 120, 870, 45), "洞穴手绘图", title);
            GUI.Label(new Rect(195, 175, 870, 30), "鼠标拖动平移 · 滚轮缩放 · 未探索区域隐藏", small);
            Rect viewport = new Rect(185, 225, 910, 305);
            Vector2 playerPosition = game.ActiveRoom.MapPosition(game.Rooms[game.CurrentRoom].map, game.Player.transform.position);
            if (!mapWasOpen) { mapView.Open(playerPosition); mapWasOpen = true; }
            var input = Event.current;
            if (input.type == EventType.MouseDown && input.button == 0 && viewport.Contains(input.mousePosition))
            { draggingMap = true; lastMapMouse = input.mousePosition; input.Use(); }
            else if (input.type == EventType.MouseDrag && draggingMap)
            { mapView.Pan(input.mousePosition - lastMapMouse); lastMapMouse = input.mousePosition; input.Use(); }
            else if (input.type == EventType.MouseUp && draggingMap)
            { draggingMap = false; input.Use(); }
            else if (input.type == EventType.ScrollWheel && viewport.Contains(input.mousePosition))
            { mapView.Zoom(input.delta.y, input.mousePosition, viewport); input.Use(); }
            Fill(viewport, new Color(.025f, .045f, .07f));
            GUI.BeginGroup(viewport);
            Rect canvas = new Rect(0, 0, viewport.width, viewport.height);
            foreach (var room in MapGeometry.VisibleRooms(game.Rooms))
            {
                var map = room.map;
                Color ink = room.id == game.CurrentRoom ? new Color(.5f, .95f, .9f) : new Color(.72f, .76f, .73f);
                foreach (var wall in MapGeometry.Walls(map))
                    foreach (var segment in game.MapFog.VisibleSegments(room.id, wall[0] - map.origin, wall[1] - map.origin))
                        MapLine(mapView.Project(segment[0] + map.origin, canvas), mapView.Project(segment[1] + map.origin, canvas), ink, room.id == game.CurrentRoom ? 2.5f : 1.5f);
                foreach (var exit in map.exits)
                {
                    if (game.Rooms[exit.targetRoom].explored) continue;
                    Vector2 tangent = new Vector2(-exit.direction.y, exit.direction.x) * .55f;
                    foreach (float side in new[] { -1f, 1f })
                        foreach (var segment in game.MapFog.VisibleSegments(room.id, exit.position + tangent * side - exit.direction * .3f,
                            exit.position + tangent * side))
                            MapLine(mapView.Project(map.origin + segment[0], canvas), mapView.Project(map.origin + segment[1], canvas), ink, 1.5f);
                }
                if (game.CheckpointActivated(room.id) && game.MapFog.Visible(room.id, map.checkpointPosition))
                {
                    Vector2 point = mapView.Project(map.origin + map.checkpointPosition, canvas);
                    MapLine(point + Vector2.up * 6, point + Vector2.right * 6, new Color(.4f, 1, .65f), 2);
                    MapLine(point + Vector2.right * 6, point + Vector2.down * 6, new Color(.4f, 1, .65f), 2);
                    MapLine(point + Vector2.down * 6, point + Vector2.left * 6, new Color(.4f, 1, .65f), 2);
                    MapLine(point + Vector2.left * 6, point + Vector2.up * 6, new Color(.4f, 1, .65f), 2);
                }
                foreach (var merchant in map.merchants)
                {
                    if (!game.LandmarkDiscovered(merchant.id) || !game.MapFog.Visible(room.id, merchant.position)) continue;
                    Vector2 point = mapView.Project(map.origin + merchant.position, canvas);
                    Fill(new Rect(point.x - 7, point.y - 7, 14, 14), new Color(.9f, .68f, .32f));
                    if (mapView.Scale >= 18)
                        GUI.Label(new Rect(point.x - 10, point.y - 12, 22, 24), "商", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 12, normal = { textColor = Color.black } });
                }
            }
            Vector2 player = mapView.Project(playerPosition, canvas);
            Fill(new Rect(player.x - 6, player.y - 6, 12, 12), new Color(.4f, .85f, 1, .3f));
            Fill(new Rect(player.x - 3, player.y - 3, 6, 6), Color.white);
            GUI.EndGroup();
            GUI.Label(new Rect(195, 545, 890, 30), "白点：当前位置　绿菱形：激活检查点　金方块：发现的商人", small);
            if (GUI.Button(new Rect(620, 574, 220, 40), "回到当前位置", button)) mapView.Recenter(playerPosition);
            if (GUI.Button(new Rect(865, 574, 220, 40), "收起地图", button)) game.SetScreen(GameScreen.None);
        }

        static void MapLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 delta = to - from;
            if (Mathf.Abs(delta.y) < .01f)
                Fill(new Rect(Mathf.Min(from.x, to.x), from.y - width / 2, Mathf.Abs(delta.x), width), color);
            else if (Mathf.Abs(delta.x) < .01f)
                Fill(new Rect(from.x - width / 2, Mathf.Min(from.y, to.y), width, Mathf.Abs(delta.y)), color);
            else
            {
                // 保持分组坐标不变，让地图窗口正确裁剪斜线图标。
                int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)));
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
                    Fill(new Rect(point.x - width / 2, point.y - width / 2, width, width), color);
                }
            }
        }

        void Bag()
        {
            GUI.Label(new Rect(190, 125, 850, 45), "背包 · 16 格", title);
            for (int i = 0; i < Inventory.Capacity; i++)
            {
                var slot = game.inventory.slots[i];
                string text = slot == null ? "空" : slot.item.displayName + "\n×" + slot.count;
                GUI.backgroundColor = selectedSlot == i ? new Color(.4f, .9f, 1) : Color.white;
                if (GUI.Button(new Rect(190 + i % 4 * 132, 195 + i / 4 * 84, 120, 72), text, button)) selectedSlot = i;
            }
            GUI.backgroundColor = Color.white;
            var selected = game.inventory.slots[selectedSlot];
            if (selected != null)
            {
                GUI.Label(new Rect(755, 215, 335, 45), selected.item.displayName, title);
                GUI.Label(new Rect(755, 278, 310, 130), selected.item.description + "\n每格上限：" + selected.item.stackLimit, label);
                if (selected.item.healing > 0 && GUI.Button(new Rect(755, 430, 250, 45), "使用物品", button))
                {
                    bool used = game.inventory.Use(selectedSlot, game.Player);
                    game.ShowNotice(used ? "生命已恢复" : "生命已满，未消耗物品");
                    if (used) game.SaveProgress();
                }
            }
            if (GUI.Button(new Rect(190, 563, 220, 40), "关闭背包", button)) game.SetScreen(GameScreen.None);
        }

        void Shop()
        {
            var shop = game.SpeakingNpc.shop;
            GUI.Label(new Rect(205, 125, 850, 48), shop.displayName, title);
            GUI.Label(new Rect(205, 180, 850, 35), "持有" + shop.currency.displayName + "：" + game.inventory.Count(shop.currency), label);
            shopScroll = GUI.BeginScrollView(new Rect(190, 230, 900, 315), shopScroll,
                new Rect(0, 0, 865, Mathf.Max(305, shop.offers.Length * 90)));
            for (int i = 0; i < shop.offers.Length; i++)
            {
                var offer = shop.offers[i];
                float y = 10 + i * 90;
                GUI.Label(new Rect(15, y, 530, 30), offer.item.displayName + " ×" + offer.quantity, label);
                GUI.Label(new Rect(15, y + 32, 530, 50), offer.item.description, small);
                if (GUI.Button(new Rect(605, y + 8, 250, 48), "购买 · " + offer.price + " " + shop.currency.displayName, button)) game.Buy(offer);
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(205, 563, 280, 40), "返回对话", button)) game.SetScreen(GameScreen.Dialogue);
        }

        static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
