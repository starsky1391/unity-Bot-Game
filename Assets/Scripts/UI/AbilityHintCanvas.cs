using UnityEngine;
using UnityEngine.UI;
namespace HollowDemo
{
    public sealed class AbilityHintCanvas : MonoBehaviour
    {
        public static AbilityHintCanvas Instance { get; private set; }
        public GameObject panel;
        public Text description;
        [Min(1)] public float displayDuration = 5;
        float remaining;
        void Awake()
        {
            Instance = this;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = ChineseFont.Shared;
            panel.SetActive(false);
        }
        public void Show(string keys) { description.text = keys; remaining = displayDuration; panel.SetActive(true); }
        void Update()
        {
            var game = DemoGame.Instance;
            if (!game.HasRun || game.Player.Dead) remaining = 0;
            remaining -= Time.deltaTime;
            panel.SetActive(remaining > 0 && game.Screen == GameScreen.None && !game.Transitioning);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
