using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HollowDemo
{
    public sealed class DemoUI : MonoBehaviour
    {
        [System.Serializable] public sealed class ScreenPanel { public GameScreen screen; public GameObject panel; }
        public ScreenPanel[] screens;
        public Slider volume;
        public Toggle fullscreen;
        public Text volumeLabel;
        public Button[] inventoryButtons;
        public Text[] inventoryLabels;
        public Text itemTitle, itemDescription;
        public Button useItem, equipItem;
        public Text shopTitle, currency;
        public Transform shopContent;
        public ShopOfferView offerTemplate;
        public MapCanvasGraphic map;
        public GameObject interactionPanel, notificationPanel;
        public Text interaction, notification;
        public RectTransform enemyLayer;
        public Text enemyTemplate;
        public Image fade;
        public Color inventorySelection = new Color(.25f, .5f, .6f);
        DemoGame game;
        int selectedSlot;
        ShopDefinition shownShop;
        readonly Dictionary<EnemyBrain, Text> labels = new Dictionary<EnemyBrain, Text>();

        void Awake()
        {
            game = DemoGame.Instance;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            var placeholderFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var text in GetComponentsInChildren<Text>(true))
                if (text.font == null || text.font == placeholderFont) text.font = ChineseFont.Shared;
            for (int i = 0; i < inventoryButtons.Length; i++)
            {
                int index = i;
                inventoryButtons[i].onClick.AddListener(() => selectedSlot = index);
            }
        }
        void LateUpdate()
        {
            foreach (var panel in screens) panel.panel.SetActive(game.Screen == panel.screen && !game.Transitioning);
            fade.gameObject.SetActive(game.Fade > 0);
            var fadeColor = fade.color; fadeColor.a = game.Fade; fade.color = fadeColor;
            bool playing = game.HasRun && game.Player != null && !game.Transitioning;
            bool ambient = playing && (game.Screen == GameScreen.None || game.Screen == GameScreen.Dialogue);
            interactionPanel.SetActive(ambient && !game.Paused && game.Nearby != null);
            notificationPanel.SetActive(game.NoticeUntil > Time.unscaledTime && !game.Transitioning);
            notification.text = game.Notice;
            if (ambient)
            {
                interaction.text = game.Nearby == null ? "" : game.Nearby.Prompt;
            }
            if (game.Screen == GameScreen.Settings)
            {
                volume.SetValueWithoutNotify(game.Volume);
                fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
                volumeLabel.text = "主音量 " + Mathf.RoundToInt(game.Volume * 100) + "%";
            }
            if (game.Screen == GameScreen.Bag) RefreshInventory();
            if (game.Screen == GameScreen.Shop) RefreshShop();
            else shownShop = null;
            UpdateEnemies(playing && !game.Paused);
        }
        void RefreshInventory()
        {
            for (int i = 0; i < inventoryLabels.Length; i++)
            {
                var slot = game.inventory.slots[i];
                inventoryLabels[i].text = slot == null ? "空" : slot.item.displayName + "\n" + (slot.item.kind == ItemKind.RefillableFlask ? game.FlaskCharges + " / " + game.BloodFlaskCapacity : slot.item.kind == ItemKind.ManaFlask ? game.ManaFlaskCharges + " / " + game.ManaFlaskCapacity : "×" + slot.count);
                var colors = inventoryButtons[i].colors;
                colors.normalColor = i == selectedSlot ? inventorySelection : Color.white;
                inventoryButtons[i].colors = colors;
            }
            var selected = game.inventory.slots[selectedSlot];
            itemTitle.text = selected == null ? "选择物品" : selected.item.displayName;
            itemDescription.text = selected == null ? "" : selected.item.description + "\n每格上限：" + selected.item.stackLimit;
            useItem.interactable = equipItem.interactable = selected != null && (selected.item.healing > 0 || selected.item.kind == ItemKind.ManaFlask);
        }
        void RefreshShop()
        {
            var shop = game.SpeakingNpc.shop;
            shopTitle.text = shop.displayName;
            currency.text = "持有" + shop.currency.displayName + "：" + game.ItemCount(shop.currency);
            if (shownShop == shop) return;
            shownShop = shop;
            foreach (Transform child in shopContent)
                if (child != offerTemplate.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach (var offer in shop.offers)
            {
                var row = Instantiate(offerTemplate, shopContent);
                row.title.text = offer.item.displayName + " ×" + offer.quantity;
                row.description.text = offer.item.description;
                row.price.text = "购买 · " + offer.price + " " + shop.currency.displayName;
                row.buy.onClick.AddListener(() => game.Buy(offer));
                row.gameObject.SetActive(true);
            }
        }
        void UpdateEnemies(bool visible)
        {
            foreach (var dead in labels.Keys.Where(e => e == null).ToArray())
            { Destroy(labels[dead].gameObject); labels.Remove(dead); }
            if (visible)
                foreach (var enemy in game.Enemies)
                {
                    if (labels.ContainsKey(enemy)) continue;
                    var label = Instantiate(enemyTemplate, enemyLayer);
                    labels.Add(enemy, label);
                }
            foreach (var pair in labels)
            {
                var enemy = pair.Key;
                Vector3 screen = Camera.main.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.1f);
                bool show = visible && enemy.gameObject.activeInHierarchy && screen.z > 0 && Vector2.Distance(enemy.transform.position, game.Player.transform.position) <= 15;
                pair.Value.gameObject.SetActive(show);
                if (!show) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(enemyLayer, screen, GetComponent<Canvas>().worldCamera, out var point);
                pair.Value.rectTransform.anchoredPosition = point;
                pair.Value.text ="";
            }
        }
        public void Resume() => game.SetScreen(GameScreen.None);
        public void Settings() => game.OpenSettings();
        public void ReturnToMenu() => game.ReturnToMenu();
        public void Quit() => game.QuitGame();
        public void CloseSettings() => game.CloseSettings();
        public void SetVolume(float value) => game.SetVolume(value);
        public void SetFullscreen(bool value) => game.SetFullscreen(value);
        public void ReturnToDialogue() => game.SetScreen(GameScreen.Dialogue);
        public void EquipSelected()
        {
            var slot = game.inventory.slots[selectedSlot];
            if (slot != null) game.EquipItem(slot.item);
        }
        public void UseSelected()
        {
            var selected = game.inventory.slots[selectedSlot];
            if (selected != null && selected.item.kind == ItemKind.ManaFlask) { game.UseManaFlask(); return; }
            if (selected != null && selected.item.kind == ItemKind.RefillableFlask) { game.UseFlask(); return; }
            bool used = game.inventory.Use(selectedSlot, game.Player);
            game.ShowNotice(used ? "生命已恢复" : "生命已满，未消耗物品");
            if (used)
            {
                game.RefreshEquipment();
                game.SaveProgress();
            }
        }
    }
}
