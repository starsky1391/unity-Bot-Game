using System.Collections.Generic;
using UnityEngine;

namespace HollowDemo
{
    public enum WaveBossPhase { Waiting, Summoning, Warning, Falling, Core, Finished }
    [RequireComponent(typeof(BossArena))]
    public sealed class WaveBoss : MonoBehaviour
    {
        public EnemyBrain patrolPrefab, shooterPrefab, pursuerPrefab;
        public Transform[] groundSpawns, flyingSpawns;
        public SpriteRenderer brick, warning;
        public Animator bossAnimator;
        static readonly int CoreExposed = Animator.StringToHash("CoreExposed");
        [Min(0)] public float summonInterval = 3;
        [Min(.1f)] public float warningDuration = 1;
        [Min(.1f)] public float fallSpeed = 18, fallHeight = 9;
        [Min(1)] public int brickDamage = 2;
        public Vector2 brickSize = new Vector2(2.5f, 1);
        public WaveBossPhase Phase { get; private set; }
        public int Wave { get; private set; }
        public IReadOnlyList<EnemyBrain> Minions => minions;
        public bool CanHitCore => Phase == WaveBossPhase.Core;
        readonly List<EnemyBrain> minions = new List<EnemyBrain>();
        BossArena arena;
        float timer;
        Vector2 impact;
        int sequence;
        readonly int[] waveOrder = { 0, 1, 2 };
        int orderIndex = 3;
        void Awake() { arena = GetComponent<BossArena>(); ResetFight(); }
        public void ResetFight()
        {
            foreach (var minion in minions) if (minion != null) Destroy(minion.gameObject);
            minions.Clear();
            Phase = WaveBossPhase.Waiting; Wave = 0; timer = 0; orderIndex = 3;
            brick.enabled = warning.enabled = false;
        }
        public void BeginWaves()
        {
            Phase = WaveBossPhase.Waiting; timer = summonInterval;
            arena.boss.GetComponent<Rigidbody2D>().simulated = true;
            arena.boss.GetComponent<Collider2D>().enabled = false;
        }
        void Spawn(EnemyBrain prefab, Transform point)
        {
            var minion = Instantiate(prefab, point.position, Quaternion.identity, transform);
            minion.runtimeSummon = true;
            minion.persistentId = "summon_" + GetInstanceID() + "_" + sequence++;
            minions.Add(minion);
        }
        void Summon()
        {
            minions.Clear(); Wave++;
            if (orderIndex == 3)
            {
                for (int i = 2; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int value = waveOrder[i]; waveOrder[i] = waveOrder[j]; waveOrder[j] = value;
                }
                orderIndex = 0;
            }
            int composition = waveOrder[orderIndex++];
            if (composition == 0) { Spawn(patrolPrefab, groundSpawns[0]); Spawn(patrolPrefab, groundSpawns[1]); Spawn(shooterPrefab, groundSpawns[2]); }
            else if (composition == 1) { Spawn(pursuerPrefab, flyingSpawns[0]); Spawn(pursuerPrefab, flyingSpawns[1]); }
            else { Spawn(patrolPrefab, groundSpawns[0]); Spawn(shooterPrefab, groundSpawns[2]); Spawn(pursuerPrefab, flyingSpawns[0]); }
            Phase = WaveBossPhase.Summoning;
        }
        void Warn()
        {
            var player = DemoGame.Instance.Player;
            var top = (Vector2)player.transform.position + Vector2.up * fallHeight;
            var floor = Physics2D.Raycast(top, Vector2.down, fallHeight + 30, 1 << 8);
            impact = floor.collider != null ? floor.point : (Vector2)player.transform.position;
            warning.transform.position = impact + Vector2.up * .05f;
            warning.transform.localScale = new Vector3(brickSize.x, .12f, 1);
            warning.enabled = true; timer = warningDuration; Phase = WaveBossPhase.Warning;
        }
        void Update()
        {
            var game = DemoGame.Instance;
            if (game.ActiveBoss != arena || arena.State != BossEncounterState.Fighting || game.Paused || game.Player.Dead) return;
            if (Phase == WaveBossPhase.Waiting) { timer -= Time.deltaTime; if (timer <= 0) Summon(); }
            else if (Phase == WaveBossPhase.Summoning)
            {
                if (minions.TrueForAll(m => m == null || m.Health <= 0))
                {
                    foreach (var minion in minions) if (minion != null) Destroy(minion.gameObject);
                    minions.Clear(); Warn();
                }
            }
            else if (Phase == WaveBossPhase.Warning)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    warning.enabled = false; brick.enabled = true;
                    brick.transform.position = impact + Vector2.up * fallHeight;
                    brick.transform.localScale = new Vector3(brickSize.x, brickSize.y, 1);
                    Phase = WaveBossPhase.Falling;
                }
            }
            else if (Phase == WaveBossPhase.Falling)
            {
                Vector2 current = brick.transform.position, target = impact + Vector2.up * (brickSize.y * .5f);
                Vector2 next = Vector2.MoveTowards(current, target, fallSpeed * Time.deltaTime);
                var hit = Physics2D.BoxCast(current, brickSize, 0, Vector2.down, Vector2.Distance(current, next), 1 << 9);
                if (hit.collider != null) game.Player.Damage(brickDamage, current);
                brick.transform.position = next;
                if (Vector2.Distance(next, target) < .01f)
                {
                    Phase = WaveBossPhase.Core;
                    arena.boss.GetComponent<Collider2D>().enabled = true;
                    arena.boss.GetComponent<SpriteRenderer>().color = Color.white;
                }
            }
        }
        void LateUpdate()
        {
            if (bossAnimator != null) bossAnimator.SetBool(CoreExposed, CanHitCore);
        }
        public void CoreStruck()
        {
            arena.boss.GetComponent<Collider2D>().enabled = false;
            brick.enabled = false;
            arena.boss.GetComponent<SpriteRenderer>().color = arena.boss.baseColor;
            Phase = arena.boss.Health <= 0 ? WaveBossPhase.Finished : WaveBossPhase.Waiting;
            timer = summonInterval;
        }
    }
}
