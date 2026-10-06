using UnityEngine;
using UnityEngine.UI;
namespace HollowDemo
{
    public sealed class BossCanvas : MonoBehaviour
    {
        public CanvasGroup introduction;
        public Text introTitle,hudTitle,healthText;
        public GameObject healthPanel;
        public Image healthFill;
        [Min(.05f)] public float fadeDuration=.35f;
        Canvas canvas;
        void Awake()
        {
            canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;
            foreach(var text in GetComponentsInChildren<Text>(true))
                if(text.font==null || text.font==Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")) text.font=ChineseFont.Shared;
        }
        void LateUpdate()
        {
            var game=DemoGame.Instance;
            var arena=game.ActiveBoss;
            canvas.enabled=arena!=null && game.HasRun && !game.Player.Dead && !game.Transitioning && game.Screen==GameScreen.None;
            var owner=GetComponentInParent<BossArena>();
            if(owner!=null && arena!=owner) canvas.enabled=false;
            if(owner==null && arena!=null && arena.GetComponentInChildren<BossCanvas>(true)!=null) canvas.enabled=false;
            if(!canvas.enabled) return;
            bool victory=arena.State==BossEncounterState.Defeated;
            bool intro=arena.State==BossEncounterState.Introduction || victory;
            introduction.gameObject.SetActive(intro);
            float duration=victory?arena.victoryDuration:arena.introDuration;
            introduction.alpha=intro?Mathf.Clamp01(Mathf.Min(arena.DisplayTime,duration-arena.DisplayTime)/fadeDuration):0;
            introTitle.text=arena.bossName+(victory?"\n已击败":"");
            healthPanel.SetActive(!victory);
            hudTitle.text=arena.bossName;
            healthFill.fillAmount=Mathf.Clamp01((float)arena.boss.Health/arena.boss.maxHealth);
            healthText.text=arena.boss.Health+" / "+arena.boss.maxHealth;
        }
    }
}
