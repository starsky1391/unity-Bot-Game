using System.Collections.Generic;
using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("能力开关")]
        public bool enableDoubleJump, enableWallClimb, enableDash, enableGrapple;
        public float moveSpeed = 7, jumpSpeed = 11, secondJumpSpeed = 10.5f;
        public float dashSpeed = 24, dashDuration = .18f, dashCooldown = .45f;
        public float climbSpeed = 4, wallSlideSpeed = 1.5f, wallJumpLock = .18f;
        public float coyoteWindow = .12f, jumpBufferWindow = .12f;
        public float attackInterval = .3f, attackDuration = .12f;
        public Vector2 attackSize = new Vector2(1.6f, 1.35f);
        [Min(1), Tooltip("新游戏和重生时的生命上限")] public int maxHealth = 6;
        public float invulnerabilityDuration = 1, hurtDuration = .2f;
        public float grappleRange = 11, grappleSpeed = 14, grappleDuration = 1.1f;
        [Range(1, 90)] public float grappleAimAngle = 60;
        public GrapplePoint HighlightedGrapplePoint { get; private set; }
        [Min(1)] public int maxMana = 100;
        [Min(0)] public int skillManaCost = 20, skillDamage = 2;
        public float skillSpeed = 14, skillCooldown = .3f;
        public SkillOrb skillPrefab;
        public int Mana { get; private set; }
        float skillReady;
        public bool Grappling => grappleTarget != null;
        public int Health { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool Grounded { get; private set; }
        public bool Dead => Health <= 0;
        public bool CanDoubleJump => enableDoubleJump && !usedDouble;
        public bool CanAirDash => enableDash && !usedAirDash;
        Rigidbody2D body;
        BoxCollider2D box;
        SpriteRenderer visual, slash;
        float horizontal, vertical, coyoteUntil, jumpUntil = -1;
        float dashUntil, dashReady, attackUntil, attackReady, hurtUntil, invulnerableUntil, wallLockUntil;
        bool usedDouble, usedAirDash, grappleGroundJump;
        int wall;
        GrapplePoint grappleTarget;
        float grappleUntil;
        LineRenderer rope;
        readonly HashSet<EnemyBrain> hit = new HashSet<EnemyBrain>();
        const int Terrain = 1 << 8;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            slash = transform.Find("Slash").GetComponent<SpriteRenderer>();
            Health = maxHealth;
            Mana = maxMana;
            var ropeObject = new GameObject("钩爪绳索");
            ropeObject.transform.SetParent(transform, false);
            rope = ropeObject.AddComponent<LineRenderer>();
            rope.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            rope.startWidth = rope.endWidth = .045f;
            rope.startColor = rope.endColor = new Color(.35f, 1, .9f);
            rope.sortingOrder = 6;
            rope.positionCount = 2;
            rope.enabled = false;
        }

        void Update()
        {
            if (DemoGame.Instance.Paused || Dead) { SetGrapplePreview(null); return; }
            horizontal = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            vertical = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            AimGrapple(new Vector2(horizontal, vertical));
            if (Input.GetKeyDown(KeyCode.U)) TryCast(new Vector2(horizontal, vertical));
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (Grappling) ReleaseGrapple();
                else if (Time.time >= hurtUntil && Time.time >= dashUntil && Time.time >= attackUntil) TryGrapple();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (vertical < 0 && TryDropThrough()) return;
                if (Grappling) ReleaseGrapple();
                jumpUntil = Time.time + jumpBufferWindow;
            }
            if (Input.GetKeyUp(KeyCode.Space) && body.velocity.y > 0 && Time.time >= dashUntil)
                body.velocity = new Vector2(body.velocity.x, body.velocity.y * .45f);
            if (Time.time >= hurtUntil && Time.time >= wallLockUntil && horizontal != 0 && Time.time >= dashUntil)
                Facing = horizontal > 0 ? 1 : -1;
            if (!Grappling && Time.time >= hurtUntil && Time.time >= attackUntil && Time.time >= dashUntil)
            {
                if (Input.GetKeyDown(KeyCode.J) && Time.time >= attackReady)
                {
                    attackUntil = Time.time + attackDuration;
                    attackReady = Time.time + attackInterval;
                    hit.Clear();
                }
                else if (enableDash && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) &&
                         Time.time >= dashReady && (Grounded || !usedAirDash))
                {
                    dashUntil = Time.time + dashDuration;
                    dashReady = Time.time + dashCooldown;
                    if (!Grounded) usedAirDash = true;
                }
            }
            bool attacking = Time.time < attackUntil;
            slash.enabled = attacking;
            slash.transform.localPosition = new Vector3(Facing * .95f, 0, 0);
            visual.color = Time.time < invulnerableUntil && Mathf.Sin(Time.time * 45) > 0
                ? new Color(.6f, .95f, 1, .35f) : new Color(.6f, .95f, 1);
        }

        void FixedUpdate()
        {
            if (Dead || DemoGame.Instance.Paused) return;
            Vector2 p = body.position;
            var ground = Physics2D.BoxCast(p, new Vector2(.65f, .12f), 0, Vector2.down, .72f, Terrain).collider;
            Grounded = body.velocity.y <= .1f && ground != null && !Physics2D.GetIgnoreCollision(box, ground) &&
                (ground.GetComponent<DropPlatform>() == null || box.bounds.min.y >= ground.bounds.max.y - .12f);
            var rightWall = Physics2D.BoxCast(p, new Vector2(.12f, .85f), 0, Vector2.right, .48f, Terrain).collider;
            var leftWall = Physics2D.BoxCast(p, new Vector2(.12f, .85f), 0, Vector2.left, .48f, Terrain).collider;
            wall = rightWall ? 1 : leftWall ? -1 : 0;
            var wallCollider = rightWall ? rightWall : leftWall;
            var surface = wallCollider == null ? null : wallCollider.GetComponent<GroundSurface>();
            bool climbingAllowed = enableWallClimb && (surface == null || surface.canClimb);
            if (!enableDash) dashUntil = 0;
            if (Grappling && (!enableGrapple || !grappleTarget.isActiveAndEnabled || !grappleTarget.isAvailable)) ReleaseGrapple();
            if (Grounded)
            {
                usedDouble = usedAirDash = false;
                grappleGroundJump = Grappling;
                coyoteUntil = Time.time + coyoteWindow;
            }
            body.gravityScale = 3.2f;
            if (Time.time < hurtUntil) return;
            if (Grappling)
            {
                Vector2 delta = (Vector2)grappleTarget.transform.position - p;
                bool obstruction = false;
                foreach (var obstacle in Physics2D.BoxCastAll(p, new Vector2(.7f, 1.3f), 0, delta.normalized,
                    grappleSpeed * Time.fixedDeltaTime + .08f, Terrain))
                    if (delta.y <= 0 || obstacle.collider.GetComponent<DropPlatform>() == null) { obstruction = true; break; }
                if (delta.magnitude < .75f || Time.time >= grappleUntil || obstruction) ReleaseGrapple();
                else
                {
                    body.gravityScale = 0;
                    body.velocity = delta.normalized * grappleSpeed;
                    rope.SetPosition(0, transform.position);
                    rope.SetPosition(1, grappleTarget.transform.position);
                    return;
                }
            }
            if (Time.time < dashUntil)
            {
                if (wall == Facing) dashUntil = Time.time;
                else
                {
                    body.gravityScale = 0;
                    body.velocity = new Vector2(Facing * dashSpeed, 0);
                    return;
                }
            }
            if (Time.time >= wallLockUntil)
                body.velocity = new Vector2(horizontal * moveSpeed, body.velocity.y);
            if (climbingAllowed && !Grounded && wall != 0 && Time.time >= wallLockUntil)
            {
                body.gravityScale = 0;
                body.velocity = new Vector2(body.velocity.x, vertical != 0 ? vertical * climbSpeed : -wallSlideSpeed);
            }
            if (Time.time <= jumpUntil)
            {
                if (climbingAllowed && wall != 0 && !Grounded)
                {
                    Facing = -wall;
                    body.velocity = new Vector2(-wall * moveSpeed * 1.3f, jumpSpeed);
                    wallLockUntil = Time.time + wallJumpLock;
                }
                else if (Grounded || Time.time <= coyoteUntil || grappleGroundJump)
                {
                    body.velocity = new Vector2(body.velocity.x, jumpSpeed);
                    coyoteUntil = -1;
                }
                else if (enableDoubleJump && !usedDouble)
                {
                    body.velocity = new Vector2(body.velocity.x, secondJumpSpeed);
                    usedDouble = true;
                }
                else return;
                grappleGroundJump = false;
                Grounded = false;
                jumpUntil = -1;
            }
            if (Time.time < attackUntil)
            {
                foreach (var collider in Physics2D.OverlapBoxAll(p + Vector2.right * Facing * .95f, attackSize, 0, 1 << 10))
                {
                    var enemy = collider.GetComponent<EnemyBrain>();
                    if (enemy != null && hit.Add(enemy)) enemy.TakeHit(1, p, Facing);
                }
            }
            if (p.y < DemoGame.Instance.World.FallY) KillInstantly();
        }

        public void Damage(int amount, Vector2 source)
        {
            if (Dead || Time.time < invulnerableUntil) return;
            Health = Mathf.Max(0, Health - amount);
            grappleGroundJump = false;
            ReleaseGrapple();
            dashUntil = attackUntil = 0;
            slash.enabled = false;
            jumpUntil = -1;
            body.gravityScale = 3.2f;
            hurtUntil = Time.time + hurtDuration;
            invulnerableUntil = Time.time + invulnerabilityDuration;
            body.velocity = new Vector2(transform.position.x >= source.x ? 7 : -7, 5);
            if (Dead) DemoGame.Instance.BeginRespawn();
        }

        public void ApplyEnvironmentalDamage(int amount) => Damage(amount, (Vector2)transform.position + Vector2.down);
        public void KillInstantly()
        {
            if (Dead) return;
            Health = 0; ClearInput(); ReleaseGrapple(); dashUntil = attackUntil = 0; slash.enabled = false;
            DemoGame.Instance.BeginRespawn();
        }
        public void RestoreMana(int amount) => Mana = Mathf.Min(maxMana, Mana + amount);
        public bool TryCast(Vector2 direction)
        {
            if (DemoGame.Instance.Paused || Dead || Time.time < hurtUntil || Time.time < dashUntil || Time.time < attackUntil || Grappling || Time.time < skillReady || Mana < skillManaCost) return false;
            if (direction.sqrMagnitude == 0) direction = Vector2.right * Facing;
            direction.Normalize();
            var orb = Instantiate(skillPrefab, transform.position, Quaternion.identity);
            orb.damage = skillDamage; orb.velocity = direction * skillSpeed;
            Mana -= skillManaCost; skillReady = Time.time + skillCooldown;
            return true;
        }
        public bool TryDropThrough()
        {
            var floor = Physics2D.BoxCast(body.position, new Vector2(.65f, .12f), 0, Vector2.down, .8f, Terrain).collider;
            var platform = floor == null ? null : floor.GetComponent<DropPlatform>();
            if (platform == null) return false;
            platform.Drop(box); Grounded = false; jumpUntil = coyoteUntil = -1;
            body.velocity = new Vector2(body.velocity.x, -3); return true;
        }
        public void Heal(int amount) => Health = Mathf.Min(maxHealth, Health + amount);

        public void Respawn(Vector2 position, bool restoreHealth = true, int facing = 1)
        {
            transform.position = position;
            ReleaseGrapple();
            body.position = position;
            body.velocity = Vector2.zero;
            body.gravityScale = 3.2f;
            if (restoreHealth) { Health = maxHealth; Mana = maxMana; }
            usedDouble = usedAirDash = false;
            grappleGroundJump = false;
            jumpUntil = coyoteUntil = -1;
            dashUntil = attackUntil = hurtUntil = wallLockUntil = 0;
            dashReady = attackReady = 0;
            Facing = facing;
            invulnerableUntil = Time.time + invulnerabilityDuration;
        }

        public void ClearInput()
        {
            horizontal = vertical = 0;
            jumpUntil = -1;
        }

        public GrapplePoint FindGrappleTarget(Vector2 direction)
        {
            if (!enableGrapple) return null;
            bool hasDirection = direction.sqrMagnitude > 0;
            direction = hasDirection ? direction.normalized : Vector2.right * Facing;
            float minimumDot = Mathf.Cos((hasDirection ? grappleAimAngle : 90) * Mathf.Deg2Rad);
            GrapplePoint best = null;
            float bestDot = -2, bestDistance = float.PositiveInfinity;
            foreach (var point in FindObjectsOfType<GrapplePoint>())
            {
                if (!point.isActiveAndEnabled || !point.isAvailable) continue;
                Vector2 delta = (Vector2)point.transform.position - body.position;
                float distance = delta.magnitude;
                if (distance < .1f || distance > grappleRange) continue;
                float dot = Vector2.Dot(direction, delta / distance);
                if (dot < minimumDot) continue;
                bool blocked = false;
                foreach (var obstacle in Physics2D.LinecastAll(body.position, point.transform.position, Terrain))
                    if (obstacle.collider.GetComponent<DropPlatform>() == null) { blocked = true; break; }
                if (blocked) continue;
                if (dot > bestDot + .01f || (Mathf.Abs(dot - bestDot) <= .01f && distance < bestDistance))
                { best = point; bestDot = dot; bestDistance = distance; }
            }
            return best;
        }
        public void AimGrapple(Vector2 direction) => SetGrapplePreview(Grappling ? grappleTarget : FindGrappleTarget(direction));
        void SetGrapplePreview(GrapplePoint point)
        {
            if (HighlightedGrapplePoint == point) return;
            if (HighlightedGrapplePoint != null) HighlightedGrapplePoint.SetHighlighted(false);
            HighlightedGrapplePoint = point;
            if (point != null) point.SetHighlighted(true);
        }
        void OnDisable() => SetGrapplePreview(null);
        public bool TryGrapple() => TryGrapple(new Vector2(horizontal, vertical));
        public bool TryGrapple(Vector2 direction)
        {
            var target = FindGrappleTarget(direction);
            if (target == null) { SetGrapplePreview(null); DemoGame.Instance.ShowNotice("当前方向没有可连接的钩点"); return false; }
            SetGrapplePreview(target);
            if (Grounded || Time.time <= coyoteUntil) grappleGroundJump = true;
            grappleTarget = target;
            grappleUntil = Time.time + grappleDuration;
            dashUntil = attackUntil = 0;
            jumpUntil = -1;
            slash.enabled = false;
            rope.enabled = true;
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, target.transform.position);
            return true;
        }

        public void ReleaseGrapple()
        {
            grappleTarget = null;
            if (rope != null) rope.enabled = false;
            if (body != null) body.gravityScale = 3.2f;
        }
    }
}
