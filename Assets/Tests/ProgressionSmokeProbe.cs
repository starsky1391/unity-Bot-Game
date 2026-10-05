#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace HollowDemo{
public sealed class ProgressionSmokeProbe:MonoBehaviour{
static void Check(bool ok,string name){if(!ok)throw new Exception("PROGRESSION FAILED: "+name);Debug.Log("DEMO_PROGRESSION_CHECK: "+name);}
IEnumerator Start(){yield return null;SaveStore.testPath=System.IO.Path.GetFullPath("Temp/ProgressionSmoke/progress.json");var game=DemoGame.Instance;game.NewGame();yield return new WaitForSecondsRealtime(1.4f);var player=game.Player;game.SetScreen(GameScreen.Pause);
Check(game.FlaskCapacity==3&&game.FlaskCharges==3&&game.inventory.Count(game.flaskItem)==1,"new game three reusable charges");Check(!game.UseFlask()&&game.FlaskCharges==3,"full health preserves charge");player.ApplyEnvironmentalDamage(3);int hp=player.Health;Check(game.UseFlask()&&player.Health==hp+2&&game.FlaskCharges==2,"flask heals two without consuming owned item");
var empty=Resources.Load<ItemDefinition>("Items/empty_flask");game.TryCollectItem(empty,1);Check(game.FlaskCapacity==3,"container waits for checkpoint");game.ActivateCheckpoint(game.Checkpoints[0]);Check(game.FlaskCapacity==4&&game.FlaskCharges==4&&game.inventory.Count(empty)==0,"checkpoint converts container and refills");
var enemy=game.Enemies.First(e=>e.kind!=EnemyKind.Shield);int crystals=game.Crystals;enemy.TakeHit(999,player.transform.position,1);Check(game.Crystals==crystals+enemy.crystalDrops.amount,"kill grants configured crystals");game.RecordEnemyDeath(enemy.persistentId,99);Check(game.Crystals==crystals+enemy.crystalDrops.amount,"same death grants reward once");
var pickup=game.World.GetComponentsInChildren<Pickup>(true).First(p=>p.item==empty);pickup.Interact(game);int count=game.inventory.Count(empty);pickup.Interact(game);Check(game.inventory.Count(empty)==count&&game.PickupCollected(pickup.persistentId),"container pickup recorded once");game.SaveProgress();game.ContinueGame();yield return new WaitForSecondsRealtime(1.4f);game.SetScreen(GameScreen.Pause);Check(game.FlaskCapacity==4&&game.Crystals==crystals+enemy.crystalDrops.amount&&game.inventory.Count(empty)==count,"save restores capacity wallet and pending container");
var hud=FindObjectOfType<PlayerHudCanvas>();Check(hud.crystalPanel!=null&&hud.crystalCount!=null,"editable crystal Canvas references");
var waterPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/DamageWater.prefab");var water=Instantiate(waterPrefab,player.transform.position,Quaternion.identity);water.transform.localScale=new Vector3(10,10,1);var damage=water.GetComponent<DamageWater>();damage.tickInterval=.12f;player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;Physics2D.SyncTransforms();game.SetScreen(GameScreen.None);yield return new WaitForSeconds(.08f);hp=player.Health;game.SetScreen(GameScreen.Pause);yield return new WaitForSecondsRealtime(.2f);Check(player.Health==hp,"paused water does not tick");game.SetScreen(GameScreen.None);yield return new WaitForSeconds(.3f);Check(player.Health<hp,"water physically ticks damage");Destroy(water);game.SetScreen(GameScreen.Pause);Debug.Log("DEMO_SMOKE_OK: progression");}
}}
#endif

