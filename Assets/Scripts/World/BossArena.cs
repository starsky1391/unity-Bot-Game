using UnityEngine;
namespace HollowDemo
{
    public enum BossEncounterState { Idle, Introduction, Fighting, Defeated }
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossArena : MonoBehaviour
    {
        public string bossName="暗影守卫";
        public EnemyBrain boss;
        public Transform spawnPoint;
        public GameObject leftBarrier,rightBarrier;
        [Min(.5f)] public float introDuration=2.5f;
        [Min(.1f)] public float victoryDuration=2;
        public BossEncounterState State { get;private set; }
        public float DisplayTime { get;private set; }
        public bool Locked => leftBarrier.activeSelf && rightBarrier.activeSelf;
        void Awake(){HideBoss();leftBarrier.SetActive(false);rightBarrier.SetActive(false);}
        void HideBoss()
        {
            boss.enabled=false;
            boss.GetComponent<Rigidbody2D>().simulated=false;
            boss.GetComponent<Collider2D>().enabled=false;
            foreach(var visual in boss.GetComponentsInChildren<SpriteRenderer>()) visual.enabled=false;
        }
        public void ResetEncounter()
        {
            var game=DemoGame.Instance;
            if(game.ActiveBoss==this) game.ActiveBoss=null;
            State=game.EnemyDefeated(boss.persistentId)?BossEncounterState.Defeated:BossEncounterState.Idle;
            DisplayTime=0;
            if(State==BossEncounterState.Idle) boss.ResetEnemy();
            HideBoss();
            leftBarrier.SetActive(false);rightBarrier.SetActive(false);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if(other.GetComponent<PlayerMotor>()!=null) BeginEncounter();
        }
        public void BeginEncounter()
        {
            var game=DemoGame.Instance;
            if(State!=BossEncounterState.Idle || game.Paused || !game.HasRun || game.Player.Dead || game.ActiveBoss!=null) return;
            game.ActiveBoss=this;
            State=BossEncounterState.Introduction;DisplayTime=0;
            leftBarrier.SetActive(true);rightBarrier.SetActive(true);
            boss.ResetEnemy();
            boss.transform.position=spawnPoint.position;
            var body=boss.GetComponent<Rigidbody2D>();body.position=spawnPoint.position;body.velocity=Vector2.zero;body.simulated=false;
            boss.enabled=false;
        }
        void Update()
        {
            var game=DemoGame.Instance;
            if(game.ActiveBoss!=this) return;
            if(game.Player.Dead){ResetEncounter();return;}
            if(game.Paused) return;
            DisplayTime+=Time.deltaTime;
            if(State==BossEncounterState.Introduction && DisplayTime>=introDuration)
            {State=BossEncounterState.Fighting;DisplayTime=0;boss.enabled=true;boss.GetComponent<Rigidbody2D>().simulated=true;}
            else if(State==BossEncounterState.Defeated && DisplayTime>=victoryDuration) game.ActiveBoss=null;
        }
        public void Victory()
        {
            State=BossEncounterState.Defeated;DisplayTime=0;
            leftBarrier.SetActive(false);rightBarrier.SetActive(false);
        }
    }
}
