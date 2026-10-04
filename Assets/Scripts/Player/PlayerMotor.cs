using System.Collections.Generic;
using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("能力开关")]
        public bool enableDoubleJump = true, enableWallClimb = true, enableDash = true, enableGrapple = true;
        public float moveSpeed = 7, jumpSpeed = 11, secondJumpSpeed = 10.5f;
        public float dashSpeed = 24, dashDuration = .18f, dashCooldown = .45f;
        public float climbSpeed = 4, wallSlideSpeed = 1.5f, wallJumpLock = .18f;
        public float coyoteWindow = .12f, jumpBufferWindow = .12f;
        public float attackInterval = .3f, attackDuration = .12f;
        public Vector2 attackSize = new Vector2(1.6f, 1.35f);
        public int maxHealth = 6;
        public float invulnerabilityDuration = 1, hurtDuration = .2f;
        public float grappleRange = 11, grappleSpeed = 14, grappleDuration = 1.1f;
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
        bool usedDouble, usedAirDash;
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
            if (DemoGame.Instance.Paused || Dead) return;
            horizontal = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            vertical = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (Grappling) ReleaseGrapple();
                else if (Time.time >= hurtUntil && Time.time >= dashUntil && Time.time >= attackUntil) TryGrapple();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Grappling)
                {
                    ReleaseGrapple();
                    body.velocity = new Vector2(Facing * moveSpeed, jumpSpeed);
                    jumpUntil = -1;
                    coyoteUntil = -1;
                }
                else jumpUntil = Time.time + jumpBufferWindow;
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
            Grounded = body.velocity.y <= .1f && Physics2D.BoxCast(p, new Vector2(.65f, .12f), 0,
                Vector2.down, .72f, Terrain).collider != null;
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
                coyoteUntil = Time.time + coyoteWindow;
            }
            body.gravityScale = 3.2f;
            if (Time.time < hurtUntil) return;
            if (Grappling)
            {
                Vector2 delta = (Vector2)grappleTarget.transform.position - p;
                bool obstruction = Physics2D.BoxCast(p, new Vector2(.7f, 1.3f), 0, delta.normalized,
                    grappleSpeed * Time.fixedDeltaTime + .08f, Terrain).collider != null;
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
                else if (Grounded || Time.time <= coyoteUntil)
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
            if (p.y < DemoGame.Instance.World.FallY) Damage(maxHealth, p + Vector2.down);
        }

        public void Damage(int amount, Vector2 source)
        {
            if (Dead || Time.time < invulnerableUntil) return;
            Health = Mathf.Max(0, Health - amount);
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

        public void Heal(int amount) => Health = Mathf.Min(maxHealth, Health + amount);

        public void Respawn(Vector2 position, bool restoreHealth = true, int facing = 1)
        {
            transform.position = position;
            ReleaseGrapple();
            body.position = position;
            body.velocity = Vector2.zero;
            body.gravityScale = 3.2f;
            if (restoreHealth) Health = maxHealth;
            usedDouble = usedAirDash = false;
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

        public bool TryGrapple()
        {
            if (!enableGrapple) return false;
            GrapplePoint nearest = null;
            float distance = grappleRange;
            foreach (var point in FindObjectsOfType<GrapplePoint>())
            {
                if (!point.isActiveAndEnabled || !point.isAvailable) continue;
                Vector2 delta = (Vector2)point.transform.position - body.position;
                if ((delta.x * Facing <= .1f && !(delta.y > 0 && Mathf.Abs(delta.x) <= 1.5f)) || delta.magnitude > distance) continue;
                if (Physics2D.Linecast(body.position, point.transform.position, Terrain).collider != null) continue;
                nearest = point;
                distance = delta.magnitude;
            }
            if (nearest == null) { DemoGame.Instance.ShowNotice("前方或头顶没有可连接的钩点"); return false; }
            grappleTarget = nearest;
            grappleUntil = Time.time + grappleDuration;
            dashUntil = attackUntil = 0;
            jumpUntil = -1;
            slash.enabled = false;
            rope.enabled = true;
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, nearest.transform.position);
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
