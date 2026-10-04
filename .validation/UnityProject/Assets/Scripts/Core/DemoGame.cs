using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowDemo
{
    public enum GameScreen { MainMenu, ConfirmNew, None, Map, Bag, Pause, Settings, Dialogue, Shop }

    [DefaultExecutionOrder(-100)]
    public sealed class DemoGame : MonoBehaviour
    {
        public static DemoGame Instance { get; private set; }
        public PlayerMotor Player { get; private set; }
        [System.NonSerialized] public Inventory inventory = new Inventory();
        public RoomContent ActiveRoom { get; private set; }
        public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
        public bool Paused => Screen != GameScreen.None || Transitioning;
        public bool Transitioning { get; private set; }
        public float Fade { get; private set; }
        public int LoadedRoom { get; private set; } = -1;
        public bool HasRun { get; private set; }
        public int CurrentRoom { get; private set; }
        public bool Completed { get; private set; }
        public RoomVolume[] Rooms { get; private set; }
        public readonly MapFog MapFog = new MapFog();
        bool mapDirty;
        float nextMapSave;
        public Checkpoint[] Checkpoints { get; private set; }
        public EnemyBrain[] Enemies { get; private set; }
        public Interactable Nearby { get; private set; }
        public Npc SpeakingNpc { get; private set; }
        public int DialogueNode { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public float Volume { get; private set; }
        GameScreen settingsReturn;
        Pickup[] pickups;
        int checkpointRoom = -1;
        string checkpointId;
        readonly HashSet<string> activatedCheckpointIds = new HashSet<string>();
        readonly HashSet<int> activatedCheckpoints = new HashSet<int>();
        readonly HashSet<string> collectedPickups = new HashSet<string>();
        readonly HashSet<string> defeatedEnemies = new HashSet<string>();
        readonly HashSet<string> discoveredLandmarks = new HashSet<string>();
        ItemDefinition[] items;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 0;
            Physics2D.gravity = new Vector2(0, -9.81f);
            Physics2D.IgnoreLayerCollision(9, 10);
            Physics2D.IgnoreLayerCollision(10, 10);
            Volume = PlayerPrefs.GetFloat("MasterVolume", 1);
            AudioListener.volume = Volume;
            if (!Application.isBatchMode) UnityEngine.Screen.fullScreen = PlayerPrefs.GetInt("FullScreen", 0) == 1;
        }

        void Start()
        {
            Player = FindObjectOfType<PlayerMotor>();
            Rooms = FindObjectsOfType<RoomVolume>().OrderBy(r => r.id).ToArray();
            Checkpoints = System.Array.Empty<Checkpoint>();
            Enemies = System.Array.Empty<EnemyBrain>();
            pickups = System.Array.Empty<Pickup>();
            items = Resources.LoadAll<ItemDefinition>("Items");
        }

        void Update()
        {
            if (Player == null || Player.Dead || Transitioning) return;
            if (Screen == GameScreen.MainMenu || Screen == GameScreen.ConfirmNew) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Screen == GameScreen.Settings) SetScreen(settingsReturn);
                else SetScreen(Screen == GameScreen.None ? GameScreen.Pause : GameScreen.None);
                return;
            }
            if (Screen == GameScreen.None || Screen == GameScreen.Map || Screen == GameScreen.Bag)
            {
                if (Input.GetKeyDown(KeyCode.M)) Toggle(GameScreen.Map);
                if (Input.GetKeyDown(KeyCode.B)) Toggle(GameScreen.Bag);
            }
            if (Paused) return;
            UpdateRoom();
            if (mapDirty && Time.unscaledTime >= nextMapSave) SaveProgress();
            foreach (var npc in ActiveRoom.GetComponentsInChildren<Npc>())
                if (!string.IsNullOrEmpty(npc.mapLandmarkId) && Vector2.Distance(Player.transform.position, npc.transform.position) <= 5 && discoveredLandmarks.Add(npc.mapLandmarkId))
                    SaveProgress();
            Nearby = FindObjectsOfType<Interactable>()
                .Where(p => Vector2.Distance(p.transform.position, Player.transform.position) <= p.interactionRange)
                .OrderBy(p => Vector2.Distance(p.transform.position, Player.transform.position)).FirstOrDefault();
            if (Nearby != null && Input.GetKeyDown(KeyCode.E)) Nearby.Interact(this);
            if (ActiveRoom != null && ActiveRoom.finish != null && !Completed && Vector2.Distance(Player.transform.position, ActiveRoom.finish.position) < 2)
            {
                Completed = true;
                ShowNotice("已抵达终点！你仍可继续探索。");
                SaveProgress();
            }
        }

        void UpdateRoom()
        {
            if (LoadedRoom < 0) return;
            var room = Rooms[CurrentRoom];
            if (!room.explored) { room.explored = true; SaveProgress(); }
        }

        public void RevealCameraMap(Camera camera)
        {
            var room = Rooms[CurrentRoom];
            mapDirty |= MapFog.Reveal(room.id, ActiveRoom.MapViewBounds(room.map, camera));
        }

        public void SetScreen(GameScreen screen)
        {
            Screen = screen;
            Time.timeScale = Paused ? 0 : 1;
            if (Player != null) Player.ClearInput();
            Nearby = null;
        }
        public void Toggle(GameScreen screen) => SetScreen(Screen == screen ? GameScreen.None : screen);
        public void OpenSettings() { settingsReturn = Screen; SetScreen(GameScreen.Settings); }
        public void CloseSettings() => SetScreen(settingsReturn);

        public void SetVolume(float value)
        {
            Volume = value;
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }
        public void SetFullscreen(bool value)
        {
            UnityEngine.Screen.fullScreen = value;
            PlayerPrefs.SetInt("FullScreen", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void RequestNewGame()
        {
            if (SaveStore.Exists || HasRun) SetScreen(GameScreen.ConfirmNew);
            else NewGame();
        }

        public void NewGame()
        {
            ResetWorld();
            HasRun = true;
            SetScreen(GameScreen.None);
            StartCoroutine(LoadRoom(0, "StartEntrance", true));
        }

        void ResetWorld()
        {
            StopAllCoroutines();
            foreach (var projectile in FindObjectsOfType<Projectile>()) Destroy(projectile.gameObject);
            foreach (var room in Rooms) room.explored = false;
            MapFog.Clear();
            mapDirty = false;
            checkpointRoom = -1;
            checkpointId = null;
            activatedCheckpointIds.Clear();
            activatedCheckpoints.Clear();
            collectedPickups.Clear();
            defeatedEnemies.Clear();
            discoveredLandmarks.Clear();
            Completed = false;
            inventory = new Inventory();
            SpeakingNpc = null;
        }

        public void ContinueGame()
        {
            try
            {
                var data = SaveStore.Read();
                if (data == null || data.version != 1) { ShowNotice("存档格式不支持"); return; }
                ResetWorld();
                MapFog.Restore(data.exploredMapCells);
                foreach (var room in Rooms) room.explored = data.exploredRooms.Contains(room.id);
                activatedCheckpoints.UnionWith(data.activatedCheckpoints);
                collectedPickups.UnionWith(data.collectedPickups);
                defeatedEnemies.UnionWith(data.defeatedEnemies ?? System.Array.Empty<string>());
                discoveredLandmarks.UnionWith(data.discoveredLandmarks ?? System.Array.Empty<string>());
                checkpointRoom = data.checkpointRoom;
                checkpointId = data.checkpointId;
                activatedCheckpointIds.UnionWith(data.activatedCheckpointIds ?? System.Array.Empty<string>());
                if (activatedCheckpointIds.Count == 0)
                    activatedCheckpointIds.UnionWith(activatedCheckpoints.Select(id => "checkpoint_" + id));
                foreach (var stack in data.inventory)
                {
                    var item = items.FirstOrDefault(i => i.id == stack.itemId);
                    if (item != null) inventory.slots[stack.slot] = new Inventory.Slot { item = item, count = stack.count };
                }
                Completed = data.completed;
                HasRun = true;
                SetScreen(GameScreen.None);
                int target = checkpointRoom >= 0 ? checkpointRoom : 0;
                StartCoroutine(LoadRoom(target, checkpointRoom >= 0 ? null : "StartEntrance", true));
                ShowNotice("已读取存档，从最近的检查点继续");
            }
            catch (IOException) { ShowNotice("无法读取存档，请检查文件访问权限"); }
            catch (System.ArgumentException) { ShowNotice("存档内容损坏，未能读取"); }
        }

        public void SaveProgress()
        {
            if (!HasRun) return;
            var stacks = new List<SaveData.Stack>();
            for (int i = 0; i < inventory.slots.Length; i++)
                if (inventory.slots[i] != null) stacks.Add(new SaveData.Stack
                { slot = i, itemId = inventory.slots[i].item.id, count = inventory.slots[i].count });
            var data = new SaveData
            {
                checkpointRoom = checkpointRoom,
                checkpointId = checkpointId,
                activatedCheckpointIds = activatedCheckpointIds.ToArray(),
                completed = Completed,
                exploredRooms = Rooms.Where(r => r.explored).Select(r => r.id).ToArray(),
                exploredMapCells = MapFog.Save(),
                activatedCheckpoints = activatedCheckpoints.ToArray(),
                collectedPickups = collectedPickups.ToArray(),
                defeatedEnemies = defeatedEnemies.ToArray(),
                discoveredLandmarks = discoveredLandmarks.ToArray(),
                inventory = stacks.ToArray()
            };
            try
            {
                SaveStore.Write(data);
                mapDirty = false;
                nextMapSave = Time.unscaledTime + 2;
            }
            catch (IOException) { ShowNotice("保存失败，请检查磁盘空间或文件访问权限"); }
            catch (System.UnauthorizedAccessException) { ShowNotice("保存失败：无法访问存档目录"); }
        }

        public void ActivateCheckpoint(Checkpoint point)
        {
            point.activated = true;
            checkpointRoom = point.roomId;
            checkpointId = point.persistentId;
            activatedCheckpointIds.Add(point.persistentId);
            activatedCheckpoints.Add(point.roomId);
            point.GetComponent<SpriteRenderer>().color = new Color(.3f, 1, .85f);
            Player.Heal(Player.maxHealth);
            SaveProgress();
            ShowNotice("检查点已记录，生命已恢复，进度已保存");
        }

        public void ReturnToMenu()
        {
            SaveProgress();
            Player.ReleaseGrapple();
            SetScreen(GameScreen.MainMenu);
        }

        public void QuitGame()
        {
            SaveProgress();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void StartDialogue(Npc npc)
        {
            SpeakingNpc = npc;
            DialogueNode = 0;
            SetScreen(GameScreen.Dialogue);
        }
        public void ChooseDialogue(DialogueDefinition.Choice choice)
        {
            if (choice.openShop && SpeakingNpc.shop != null) SetScreen(GameScreen.Shop);
            else if (choice.nextNode >= 0) DialogueNode = choice.nextNode;
            else SetScreen(GameScreen.None);
        }
        public bool Buy(ShopDefinition.Offer offer)
        {
            var currency = SpeakingNpc.shop.currency;
            if (!inventory.TryBuy(offer.item, offer.quantity, currency, offer.price))
            { ShowNotice("交易失败：晶石不足或背包空间不足"); return false; }
            SaveProgress();
            ShowNotice("购入 " + offer.item.displayName + " ×" + offer.quantity);
            return true;
        }

        public void ShowNotice(string message) { Notice = message; NoticeUntil = Time.unscaledTime + 3; }
        public void BeginRespawn() => StartCoroutine(RespawnAfterDelay());
        IEnumerator RespawnAfterDelay()
        {
            int challenge = CurrentRoom;
            ShowNotice("你倒下了，正在返回检查点……");
            yield return new WaitForSeconds(.7f);
            foreach (var projectile in FindObjectsOfType<Projectile>()) Destroy(projectile.gameObject);
            int target = checkpointRoom >= 0 ? checkpointRoom : 0;
            defeatedEnemies.RemoveWhere(id => id.StartsWith(challenge + ":") || id.StartsWith(target + ":"));
            yield return LoadRoom(target, checkpointRoom >= 0 ? null : "StartEntrance", true);
        }

        public void SpawnProjectile(Vector2 position, Vector2 velocity, int room)
        {
            var go = new GameObject("怪物弹丸");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * .3f;
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = Resources.Load<Sprite>("Sprites/Circle");
            sprite.color = new Color(1, .4f, .2f);
            sprite.sortingOrder = 5;
            var projectile = go.AddComponent<Projectile>();
            projectile.velocity = velocity;
            projectile.roomId = room;
            if (LoadedRoom >= 0) SceneManager.MoveGameObjectToScene(go, SceneManager.GetSceneByPath(Rooms[LoadedRoom].scenePath));
        }
        public bool CheckpointActivated(int id) => activatedCheckpoints.Contains(id);
        public void RecordPickup(string id) => collectedPickups.Add(id);
        public bool PickupCollected(string id) => collectedPickups.Contains(id);
        public bool LandmarkDiscovered(string id) => discoveredLandmarks.Contains(id);
        public void RecordEnemyDeath(string id)
        {
            if (defeatedEnemies.Add(id)) SaveProgress();
        }

        public void EnterRoom(int target, string entranceId)
        {
            if (Paused || Player.Dead || target == LoadedRoom) return;
            StartCoroutine(LoadRoom(target, entranceId, false));
        }

        IEnumerator LoadRoom(int target, string entranceId, bool restoreHealth)
        {
            Transitioning = true;
            Time.timeScale = 0;
            Player.ClearInput();
            Player.ReleaseGrapple();
            var body = Player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            yield return FadeTo(1);
            Enemies = System.Array.Empty<EnemyBrain>();
            Checkpoints = System.Array.Empty<Checkpoint>();
            pickups = System.Array.Empty<Pickup>();
            SpeakingNpc = null;
            if (LoadedRoom >= 0) yield return SceneManager.UnloadSceneAsync(Rooms[LoadedRoom].scenePath);
            else
                foreach (var room in Rooms)
                    if (SceneManager.GetSceneByPath(room.scenePath).isLoaded)
                        yield return SceneManager.UnloadSceneAsync(room.scenePath);
            yield return SceneManager.LoadSceneAsync(Rooms[target].scenePath, LoadSceneMode.Additive);
            LoadedRoom = CurrentRoom = target;
            var roots = SceneManager.GetSceneByPath(Rooms[target].scenePath).GetRootGameObjects();
            Enemies = roots.SelectMany(r => r.GetComponentsInChildren<EnemyBrain>(true)).ToArray();
            Checkpoints = roots.SelectMany(r => r.GetComponentsInChildren<Checkpoint>(true)).ToArray();
            pickups = roots.SelectMany(r => r.GetComponentsInChildren<Pickup>(true)).ToArray();
            foreach (var enemy in Enemies) if (defeatedEnemies.Contains(enemy.persistentId)) enemy.gameObject.SetActive(false);
            foreach (var pickup in pickups) pickup.gameObject.SetActive(!collectedPickups.Contains(pickup.persistentId));
            foreach (var point in Checkpoints)
            {
                point.activated = activatedCheckpointIds.Contains(point.persistentId) ||
                    (activatedCheckpointIds.Count == 0 && activatedCheckpoints.Contains(point.roomId));
                if (point.activated) point.GetComponent<SpriteRenderer>().color = new Color(.3f, 1, .85f);
            }
            ActiveRoom = roots.SelectMany(r => r.GetComponentsInChildren<RoomContent>()).Single();
            var entrance = entranceId == null ? null : ActiveRoom.GetComponentsInChildren<RoomEntrance>().Single(e => e.entranceId == entranceId);
            var savedPoint = entrance == null ? (string.IsNullOrEmpty(checkpointId) ? ActiveRoom.checkpoint : Checkpoints.Single(p => p.persistentId == checkpointId)) : null;
            Player.Respawn(entrance == null ? (Vector2)savedPoint.spawnPoint.position : (Vector2)entrance.transform.position, restoreHealth, entrance == null ? 1 : entrance.facing);
            Physics2D.SyncTransforms();
            FindObjectOfType<CameraFollow>().Snap();
            UpdateRoom();
            SaveProgress();
            yield return FadeTo(0);
            body.simulated = true;
            Transitioning = false;
            Time.timeScale = Screen == GameScreen.None ? 1 : 0;
        }

        IEnumerator FadeTo(float target)
        {
            while (!Mathf.Approximately(Fade, target))
            {
                Fade = Mathf.MoveTowards(Fade, target, Time.unscaledDeltaTime / .16f);
                yield return null;
            }
        }
        void OnApplicationQuit() => SaveProgress();
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
    }
}
