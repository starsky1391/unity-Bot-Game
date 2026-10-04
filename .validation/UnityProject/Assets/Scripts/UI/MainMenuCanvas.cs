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
        bool wasVisible;

        void Awake()
        {
            GetComponent<Canvas>().enabled = true;
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = ChineseFont.Shared;
        }

        void LateUpdate()
        {
            bool visible = game.Screen == GameScreen.MainMenu && !game.Transitioning;
            menuPanel.SetActive(visible);
            if (visible)
            {
                bool hasSave = SaveStore.Exists;
                continueButton.interactable = hasSave;
                saveStatus.text = hasSave ? "已有本地存档，继续游戏将从检查点出发。" : "尚无存档，请选择新游戏。";
                if (!wasVisible) EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
            }
            else if (wasVisible) EventSystem.current.SetSelectedGameObject(null);
            wasVisible = visible;
        }

        public void NewGame() => game.RequestNewGame();
        public void ContinueGame() => game.ContinueGame();
        public void Settings() => game.OpenSettings();
        public void Quit() => game.QuitGame();
    }
}
