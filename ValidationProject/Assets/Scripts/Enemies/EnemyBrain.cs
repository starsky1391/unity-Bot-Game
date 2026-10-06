using UnityEngine;

namespace HollowDemo
{
    public enum EnemyKind { Patrol, Chaser, Charger, Hopper, Shooter, Shield, Hammer, Burrower, Pursuer, Diver }
    public enum AttackPhase { Idle, Warning, Attack, Recovery }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public EnemyKind kind;
        public int roomId, maxHealth = 3;
        public float moveSpeed = 2, detectionRange = 8, warningTime = .7f, attackTime = .4f, recoveryTime = 1.1f;
        public Color baseColor = Color.red;
        public string persistentId;
        public CrystalDropSettings crystalDrops;
        public bool overrideCrystalDrop;
        [Min(0)] public int crystalDropAmount = 3;
        public AttackPhase Phase { get; private set; }
        public int Health { get; private set; }
        Vector2 homeLocal;
        public Vector2 Home => transform.parent == null ? homeLocal : (Vector2)transform.parent.TransformPoint(homeLocal);
        Rigidbody2D body;
        BoxCollider2D box;
        SpriteRenderer visual, marker, shield;
        Vector2 target, attackDirection;
        float nextPhase, staggerUntil, idleUntil;
        float regionPausedAt = -1;
        int facing = -1;
        bool fired;
        bool Flying => kind == EnemyKind.Pursuer || kind == EnemyKind.Diver;
        const int Terrain = 1 << 8;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            marker = transform.Find("Warning").GetComponent<SpriteRenderer>();
            shield = transform.Find("Shield").GetComponent<SpriteRenderer>();
            homeLocal = transform.localPosition;
            ResetEnemy();
        }

        public void PauseForRegion() { if (Health > 0) regionPausedAt = Time.time; }
        public void ResumeForRegion()
        {
            if (regionPausedAt < 0) return;
            float elapsed = Time.time - regionPausedAt;
            nextPhase += elapsed; staggerUntil += elapsed; idleUntil += elapsed;
            regionPausedAt = -1;
        }
        public void ResetEnemy()
        {
            gameObject.SetActive(true);
            regionPausedAt = -1;
            transform.position = Home;
            body.velocity = Vector2.zero;
            body.gravityScale = Flying ? 0 : 3.2f;
            Health = maxHealth;
            Phase = AttackPhase.Idle;
            idleUntil = Time.time + .6f;
            staggerUntil = 0;
            facing = -1;
            box.enabled = true;
            visual.enabled = true;
            visual.color = baseColor;
            marker.enabled = false;
            shield.enabled = kind == EnemyKind.Shield;
        }

        void FixedUpdate()
        {
            if (DemoGame.Instance.Paused) return;
            var player = DemoGame.Instance.Player;
            if (player == null || player.Dead) { body.velocity = Vector2.zero; return; }
            Vector2 delta = (Vector2)player.transform.position - body.position;
            if (body.position.y < DemoGame.Instance.World.FallY)
            {
                Health = 0;
                DemoGame.Instance.RecordEnemyDeath(persistentId);
                gameObject.SetActive(false);
                return;
            }
            if (Time.time < staggerUntil) return;
            if (delta.magnitude > 20)
            {
                body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y);
                return;
            }
            if (Phase != AttackPhase.Idle && Time.time >= nextPhase) AdvancePhase();
            visual.color = Phase == AttackPhase.Warning ? Color.yellow : Phase == AttackPhase.Attack
                ? new Color(1, .2f, .25f) : Phase == AttackPhase.Recovery ? baseColor * .55f : baseColor;
            shield.transform.localPosition = new Vector3(facing * .55f, 0, 0);
            marker.enabled = Phase == AttackPhase.Warning;
            if (Phase == AttackPhase.Warning)
            {
                body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y);
                return;
            }
            if (Phase == AttackPhase.Recovery)
            {
                body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y);
                if (kind == EnemyKind.Diver)
                {
                    body.MovePosition(Vector2.MoveTowards(body.position, Home, 5 * Time.fixedDeltaTime));
                    if (Vector2.Distance(body.position, Home) > .5f) nextPhase = Time.time + .1f;
                }
            }
            else if (Phase == AttackPhase.Attack) Attack(player);
            else Idle(delta);
            if (kind != EnemyKind.Burrower || Phase == AttackPhase.Attack)
                if (box.bounds.Intersects(player.GetComponent<Collider2D>().bounds)) player.Damage(1, body.position);
        }

        void Idle(Vector2 delta)
        {
            bool grounded = Physics2D.Raycast(body.position, Vector2.down, box.bounds.extents.y + .2f, Terrain);
            if (kind == EnemyKind.Patrol || kind == EnemyKind.Shield)
            {
                Walk(facing, true);
                return;
            }
            if (kind == EnemyKind.Chaser)
            {
                if (delta.magnitude < detectionRange) facing = delta.x > 0 ? 1 : -1;
                Walk(facing, false);
                return;
            }
            if (kind == EnemyKind.Burrower)
            {
                visual.enabled = false;
                box.enabled = false;
                body.gravityScale = 0;
                body.velocity = Vector2.zero;
            }
            if (Flying)
            {
                Vector2 destination = kind == EnemyKind.Diver
                    ? Home + Vector2.right * Mathf.Sin(Time.time) * 2
                    : (Vector2)DemoGame.Instance.Player.transform.position + Vector2.up * 1.2f;
                body.MovePosition(Vector2.MoveTowards(body.position, destination, moveSpeed * Time.fixedDeltaTime));
            }
            else body.velocity = new Vector2(0, body.velocity.y);
            float range = kind == EnemyKind.Hammer ? 3 : kind == EnemyKind.Pursuer ? 4 : detectionRange;
            if (Time.time >= idleUntil && delta.magnitude < range && (Flying || grounded))
            {
                facing = delta.x >= 0 ? 1 : -1;
                target = DemoGame.Instance.Player.transform.position;
                attackDirection = (target - body.position).normalized;
                if (kind == EnemyKind.Burrower)
                {
                    target.x = Mathf.Clamp(target.x, Home.x - 5, Home.x + 5);
                    target.y = Home.y;
                    body.position = target;
                }
                if (kind == EnemyKind.Burrower || kind == EnemyKind.Diver)
                {
                    var floor = Physics2D.Raycast(target, Vector2.down, 100, Terrain);
                    marker.transform.position = new Vector3(target.x, floor.collider != null ? floor.point.y + .16f : target.y, 0);
                }
                else marker.transform.position = (Vector3)body.position + new Vector3(facing * 1.15f, .1f, 0);
                marker.transform.localScale = new Vector3(kind == EnemyKind.Hammer ? 2.7f : 1.4f, .15f, 1);
                marker.color = Color.yellow;
                Phase = AttackPhase.Warning;
                nextPhase = Time.time + warningTime;
                fired = false;
            }
        }

        void Walk(int direction, bool reverseAtEdge)
        {
            bool wall = Physics2D.Raycast(body.position, Vector2.right * direction, .8f, Terrain);
            bool floor = Physics2D.Raycast(body.position + Vector2.right * direction * .75f, Vector2.down, 1.3f, Terrain);
            if (wall || !floor)
            {
                body.velocity = new Vector2(0, body.velocity.y);
                if (reverseAtEdge) facing = -direction;
            }
            else body.velocity = new Vector2(direction * moveSpeed, body.velocity.y);
        }

        void AdvancePhase()
        {
            if (Phase == AttackPhase.Warning)
            {
                Phase = AttackPhase.Attack;
                nextPhase = Time.time + attackTime;
            }
            else if (Phase == AttackPhase.Attack)
            {
                Phase = AttackPhase.Recovery;
                nextPhase = Time.time + recoveryTime;
            }
            else
            {
                Phase = AttackPhase.Idle;
                idleUntil = Time.time + .6f;
            }
        }

        void Attack(PlayerMotor player)
        {
            switch (kind)
            {
                case EnemyKind.Charger:
                    body.velocity = new Vector2(facing * 11, body.velocity.y);
                    if (Physics2D.Raycast(body.position, Vector2.right * facing, .85f, Terrain) ||
                        !Physics2D.Raycast(body.position + Vector2.right * facing * .85f, Vector2.down, 1.4f, Terrain))
                        nextPhase = Time.time;
                    break;
                case EnemyKind.Hopper:
                    if (!fired) { body.velocity = new Vector2(Mathf.Clamp(target.x - body.position.x, -5, 5), 9); fired = true; }
                    break;
                case EnemyKind.Shooter:
                    if (!fired) { DemoGame.Instance.SpawnProjectile(body.position + Vector2.right * facing * .8f, Vector2.right * facing * 9, roomId); fired = true; }
                    break;
                case EnemyKind.Hammer:
                    Vector2 center = body.position + Vector2.right * facing * 1.5f;
                    if (Physics2D.OverlapBox(center, new Vector2(2.7f, 1.8f), 0, 1 << 9)) player.Damage(2, body.position);
                    marker.enabled = true;
                    marker.color = new Color(1, .2f, .25f, .8f);
                    break;
                case EnemyKind.Burrower:
                    box.enabled = visual.enabled = true;
                    body.gravityScale = 3.2f;
                    break;
                case EnemyKind.Pursuer:
                case EnemyKind.Diver:
                    body.velocity = attackDirection * (kind == EnemyKind.Diver ? 13 : 8);
                    if (Physics2D.Raycast(body.position, attackDirection, .8f, Terrain)) nextPhase = Time.time;
                    break;
            }
        }

        public bool TakeHit(int damage, Vector2 source, int direction)
        {
            if (Health <= 0 || !box.enabled) return false;
            if (kind == EnemyKind.Shield && Mathf.Sign(source.x - body.position.x) == facing)
            {
                DemoGame.Instance.ShowNotice("盾牌挡住了攻击，尝试绕到背后");
                return false;
            }
            Health -= damage;
            if (Health <= 0)
            {
                DemoGame.Instance.RecordEnemyDeath(persistentId, overrideCrystalDrop || crystalDrops == null ? crystalDropAmount : crystalDrops.amount);
                gameObject.SetActive(false);
                return true;
            }
            staggerUntil = Time.time + .16f;
            body.velocity = new Vector2(direction * 5, Flying ? 0 : 3);
            visual.color = Color.white;
            return true;
        }
    }
}


