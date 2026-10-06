# 预制件使用说明

本目录包含 33 个预制件。说明以当前脚本为准；关卡支持自动初始化，可以使用自己的地图场景，不必保留原 Demo 地图。旧房间相关资产仍保留。

## 通用使用方法

1. 从 Project 拖入预制件，在非 Play 模式下调整位置和 Inspector 参数。
2. 地形、敌人、拾取物、钩点、检查点可以放在自己场景的根层级，也可以自行分组；编辑器不会自动改父节点。主角、系统、相机和屏幕 UI 保留在主场景中。
3. 只修改当前实例时，不要 Apply 到预制件；需要统一修改所有实例时，再修改预制件资产。
4. 调整完成后保存场景。运行时修改通常不会在退出 Play 后保留。

拾取物和检查点在编辑器中由 `ScenePrefabIds` 为场景实例分配独立 ID，并修复当前已加载场景内的重复 ID。不要将领取记录用的实例 ID 写回公共预制件。敌人的 `persistentId` 和 `roomId` 需要单独配置：每个敌人实例使用稳定、非空、唯一的 ID；移动位置时不要更改 ID。`roomId` 用于主角死亡时判断需要重置的挑战区。

### 自建关卡与初始出生点

1. 保留 `DemoGame`、一个主角、带 `CameraFollow` 的游戏相机、UI 和 EventSystem。
2. 打开或新建自己的地图场景。多场景编辑时，右键目标场景标题选择 Set Active Scene，再往 Scene 窗口拖入预制件；也可以直接拖到目标场景的 Hierarchy 中。不同场景需在运行前共同加载。
3. 放入 `Ground` 或 `Platform`，保持 Terrain 层和有效碰撞体。
4. 放入 `Checkpoint`，在这个场景实例上勾选“初始出生点”（`isInitialSpawn`），每关只勾选一个。公共预制件默认不勾选。
5. 将其 `spawnPoint` 重生点子对象放到安全地面上方，保存场景，再开始新游戏。

运行时自动收集已加载场景中的有效关卡对象；没有 `WorldMap` 时自动创建“关卡（自动初始化）”。无需额外创建起点、死亡线对象或固定的地图根节点。未配置死亡线时，使用有效地形最低位置下方 5 单位；未配置地图轮廓时，生成地形整体范围的简化矩形轮廓，这不等同于手绘房间轮廓。已有 `WorldMap` 的轮廓、死亡线、终点配置继续使用；旧起点在未勾选新检查点时兼容保留。终点可不配置。

新游戏从初始检查点的 `spawnPoint` 出生，勾选不会自动激活它；尚未记录其他检查点时，死亡回到初始位置。互动激活其他检查点后，死亡与继续游戏使用保存的检查点。移动检查点根对象会同时移动其重生点。缺失或重复初始点、重生点没有安全地面时，会提示并停留在主菜单。

### 禁用旧地图与相机

- 取消旧地图根对象的 GameObject 激活勾选，才会让它退出运行；眼睛图标只隐藏编辑器显示。
- 禁用的旧地图中的敌人、Boss、检查点、NPC 和拾取物不参与新关卡初始化；当前关卡已死亡或已拾取的实例仍可按存档恢复。
- 运行时保留带 `CameraFollow` 的游戏相机，并停用其他启用且标记为 MainCamera 的相机，避免自建场景的默认相机覆盖画面。这个规则不关闭其他标签的特殊相机。
- 编辑器不再自动重开原 Demo 场景。“洞穴 Demo/打开整体地图”菜单是手动打开原 Demo 的操作。

## Player：主角

| 预制件 | 作用 | 主要配置 |
| --- | --- | --- |
| `Player/Player.prefab` | 主角模型、2D 碰撞与移动、近战、生命、受伤和钩爪能力 | `PlayerMotor` 的速度、跳跃力度、冲刺时间、攻击范围、最大生命和钩爪参数 |
| `Player/SkillOrb.prefab` | 主角远程技能弹体 | 赋给 `PlayerMotor.skillPrefab`；技能发射时由主角设置伤害和速度，`SkillOrb` 配置寿命、检测半径 |

场景中只保留一个主角。不要删除预制件的 `Slash` 子对象，脚本用它表现攻击。

四个能力开关：`enableDoubleJump` 二段跳、`enableWallClimb` 墙面交互、`enableDash` 冲刺、`enableGrapple` 钩爪。关闭墙面交互也会关闭滑墙和蹬墙跳。二段跳和空中冲刺落地恢复，贴墙和连接钩点不恢复次数。

默认操作：A/D 移动、Space 跳跃、Shift 冲刺、W/S 爬墙、J 攻击、K 连接/松开钩点；连接钩点时 Space 跳离。S+Space 穿过支持下落的单向平台；U 发射技能，配合方向键选择方向，无方向输入时按面向发射。E 互动、Q 切换已装备道具、F 使用道具、M 地图、B 背包、Esc 暂停。

远程技能消耗法力，使用 `maxMana`、`skillManaCost`、`skillDamage`、`skillSpeed`、`skillCooldown` 调整。弹体命中地形或敌人后销毁；命中敌人造成伤害。当前法力由蓝瓶、检查点和重生恢复。

## World：地形与互动对象

| 预制件 | 作用 | 使用与配置 |
| --- | --- | --- |
| `World/Ground.prefab` | 实心地面或墙体 | 调整位置和尺寸；保持 Terrain 层及 BoxCollider2D。`GroundSurface.canClimb` 控制是否可攀爬 |
| `World/Platform.prefab` | 较薄的实心平台 | 调整平台长度；同样支持 `canClimb`。当前不是单向平台，不能默认从下方穿过 |
| `World/GrapplePoint.prefab` | 固定钩爪目标 | 放到可达位置，`GrapplePoint.isAvailable` 控制能否抓取；主角按 K 自动选择范围内、符合朝向条件且未被地形遮挡的目标 |
| `World/Checkpoint.prefab` | 初始出生、E 互动休息、恢复生命与法力、补满血蓝瓶、记录重生位置并打开分配界面 | 实例上勾选 `isInitialSpawn` 设置初始点；`spawnPoint` 指向安全重生点子对象。靠近不自动激活 |
| `World/PickupPotion.prefab` | 默认回复药拾取物 | `Pickup.item` 指定物品定义，`count` 指定数量；靠近按 E 拾取 |
| `World/PickupMaterial.prefab` | 默认晶石材料拾取物 | 默认数量为 5；可以替换 `item` 和数量，复用为其他拾取物 |
| `World/PickupEmptyFlask.prefab` | 空血瓶容器拾取物 | 拾取后先进入背包；与检查点互动时按数量消耗容器并增加血蓝瓶共享总容量 |
| `World/DropPlatform.prefab` | 可以从下方穿过、从上方站立的单向平台 | 保留 BoxCollider2D、PlatformEffector2D 和 `DropPlatform`；S+Space 暂时忽略主角与平台碰撞，`dropDuration` 调整忽略时长 |
| `World/DamageWater.prefab` | 持续伤害水域 | 调整触发范围，`damagePerTick` 为每次伤害、`tickInterval` 为间隔；进入后按间隔伤害，离开后计时重置 |
| `World/InstantDeathZone.prefab` | 接触即死区域 | 调整触发范围，主角进入时立即死亡；可用于深坑或特殊危险区域 |
| `World/RoomExit.prefab` | 旧独立房间方案的出口配置 | `targetRoom` 和 `targetEntrance` 保存目标房间与入口编号。当前 `RoomExit` 脚本只有配置字段，整体地图流程不靠它触发场景切换 |

### 拾取物与物品定义

拾取预制件是场景中的实体；`ItemDefinition` 是类型、名称、说明、堆叠上限、生命回复量、法力回复量、颜色、图标和耗尽规则的数据资产。添加新物品时，通过 Create → 空洞原型 → 物品创建定义，放到 `Assets/Resources/Items`，填写唯一 `id`，再赋给拾取预制件。商店也引用同一物品定义。

- 背包容量 16 格，同类物品按定义堆叠，配置上限为 1～99。
- 容量不足时拾取失败，物品保留在场景中；成功领取后记录实例 ID，死亡与继续游戏不重复生成。
- 回复药满血时不消耗。
- `retainWhenEmpty` 勾选：固定道具耗尽后保留装备图标、数量为 0，并显示灰色蒙版；再次获得同类物品后可以使用。
- `retainWhenEmpty` 不勾选：一次性道具耗尽后清空装备槽。
- 普通消耗品耗尽后更新装备状态并自动切换到背包仍有数量的已装备道具；Q 切换按背包数量跳过空槽。血蓝瓶的剩余次数独立于背包数量，当前切换逻辑没有按血蓝瓶次数跳过耗尽瓶。

物品 `kind` 对应行为：

| 类型 | 作用 |
| --- | --- |
| `Consumable` | 普通一次性回复物品，满血不消耗 |
| `Material` | 普通背包材料，当前没有合成流程 |
| `RefillableFlask` | 血瓶，使用独立次数恢复生命，检查点补满 |
| `ManaFlask` | 蓝瓶，使用独立次数恢复法力，检查点补满 |
| `EmptyFlask` | 空容器，检查点互动时转为总容量 |
| `Currency` | 晶石货币，拾取后直接增加货币计数，不占背包格 |

`DemoGame.flaskItem` 和 `manaFlaskItem` 分别引用血瓶和蓝瓶定义，`initialFlaskCapacity` 为新游戏总容量。默认先分配为血瓶；检查点的分配界面调整血瓶与蓝瓶数量，两者共享总容量。分配或休息时补满对应次数。`retainWhenEmpty` 本身只控制装备图标保留，补充机制来自血蓝瓶类型。

## NPCs：对话与商人

| 预制件 | 作用 | 主要配置 |
| --- | --- | --- |
| `NPCs/Guide.prefab` | 向导微光，提供教学与分支对话 | `Npc.displayName` 为名称，`dialogue` 为对话数据；默认不配置商店 |
| `NPCs/Merchant.prefab` | 游商阿砾，通过对话进入商店 | 配置 `dialogue`、`shop`，以及地图发现标记 `mapLandmarkId` |

靠近按 E 开始对话，对话期间暂停游戏。台词和分支使用 `DialogueDefinition`：节点包含说话者、文本和选项；选项通过 `nextNode` 跳转，负值结束对话，`openShop` 打开当前 NPC 的商店。打开商店的 NPC 必须配置 `ShopDefinition`。

商店数据指定货币、售卖物品、数量和价格。货币不足或背包空间不足时交易失败，不扣款。复制商人时，为需要独立发现记录的 NPC 配置不同的 `mapLandmarkId`。

## UI：界面

| 预制件 | 作用 | 使用说明 |
| --- | --- | --- |
| `UI/GameInterface.prefab` | 设置、暂停、背包、商店、地图及通用提示、淡入淡出 | `DemoUI` 管理面板与数据；保留按钮事件和组件引用。地图由 `MapCanvasGraphic` 动态绘制，支持探索迷雾、平移和缩放 |
| `UI/DialogueBox.prefab` | NPC 对话框、说话者、正文和选项列表 | `DialogueCanvas` 根据对话节点刷新，复制选项按钮模板；保留滚动区域与模板引用 |
| `UI/PlayerHUD.prefab` | 玩家生命方块、法力条、晶石数量、三格快捷道具栏、数量与耗尽蒙版 | `PlayerHudCanvas` 刷新数据；中间格为当前道具。绑定 `manaFill`、`crystalPanel`、`crystalCount` 和道具图标引用 |
| `UI/ObjectName.prefab` | 放在场景中的世界空间文字标签 | `WorldLabel.text` 设置文字，`label` 指向 Text；放在 NPC 下时运行时使用 NPC 名称 |
| `UI/BossInterface.prefab` | Boss 出场提示、名称、生命条和胜利提示 | 场景只保留一份；`BossCanvas` 自动读取 `DemoGame.ActiveBoss`，多个竞技场顺序共用 |
| `UI/FlaskAllocation.prefab` | 检查点血瓶与蓝瓶容量分配界面 | `FlaskAllocationCanvas` 绑定数量文本、增加血瓶、增加蓝瓶和关闭按钮；E 互动检查点后显示 |

屏幕界面通过 RectTransform、锚点和布局组件调整，不通过移动 `DemoGame` 的世界坐标定位。世界空间标签随场景对象移动。屏幕 UI 需要主场景的管理器、相机和 EventSystem，不能只拖入孤立空场景就完整运行。

主菜单使用主场景中的 `MainMenuCanvas`，当前没有单独的主菜单预制件。它不在上表的六个 UI 预制件中。确认新游戏时保留菜单背景、禁用菜单按钮并显示确认框，确认后才进入关卡。

## Bosses：Boss 与竞技场

| 预制件 | 作用 | 主要配置 |
| --- | --- | --- |
| `Bosses/ShadowGuard.prefab` | 暗影守卫 Boss 本体 | 使用 `EnemyBrain` 的 Charger 冲撞机制，默认生命 24，预警 0.8 秒、攻击 0.65 秒、恢复 1.2 秒；尚无独立多阶段 Boss AI |
| `Bosses/BossArena.prefab` | 完整竞技场，包含 Boss、出场点、左右封锁屏障和触发区 | `BossArena` 配置 `bossName`、`boss`、`spawnPoint`、`leftBarrier`、`rightBarrier`、`introDuration`、`victoryDuration` |

### 多个 Boss 复用步骤

1. 拖入或复制整个 `BossArena`，调整触发区域、屏障和出场点。保留触发 Collider 的 Trigger 设置。
2. 确认 `BossArena.boss` 指向本竞技场的 Boss，而不是另一个实例。
3. 在 Boss 本体的 `EnemyBrain.persistentId` 填写唯一 ID，例如 `boss_shadow_guard_01`、`boss_shadow_guard_02`。ID 位于 Boss 本体，不在竞技场或 UI 上；复制本体不会自动生成新 ID。
4. 设置 Boss 名称及生命、攻击节奏。需要替换 Boss 时，将新的 `EnemyBrain` 本体放入竞技场，并更新 `boss` 引用。
5. 整个游戏共用一份 `UI/BossInterface`，无需为每个 Boss 复制 UI。

玩家进入触发区后封锁左右屏障，显示出场提示，随后开启 Boss 行为；击败后解除屏障并显示胜利提示。UI 从当前竞技场自动读取名称与血量。当前同时只允许一个竞技场激活，不支持双 Boss 分别显示两条血条。

Boss 击败记录随 `persistentId` 保存；主角死亡会重置尚未击败的遭遇，已记录击败的 Boss 保持完成状态。当前终点判定要求所有参与初始化的 Boss 竞技场都已击败。禁用旧地图中的竞技场不计入这个判定。

## Enemies：十种敌人

### 公共机制与配置

所有敌人使用 `EnemyBrain`。调整 `kind`、`maxHealth`、`moveSpeed`、`detectionRange`、`warningTime`、`attackTime`、`recoveryTime` 和 `baseColor`。不要删除 `Warning`、`Shield` 子对象；脚本会读取它们。

击杀晶石奖励由 `crystalDrops`（`CrystalDropSettings` 公共资产）设置；勾选 `overrideCrystalDrop` 使用本实例的 `crystalDropAmount`。未指定公共资产时也使用本实例数量。致命受击结算奖励，跌落死亡只记录死亡、不发晶石奖励。

普通接触伤害为 1，重锤范围攻击伤害为 2。非致命受击产生短暂硬直与击退，死亡关闭对象并记录 `persistentId`。玩家受伤无敌防止持续接触在每个物理帧重复扣血；一次近战挥击对同一个敌人只结算一次。

主动攻击类型使用 Idle → Warning → Attack → Recovery：预警变黄并显示标记，攻击变红，恢复期变暗。恢复后还有约 0.6 秒的等待窗口。巡逻、追逐和盾甲三种主要使用移动、接触伤害与防御，不进入这套主动攻击阶段。

主动攻击的冲撞、跳跃、弹丸和扑击速度目前有代码固定值，不能仅靠 `moveSpeed` 修改；`moveSpeed` 主要控制普通移动。离玩家超过 20 单位时暂停横向移动或飞行移动。

### 八种地面敌人

| 预制件 | 名称 | 实际机制 | 应对方式 |
| --- | --- | --- | --- |
| `Enemies/Patrol.prefab` | 巡逻虫 | 持续往返，前方检测到墙或没有地面时反向；接触造成伤害，无独立蓄力攻击 | 熟悉近战距离，接近攻击后退出接触范围 |
| `Enemies/Chaser.prefab` | 追逐虫 | 玩家进入侦测范围后朝玩家移动；墙前、悬崖边停止，不自动跨越；接触伤害，无独立蓄力攻击 | 控制距离，利用平台边缘或转向寻找攻击时机 |
| `Enemies/Charger.prefab` | 冲撞虫 | 预警时确定朝向，攻击阶段以水平速度 11 冲撞；遇墙或边缘提前结束攻击，随后恢复 | 蓄力时离开冲撞路径，跳跃躲避，恢复期反击 |
| `Enemies/Hopper.prefab` | 跳跃虫 | 锁定预警时的玩家位置；攻击开始跳一次，水平速度按目标距离限制在 ±5，竖直速度为 9 | 离开锁定落点，侧面攻击；阶段按计时切换，当前不是严格等落地才进入恢复 |
| `Enemies/Shooter.prefab` | 射击虫 | 原地预警后朝确定方向发射一次水平弹丸，弹丸速度为 9；自身不追逐 | 观察朝向，跳过弹丸并接近；弹丸发射后仍需单独躲避 |
| `Enemies/Shield.prefab` | 盾甲虫 | 与巡逻虫一样往返，盾牌跟随朝向；来自正面的攻击被阻挡，背面可受伤；仍有接触伤害 | 绕到背后攻击，避免在正面持续挥击；没有独立蓄力攻击 |
| `Enemies/Hammer.prefab` | 重锤虫 | 玩家距其小于 3 单位时预警；攻击前方约 2.7×1.8 的区域，伤害为 2，攻击标记变红；攻击阶段会持续检测范围 | 引出蓄力后退出前方范围，再利用恢复期靠近攻击 |
| `Enemies/Burrower.prefab` | 钻地虫 | 待机隐藏外观、关闭碰撞并停止重力；触发时移动到玩家附近、限制在出生点左右 5 单位内，显示地面预警；攻击时现身并恢复碰撞 | 看到地面标记立即移动，现身后攻击；隐藏时不能受击。恢复期仍现身，重新待机后再次隐藏 |

巡逻虫、追逐虫和盾甲虫通过射线处理墙与边缘；冲撞虫在冲撞中额外检测边缘。跳跃虫、射击虫、重锤虫和钻地虫没有完整的跨平台寻路逻辑，布置时应提供适合其行为的地形。

### 两种飞行敌人

| 预制件 | 名称 | 实际机制 | 应对方式 |
| --- | --- | --- | --- |
| `Enemies/Pursuer.prefab` | 追踪飞虫 | 无重力，缓慢追向玩家上方约 1.2 单位的位置；玩家距其小于 4 单位时预警，随后沿锁定方向以速度 8 扑击；碰到地形可提前结束攻击 | 预警后改变位置，避免直线扑击，恢复期使用空中攻击 |
| `Enemies/Diver.prefab` | 俯冲飞虫 | 无重力，在出生位置左右约 2 单位巡航；预警时锁定玩家位置并显示地面标记，攻击沿锁定方向以速度 13 飞出；恢复期以速度 5 返回出生位置，未接近出生点时延长恢复 | 离开预警标记对应位置，避开俯冲路线，返航时反击 |

飞行敌人攻击阶段具有地形射线检测，普通飞行没有完整寻路；不要放到封闭墙体内。俯冲飞虫需要上方空间和清晰攻击路线。

### 死亡与存档

- 敌人死亡写入本地进度，继续游戏会恢复死亡记录。
- 主角死亡会重置当前挑战区及检查点所属区域的敌人；当前挑战区通过离主角最近的检查点 `roomId` 判断。
- 没有激活检查点时，重生使用标记的初始检查点；整组关闭的旧地图对象不会被重置。Boss 的完成记录按上文竞技场规则处理。
- 新放置的敌人必须设置独立 `persistentId` 和正确的 `roomId`。当前自动 ID 工具只处理拾取物与检查点，不会为敌人自动生成 ID。
- 改变场景摆放位置不应改变持久化 ID，否则旧存档会把它视为新对象。
