using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HollowDemo
{
    public sealed class MainMenuCanvas : MonoBehaviour
    {
        public DemoGame game;
        public GameObject menuPanel;
        public Button newGameButton, continueButton, settingsButton, quitButton;
        public Text saveStatus;
        public CanvasGroup menuButtons;
        public GameObject confirmationPanel;
        public Button confirmButton, cancelButton;
        GameScreen previousScreen;
        bool wasVisible;

        void Awake()
        {
            GetComponent<Canvas>().enabled = true;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = ChineseFont.Shared;
        }

        void LateUpdate()
        {
            bool confirming = game.Screen == GameScreen.ConfirmNew;
            bool visible = (game.Screen == GameScreen.MainMenu || confirming) && !game.Transitioning;
            menuPanel.SetActive(visible);
            menuButtons.gameObject.SetActive(visible);
            menuButtons.interactable = !confirming;
            menuButtons.blocksRaycasts = !confirming;
            confirmationPanel.SetActive(visible && confirming);
            if (visible)
            {
                bool hasSave = SaveStore.Exists;
                continueButton.interactable = hasSave;
                saveStatus.text = hasSave ? "已有本地存档，继续游戏将从检查点出发。" : "尚无存档，请选择新游戏。";
                if (!wasVisible || previousScreen != game.Screen)
                    EventSystem.current.SetSelectedGameObject(confirming ? cancelButton.gameObject : newGameButton.gameObject);
            }
            else if (wasVisible) EventSystem.current.SetSelectedGameObject(null);
            wasVisible = visible;
            previousScreen = game.Screen;
        }

        public void ConfirmNew() => game.NewGame();
        public void CancelNew() => game.SetScreen(GameScreen.MainMenu);
        public void NewGame() => game.RequestNewGame();
        public void ContinueGame() => game.ContinueGame();
        public void Settings() => game.OpenSettings();
        public void Quit() => game.QuitGame();
    }
}
