#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class BossSmokeProbe:MonoBehaviour
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("BOSS FAILED: "+message);Debug.Log("DEMO_BOSS_CHECK: "+message);}
        IEnumerator Start()
        {
            yield return null;
            var game=DemoGame.Instance;SaveStore.testPath=System.IO.Path.GetFullPath("Temp/BossSmoke/progress.json");
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            var arena=game.BossArenas.Single();var ui=FindObjectOfType<BossCanvas>();var player=game.Player;var body=player.GetComponent<Rigidbody2D>();
            Check(arena.State==BossEncounterState.Idle && !arena.Locked && !arena.boss.GetComponent<Rigidbody2D>().simulated,"boss dormant before entry");
            Check(arena.spawnPoint.IsChildOf(arena.transform) && arena.leftBarrier.transform.IsChildOf(arena.transform) && arena.rightBarrier.transform.IsChildOf(arena.transform),"arena spawn and walls use child references");
            game.SetScreen(GameScreen.Pause);
            var delta=new Vector3(-170,90);game.World.transform.position+=delta;
            var spawn=arena.spawnPoint.position;
            Physics2D.SyncTransforms();
            var checkpoint=game.Checkpoints.Single(p=>p.persistentId=="checkpoint_boss_showcase");game.ActivateCheckpoint(checkpoint);
            arena.introDuration=.6f;
            player.Respawn(arena.transform.TransformPoint(new Vector3(-11,1)));body.simulated=true;
            game.SetScreen(GameScreen.None);Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            Check(arena.Locked && arena.State==BossEncounterState.Introduction && game.ActiveBoss==arena,"physical entry closes both sides and starts intro");
            Check(Vector2.Distance(arena.boss.transform.position,spawn)<.01f,"boss uses translated spawn point");
            Check(ui.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceCamera && ui.introTitle.text==arena.bossName,"editable Canvas presents boss name");
            Check(!arena.leftBarrier.GetComponent<GroundSurface>().canClimb,"air walls cannot be climbed");
            var wall=Physics2D.Raycast(player.transform.position,Vector2.left,8,1<<8);
            Check(wall.collider==arena.leftBarrier.GetComponent<Collider2D>(),"left air wall blocks exit physically");
            float timer=arena.DisplayTime;game.SetScreen(GameScreen.Pause);yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Abs(arena.DisplayTime-timer)<.001f,"pause freezes boss introduction");
            game.SetScreen(GameScreen.None);yield return new WaitForSeconds(.7f);
            Check(arena.State==BossEncounterState.Fighting && arena.boss.enabled && arena.boss.GetComponent<Rigidbody2D>().simulated,"intro enters active boss combat");
            Check(arena.boss.Phase==AttackPhase.Warning,"boss shows a visible attack warning");
            arena.boss.TakeHit(3,player.transform.position,1);yield return null;
            Check(arena.boss.Health==21 && Mathf.Abs(ui.healthFill.fillAmount-21f/24)<.001f,"melee damage updates Canvas health bar");
            yield return new WaitForSeconds(1.1f);
            Check(arena.boss.Phase==AttackPhase.Attack && arena.boss.GetComponent<Rigidbody2D>().velocity.x<0,"boss charge follows warning toward player");
            player.Damage(player.maxHealth,player.transform.position+Vector3.left);
            Check(player.Dead,"player dies during encounter");yield return new WaitForSecondsRealtime(1.3f);
            Check(!player.Dead && !arena.Locked && arena.State==BossEncounterState.Idle,"death opens gates and resets arena for retry");
            Check(Vector2.Distance(player.transform.position,checkpoint.spawnPoint.position)<1,"respawn at pre-arena checkpoint");
            player.Respawn(arena.transform.TransformPoint(new Vector3(-11,1)));Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.7f);
            Check(arena.State==BossEncounterState.Fighting && arena.Locked,"second entry starts a fresh fight");
            arena.boss.TakeHit(100,player.transform.position,1);yield return null;
            Check(arena.State==BossEncounterState.Defeated && !arena.Locked && game.EnemyDefeated(arena.boss.persistentId),"victory unlocks both air walls and records boss defeat");
            game.SaveProgress();Check(SaveStore.Read().defeatedEnemies.Contains(arena.boss.persistentId),"boss defeat written to existing local save");
            game.ReturnToMenu();game.ContinueGame();yield return new WaitForSecondsRealtime(.6f);
            Check(arena.State==BossEncounterState.Defeated && !arena.Locked && game.ActiveBoss==null,"continue does not restart defeated boss");
            arena.BeginEncounter();Check(!arena.Locked,"defeated arena remains passable on re-entry");
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            Check(arena.State==BossEncounterState.Idle && !game.EnemyDefeated(arena.boss.persistentId),"new game restores boss encounter");
            Debug.Log("DEMO_SMOKE_OK: boss arena entry, walls, translated spawn, Canvas, pause, retry and victory persistence.");
        }
    }
}
#endif

