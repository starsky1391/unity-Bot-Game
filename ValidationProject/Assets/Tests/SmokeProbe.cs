#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class SmokeProbe:MonoBehaviour
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);Debug.Log("DEMO_CHECK: "+message);}
        IEnumerator Start()
        {
            yield return null;
            var game=DemoGame.Instance;
            SaveStore.testPath=System.IO.Path.GetFullPath("Temp/SmokeSave/progress.json");
            var world=game.World;
            var names=world.GetComponentsInChildren<WorldLabel>(true);
            Check(game.Npcs.Length>=2 && game.Checkpoints.Length>=5 && names.Length==game.Npcs.Length+game.Checkpoints.Length && names.All(n=>n.label!=null && (n.transform.parent.GetComponent<Npc>()!=null || n.transform.parent.GetComponent<Checkpoint>()!=null)),"only NPC and checkpoint Canvas names remain");
            var npc=game.Npcs[0];var name=npc.GetComponentInChildren<WorldLabel>();
            var namePosition=name.transform.position;var move=new Vector3(7,5);
            npc.transform.position+=move;
            Check(Vector3.Distance(name.transform.position,namePosition+move)<.001f,"object name follows independent NPC movement");
            npc.transform.position-=move;
            Check(world!=null && UnityEngine.SceneManagement.SceneManager.sceneCount==1,"single map scene");
            game.RequestNewGame();yield return null;Check(game.Screen==GameScreen.ConfirmNew,"confirmation before new game");
            game.SetScreen(GameScreen.MainMenu);
            var delta=new Vector3(180,-140);
            world.transform.position+=delta;
            game.transform.position+=new Vector3(-400,300);
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun&&!game.Transitioning&&!game.Player.Dead,"new game after moving map");
            Check(Vector2.Distance(game.Player.transform.position,world.startPoint.position)<1,"spawn follows map anchor");
            var expected=world.MapPosition(game.Player.transform.position);
            world.transform.position-=delta;game.Player.transform.position-=delta;
            Check(Vector2.Distance(expected,world.MapPosition(game.Player.transform.position))<.001f,"map marker invariant under translation");
            var camera=Camera.main;game.Player.transform.position+=Vector3.up*30;
            FindObjectOfType<CameraFollow>().Snap();
            Check(camera.transform.position.y>game.Player.transform.position.y,"camera follows unrestricted height");
            var point=game.Checkpoints.First(p=>game.Enemies.Any(e=>e.roomId==p.roomId && e.kind==EnemyKind.Patrol));
            game.ActivateCheckpoint(point);
            var pickup=world.GetComponentInChildren<Pickup>();pickup.Interact(game);
            string pickupId=pickup.persistentId;
            var enemy=game.Enemies.First(e=>e.roomId==point.roomId && e.kind==EnemyKind.Patrol);
            var home=enemy.Home;var translation=new Vector3(12,5);
            world.transform.position+=translation;
            Check(Vector2.Distance(enemy.Home,home+(Vector2)translation)<.001f,"enemy reset follows moved map");
            enemy.TakeHit(100,enemy.transform.position,1);
            game.MapFog.Reveal(0,new Rect(10,2,2,2));
            game.SaveProgress();game.ReturnToMenu();
            world.transform.position+=new Vector3(-350,220);
            game.ContinueGame();yield return new WaitForSecondsRealtime(.6f);
            Check(Vector2.Distance(game.Player.transform.position,point.spawnPoint.position)<1,"continue uses moved checkpoint");
            Check(game.PickupCollected(pickupId)&&!pickup.gameObject.activeSelf,"pickup stays collected after reload");
            Check(!enemy.gameObject.activeSelf,"defeated enemy stays dead after continue");
            Check(game.MapFog.Visible(0,new Vector2(11,3)),"camera fog persists on continue");
            Check(!game.MapFog.Visible(0,new Vector2(1000,1000)),"unexplored map stays hidden");
            yield return new WaitForSeconds(1);
            game.Player.Damage(game.Player.maxHealth,game.Player.transform.position+Vector3.left);
            Check(game.Player.Dead,"lethal damage starts death flow");
            yield return new WaitForSecondsRealtime(1.2f);
            Check(!game.Player.Dead && Vector2.Distance(game.Player.transform.position,point.spawnPoint.position)<1,"death respawns at moved checkpoint");
            Check(enemy.gameObject.activeSelf,"death resets checkpoint challenge enemies");
            Check(game.PickupCollected(pickupId)&&game.MapFog.Visible(0,new Vector2(11,3)),"death preserves pickup and fog");
            var old=SaveStore.Read(); old.version=1;
            var region=world.legacyFogRegions[0];
            int cellX=Mathf.FloorToInt(region.oldBounds.center.x/MapFog.CellSize),cellY=Mathf.FloorToInt(region.oldBounds.center.y/MapFog.CellSize);
            old.exploredMapCells=new[]{new SaveData.MapCell{room=region.id,x=cellX,y=cellY}};
            SaveStore.Write(old);game.ReturnToMenu();SaveStore.Write(old);game.ContinueGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun && game.MapFog.Save().Any(c=>c.room==0),"legacy save fog migrates to global map");
            Check(game.PickupCollected(pickupId),"legacy save preserves collected IDs");
            var original=world.startPoint.localPosition;world.startPoint.localPosition+=Vector3.down*100;
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            Check(!game.HasRun&&game.Screen==GameScreen.MainMenu,"invalid spawn blocks void death loop");
            world.startPoint.localPosition=original;
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun&&!game.Player.Dead,"valid new game recovers");
            game.SaveProgress();
            point.isInitialSpawn=true;
            DestroyImmediate(world);
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun && !game.Transitioning && game.Fade==0 && game.World!=null,"missing map manager automatically initializes from checkpoint");
            game.ContinueGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun && game.Fade==0,"auto-initialized map supports continue");
            Debug.Log("DEMO_SMOKE_OK: global map movement, spawn, camera, save and one-time pickup.");
        }
    }
}
#endif







