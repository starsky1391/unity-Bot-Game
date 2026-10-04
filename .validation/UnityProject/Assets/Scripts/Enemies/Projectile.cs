using UnityEngine;

namespace HollowDemo
{
    public sealed class Projectile : MonoBehaviour
    {
        public int roomId;
        public Vector2 velocity;
        float expires;
        void Start() => expires = Time.time + 4;
        void FixedUpdate()
        {
            if (Time.time >= expires) { Destroy(gameObject); return; }
            var hit = Physics2D.CircleCast(transform.position, .16f, velocity.normalized,
                velocity.magnitude * Time.fixedDeltaTime, (1 << 8) | (1 << 9));
            if (hit.collider != null)
            {
                var player = hit.collider.GetComponent<PlayerMotor>();
                if (player != null) player.Damage(1, transform.position);
                Destroy(gameObject);
            }
            else transform.position += (Vector3)velocity * Time.fixedDeltaTime;
        }
    }

}
