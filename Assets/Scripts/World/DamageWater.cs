using UnityEngine;
namespace HollowDemo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DamageWater:MonoBehaviour
    {
        [Min(1)] public int damagePerTick=1;
        [Min(.05f)] public float tickInterval=1;
        PlayerMotor player;
        float elapsed;
        void OnTriggerEnter2D(Collider2D other)
        {
            var motor=other.GetComponent<PlayerMotor>();
            if(motor!=null){player=motor;elapsed=0;}
        }
        void OnTriggerExit2D(Collider2D other)
        {if(other.GetComponent<PlayerMotor>()==player){player=null;elapsed=0;}}
        void FixedUpdate()
        {
            if(player==null || DemoGame.Instance.Paused) return;
            if(player.Dead){elapsed=0;return;}
            elapsed+=Time.fixedDeltaTime;
            while(elapsed>=tickInterval && !player.Dead)
            {elapsed-=tickInterval;player.ApplyEnvironmentalDamage(damagePerTick);}
        }
    }
}
