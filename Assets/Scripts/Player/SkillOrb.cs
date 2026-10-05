using UnityEngine;
namespace HollowDemo {
 public sealed class SkillOrb : MonoBehaviour {
  public int damage = 2;
  public Vector2 velocity;
  public float lifetime = 4, radius = .2f;
  float expires;
  void Start(){expires=Time.time+lifetime;}
  void FixedUpdate(){
   if(DemoGame.Instance.Paused) return;
   if(Time.time>=expires){Destroy(gameObject);return;}
   var hit=Physics2D.CircleCast(transform.position,radius,velocity.normalized,velocity.magnitude*Time.fixedDeltaTime,(1<<8)|(1<<10));
   if(hit.collider!=null){var enemy=hit.collider.GetComponent<EnemyBrain>();if(enemy!=null)enemy.TakeHit(damage,transform.position,velocity.x>=0?1:-1);Destroy(gameObject);}
   else transform.position+=(Vector3)velocity*Time.fixedDeltaTime;
  }
 }
}
