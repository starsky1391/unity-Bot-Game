using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HollowDemo
{
    public enum GameScreen { MainMenu, ConfirmNew, None, Map, Bag, Pause, Settings, Dialogue, Shop, Checkpoint }

    [DefaultExecutionOrder(-100)]
    public sealed class DemoGame : MonoBehaviour
    {
        public static DemoGame Instance { get; private set; }
        public PlayerMotor Player { get; private set; }
        [Min(1)] public int initialFlaskCapacity = 3;
        public ItemDefinition flaskItem, manaFlaskItem;
        public int ManaFlaskCapacity { get; private set; }
        public int ManaFlaskCharges { get; private set; }
        public int BloodFlaskCapacity => FlaskCapacity - ManaFlaskCapacity;
        public int FlaskCapacity { get; private set; }
        public int FlaskCharges { get; private set; }
        public int Crystals { get; private set; }
        [System.NonSerialized] public Inventory inventory = new Inventory();
        public WorldMap World { get; private set; }
        public Npc[] Npcs { get; private set; }
        public BossArena[] BossArenas { get; private set; }
        public BossArena ActiveBoss { get; set; }
        public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
        public bool Paused => Screen != GameScreen.None || Transitioning;
        public bool Transitioning { get; private set; }
        public float Fade { get; private set; }
        public bool HasRun { get; private set; }
        public bool Completed { get; private set; }
        public readonly MapFog MapFog = new MapFog();
        public ItemDefinition[] EquippedItems { get; private set; } = new ItemDefinition[3];
        public int SelectedEquipment { get; private set; }
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
            var follow = FindObjectOfType<CameraFollow>();
            if (follow != null)
            {
                var gameCamera = follow.GetComponent<Camera>();
                gameCamera.enabled = true;
                foreach (var camera in FindObjectsOfType<Camera>())
                    if (camera != gameCamera && camera.CompareTag("MainCamera")) camera.enabled = false;
            }
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
            items = Resources.LoadAll<ItemDefinition>("Items");
            InitializeLevel();
        }

        bool InitializeLevel()
        {
            Checkpoints = FindObjectsOfType<Checkpoint>();
            BossArenas = FindObjectsOfType<BossArena>();
            Enemies = FindObjectsOfType<EnemyBrain>(true)
                .Where(e => HasActiveParents(e) && e.GetComponentInParent<BossArena>() == null).ToArray();
            pickups = FindObjectsOfType<Pickup>(true).Where(HasActiveParents).ToArray();
            Npcs = FindObjectsOfType<Npc>();
            World = FindObjectOfType<WorldMap>();
            if (World == null) World = new GameObject("关卡（自动初始化）").AddComponent<WorldMap>();
            if (!World.Initialize(Checkpoints, FindObjectsOfType<GroundSurface>(true), out string error))
            {
                SetScreen(GameScreen.MainMenu);
                ShowNotice(error);
                Debug.LogWarning(error);
                return false;
            }
            return true;
        }

        static bool HasActiveParents(Component component)
        {
            // 保留已死亡或已拾取的实例，但排除被整个关闭的旧关卡。
            return component.transform.parent == null || component.transform.parent.gameObject.activeInHierarchy;
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
            if (Input.GetKeyDown(KeyCode.Q)) CycleEquipment();
            if (Input.GetKeyDown(KeyCode.F)) UseEquippedItem();
            if (mapDirty && Time.unscaledTime >= nextMapSave) SaveProgress();
            foreach (var npc in Npcs)
                if (!string.IsNullOrEmpty(npc.mapLandmarkId) && Vector2.Distance(Player.transform.position, npc.transform.position) <= 5 && discoveredLandmarks.Add(npc.mapLandmarkId))
                    SaveProgress();
            Nearby = FindObjectsOfType<Interactable>()
                .Where(p => Vector2.Distance(p.transform.position, Player.transform.position) <= p.interactionRange)
                .OrderBy(p => Vector2.Distance(p.transform.position, Player.transform.position)).FirstOrDefault();
            if (Nearby != null && Input.GetKeyDown(KeyCode.E)) Nearby.Interact(this);
            if (World.finish != null && !Completed && BossArenas.All(a => EnemyDefeated(a.boss.persistentId)) && Vector2.Distance(Player.transform.position, World.finish.position) < 2)
            {
                Completed = true;
                ShowNotice("已抵达终点！你仍可继续探索。");
                SaveProgress();
            }
        }

        public void RevealCameraMap(Camera camera)
        {
            if (World != null && HasRun) mapDirty |= MapFog.Reveal(0, World.MapView(camera));
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
            SetScreen(GameScreen.ConfirmNew);
        }

        public void NewGame()
        {
            if (!InitializeLevel()) return;
            ResetWorld();
            FlaskCapacity = initialFlaskCapacity;
            FlaskCharges = FlaskCapacity;
            EnsureFlask();
            HasRun = true;
            SetScreen(GameScreen.None);
            StartCoroutine(EnterWorld(true));
        }

        void ResetWorld()
        {
            StopAllCoroutines();
            foreach (var projectile in FindObjectsOfType<Projectile>()) Destroy(projectile.gameObject);
            foreach (var orb in FindObjectsOfType<SkillOrb>()) Destroy(orb.gameObject);
            MapFog.Clear();
            EquippedItems = new ItemDefinition[3];
            SelectedEquipment = 0;
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
            Crystals = 0;
            FlaskCapacity = FlaskCharges = ManaFlaskCapacity = ManaFlaskCharges = 0;
            SpeakingNpc = null;
        }

        public void ContinueGame()
        {
            if (!InitializeLevel()) return;
            try
            {
                var data = SaveStore.Read();
                if (data == null || (data.version < 1 || data.version > 4)) { ShowNotice("存档格式不支持"); return; }
                ResetWorld();
                World.RestoreFog(MapFog, data);
                if (data.equippedItems != null)
                    for (int i = 0; i < Mathf.Min(3, data.equippedItems.Length); i++)
                        EquippedItems[i] = items.FirstOrDefault(item => item.id == data.equippedItems[i]);
                SelectedEquipment = Mathf.Clamp(data.selectedEquipment, 0, 2);
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
                    if (item == null) continue;
                    if (item.kind == ItemKind.Currency) Crystals += stack.count;
                    else inventory.slots[stack.slot] = new Inventory.Slot { item = item, count = stack.count };
                }
                Crystals += data.crystals;
                FlaskCapacity = data.version >= 3 ? Mathf.Max(1, data.flaskCapacity) : initialFlaskCapacity;
                FlaskCharges = data.version >= 3 ? Mathf.Clamp(data.flaskCharges, 0, FlaskCapacity) : FlaskCapacity;
                ManaFlaskCapacity = Mathf.Clamp(data.manaFlaskCapacity, 0, FlaskCapacity);
                ManaFlaskCharges = Mathf.Clamp(data.manaFlaskCharges, 0, ManaFlaskCapacity);
                FlaskCharges = Mathf.Min(FlaskCharges, BloodFlaskCapacity);
                EnsureFlask();
                Completed = data.completed;
                RefreshEquipment();
                HasRun = true;
                SetScreen(GameScreen.None);
                StartCoroutine(EnterWorld(true));
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
                manaFlaskCapacity = ManaFlaskCapacity, manaFlaskCharges = ManaFlaskCharges,
                flaskCapacity = FlaskCapacity, flaskCharges = FlaskCharges, crystals = Crystals,
                checkpointRoom = checkpointRoom,
                checkpointId = checkpointId,
                activatedCheckpointIds = activatedCheckpointIds.ToArray(),
                completed = Completed,
                exploredRooms = System.Array.Empty<int>(),
                exploredMapCells = MapFog.Save(),
                equippedItems = EquippedItems.Select(item => item == null ? null : item.id).ToArray(),
                selectedEquipment = SelectedEquipment,
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

        void EnsureFlask()
        {
            if (inventory.Count(flaskItem) == 0) inventory.TryAdd(flaskItem, 1);
            if (System.Array.IndexOf(EquippedItems, flaskItem) < 0) EquippedItems[0] = flaskItem;
        }
        public int ItemCount(ItemDefinition item) => item.kind == ItemKind.ManaFlask ? ManaFlaskCharges : item.kind == ItemKind.RefillableFlask ? FlaskCharges : item.kind == ItemKind.Currency ? Crystals : inventory.Count(item);
        public bool TryCollectItem(ItemDefinition item, int count)
        {
            if (item.kind == ItemKind.Currency) { Crystals += count; return true; }
            return inventory.TryAdd(item, count);
        }
        public bool UseFlask()
        {
            if (Player.Dead || flaskItem.healing <= 0) return false;
            if (Player.Health >= Player.maxHealth) { ShowNotice("生命已满，未消耗血瓶次数"); return false; }
            if (FlaskCharges <= 0) { ShowNotice("血瓶已用尽，到检查点互动可补满"); return false; }
            Player.Heal(flaskItem.healing);
            FlaskCharges--;
            SaveProgress();
            ShowNotice("血瓶恢复了生命，剩余 " + FlaskCharges + " / " + BloodFlaskCapacity);
            return true;
        }
        public bool UseManaFlask()
        {
            if (Player.Dead || ManaFlaskCharges <= 0 || Player.Mana >= Player.maxMana || manaFlaskItem.manaRecovery <= 0) return false;
            Player.RestoreMana(manaFlaskItem.manaRecovery); ManaFlaskCharges--; SaveProgress(); return true;
        }
        public void ChangeFlaskAllocation(int delta)
        {
            if (Screen != GameScreen.Checkpoint) return;
            ManaFlaskCapacity = Mathf.Clamp(ManaFlaskCapacity + delta, 0, FlaskCapacity);
            FlaskCharges = BloodFlaskCapacity; ManaFlaskCharges = ManaFlaskCapacity;
            if (inventory.Count(manaFlaskItem) == 0) inventory.TryAdd(manaFlaskItem, 1);
            if (System.Array.IndexOf(EquippedItems, manaFlaskItem) < 0) EquippedItems[1] = manaFlaskItem;
            SaveProgress();
        }
        public void EquipItem(ItemDefinition item)
        {
            if ((item.healing <= 0 && item.kind != ItemKind.ManaFlask) || inventory.Count(item) == 0) return;
            int slot = System.Array.IndexOf(EquippedItems, item);
            if (slot < 0) slot = System.Array.IndexOf(EquippedItems, null);
            if (slot < 0) slot = SelectedEquipment;
            EquippedItems[slot] = item;
            SelectedEquipment = slot;
            SaveProgress();
            ShowNotice("已装备：" + item.displayName);
        }

        public void CycleEquipment()
        {
            for (int offset = 1; offset <= 3; offset++)
            {
                int slot = (SelectedEquipment + offset) % 3;
                if (EquippedItems[slot] == null || inventory.Count(EquippedItems[slot]) == 0) continue;
                SelectedEquipment = slot;
                SaveProgress();
                return;
            }
        }

        public bool UseEquippedItem()
        {
            if (Paused || Player.Dead) return false;
            var item = EquippedItems[SelectedEquipment];
            if (item == null) { ShowNotice("请先在背包装备道具"); return false; }
            if (item.kind == ItemKind.RefillableFlask) return UseFlask();
            if (item.kind == ItemKind.ManaFlask) return UseManaFlask();
            int slot = System.Array.FindIndex(inventory.slots, s => s != null && s.item == item);
            if (slot < 0) { ShowNotice("道具已用完"); return false; }
            bool used = inventory.Use(slot, Player);
            ShowNotice(used ? "已使用：" + item.displayName : "生命已满，未消耗物品");
            if (used)
            {
                RefreshEquipment();
                SaveProgress();
            }
            return used;
        }

        public void RefreshEquipment()
        {
            for (int i = 0; i < EquippedItems.Length; i++)
                if (EquippedItems[i] != null && !EquippedItems[i].retainWhenEmpty && inventory.Count(EquippedItems[i]) == 0)
                    EquippedItems[i] = null;
            var selected = EquippedItems[SelectedEquipment];
            if (selected != null && inventory.Count(selected) > 0) return;
            for (int offset = 1; offset < EquippedItems.Length; offset++)
            {
                int slot = (SelectedEquipment + offset) % EquippedItems.Length;
                if (EquippedItems[slot] == null || inventory.Count(EquippedItems[slot]) == 0) continue;
                SelectedEquipment = slot;
                break;
            }
        }

        public void ActivateCheckpoint(Checkpoint point)
        {
            foreach (var stack in inventory.slots.ToArray())
                if (stack != null && stack.item.kind == ItemKind.EmptyFlask)
                { int count = stack.count; FlaskCapacity += count; inventory.TryRemove(stack.item, count); }
            FlaskCharges = BloodFlaskCapacity; ManaFlaskCharges = ManaFlaskCapacity;
            EnsureFlask();
            Player.RestoreMana(Player.maxMana);
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
            bool purchased;
            if (currency.kind == ItemKind.Currency)
            {
                purchased = offer.price >= 0 && Crystals >= offer.price && inventory.TryAdd(offer.item, offer.quantity);
                if (purchased) Crystals -= offer.price;
            }
            else purchased = inventory.TryBuy(offer.item, offer.quantity, currency, offer.price);
            if (!purchased)
            { ShowNotice("交易失败：晶石不足或背包空间不足"); return false; }
            SaveProgress();
            ShowNotice("购入 " + offer.item.displayName + " ×" + offer.quantity);
            return true;
        }

        public void ShowNotice(string message) { Notice = message; NoticeUntil = Time.unscaledTime + 3; }
        public void BeginRespawn() => StartCoroutine(RespawnAfterDelay());
        IEnumerator RespawnAfterDelay()
        {
            var nearest = Checkpoints.OrderBy(p => Vector2.Distance(p.transform.position, Player.transform.position)).FirstOrDefault();
            int challenge = nearest == null ? 0 : nearest.roomId;
            ShowNotice("你倒下了，正在返回检查点……");
            yield return new WaitForSeconds(.7f);
            foreach (var projectile in FindObjectsOfType<Projectile>()) Destroy(projectile.gameObject);
            foreach (var orb in FindObjectsOfType<SkillOrb>()) Destroy(orb.gameObject);
            var initial = Checkpoints.FirstOrDefault(p => p.isInitialSpawn);
            int target = checkpointRoom >= 0 ? checkpointRoom : initial == null ? challenge : initial.roomId;
            foreach (var enemy in Enemies)
                if (enemy.roomId == challenge || enemy.roomId == target)
                {
                    defeatedEnemies.Remove(enemy.persistentId);
                    enemy.ResetEnemy();
                }
            yield return EnterWorld(false);
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
            go.transform.SetParent(World.transform, true);
        }
        public bool CheckpointActivated(string id) => activatedCheckpointIds.Contains(id);
        public void RecordPickup(string id) => collectedPickups.Add(id);
        public bool PickupCollected(string id) => collectedPickups.Contains(id);
        public bool LandmarkDiscovered(string id) => discoveredLandmarks.Contains(id);
        public bool EnemyDefeated(string id) => defeatedEnemies.Contains(id);
        public void RecordEnemyDeath(string id, int crystalReward = 0)
        {
            if (!defeatedEnemies.Add(id)) return;
            Crystals += crystalReward;
            foreach (var arena in BossArenas)
                if (arena.boss.persistentId == id) arena.Victory();
            SaveProgress();
        }

        IEnumerator EnterWorld(bool resetActors)
        {
            Transitioning = true;
            Time.timeScale = 0;
            Player.ClearInput();
            Player.ReleaseGrapple();
            var body = Player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            yield return FadeTo(1);
            if (resetActors)
                foreach (var enemy in Enemies)
                {
                    enemy.ResetEnemy();
                    if (defeatedEnemies.Contains(enemy.persistentId)) enemy.gameObject.SetActive(false);
                }
            foreach (var arena in BossArenas) arena.ResetEncounter();
            foreach (var pickup in pickups) pickup.gameObject.SetActive(!collectedPickups.Contains(pickup.persistentId));
            foreach (var point in Checkpoints)
            {
                point.activated = activatedCheckpointIds.Contains(point.persistentId);
                point.GetComponent<SpriteRenderer>().color = point.activated ? new Color(.3f, 1, .85f) : new Color(.25f, .4f, .5f);
            }
            SpeakingNpc = null;
            var checkpoint = Checkpoints.FirstOrDefault(p => p.persistentId == checkpointId);
            if (checkpoint == null && checkpointRoom >= 0) checkpoint = Checkpoints.FirstOrDefault(p => p.roomId == checkpointRoom);
            Transform spawn = checkpoint == null ? World.startPoint : checkpoint.spawnPoint;
            if (!World.TryGetSpawn(spawn, out var position))
            {
                HasRun = false;
                Transitioning = false;
                Fade = 0;
                SetScreen(GameScreen.MainMenu);
                ShowNotice("出生点没有安全地面，请调整检查点的重生点位置。");
                Debug.LogWarning("Spawn blocked: " + (spawn == null ? "未配置重生点" : spawn.name + " at " + spawn.position));
                yield break;
            }
            if (!resetActors) { FlaskCharges = BloodFlaskCapacity; ManaFlaskCharges = ManaFlaskCapacity; }
            Player.Respawn(position);
            Physics2D.SyncTransforms();
            FindObjectOfType<CameraFollow>().Snap();
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



