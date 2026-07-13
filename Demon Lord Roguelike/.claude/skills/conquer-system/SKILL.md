---
name: conquer-system
description: Demon Lord Roguelike 游戏的征服模式(Conquer)系统开发指南。使用此SKILL当需要创建或修改征服模式战斗流程、多关卡推进、BOSS关逻辑(enemy_boss_ids/attack_boss_num/UIDialogBossShow 特写)、普通敌人波次排程、关卡间深渊馈赠衔接、征服配置表(excel_fight_type_conquer_info)、关卡数/道路数/道路长度的随机区间(x 或 x-y)、难度等级、征服结算与奖励等，包括 GameFightLogicConquer、FightBeanForConquer、FightTypeConquerInfoBean(Partial)、GameWorldInfoRandomBean.SetRandomDataForConquer、FightTypeConquerEditorWindow。
watched_files:
  - Assets/Scripts/Game/Logic/GameFightLogicConquer.cs
  - Assets/Scripts/Bean/Game/FightBeanForConquer.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Dialog/UIDialogBossShow.cs
  - Assets/Scripts/Bean/UI/DialogBossShowBean.cs
  - Assets/Editor/FightTypeConquerEditorWindow.cs
  - Assets/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx
  - Assets/Resources/JsonText/FightTypeConquerInfo.txt
---

# 征服模式 (Conquer) 系统开发指南

## 核心概念

征服模式是游戏的主线 Roguelike 塔防战斗模式：一局由**多个连续关卡**组成，玩家防守魔王核心、布置防御生物抵挡一波波进攻敌人。

- **关卡推进**：`fightNum` 从 1 递增到 `figthNumMax`（最大关卡数，开局随机自 `fight_num` 区间）。
- **最后一关 = BOSS 关**：`fightNum >= figthNumMax`。BOSS 关照常出 `enemy_ids` 普通敌人，并**额外**从 `enemy_boss_ids` 生成 BOSS。
- **关卡间深渊馈赠**：非 BOSS 关胜利后弹出 `UIFightAbyssalBlessing` 三选一（不重载场景、保留战场）；详见 `abyssal-blessing-system` skill。
- **失败 / 通关 BOSS**：弹出 `UIFightSettlement` 结算；通关 BOSS 再进 `UIRewardSelect` 领奖。

### 系统架构

```
GameWorldInfoRandomBean (GameWorldInfoBeanPartial)
    │  SetRandomDataForConquer：创建时把 1~已解锁最高难度逐档随机(roadNum/roadLength/fightNum)缓存到 listDifficultyRandom
    │  SetDifficultyLevel(level)：切换难度时把 roadNum/roadLength/fightNum 同步为该难度缓存值(气泡与战斗都读这些字段)
    ▼
FightBeanForConquer  (征服战斗运行时数据，继承 FightBean)
    │  持有 fightTypeConquerInfo、关卡进度、进攻队列、深渊馈赠
    ▼
GameFightLogicConquer (征服战斗逻辑，继承 GameFightLogic)
    │  状态流转、结算分流、关卡推进、各回调
    ▼
FightTypeConquerInfoBean / Cfg  (征服配置表，按 world_id + level 取行)
```

### 一局完整流程

```
入口(传送门/世界地图) new FightBeanForConquer(gameWorldInfoRandomData)
        │  InitData：取配置、设魔王核心、阵容防御生物、首关进攻数据
        ▼
WorldHandler.EnterGameForFightScene(fightData)  → GameFightLogicConquer 跑起来
        │
        ▼
  ┌──────────────── 每关 Gaming ────────────────┐
  │  普通敌人按 attack_show_time 内排程出怪       │
  │  BOSS 关：中后段额外刷 BOSS + 弹特写 UI       │
  └──────────────────────────────────────────────┘
        │  CheckGameEnd：队列空且无进攻生物 → 胜利；魔王死 → 失败
        ▼
   ChangeGameState(Settlement) → HandleForChangeGameStateSettlement
        │
        ├─ 失败 或 通关BOSS → ClearGameForSimple + UIFightSettlement
        │        └─ Next：通关BOSS→UIRewardSelect领奖→返回基地；失败→返回基地
        │
        └─ 非BOSS关胜利 → OpenAbyssalBlessingUI（不清场）
                 └─ 选择/跳过 → GoToNextLevel
                        ├─ 下一关是BOSS → StartNextGameForBoss（重载BOSS场景）
                        └─ 否则 → ContinueNextLevelInSameScene（同场景继续）
```

---

## FightTypeConquerInfo - 征服配置表

**Excel 源表**：`Assets/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx`（工作表 `FightTypeConquerInfo`，三行表头：字段名/类型/中文注释，数据从第 4 行起）
**导出 JSON**：`Assets/Resources/JsonText/FightTypeConquerInfo.txt`（派生产物，不可单独改）
**取行键**：`world_id` + `level`（`FightTypeConquerInfoCfg.GetItemData(worldId, difficultyLevel)`），一个世界的每个难度等级一行。

### 字段一览

| 字段 | 类型 | 含义 |
|------|------|------|
| `id` | long | 编号（约定 `世界*1000000 + 难度`，如 1000001） |
| `world_id` | long | 世界 ID |
| `fight_scene_ids` | string | 普通关战斗场景列表（`&` 分割，随机取） |
| `fight_scene_boss_ids` | string | BOSS 关战斗场景列表（`&` 分割） |
| `enemy_ids` | string | 普通敌人池 npcInfoId（`&` 分割） |
| `enemy_boss_ids` | string | **BOSS 敌人池** npcInfoId（`&` 分割） |
| `attack_boss_num` | string | **BOSS 数量**，单值 `2` 或区间 `2-5`（空/0 = 不刷 BOSS） |
| `attack_start_num` | int | 第一关敌人数量 |
| `attack_show_time` | float | 单关进攻总时间(秒) |
| `attack_num_addrate` | float | 每关敌人数量倍率 |
| `attack_num_add` | int | 每关额外增加敌人数量 |
| `attack_intensity_addrate` | float | **每关强度倍率**（默认 1；如 1.1 则每关 HP/护甲/攻击力累计 ×1.1，第 N 关 = `rate^(N-1)`；**普通敌人与 BOSS 均适用**，BOSS 关 N = `figthNumMax`） |
| `fight_num` | string | **关卡总数**，单值或区间 `x-y`（开局随机） |
| `road_num` | string | **道路数量**，单值或区间 `x-y` |
| `road_length` | string | **道路长度**，单值或区间 `x-y` |
| `level` | int | 难度等级（与 world_id 联合作键） |
| `drop_crystal` | int | 敌人死亡掉落魔晶 |
| `reward_crystal` | int | 通关奖励魔晶 |
| `reward_equip_rarity` | int | 奖励装备稀有度（只决定稀有度；属性加点数量见 `RarityInfo.equip_attribute_add`） |
| `reward_exp` | int | 普通关通关经验 |
| `reward_exp_boss` | int | BOSS 关通关经验 |
| `reward_reputation` | int | **通关声望奖励**：完整通关（打完 BOSS、领奖结束）按难度增加玩家「声望(reputation)」，需先解锁研究 `UnlockEnum.ConquerReputationReward`；world_id=1 的 10 个难度(level 1~10)依次填 1~10 |
| `remark` | string | 备注 |

### 区间字段约定（x 或 x-y）

`attack_boss_num` / `fight_num` / `road_num` / `road_length` 四个字段均为**字符串区间**：
- 填单个数 `3` → 固定取 3
- 填区间 `2-5` → 闭区间 `[2,5]` 内随机一个整数

解析统一走 `FightTypeConquerInfoBean.ParseRandomRange(value, defaultValue)`（在 Partial 中），封装为：

```csharp
fightTypeConquerInfo.GetRandomFightNum();   // 解析 fight_num
fightTypeConquerInfo.GetRandomRoadNum();    // 解析 road_num
fightTypeConquerInfo.GetRandomRoadLength(); // 解析 road_length
fightTypeConquerInfo.GetRandomBossNum();    // 解析 attack_boss_num（默认 0）
```

> ⚠️ 历史上 `fight_num`/`road_num`/`road_length` 曾是 `_min`/`_max` 两个 int 列，现已**合并为单个 string 区间列**，旧字段已不存在。

---

## FightTypeConquerInfoBean(Partial) - 配置 Bean

**文件**：
- `Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs`（**自动生成，禁改**，由 Excel `生成Entity` 产出）
- `Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs`（**手写扩展**，解析/随机逻辑都写这里）

### Partial 中的关键方法

```csharp
// 随机一个战斗场景（isBoss 决定取 fight_scene_boss_ids 还是 fight_scene_ids）
public long GetRandomFightScene(bool isBoss);

// 随机一个敌人 npcId（isBoss=false 取 enemy_ids；isBoss=true 取 enemy_boss_ids）
public long GetRandomEmenyId(bool isBoss);

// 区间字段解析
public int GetRandomFightNum();
public int GetRandomRoadNum();
public int GetRandomRoadLength();
public int GetRandomBossNum();
public static int ParseRandomRange(string value, int defaultValue = 0);

// 难度背景色：解析 bg_color(十六进制字符串如 "#2ECC71")为 Color，空/失败返回白色
// 用于传送门详情弹窗 UIViewDialogPortalDetailsItem.IconBG。bg_color 为难度表新增列，
// 新增列后需在 Unity 运行配置导出工具重新生成 Bean 才会有该字段
public UnityEngine.Color GetBGColor();

// 通关声望奖励：返回 reward_reputation 字段（完整通关后按难度发放的声望值）。
// reward_reputation 为难度表新增列，需在 Unity 重新导出 Bean 后才存在（仿照 GetBGColor 写法）
public int GetRewardReputation();
```

`FightTypeConquerInfoCfg.GetItemData(worldId, difficultyLevel)` 用 `world_id`+`level` 联合查行。
`FightTypeConquerInfoCfg.GetMaxLevel(worldId)` 返回该世界在配置表中存在的最高难度等级（无配置返回 0），用于传送门详情弹窗判断是否展示未解锁的「下一个难度」预览 item。

---

## FightBeanForConquer - 征服战斗运行时数据

**文件**：`Assets/Scripts/Bean/Game/FightBeanForConquer.cs`（继承 `FightBean`）

### 关卡判定

```csharp
public bool IsBossFight()      => figthNumMax > 0 && fightNum >= figthNumMax;       // 当前是最后一关
public bool IsNextBossFight()  => figthNumMax > 0 && fightNum + 1 >= figthNumMax;   // 下一关是最后一关
```

### 初始化（InitData）

- 取 `fightTypeConquerInfo`（按 worldId+difficultyLevel）
- `sceneRoadNum/Length` 来自 `gameWorldInfoRandomData`
- `figthNumMax = gameWorldInfoRandomData.fightNum`，`fightNum = 1`
- 设魔王核心 `fightDefenseCoreData`、收集**全部**阵容生物到 `dlDefenseCreatureData`（不再按 `== Idle` 过滤：进阶中的魔物在开始进阶时已被移出阵容，其余 Idle/Fight/Rest 残留状态都应参战；旧 Idle 过滤会误伤上一场残留状态的阵容生物、也会缩小深渊馈赠「复制魔物」的随机池）
- `fightSceneId = GetRandomFightScene(IsBossFight())`
- `InitFightAttackData()` 生成首关进攻数据

### 进攻数据生成（InitFightAttackData）—— 含 BOSS 逻辑（重点）

普通波次与 BOSS 出怪先收集为**绝对时间事件**，统一排序后再转成带相对延迟的进攻队列：

1. **普通波次**（BOSS 关与非 BOSS 关一致）：数量 = `CalcCurrentEnemyNum()`（递推 `num = num*addrate + add`，首关 = `attack_start_num`）；把 `[0, attack_show_time]` 均分 `waveNum` 段，每段内随机一个出现时刻；敌人 **始终取 `enemy_ids`**（`GetRandomEmenyId(false)`）。每条普通出怪事件带上**强度倍率** `GetCurrentIntensityRate(fightNum)`（= `attack_intensity_addrate^(fightNum-1)`），创建时作用到 HP/护甲/攻击力（见 `FightCreatureBean.intensityRate`）。
2. **BOSS 关额外出怪**（`AddBossSpawnEvents(spawnEvents, showTime, intensityRate)`）：
   - 数量 = `GetRandomBossNum()`（≤0 直接不刷）
   - 出现时刻 = `Random.Range(showTime*0.5f, showTime*0.9f)`（进攻总时间**中后段 50%~90%**）
   - 多个 BOSS 在该时刻按 `0.3f` 依次错开入场
   - **首个 BOSS 事件携带全部 BOSS 的 npcId**（`bossShowNpcIds`），用于一次性弹 BOSS 特写 UI
   - BOSS 敌人取 `enemy_boss_ids`（`GetRandomEmenyId(true)`）
   - **BOSS 同样享受本关强度倍率** `intensityRate`（与普通敌人取同一 `GetCurrentIntensityRate(fightNum)`，BOSS 关 `fightNum = figthNumMax`），作用到 HP/护甲/攻击力
3. 所有事件按时间排序 → 转 `FightAttackDetailsBean(delay, npcId)` 入队，首个 BOSS 那条带 `bossShowNpcIds`。

> **强度倍率还叠加终焉议会议案**：`InitFightAttackData` 计算出 `GetCurrentIntensityRate(fightNum)` 后，再 `intensityRate *= userTempData.GetEnemyIntensityRate()`（连乘所有在列议案的 `GetEnemyIntensityRate`）。终焉议会「挑战更强/更弱的敌人」议案(`DoomCouncilEntityEnemyIntensity`, ×2/×0.5)即经此叠加，作用于**下一整场征服 run 所有关卡+BOSS**，run 结束时议案自身在 `TriggerGameFightLogicEndGame` 消耗移除。详见 doom-council-system。

### 关卡推进时的数据刷新

```csharp
// 重载场景进 BOSS 关用：保留场上防御生物快照、fightNum++、重选场景、重算进攻数据
public void InitNextData();
// 同场景继续下一关用：不保留生物（仍在场）、fightNum++、重置进攻计时器、重算进攻数据
public void InitNextDataForContinue();
```

---

## GameFightLogicConquer - 征服战斗逻辑

**文件**：`Assets/Scripts/Game/Logic/GameFightLogicConquer.cs`（继承 `GameFightLogic`）

### 结算分流（HandleForChangeGameStateSettlement）

```
胜利 → AddLevelExpForLineupCreature（普通关给 reward_exp，BOSS 关给 reward_exp_boss；满级/魔王 IsDemonLord 跳过不加经验）
失败 或 通关BOSS → ClearGameForSimple() + 打开 UIFightSettlement
非BOSS关胜利     → OpenAbyssalBlessingUI()（不清场，保留防御生物 / BUFF）
```

> `reward_exp` 只是给生物累加成长经验 `CreatureBean.levelExp`（满级不再加）；经验达标后的**升级走"祭坛献祭"**，见 [`sacrifice-system`](../sacrifice-system/SKILL.md) Skill。

> **通关 BOSS 领奖 = 消费预生成奖励（预览即实领）**：`ActionForUIFightSettlementNext` 取 `gameWorldInfoRandomData.GetDifficultyReward(difficultyLevel)` 作基础奖励，调 `RewardSelectBean.InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)`。深渊馈赠「奖励多多」的额外件数 `rewardAddItemNum`（魔晶）在预生成基础奖励**之后追加**；可选数量 `selectNumMax += rewardAddSelectNum` 并钳制到 `listReward.Count`。

### 关卡推进

```csharp
GoToNextLevel()
  ├─ IsNextBossFight() → StartNextGameForBoss()       // InitNextData + 清场 + Mask + 重载BOSS场景
  └─ else              → ContinueNextLevelInSameScene() // InitNextDataForContinue + 重开UIFightMain + 继续Gaming
```

### 主要回调

| 回调 | 时机 / 作用 |
|------|------------|
| `ActionForUIFightAbyssalBlessingSelect(info)` | 选了深渊馈赠 → `FightBeanForConquer.AddAbyssalBlessing` → `GoToNextLevel` |
| `ActionForUIFightAbyssalBlessingSkip()` | 跳过馈赠 → `GoToNextLevel` |
| `ActionForUIFightSettlementNext()` | 结算「下一步」：通关BOSS→**消费预生成奖励**（预览即实领）：取 `gameWorldInfoRandomData.GetDifficultyReward(difficultyLevel)` 作基础奖励，调 `RewardSelectBean.InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)` → `UIRewardSelect.SetData(..., isClearLastGame: true)`（进领奖前 `ClearGame` 卸载BOSS战斗场景，否则战斗场景会叠加残留在领奖场景上）；失败→`EndGameAndReturnToBase` |
| `ActionForUIRewardSelectEnd()` | 领奖结束（=完整通关）：触发 `Achievement_ConquerComplete`(worldId, 难度) + `AddReputationForConquerComplete(fightTypeConquerInfo)` 发放通关声望 → `EndGameAndReturnToBase`（存档前发放，随存档落盘） |

### 通关声望奖励（AddReputationForConquerComplete）

完整通关（`ActionForUIRewardSelectEnd` 领奖结束）时给玩家发放「声望(reputation)」：

```csharp
// 研究门控：先判断是否解锁「征服通关获得声望」研究，再按难度配置发声望
private void AddReputationForConquerComplete(FightTypeConquerInfoBean conquerInfo)
{
    if (!userData.GetUserUnlockData().CheckIsUnlock(UnlockEnum.ConquerReputationReward)) return;
    userData.AddReputation(conquerInfo.GetRewardReputation());   // 声望≤0 不发放
}
```

- 声望值 = `conquerInfo.GetRewardReputation()`（= 难度表 `reward_reputation` 列，world_id=1 各难度依次 1~10）。
- 在 `EndGameAndReturnToBase` **存档前**调用，随 `SaveUserData` 一起落盘。
- 声望系统（第二货币，与魔晶并列，终焉议会消耗它）本已存在于 `UserDataBean.reputation` + `AddReputation(long)`，本功能只是**新增一个获取来源**，未新建声望系统。
- 研究节点/解锁配置见 [`research-system`](../research-system/SKILL.md) 的「征服通关获得声望」节点（unlock_id 100200004，前置=终焉议会 DoomCouncil）。

### 返回基地（EndGameAndReturnToBase）

清深渊馈赠 → 还原阵容生物战斗状态(`RestoreDefenseCreatureFightState`) → `SaveUserData` → 回基地。

---

## BOSS 关特写 UI (UIDialogBossShow)

**文件**：`Assets/Scripts/Component/UI/Dialog/UIDialogBossShow.cs`、数据 `Assets/Scripts/Bean/UI/DialogBossShowBean.cs`（`List<long> npcIds`）

- BOSS 出怪时由 `GameFightLogic.UpdateGameForAttackCreate` 检测到出怪数据带 `bossShowNpcIds` → 调 `GameFightLogic.ShowBossDialog(ids)` → `UIHandler.Instance.ShowDialogBossShow(DialogBossShowBean)`。
- 特写 UI 内部会把 `Time.timeScale` 临时压到 `0.01`（慢镜头），淡入展示约 2 秒后淡出并自动 `DestroyDialog` 还原时间。
- BOSS 生物本身就是普通进攻生物（同 `CreateAttackCreature` 路径），只是 npcId 取自 `enemy_boss_ids`；必须击杀才能清空进攻队列通关。

> 出怪→弹特写的钩子在基类 `GameFightLogic.UpdateGameForAttackCreate` / `ShowBossDialog`，模式无关；征服模式只负责往 `bossShowNpcIds` 填数据。

---

## 进入征服模式（入口）

```csharp
GameWorldInfoRandomBean gameWorldInfoRandomData = ...;        // 世界随机数据(含难度/道路/关卡数)
FightBeanForConquer fightData = new FightBeanForConquer(gameWorldInfoRandomData);
WorldHandler.Instance.EnterGameForFightScene(fightData);      // 加载场景并跑 GameFightLogicConquer
```

实际触发点：
- `Assets/Scripts/Component/UI/Game/BasePortal/UIViewBasePortalItem.cs`（基地传送门）
- `Assets/Scripts/Component/UI/Game/GameWorldMap/UIViewGameWorldMapPoint.cs`（世界地图节点）
- `Assets/Scripts/Game/Launcher/LauncherTest.cs`（测试直入）

### 随机数据 & 难度

- `GameWorldInfoRandomBean.SetRandomDataForConquer()`（`GameWorldInfoBeanPartial.cs`）：创建传送门时一次性把 `1~已解锁最高难度` 逐档用 `GetRandomRoadNum/RoadLength/FightNum` 随出，缓存进 `listDifficultyRandom`（每档一个 `GameWorldDifficultyRandomBean`），默认难度取已解锁最高。
- `SetDifficultyLevel(level)`：切换/初始化难度时调用，把当前 `difficultyLevel` 及 `roadNum/roadLength/fightNum` 同步为该难度缓存的随机值——`FightBeanForConquer` 与传送门详情气泡 `UIPopupPortalDetails` 都直接读这些字段，故切换难度后必须同步才能反映新难度。
- `GetDifficultyRandom(level)`：取某难度缓存数据，缺失（老存档/仅预览的未解锁难度）时懒生成并缓存，保证同一难度数值稳定。气泡按各 item 自身难度取数。
- 难度解锁存档：`UserUnlockBean.GetUnlockGameWorldConquerDifficultyLevel`。

#### 奖励预生成与冻结（预览即实领）

- **创建即冻结奖励**：`CreateDifficultyRandom` 生成每档 `GameWorldDifficultyRandomBean` 时，除 roadNum/roadLength/fightNum 外，**一并按难度预生成并冻结**该档的通关奖励列表 `listReward`，同时记录 `rewardUnlockSign`（= 生成那一刻的「装备奖励池解锁签名」）。传送门详情气泡展示的奖励就是这份预生成数据，**预览即实领**（领奖时直接消费这份冻结奖励，不再二次随机）。
- `GameWorldInfoRandomBean.GetDifficultyReward(difficulty)`：取该难度预生成奖励。**两种情况会重新生成并刷新签名**：① `listReward` 为空（老存档无此字段）；② 解锁了新魔物掉落致装备奖励池签名变化（`rewardUnlockSign != RewardSelectBean.GetConquerEquipPoolSign()`）。重新生成走 `RewardSelectBean.CreateRewardListForConquer(fightTypeConquerInfo)`。
- **解锁重生成机制**：魔物掉落装备需研究解锁（生物分支 `EquipReward*`）。解锁新魔物掉落后，`GetConquerEquipPoolSign()`（= 可生成装备的已解锁生物模型数量，见 `RewardSelectBean.GetUnlockCreatureModelIdsForEquip()`）变化 → 传送门预生成奖励在下次 `GetDifficultyReward` 时按新池重新生成。
- 奖励生成单一真实源是 `RewardSelectBean`（见 [`fight-reward-system`](../fight-reward-system/SKILL.md)）：`CreateRewardListForConquer(conquerInfo)` 静态产出 `List<ItemBean>`；私有 `CreateItemEquip/CreateItemCrystal` 现吃 `FightTypeConquerInfoBean`。

#### 传送门详情气泡四项预览受「设施」研究门控

- `UIPopupPortalDetails`（`Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs`）展示四项预览 + 奖励道具，每项受对应「设施」研究节点解锁门控（`UserUnlock.CheckIsUnlock(UnlockEnum)`，未解锁则该详情项整行隐藏；奖励区不显示）：
  - **线路数(roadNum)** → `UnlockEnum.PortalPreviewRoadNum`(100300002)
  - **关卡数(fightNum)** → `UnlockEnum.PortalPreviewFightNum`(100300003)
  - **路径长度(roadLength)** → `UnlockEnum.PortalPreviewRoadLength`(100300004)，UI 文本 id 414
  - **奖励道具** → `UnlockEnum.PortalPreviewReward`(100300005)，按 `GetDifficultyReward` 预生成奖励实时生成
  - **名字行始终显示**（不门控）。**无尽模式不展示关卡数/路径长度/奖励**。
- 这四个 `UnlockEnum` 在 `excel_research_info` 新增 4 个设施节点（`research_type=1`，`unlock_id` 同上）+ `excel_unlock_info` + 多语言；详见 [`research-system`](../research-system/SKILL.md)。

---

## FightTypeConquerEditorWindow - 配置编辑器

**文件**：`Assets/Editor/FightTypeConquerEditorWindow.cs`（菜单：自定义编辑窗口）

- 提供按 world/难度浏览编辑各行字段；区间字段（`attack_boss_num`/`fight_num`/`road_num`/`road_length`）用字符串输入框。
- 「保存到Excel并生成Json」：用**反射按字段名**对比变更写回 Excel，并 `RegenerateJson` 重新导出 JSON（同样按字段名 + `Convert.ChangeType` 反射赋值）。
- 改字段结构（增删列）后需保证 Excel 表头与 Bean 字段名一致，否则反射赋值会跳过该列。

---

## 关键文件速查

| 功能 | 路径 |
|------|------|
| 征服战斗逻辑 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs` |
| 征服战斗数据 | `Assets/Scripts/Bean/Game/FightBeanForConquer.cs` |
| 战斗基类 / 出怪钩子 | `Assets/Scripts/Game/Logic/GameFightLogic.cs`（UpdateGameForAttackCreate / ShowBossDialog） |
| 进攻队列数据 | `Assets/Scripts/Bean/Game/FightAttackBean.cs`（含 bossShowNpcIds） |
| 配置 Bean（禁改） | `Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs` |
| 配置 Bean 扩展 | `Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs` |
| 随机数据生成 | `Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs`（SetRandomDataForConquer / CreateDifficultyRandom / GetDifficultyReward） |
| 奖励生成单一真实源 | `Assets/Scripts/Bean/Game/RewardSelectBean.cs`（CreateRewardListForConquer / InitDataForReward / GetConquerEquipPoolSign） |
| 传送门详情气泡（四项预览+奖励，研究门控） | `Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs` |
| BOSS 特写 UI | `Assets/Scripts/Component/UI/Dialog/UIDialogBossShow.cs` |
| BOSS 特写数据 | `Assets/Scripts/Bean/UI/DialogBossShowBean.cs` |
| Excel 源表 | `Assets/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx` |
| 导出 JSON | `Assets/Resources/JsonText/FightTypeConquerInfo.txt` |
| 配置编辑器 | `Assets/Editor/FightTypeConquerEditorWindow.cs` |
| 难度解锁存档 | `Assets/Scripts/Bean/Game/UserUnlockBean.cs`（GetUnlockGameWorldConquerDifficultyLevel） |

---

## 注意事项

1. **配置唯一真实源是 Excel**：任何数据变更必须改 `excel_fight_type_conquer_info`，再由 Unity 编辑器（ExcelEditorWindow 或 FightTypeConquerEditorWindow）导出 JSON；只改 JSON 下次导出会被覆盖。
2. **Bean 自动生成禁改**：`FightTypeConquerInfoBean.cs` 改结构要改 Excel 表头后重新「生成Entity」；解析/辅助逻辑写到 `FightTypeConquerInfoBeanPartial.cs`。
3. **区间字段是字符串**：`attack_boss_num`/`fight_num`/`road_num`/`road_length` 用 `x` 或 `x-y`；统一走 `ParseRandomRange`，别再当 int 读。
4. **BOSS 关仍出普通敌人**：普通波次始终取 `enemy_ids`（`GetRandomEmenyId(false)`），BOSS 是 `enemy_boss_ids` 的**额外**敌人，不要把普通波次换成 boss 池。
5. **BOSS 特写只弹一次**：仅「首个 BOSS 出怪事件」带 `bossShowNpcIds`；多 BOSS 不要每只都弹。
6. **关卡推进两条路**：下一关是 BOSS → 重载场景（`StartNextGameForBoss`/`InitNextData`）；否则同场景继续（`ContinueNextLevelInSameScene`/`InitNextDataForContinue`），后者**不要**重建卡片以免丢卡片状态。
7. **结算分流**：非 BOSS 关胜利只弹深渊馈赠、不清场；失败/通关 BOSS 才走 `UIFightSettlement` 完整结算。
8. **存盘前还原状态**：返回基地前必须 `RestoreDefenseCreatureFightState`，否则阵容生物中间状态写进存档会导致回基地「只剩 1 个」。

---

## 关联 Skill 与 Agent

- 传送门世界选择 / 进入 / 难度选择 / 详情气泡（进入征服的上游）：`game-portal` agent + `portal-system` skill
- 战斗逻辑基类 / 其他战斗模式：`game-fight-logic` agent + `game-fight-system` skill
- 关卡间深渊馈赠：`game-abyssal-blessing` agent + `abyssal-blessing-system` skill
- 战斗结算 / 通关领奖 / 掉落：`game-fight-reward` agent + `fight-reward-system` skill
- 进攻/防御生物、魔王核心：`game-creature` agent + `creature-system` skill
- 敌人 AI 行为：`game-ai` agent + `ai-system` skill
- BUFF / 属性管线：`game-buff` agent + `buff-system` skill
- 配置表 Excel 导入导出：`data-excel` agent + `excel-io` skill
- BOSS 特写等弹窗 UI：`ui-dialog` agent + `ui-framework` skill
- 成就统计（征服通关）：`game-achievement` agent + `achievement-system` skill
