#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
namespace HollowDemo
{
    public sealed class AbilitySmokeProbe : MonoBehaviour
    {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        IEnumerator Start()
        {
            SaveStore.testPath = System.IO.Path.GetFullPath("Temp/Ability/progress.json");
            var game = DemoGame.Instance;
            while (game.Activation == null) yield return null;
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            game.SetScreen(GameScreen.Pause);
            var player = game.Player;
            Check(!player.enableDoubleJump && !player.enableWallClimb && !player.enableDash && !player.enableGrapple, "new game abilities must be disabled");
            var unlock = System.Linq.Enumerable.First(FindObjectsOfType<AbilityUnlock>(true), u => u.persistentId == "ability_test");
            var hint = AbilityHintCanvas.Instance;hint.displayDuration = 1;
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            game.SetScreen(GameScreen.None);
            unlock.Interact(game);
            Check(game.Screen == GameScreen.None && Time.timeScale == 1, "ability hint does not pause gameplay");
            Check(hint.panel.activeSelf && hint.description.text.Contains("Space") && hint.description.text.Contains("K") && !hint.description.text.Contains("Shift"), "matching keys combined");
            yield return new WaitForSecondsRealtime(1.2f);
            Check(!hint.panel.activeSelf, "hint hides automatically");
            Check(player.enableDoubleJump && player.enableGrapple && !player.enableDash && !player.enableWallClimb, "selected abilities only");
            Check(!unlock.gameObject.activeSelf && game.PickupCollected(unlock.persistentId), "one time collection");
            game.ContinueGame();
            yield return new WaitForSecondsRealtime(.6f);
            game.SetScreen(GameScreen.Pause);
            Check(player.enableDoubleJump && player.enableGrapple && !unlock.gameObject.activeSelf, "continue restores unlock state");
            player.KillInstantly();
            game.SetScreen(GameScreen.None);
            yield return new WaitForSecondsRealtime(1.3f);
            game.SetScreen(GameScreen.Pause);
            Check(!player.Dead && player.enableDoubleJump && player.enableGrapple && !unlock.gameObject.activeSelf, "death preserves unlock state");
            game.NewGame();
            yield return new WaitForSecondsRealtime(.6f);
            game.SetScreen(GameScreen.Pause);
            Check(!player.enableDoubleJump && !player.enableGrapple && unlock.gameObject.activeSelf, "new game resets unlocks");
            Debug.Log("DEMO_SMOKE_OK: ability unlock, save, death and new game");
        }
    }
}
#endif
