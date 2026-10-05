#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
namespace HollowDemo {
 public sealed class GrappleAimSmokeProbe:MonoBehaviour {
 static void Check(bool ok,string msg){if(!ok)throw new Exception("GRAPPLE AIM FAILED: "+msg);Debug.Log("DEMO_GRAPPLE_AIM_CHECK: "+msg);}
 static GrapplePoint Point(Vector2 position){var obj=new GameObject("Aim test",typeof(SpriteRenderer));obj.GetComponent<SpriteRenderer>().color=Color.cyan;obj.transform.position=position;return obj.AddComponent<GrapplePoint>();}
 IEnumerator Start(){yield return null;SaveStore.testPath=System.IO.Path.GetFullPath("Temp/GrappleAim/progress.json");var game=DemoGame.Instance;game.NewGame();yield return new WaitForSecondsRealtime(.6f);var p=game.Player;var body=p.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeAll;p.Respawn(new Vector2(1000,1000));var origin=body.position;
 var up=Point(origin+Vector2.up*6);var down=Point(origin+Vector2.down*3);var left=Point(origin+Vector2.left*6);var right=Point(origin+Vector2.right*6);var nearRight=Point(origin+Vector2.right*3);var off=Point(origin+new Vector2(1,-1));var diagonal=Point(origin+new Vector2(4,4));
 Check(p.FindGrappleTarget(Vector2.up)==up,"W targets above");Check(p.FindGrappleTarget(Vector2.down)==down,"S targets below");Check(p.FindGrappleTarget(Vector2.left)==left,"A targets behind current facing");Check(p.FindGrappleTarget(Vector2.right)==nearRight,"D prioritizes direction over nearby diagonal, then distance");Check(p.FindGrappleTarget(Vector2.one)==diagonal,"W D targets diagonal");
 p.AimGrapple(Vector2.up);Check(up.IsHighlighted&&p.HighlightedGrapplePoint==up&&up.GetComponent<SpriteRenderer>().color==up.highlightColor,"selected target highlighted");p.AimGrapple(Vector2.down);Check(!up.IsHighlighted&&up.GetComponent<SpriteRenderer>().color==Color.cyan&&down.IsHighlighted,"target switching restores old color");Check(p.TryGrapple(Vector2.down)&&p.HighlightedGrapplePoint==down,"K attaches to selected lower target");p.ReleaseGrapple();
 right.isAvailable=nearRight.isAvailable=false;Check(p.FindGrappleTarget(Vector2.zero)==off,"no direction does not exclude lower forward point");right.isAvailable=nearRight.isAvailable=true;
 off.isAvailable=false;var wall=new GameObject("solid",typeof(BoxCollider2D));wall.layer=8;wall.transform.position=origin+Vector2.right*1.5f;wall.GetComponent<BoxCollider2D>().size=new Vector2(.4f,5);Physics2D.SyncTransforms();Check(p.FindGrappleTarget(Vector2.right)==null,"solid wall blocks all right candidates");wall.GetComponent<BoxCollider2D>().enabled=false;Physics2D.SyncTransforms();nearRight.isAvailable=false;Check(p.FindGrappleTarget(Vector2.right)==right,"unavailable point excluded");p.grappleRange=2;Check(p.FindGrappleTarget(Vector2.up)==null,"out of range excluded");p.grappleRange=11;
 p.AimGrapple(Vector2.up);game.SetScreen(GameScreen.Pause);yield return null;Check(!up.IsHighlighted&&p.HighlightedGrapplePoint==null,"pause clears highlight");Debug.Log("DEMO_SMOKE_OK: WASD grapple selection and highlight");}
 }
}
#endif

