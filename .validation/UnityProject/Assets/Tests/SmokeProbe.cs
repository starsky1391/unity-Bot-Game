#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowDemo
{
    public sealed class SmokeProbe : MonoBehaviour
    {
        PlayerMotor player;
        Rigidbody2D body;
        DemoGame game;
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception("SMOKE FAILED: " + message);
            Debug.Log("SMOKE PASS: " + message);
        }

        IEnumerator Start()
        {
            yield return null;
            game = DemoGame.Instance;
            SaveStore.testPath = System.IO.Path.GetFullPath("Temp/SmokeSave/progress.json");
            Check(game.Screen == GameScreen.MainMenu && Time.timeScale == 0, "startup opens paused main menu");
            var menu = FindObjectOfType<MainMenuCanvas>();
            Check(menu != null && menu.menuPanel.activeInHierarchy && menu.GetComponent<Canvas>().enabled, "startup shows serialized Canvas menu");
            Check(menu.newGameButton.onClick.GetPersistentEventCount() == 1 && menu.continueButton.onClick.GetPersistentEventCount() == 1 && menu.settingsButton.onClick.GetPersistentEventCount() == 1 && menu.quitButton.onClick.GetPersistentEventCount() == 1, "all standard menu buttons have persistent actions");
            menu.settingsButton.onClick.Invoke();
            yield return null;
            Check(game.Screen == GameScreen.Settings && !menu.menuPanel.activeSelf && game.Paused, "Canvas settings button opens existing settings screen");
            game.CloseSettings();
            yield return null;
            Check(game.Screen == GameScreen.MainMenu && menu.menuPanel.activeSelf, "return from settings restores Canvas menu");
            menu.newGameButton.onClick.Invoke();
            if (game.Screen == GameScreen.ConfirmNew) game.NewGame();
            yield return new WaitUntil(() => !game.Transitioning);
            yield return null;
            Check(!menu.menuPanel.activeSelf, "starting game hides Canvas menu");
            Check(SceneManager.sceneCount == 2 && game.LoadedRoom == 0, "only shell and current room are loaded");
            Check(MapGeometry.VisibleRooms(game.Rooms).Length == 1 && !game.Rooms[1].explored, "unvisited rooms are absent from map");
            Check(!game.LandmarkDiscovered("merchant_arli"), "merchant is hidden before discovery");
            var fog = new MapFog();
            fog.Reveal(0, new Rect(0, 0, 2, 2));
            Check(fog.Visible(0, new Vector2(1, 1)) && !fog.Visible(0, new Vector2(8, 1)) && !fog.Visible(1, new Vector2(1, 1)),
                "fog reveals the camera rectangle only in the current room");
            Check(fog.Visible(0, new Vector2(.25f, .25f)) && fog.Visible(0, new Vector2(1.75f, 1.75f)),
                "camera rectangle corners are revealed instead of a circle");
            var revealedWalls = fog.VisibleSegments(0, new Vector2(0, 1), new Vector2(10, 1)).ToArray();
            Check(revealedWalls.Length > 0 && revealedWalls.All(w => w[1].x <= 2.5f && fog.Visible(0, (w[0] + w[1]) / 2)),
                "partially explored room walls stop at the fog boundary");
            var restoredFog = new MapFog();
            restoredFog.Restore(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(new SaveData { exploredMapCells = fog.Save() })).exploredMapCells);
            Check(restoredFog.Visible(0, new Vector2(1, 1)) && !restoredFog.Visible(0, new Vector2(8, 1)), "fog cell data survives JSON save round trip");
            restoredFog.Restore(null);
            Check(!restoredFog.Visible(0, Vector2.one), "legacy saves do not reveal entire visited rooms");
            Rect knownMapBounds = MapGeometry.KnownBounds(game.Rooms);
            Vector2 hiddenOrigin = game.Rooms[1].map.origin;
            game.Rooms[1].map.origin = new Vector2(10000, 10000);
            Check(MapGeometry.KnownBounds(game.Rooms) == knownMapBounds, "unknown room geometry cannot affect visible map framing");
            var mapViewport = new MapViewport();
            Vector2 mapPlayer = game.ActiveRoom.MapPosition(game.Rooms[0].map, game.Player.transform.position);
            mapViewport.Open(mapPlayer);
            var mapRect = new Rect(0, 0, 910, 305);
            Check(mapViewport.Project(mapPlayer, mapRect) == mapRect.center && mapViewport.Scale == MapViewport.DefaultScale, "map opens centered at a fixed readable scale");
            mapViewport.Pan(new Vector2(64, 32));
            Check(Vector2.Distance(mapViewport.Project(mapPlayer, mapRect), mapRect.center + new Vector2(64, 32)) < .01f, "map drag pans in screen coordinates");
            Vector2 zoomAnchor = mapPlayer + Vector2.right;
            Vector2 anchorPixel = mapViewport.Project(zoomAnchor, mapRect);
            mapViewport.Zoom(-3, anchorPixel, mapRect);
            Check(Vector2.Distance(mapViewport.Project(zoomAnchor, mapRect), anchorPixel) < .01f, "scroll zoom keeps pointer anchor fixed");
            float zoomScale = mapViewport.Scale;
            mapViewport.Recenter(mapPlayer);
            Check(mapViewport.Project(mapPlayer, mapRect) == mapRect.center && mapViewport.Scale == zoomScale, "return to player preserves selected zoom");
            game.Rooms[1].map.origin = hiddenOrigin;
            Check(game.Rooms[0].map.exits.Length == 1 && game.Rooms[0].map.exits[0].targetRoom == 1,
                "known room contains only its authored exit stub toward unknown area");
            Vector2 branchMarker = game.Rooms[0].map.PlayerPosition(new Vector2(34, 6.8f), game.Rooms[0].bounds) - game.Rooms[0].map.origin;
            Check(branchMarker.y >= 6 && branchMarker.y <= 8, "player marker remains inside upper branch instead of dark gap");
            player = game.Player;
            body = player.GetComponent<Rigidbody2D>();
            player.enabled = false;
            var enemies = game.Enemies;
            foreach (var enemy in enemies) enemy.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            player.Respawn(new Vector2(0, .69f));
            yield return Step(4);
            Check(player.Grounded, "ground detection");
            Set(player, "horizontal", 1f);
            yield return Step(12);
            Check(player.transform.position.x > 1, "horizontal motion with real 2D physics");
            Set(player, "horizontal", 0f);
            Set(player, "jumpUntil", Time.time + .12f);
            yield return Step(1);
            Check(body.velocity.y > 9 && !player.Grounded, "ground jump");
            yield return Step(5);
            Set(player, "jumpUntil", Time.time + .12f);
            yield return Step(1);
            Check(!player.CanDoubleJump && body.velocity.y > 9, "one double jump");
            Set(player, "jumpUntil", Time.time + .12f);
            float before = body.velocity.y;
            yield return Step(1);
            Check(body.velocity.y < before, "third jump rejected");
            player.Respawn(new Vector2(5, 3));
            player.enableDoubleJump = false;
            Set(player, "jumpUntil", Time.time + .12f);
            yield return Step(1);
            Check(body.velocity.y <= 0 && !player.CanDoubleJump, "public switch disables double jump");
            player.enableDoubleJump = true;
            player.Respawn(new Vector2(5, 3));
            player.enableDash = false;
            Set(player, "dashUntil", Time.time + player.dashDuration);
            yield return Step(1);
            Check(Mathf.Abs(body.velocity.x) < player.dashSpeed && body.gravityScale > 0 && !player.CanAirDash, "public switch cancels active dash");
            player.enableDash = true;
            Set(player, "usedAirDash", true);
            Set(player, "dashUntil", Time.time + player.dashDuration);
            Set(player, "jumpUntil", -1f);
            yield return Step(1);
            Check(Mathf.Abs(body.velocity.x - player.dashSpeed) < .1f && Mathf.Abs(body.velocity.y) < .1f, "horizontal dash suspends gravity");
            player.Respawn(new Vector2(0, .69f));
            Set(player, "usedDouble", true);
            Set(player, "usedAirDash", true);
            yield return Step(4);
            Check(player.CanDoubleJump && player.CanAirDash, "landing restores both air abilities");
            player.Respawn(new Vector2(28.99f, 2));
            Set(player, "usedDouble", true);
            Set(player, "usedAirDash", true);
            Set(player, "vertical", 1f);
            yield return Step(8);
            Check(player.transform.position.y > 2.4f, "wall climb");
            Check(!player.CanDoubleJump && !player.CanAirDash, "wall contact does not recharge air abilities");
            player.enableWallClimb = false;
            player.Respawn(new Vector2(28.99f, 2));
            Set(player, "vertical", 1f);
            yield return Step(4);
            Check(player.transform.position.y < 2 && body.gravityScale > 0, "public switch disables wall climbing and sliding");
            player.enableWallClimb = true;
            var noClimbWall = Physics2D.BoxCast(body.position, new Vector2(.12f, .85f), 0, Vector2.right, .48f, 1 << 8).collider.GetComponent<GroundSurface>();
            noClimbWall.canClimb = false;
            player.Respawn(new Vector2(28.99f, 2));
            Set(player, "vertical", 1f);
            yield return Step(4);
            Check(body.gravityScale > 0 && player.transform.position.y < 2, "ground prefab can prohibit wall climbing");
            noClimbWall.canClimb = true;
            player.Respawn(new Vector2(28.99f, 2));
            Set(player, "usedDouble", true);
            Set(player, "usedAirDash", true);
            Set(player, "vertical", 1f);
            yield return Step(2);
            Set(player, "jumpUntil", Time.time + .12f);
            yield return Step(1);
            Check(body.velocity.x < -7 && body.velocity.y > 9, "wall jump moves away from wall");
            player.Respawn(new Vector2(0, .69f));
            Set(player, "vertical", 0f);
            yield return Step(4);
            Set(player, "invulnerableUntil", 0f);
            player.Damage(1, Vector2.left);
            int health = player.Health;
            player.Damage(1, Vector2.left);
            Check(player.Health == health, "hurt invulnerability rejects repeated damage");
            var potion = Resources.Load<ItemDefinition>("Items/potion");
            var crystal = Resources.Load<ItemDefinition>("Items/crystal");
            Check(game.inventory.TryAdd(potion, 2), "potion pickup");
            Check(game.inventory.Use(0, player) && player.Health == player.maxHealth, "potion healing");
            Check(!game.inventory.Use(0, player) && game.inventory.slots[0].count == 1, "full health does not consume potion");
            player.Respawn(new Vector2(40.5f, .69f));
            Set(player, "invulnerableUntil", 0f);
            player.Damage(1, Vector2.left);
            Set(player, "hurtUntil", 0f);
            Set(player, "horizontal", 1f);
            yield return Step(12);
            yield return new WaitUntil(() => !game.Transitioning);
            Check(game.LoadedRoom == 1 && SceneManager.sceneCount == 2, "walking through exit loads next room automatically");
            Check(player.Health == player.maxHealth - 1, "room transition preserves health");
            foreach (var enemy in game.Enemies) enemy.enabled = false;
            yield return SwitchRoom(2);
            var shield = game.Enemies.First(e => e.kind == EnemyKind.Shield);
            shield.ResetEnemy();
            Check(!shield.TakeHit(1, shield.Home + Vector2.left, 1), "shield blocks frontal attack");
            Check(shield.TakeHit(1, shield.Home + Vector2.right, -1), "shield accepts rear attack");
            shield.ResetEnemy();
            yield return SwitchRoom(1);
            var patrol = game.Enemies.First(e => e.kind == EnemyKind.Patrol);
            player.Respawn(patrol.Home + Vector2.left * .95f);
            Set(player, "attackUntil", Time.time + .12f);
            int enemyHealth = patrol.Health;
            yield return Step(4);
            Check(patrol.Health == enemyHealth - 1, "one melee swing damages target only once");
            patrol.ResetEnemy();
            string defeatedId = patrol.persistentId;
            patrol.TakeHit(patrol.Health, patrol.Home + Vector2.left, 1);
            Check(!patrol.gameObject.activeSelf, "lethal hit removes enemy");
            yield return SwitchRoom(2);
            yield return SwitchRoom(1);
            Check(!game.Enemies.First(e => e.persistentId == defeatedId).gameObject.activeSelf, "room revisit preserves defeated enemy");
            yield return SwitchRoom(2);
            var burrow = game.Enemies.First(e => e.kind == EnemyKind.Burrower);
            player.Respawn(burrow.Home + Vector2.left * 4);
            Set(burrow, "idleUntil", Time.time + 10);
            for (int i = 0; i < 20; i++)
            {
                Call(burrow, "FixedUpdate");
                Physics2D.Simulate(.02f);
                yield return new WaitForFixedUpdate();
            }
            Check(burrow.transform.position.y > game.ActiveRoom.Bounds.yMin + 3, "hidden burrower does not fall through terrain");
            burrow.ResetEnemy();
            game.Toggle(GameScreen.Map);
            Check(game.Paused && Time.timeScale == 0, "map pauses simulation");
            game.Toggle(GameScreen.Bag);
            Check(game.Paused && game.Screen == GameScreen.Bag, "bag replaces map");
            game.Toggle(GameScreen.Bag);
            Check(!game.Paused && Time.timeScale == 1, "closing UI restores simulation");
            var pickup = FindObjectsOfType<Pickup>().First();
            string pickupId = pickup.persistentId;
            pickup.Interact(game);
            Check(!pickup.gameObject.activeSelf && game.inventory.Count(pickup.item) >= pickup.count, "collect branch item");
            int pickedCount = game.inventory.Count(pickup.item);
            pickup.Interact(game);
            Check(game.inventory.Count(pickup.item) == pickedCount, "same world item cannot be collected twice");
            var room = FindObjectsOfType<RoomVolume>().First(r => r.id == 2);
            room.explored = true;
            var point = FindObjectsOfType<Checkpoint>().First(p => p.roomId == 2);
            Vector2 checkpointSpawn = point.spawnPoint.position;
            player.Respawn(point.spawnPoint.position);
            Call(game, "Update");
            Check(!point.activated, "checkpoint proximity does not activate it");
            point.Interact(game);
            Check(point.activated && SaveStore.Exists, "checkpoint interaction activates and saves");
            Set(player, "invulnerableUntil", 0f);
            player.Damage(player.maxHealth, Vector2.left);
            yield return new WaitUntil(() => !player.Dead && !game.Transitioning);
            Check(player.Health == player.maxHealth && Vector2.Distance(player.transform.position, checkpointSpawn) < .2f, "death respawns at checkpoint with full health");
            Check(room.explored && game.inventory.slots[0].count > 0 && !FindObjectsOfType<Pickup>(true).First(p => p.persistentId == pickupId).gameObject.activeSelf, "death preserves exploration, inventory and picked items");
            yield return SwitchRoom(0);
            player.Respawn(new Vector2(10, 5));
            player.enableGrapple = false;
            Check(!player.TryGrapple(), "public switch disables grapple activation");
            player.enableGrapple = true;
            var nearbyHook = FindObjectsOfType<GrapplePoint>().OrderBy(h => Vector2.Distance(h.transform.position, player.transform.position)).First();
            nearbyHook.isAvailable = false;
            Check(!player.TryGrapple(), "disabled hook cannot be selected");
            nearbyHook.isAvailable = true;
            Set(player, "usedDouble", true);
            Set(player, "usedAirDash", true);
            Check(player.TryGrapple(), "grapple selects visible anchor ahead");
            yield return Step(4);
            Check(player.Grappling && player.transform.position.x > 10.7f && body.gravityScale == 0, "grapple pulls with real physics");
            Check(!player.CanDoubleJump && !player.CanAirDash, "grapple does not recharge air abilities");
            player.enableGrapple = false;
            yield return Step(1);
            Check(!player.Grappling, "public switch cancels active grapple");
            player.enableGrapple = true;
            player.Respawn(new Vector2(10, 5));
            Check(player.TryGrapple(), "grapple can be reenabled");
            Set(player, "invulnerableUntil", 0f);
            player.Damage(1, Vector2.left);
            Check(!player.Grappling, "hurt interrupts grapple");
            player.Respawn(new Vector2(24, 1));
            Check(!player.TryGrapple(), "terrain blocks grapple line of sight");
            player.Respawn(new Vector2(185, 1));
            Check(!player.TryGrapple(), "grapple does not select anchors behind player");
            var merchant = FindObjectsOfType<Npc>().First(n => n.shop != null);
            player.Respawn(merchant.transform.position);
            Call(game, "Update");
            Check(game.LandmarkDiscovered("merchant_arli"), "approaching merchant reveals map icon");
            merchant.Interact(game);
            Check(game.Screen == GameScreen.Dialogue && game.Paused, "NPC opens reusable dialogue and pauses");
            var dialogue = merchant.dialogue;
            yield return null;
            yield return null;
            var dialogueUI = FindObjectOfType<DialogueCanvas>();
            Check(dialogueUI.panel.activeSelf && dialogueUI.body.text == dialogue.nodes[0].text && dialogueUI.speaker.text == dialogue.nodes[0].speaker, "Canvas dialogue prefab displays assigned speaker and text");
            dialogueUI.choices.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == dialogue.nodes[0].choices.First(c => c.nextNode == 1).text).onClick.Invoke();
            Check(game.DialogueNode == 1, "dialogue choice follows branch");
            yield return null;
            yield return null;
            Check(dialogueUI.body.text == dialogue.nodes[1].text, "Canvas dialogue refreshes when following a branch");
            dialogueUI.Close();
            Check(game.Screen == GameScreen.None && !game.Paused, "Canvas dialogue close resumes game");
            game.StartDialogue(merchant);
            game.ChooseDialogue(dialogue.nodes[0].choices.First(c => c.openShop));
            Check(game.Screen == GameScreen.Shop, "merchant dialogue opens shop");
            game.inventory.TryAdd(crystal, 30);
            int money = game.inventory.Count(crystal);
            int potions = game.inventory.Count(potion);
            Check(game.Buy(merchant.shop.offers[0]), "merchant purchase succeeds");
            Check(game.inventory.Count(crystal) == money - 5 && game.inventory.Count(potion) == potions + 1, "purchase exchanges currency atomically");
            game.SetScreen(GameScreen.None);
            int savedPotions = game.inventory.Count(potion);
            var savedFog = game.MapFog.Save();
            game.ReturnToMenu();
            Check(game.Screen == GameScreen.MainMenu && Time.timeScale == 0, "return to main menu pauses and saves");
            game.RequestNewGame();
            Check(game.Screen == GameScreen.ConfirmNew, "new game asks before replacing saved progress");
            game.SetScreen(GameScreen.MainMenu);
            game.inventory = new Inventory();
            room.explored = false;
            game.MapFog.Clear();
            menu.continueButton.onClick.Invoke();
            yield return new WaitUntil(() => !game.Transitioning);
            Check(game.Screen == GameScreen.None && game.inventory.Count(potion) == savedPotions, "continue loads inventory from disk");
            Check(savedFog.Length > 0 && savedFog.All(c => game.MapFog.Visible(c.room, new Vector2((c.x + .5f) * MapFog.CellSize, (c.y + .5f) * MapFog.CellSize))),
                "continue restores every revealed fog cell from disk");
            Check(game.LandmarkDiscovered("merchant_arli"), "merchant discovery survives continue from disk");
            Check(room.explored && !FindObjectsOfType<Pickup>(true).First(p => p.persistentId == pickupId).gameObject.activeSelf && Vector2.Distance(player.transform.position, checkpointSpawn) < .2f, "continue restores checkpoint, map and collected items");
            yield return SwitchRoom(1);
            Check(!game.Enemies.First(e => e.persistentId == defeatedId).gameObject.activeSelf, "continue restores defeated enemies from disk");
            Set(player, "invulnerableUntil", 0f);
            player.Damage(player.maxHealth, Vector2.left);
            yield return new WaitUntil(() => !player.Dead && !game.Transitioning);
            yield return SwitchRoom(1);
            Check(game.Enemies.First(e => e.persistentId == defeatedId).gameObject.activeSelf, "player death resets current challenge enemies");
            yield return SwitchRoom(3);
            Set(player, "invulnerableUntil", 0f);
            player.Damage(player.maxHealth, Vector2.left);
            yield return new WaitUntil(() => !player.Dead && !game.Transitioning);
            Check(game.LoadedRoom == 2 && Vector2.Distance(player.transform.position, checkpointSpawn) < .2f,
                "death in another room reloads checkpoint room");
            Check(game.Rooms[3].explored && !game.Rooms[4].explored && game.inventory.Count(potion) == savedPotions,
                "cross-room death preserves discovery and inventory without revealing next room");
            Check(game.LandmarkDiscovered("merchant_arli"), "merchant discovery survives death");
            Check(savedFog.All(c => game.MapFog.Visible(c.room, new Vector2((c.x + .5f) * MapFog.CellSize, (c.y + .5f) * MapFog.CellSize))),
                "cross-room death preserves previously revealed fog cells");
            game.SetScreen(GameScreen.Pause);
            game.OpenSettings();
            Check(game.Screen == GameScreen.Settings && Time.timeScale == 0, "settings opened from pause stays paused");
            game.CloseSettings();
            Check(game.Screen == GameScreen.Pause, "settings returns to pause menu");
            game.SetScreen(GameScreen.None);
            yield return SwitchRoom(4);
            player.Respawn(game.ActiveRoom.finish.position);
            Call(game, "Update");
            Check(game.Completed, "finish gate completes demo");
            yield return SwitchRoom(3);
            var droppedPickup = FindObjectsOfType<Pickup>().FirstOrDefault(p => p.name == "Smoke Pickup");
            if (droppedPickup != null)
            {
                string droppedId = droppedPickup.persistentId;
                droppedPickup.Interact(game);
                int droppedCount = game.inventory.Count(droppedPickup.item);
                droppedPickup.Interact(game);
                Check(game.inventory.Count(droppedPickup.item) == droppedCount, "dragged pickup remains single-use");
                var droppedPoint = FindObjectsOfType<Checkpoint>().Single(p => p.name == "Smoke Checkpoint");
                string droppedPointId = droppedPoint.persistentId;
                Vector2 droppedSpawn = droppedPoint.spawnPoint.position;
                droppedPoint.Interact(game);
                Set(player, "invulnerableUntil", 0f);
                player.Damage(player.maxHealth, Vector2.left);
                yield return new WaitUntil(() => !player.Dead && !game.Transitioning);
                Check(Vector2.Distance(player.transform.position, droppedSpawn) < .15f, "dragged checkpoint selected spawn survives death");
                game.ReturnToMenu();
                menu.continueButton.onClick.Invoke();
                yield return new WaitUntil(() => !game.Transitioning);
                Check(Vector2.Distance(player.transform.position, droppedSpawn) < .15f && FindObjectsOfType<Checkpoint>().Single(p => p.persistentId == droppedPointId).activated, "continue restores selected prefab checkpoint");
                Check(!FindObjectsOfType<Pickup>(true).Single(p => p.persistentId == droppedId).gameObject.activeSelf, "dragged pickup does not return after continue");
            }
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Debug.Log("DEMO_SMOKE_OK: independent room loading/unloading, hidden map, camera bounds, persistence and gameplay passed.");
        }

        IEnumerator Step(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                Call(player, "FixedUpdate");
                Physics2D.Simulate(.02f);
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator SwitchRoom(int id)
        {
            int previous = game.LoadedRoom;
            game.EnterRoom(id, "LeftEntrance");
            yield return new WaitUntil(() => !game.Transitioning);
            foreach (var enemy in game.Enemies) enemy.enabled = false;
            Check(game.LoadedRoom == id && SceneManager.sceneCount == 2, "room " + id + " loaded exclusively");
            Check(!SceneManager.GetSceneByPath(game.Rooms[previous].scenePath).isLoaded, "previous room unloaded");
            var camera = Camera.main;
            float halfWidth = camera.orthographicSize * camera.aspect;
            var bounds = game.ActiveRoom.Bounds;
            var entrance = game.ActiveRoom.GetComponentsInChildren<RoomEntrance>().Single(e => e.entranceId == "LeftEntrance");
            Check(Vector2.Distance(player.transform.position, entrance.transform.position) < .15f, "spawn resolves entrance object after room movement");
            Vector2 beforeMap = game.ActiveRoom.MapPosition(game.Rooms[id].map, player.transform.position);
            Vector3 offset = new Vector3(80, -20);
            game.ActiveRoom.transform.position += offset;
            Check(Vector2.Distance(beforeMap, game.ActiveRoom.MapPosition(game.Rooms[id].map, player.transform.position + offset)) < .01f, "room translation cannot change independent map coordinates");
            game.ActiveRoom.transform.position -= offset;
            Physics2D.SyncTransforms();
            Check(camera.transform.position.x - halfWidth >= bounds.xMin - .05f && camera.transform.position.x + halfWidth <= bounds.xMax + .05f,
                "camera stays inside current room");
        }
    }
}
#endif
