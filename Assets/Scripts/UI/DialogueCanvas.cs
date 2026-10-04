using UnityEngine;
using UnityEngine.UI;

namespace HollowDemo
{
    public sealed class DialogueCanvas : MonoBehaviour
    {
        public GameObject panel;
        public Text speaker, body;
        public ScrollRect bodyScroll, choicesScroll;
        public Transform choices;
        public Button choiceTemplate;
        DemoGame game;
        Npc shownNpc;
        int shownNode = -1;

        void Awake()
        {
            game = DemoGame.Instance;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = ChineseFont.Shared;
        }

        void LateUpdate()
        {
            bool visible = game.Screen == GameScreen.Dialogue && !game.Transitioning;
            panel.SetActive(visible);
            if (!visible) { shownNpc = null; shownNode = -1; return; }
            if (shownNpc == game.SpeakingNpc && shownNode == game.DialogueNode) return;
            shownNpc = game.SpeakingNpc;
            shownNode = game.DialogueNode;
            var node = shownNpc.dialogue.nodes[shownNode];
            speaker.text = string.IsNullOrEmpty(node.speaker) ? shownNpc.displayName : node.speaker;
            body.text = node.text;
            foreach (Transform child in choices)
                if (child != choiceTemplate.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach (var choice in node.choices)
            {
                var button = Instantiate(choiceTemplate, choices);
                button.gameObject.name = choice.text;
                button.GetComponentInChildren<Text>(true).text = choice.text;
                button.onClick.AddListener(() => game.ChooseDialogue(choice));
                button.gameObject.SetActive(true);
            }
            Canvas.ForceUpdateCanvases();
            bodyScroll.verticalNormalizedPosition = choicesScroll.verticalNormalizedPosition = 1;
        }

        public void Close() => game.SetScreen(GameScreen.None);
    }
}
