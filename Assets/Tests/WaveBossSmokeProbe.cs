#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class WaveBossSmokeProbe : MonoBehaviour
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception("WAVE BOSS: " + message); Debug.Log("DEMO_WAVE_CHECK: " + message); }
        IEnumerator Start()
        {
            SaveStore.testPath=System.IO.Path.GetFullPath("Temp/WaveBoss/progress.json");
            var game=DemoGame.Instance;
            while(game.Activation==null) yield return null;
            var waves=FindObjectOfType<WaveBoss>(true);var arena=waves.GetComponent<BossArena>();
            game.Player.maxHealth=99;game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            game.Activation.ActivateAt(arena.transform.position);
            game.Player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            arena.introDuration=.01f;waves.summonInterval=.03f;waves.fallSpeed=200;waves.fallHeight=3;
            arena.BeginEncounter();yield return new WaitForSeconds(.12f);
            Check(arena.Locked,"barriers lock");
            Check(waves.bossAnimator!=null && !waves.bossAnimator.GetBool("CoreExposed"),"idle animator parameter");
            var compositions=new System.Collections.Generic.HashSet<int>();
            int health=arena.boss.Health;
            for(int wave=1;wave<=6;wave++)
            {
                Check(waves.Wave==wave && waves.Phase==WaveBossPhase.Summoning,"wave "+wave+" summoned");
                var kinds=waves.Minions.Select(m=>m.kind).ToArray();
                int composition=kinds.Count(k=>k==EnemyKind.Patrol)==2?0:kinds.Length==2 && kinds.All(k=>k==EnemyKind.Pursuer)?1:kinds.Length==3 && kinds.Distinct().Count()==3?2:-1;
                Check(composition>=0,"original composition retained");
                Check(compositions.Add(composition),"no repeats within shuffled cycle");
                if(wave%3==0){Check(compositions.Count==3,"all combinations per cycle");compositions.Clear();}
                Check(!arena.boss.TakeHit(999,Vector2.zero,1),"protected core");
                foreach(var enemy in waves.Minions)enemy.TakeHit(999,enemy.transform.position,1);
                yield return new WaitForSeconds(.1f);
                Check(waves.Phase==WaveBossPhase.Warning && waves.warning.enabled && !waves.brick.enabled,"warning before falling");
                yield return new WaitForSeconds(.5f);
                Check(waves.Phase==WaveBossPhase.Warning,"one second warning retained");
                yield return new WaitForSeconds(.65f);
                Check(waves.Phase==WaveBossPhase.Core && waves.brick.enabled,"core exposed after impact");
                Check(waves.bossAnimator.GetBool("CoreExposed") && waves.bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("核心暴露"),"exposed animator state");
                int damage=wave==6?health:2;
                Check(arena.boss.TakeHit(damage,Vector2.zero,1),"core takes hit");
                health-=damage;
                Check(arena.boss.Health==health,"actual attack damage applied");
                if(wave<6)Check(arena.State==BossEncounterState.Fighting,"continues beyond three exposures");
                yield return new WaitForSeconds(.12f);
                if(wave<6)Check(!waves.bossAnimator.GetBool("CoreExposed") && waves.bossAnimator.GetCurrentAnimatorStateInfo(0).IsName("待机"),"returns to idle animator state");
            }
            Check(arena.State==BossEncounterState.Defeated && !arena.Locked,"empty health wins and unlocks barriers");
            Check(!SaveStore.Read().defeatedEnemies.Any(id=>id.StartsWith("summon_")),"summons not stored as permanent enemies");
            game.ContinueGame();yield return new WaitForSecondsRealtime(.6f);
            Check(arena.State==BossEncounterState.Defeated,"victory persists on continue");
            game.NewGame();yield return new WaitForSecondsRealtime(.6f);
            Check(arena.State==BossEncounterState.Idle && waves.Wave==0 && !waves.brick.enabled,"new game resets encounter");
            Debug.Log("DEMO_SMOKE_OK: random looping waves and health based victory");
        }
    }
}
#endif
