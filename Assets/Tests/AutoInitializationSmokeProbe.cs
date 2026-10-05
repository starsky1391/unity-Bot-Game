#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace HollowDemo
{
    public sealed class AutoInitializationSmokeProbe : MonoBehaviour
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("AUTO_INIT_FAILED: " + message);
            Debug.Log("DEMO_AUTO_INIT_CHECK: " + message);
        }

        IEnumerator Start()
        {
            yield return null;
            var game = DemoGame.Instance;
            SaveStore.testPath = System.IO.Path.GetFullPath("Temp/AutoInitSave/progress.json");
            var initial = game.Checkpoints.Single(p => p.isInitialSpawn);
            var other = game.Checkpoints.Single(p => !p.isInitialSpawn);
            Check(game.World != null && game.Checkpoints.Length == 2 && game.World.fallBoundary == null,
                "custom map initializes without demo root, start object or death-line object");
            Check(game.BossArenas.Length == 0 && game.Enemies.Length == 0 && game.Npcs.Length == 0,
                "disabled old map enemies and arenas do not enter initialization or reset");
            Check(Camera.main == FindObjectOfType<CameraFollow>().GetComponent<Camera>() &&
                FindObjectsOfType<Camera>().Count(c => c.enabled && c.CompareTag("MainCamera")) == 1,
                "extra default main camera cannot overwrite gameplay rendering");
            Check(game.Checkpoints.All(p => !string.IsNullOrEmpty(p.persistentId)) && initial.persistentId != other.persistentId,
                "standalone checkpoint prefab instances have independent saved IDs");
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun && !game.Transitioning && Vector2.Distance(game.Player.transform.position, initial.spawnPoint.position) < 1,
                "new game spawns at marked checkpoint in translated custom level");
            Check(!initial.activated && Camera.main.transform.position.x > 200 && game.World.outline.Length >= 3,
                "spawn does not activate checkpoint; camera and map initialize");
            var pickup = FindObjectOfType<Pickup>();
            pickup.Interact(game);
            string pickupId = pickup.persistentId;
            int count = game.ItemCount(pickup.item);
            pickup.Interact(game);
            Check(count == game.ItemCount(pickup.item) && !string.IsNullOrEmpty(pickupId), "standalone pickup is collected once");
            yield return new WaitForSeconds(1.1f);
            game.Player.Damage(game.Player.maxHealth, game.Player.transform.position + Vector3.left);
            yield return new WaitForSecondsRealtime(1.3f);
            Check(!game.Player.Dead && Vector2.Distance(game.Player.transform.position, initial.spawnPoint.position) < 1,
                "death before activation returns to initial checkpoint");
            game.ActivateCheckpoint(other);
            game.ReturnToMenu();
            game.ContinueGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(Vector2.Distance(game.Player.transform.position, other.spawnPoint.position) < 1 && game.PickupCollected(pickupId),
                "continue uses activated checkpoint and keeps collected items");
            other.isInitialSpawn = true;
            game.NewGame();
            Check(game.Screen == GameScreen.MainMenu && !game.Transitioning && game.Fade == 0,
                "duplicate initial checkpoint blocks start without black screen");
            initial.isInitialSpawn = other.isInitialSpawn = false;
            game.NewGame();
            Check(game.Screen == GameScreen.MainMenu && game.Notice.Contains("初始出生点"), "missing initial checkpoint gives actionable message");
            initial.isInitialSpawn = true;
            var original = initial.spawnPoint.localPosition;
            initial.spawnPoint.localPosition += Vector3.down * 100;
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(!game.HasRun && !game.Transitioning && game.Screen == GameScreen.MainMenu,
                "unsafe spawn blocks void death loop");
            initial.spawnPoint.localPosition = original;
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.HasRun && !game.Player.Dead, "corrected spawn can start again");
            Debug.Log("DEMO_AUTO_INIT_OK: custom map, marked spawn, independent IDs, death, continue and configuration errors passed.");
        }
    }
}
#endif
