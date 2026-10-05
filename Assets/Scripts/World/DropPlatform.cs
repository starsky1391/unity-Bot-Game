using System.Collections;
using UnityEngine;
namespace HollowDemo {
 [RequireComponent(typeof(BoxCollider2D),typeof(PlatformEffector2D))]
 public sealed class DropPlatform : MonoBehaviour {
  public float dropDuration = .35f;
  public void Drop(Collider2D player){StartCoroutine(Ignore(player));}
  IEnumerator Ignore(Collider2D player){var platform=GetComponent<BoxCollider2D>();Physics2D.IgnoreCollision(player,platform,true);yield return new WaitForSeconds(dropDuration);while(player.bounds.max.y>platform.bounds.max.y && player.bounds.Intersects(platform.bounds))yield return new WaitForFixedUpdate();Physics2D.IgnoreCollision(player,platform,false);}
 }
}
