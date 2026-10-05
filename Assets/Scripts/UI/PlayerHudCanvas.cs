using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HollowDemo
{
    public sealed class PlayerHudCanvas : MonoBehaviour
    {
        public GameObject panel, crystalPanel;
        public Text crystalCount;
        public Image manaFill;
        public Image[] healthSquares;
        public Color filledHealthColor = new Color(.45f, .92f, 1);
        public Color emptyHealthColor = new Color(.19f, .24f, .3f);
        public Image[] itemIcons;
        public Text[] itemCounts = new Text[3];
        public Image[] emptyItemMasks = new Image[3];
        readonly List<Image> squares = new List<Image>();

        void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            squares.AddRange(healthSquares);
            crystalCount.font = ChineseFont.Shared;
            EnsureItemIndicators();
        }

        public void EnsureItemIndicators()
        {
            for (int i = 0; i < itemIcons.Length; i++)
            {
                if (emptyItemMasks[i] == null)
                {
                    var overlay = new GameObject("耗尽蒙版", typeof(RectTransform), typeof(Image));
                    overlay.transform.SetParent(itemIcons[i].transform, false);
                    var rect = overlay.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    emptyItemMasks[i] = overlay.GetComponent<Image>();
                    emptyItemMasks[i].color = new Color(.3f, .3f, .3f, .75f);
                    emptyItemMasks[i].raycastTarget = false;
                    overlay.SetActive(false);
                }
                if (itemCounts[i] == null)
                {
                    var counter = new GameObject("物品数量", typeof(RectTransform), typeof(Text));
                    counter.transform.SetParent(itemIcons[i].transform.parent, false);
                    var rect = counter.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                    rect.anchoredPosition = new Vector2(-5, -5);
                    rect.sizeDelta = new Vector2(48, 24);
                    itemCounts[i] = counter.GetComponent<Text>();
                    itemCounts[i].font = ChineseFont.Shared;
                    itemCounts[i].fontSize = 18;
                    itemCounts[i].alignment = TextAnchor.UpperRight;
                    itemCounts[i].color = Color.white;
                    itemCounts[i].raycastTarget = false;
                }
            }
        }

        void LateUpdate()
        {
            var game = DemoGame.Instance;
            bool visible = game.HasRun && game.Player != null &&
                (game.Screen == GameScreen.None || game.Screen == GameScreen.Dialogue);
            panel.SetActive(visible);
            crystalPanel.SetActive(visible);
            crystalCount.text = game.Crystals.ToString();
            if (!visible) return;
            manaFill.fillAmount = (float)game.Player.Mana / game.Player.maxMana;
            while (squares.Count < game.Player.maxHealth)
                squares.Add(Instantiate(healthSquares[0], healthSquares[0].transform.parent));
            for (int i = 0; i < squares.Count; i++)
            {
                squares[i].gameObject.SetActive(i < game.Player.maxHealth);
                squares[i].color = i < game.Player.Health ? filledHealthColor : emptyHealthColor;
            }
            for (int i = 0; i < 3; i++)
            {
                var item = game.EquippedItems[(game.SelectedEquipment + i + 2) % 3];
                itemIcons[i].sprite = item == null ? null : item.icon;
                itemIcons[i].color = item == null ? Color.clear : item.icon == null ? item.color : Color.white;
                int count = item == null ? 0 : game.ItemCount(item);
                itemCounts[i].text = item == null ? "" : "×" + count;
                emptyItemMasks[i].gameObject.SetActive(item != null && count == 0 && item.retainWhenEmpty);
            }
        }
    }
}
