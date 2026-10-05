using UnityEngine;
namespace HollowDemo {
 [RequireComponent(typeof(BoxCollider2D))]
 public sealed class InstantDeathZone : MonoBehaviour {
  void OnTriggerEnter2D(Collider2D other){var player=other.GetComponent<PlayerMotor>();if(player!=null&&!DemoGame.Instance.Paused)player.KillInstantly();}
 }
}
