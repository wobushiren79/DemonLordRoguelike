---
name: fight-reward-system
description: Demon Lord Roguelike 游戏的战斗结算奖励系统开发指南。使用此SKILL当需要创建或修改战斗结束后的奖励逻辑，包括战斗结算面板(UIFightSettlement 伤害/击杀/受伤/经验排行榜)、BOSS通关领奖界面(UIRewardSelect 宝箱选择)、奖励生成规则(RewardSelectBean 装备/魔晶)、敌人死亡水晶掉落(FightCreatureEntity.DropCrystal)、战斗统计记录(FightRecordsBean)、奖励入账与存档链路、各战斗模式(征服/终焉议会/测试)结算差异、征服奖励配置(reward_crystal/reward_equip_rarity/drop_crystal)、装备属性加点数量由稀有度配置表(RarityInfo.equip_attribute_add)决定等。
watched_files:
  - Assets/Scripts/Component/UI/Game/FightSettlement/
  - Assets/Scripts/Component/UI/Game/RewardSelect/
  - Assets/Scripts/Bean/Game/RewardSelectBean.cs
  - Assets/Scripts/Bean/Game/FightRecordsBean.cs
  - Assets/Scripts/Bean/Game/FightDropCrystalBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs
---

# 战斗结算奖励系统开发指南

## 核心概念

战斗结束后的"奖励"由**两条完全独立**的通道组成，分析和修改时必须先分清：

| 通道 | 触发时机 | 内容 | 入账方式 |
|------|---------|------|---------|
| **A. 战斗内即时掉落** | 敌人死亡（每关都有） | 水晶 Crystal | 实时拾取直接入账 |
| **B. 结算后领奖界面** | 仅"征服模式 BOSS 关通关" | 装备 + 魔晶 | 玩家在 UIRewardSelect 选择后入账 |

> **最关键的一点**：结算面板 `UIFightSettlement` 本身**不发任何奖励**，它只是战斗过程统计的可排序排行榜（伤害/击杀/受伤/治疗/受疗/经验）。真正的发奖逻辑在 `UIRewardSelect` + `RewardSelectBean`。

## 系统架构

```
战斗结算状态 (GameStateEnum.Settlement)
    │
    ├─ GameFightLogicConquer.HandleForChangeGameStateSettlement
    │     ├─ 失败           → 弹结算UI → Next → 直接返回基地（无奖励）
    │     ├─ 非BOSS关胜利   → 不弹结算UI，直接弹 UIFightAbyssalBlessing（深渊馈赠）
    │     └─ BOSS关胜利     → 弹结算UI → Next → 弹 UIRewardSelect 领奖
    │
    ├─ GameFightLogicDoomCouncil → 弹结算UI(展示投票) → SaveUserData → 返回基地（无领奖）
    │
    └─ GameFightLogicTest → 弹结算UI → Next 重启战斗（不发奖不存档）

UIFightSettlement (结算排行榜，只展示)
    │  数据源: FightBean.fightRecordsData (FightRecordsBean)
    │  6 维度展示: 伤害 / 击杀 / 受伤 / 治疗(输出治疗量) / 受疗(接收治疗量) / 经验
    │  （排序当前仍只接通 4 维: 伤害/击杀/受伤/经验，治疗/受疗只展示进度条未接 OrderFilter）
    │
RewardSelectBean (奖励生成，发奖核心)
    │  InitData() → listReward: List<ItemBean> (装备 + 魔晶)
    │
UIRewardSelect (领奖界面)
    │  3D 宝箱场景 ScenePrefabForRewardSelect，射线点击选择
    │  选中 → userData.AddBackpackItem(itemData)
```

## 数据结构

### FightRecordsBean（战斗统计记录）
挂在 `FightBean.fightRecordsData`，记录整场战斗的统计：
- `totalAddExp` / `totalDamageForDef` / `totalKillNumForDef` 等总量
- `dicRecordsCreatureData: Dictionary<string, FightRecordsCreatureBean>` 每个生物一条
- 写入方法：`AddCreatureExp` / `AddCreatureRegainHP` 等（均通过 `GetRecordsForCreatureData(id, true)` 取或建记录）

### FightRecordsCreatureBean（单生物记录）
- `damage` 造成伤害 / `killNum` 杀敌 / `damageReceived` 受伤 / `exp` 经验
- `regainHP` 输出治疗量(治疗别人) / `regainHPReceived` 接收治疗量(被别人治疗) / `regainDR/regainDRReceived` 护甲恢复
- 总量字段：`totalRegainHPForDef`（输出治疗总量，结算"治疗"进度条 max）/ `totalRegainHPReceivedForDef`（接收治疗总量，结算"受疗"进度条 max）

### RewardSelectBean（奖励数据 + 生成逻辑）
- `listReward: List<ItemBean>` 生成的奖励物品列表
- `selectNum / selectNumMax` 已选/可选次数
- `createItemNum`（默认3）/ `createEquipNum`（默认1）/ `createEquipDemonLordRate`（默认0.1）

### RewardSelectTestData（测试模式参数）
仅测试模式（`fightData == null`）使用：`rarity / addAttribute / crystalNum / createEquipNum / createItemNum / selectNumMax / createEquipDemonLordRate`。

## 奖励生成规则（RewardSelectBean.InitData）

```
InitData(fightData, testData = null)
  1. 取已解锁生物 unlockCreatureModelIds，过滤掉无对应装备道具的生物
  2. 测试模式(fightData==null && testData!=null)：用 testData 覆盖数量参数
  3. for i in createItemNum:
       if i < createEquipNum:  CreateItemEquip(...)   // 优先生成装备
       else:                   CreateItemCrystal(...) // 其余生成魔晶
```

### 装备生成 CreateItemEquip
- 随机挑一个解锁生物 → 取该生物的随机装备道具（无道具则容错改生成魔晶）
- **正常模式**：品质 `rarityItem = fightTypeConquerInfo.reward_equip_rarity`，属性加点数量 `addAttribute = RarityInfoCfg.GetItemData(rarityItem).equip_attribute_add`（由稀有度配置表决定，征服表只控制出什么稀有度）；按 `createEquipDemonLordRate` 概率设为魔王专属 `ItemUserTypeEnum.DemonLord`
- **测试模式**：用 `testData.rarity / addAttribute / createEquipDemonLordRate`
- `new ItemBean(id, 1, rarityItem, userType)` → `InitRandomAttributeForCreate(addAttribute)` 随机属性

### 魔晶生成 CreateItemCrystal
- 基础数量 `itemCrystalNum = fightTypeConquerInfo.reward_crystal`（测试模式用 `testData.crystalNum`）
- 在 `±itemCrystalNum/2` 范围随机浮动
- `new ItemBean(ItemIdEnum.Crystal, itemCrystalNum + randomNum)`

## 领奖界面交互（UIRewardSelect）

- `SetData(rewardSelectData, actionForEnd, isClearLastGame)` → `WorldHandler.EnterRewardSelectScene(isClearLastGame)` 加载独立领奖场景 → `scenePrefab.InitRewardBox(listReward)` 初始化 3D 宝箱
  - `isClearLastGame=true`：进入领奖场景前先 `gameLogic.ClearGame()` 卸载上一场战斗场景并清理战斗实体。**征服模式通关 BOSS 进领奖必须传 true**（`ActionForUIFightSettlementNext` 已传），否则 BOSS 战斗场景不会卸载，会与领奖场景叠加残留；独立测试(LauncherTest)无上一场战斗，保持默认 false。
  - 注意：结算流程里 `ClearGameForSimple()` 只清 AI/BUFF/在途弹道，**不卸载战斗场景**；战斗场景的卸载靠领奖入口的 `isClearLastGame` 或返回基地时的 `ClearWorldData`。
- 点击宝箱 `OnClickForSelectBox`：射线检测命中宝箱 → `scenePrefab.OpenRewardBox` 返回状态：
  - `0` 没有次数 → Toast 提示
  - `1` 打开宝箱 → `userData.AddBackpackItem(itemData)` 入账 + `selectNum++` + 展示道具详情
  - `2` 已打开 → 仅展示道具详情
- 点击跳过 `OnClickForSkip`：若还有未选次数先弹确认框 → `OpenAllRewardBoxPreview()` 展示全部宝箱后回调 `actionForEnd`

## 战斗内水晶掉落（FightCreatureEntity.DropCrystal）

```
生物死亡 → DropCrystal(state)
  → dropCrystal = conquerFightData.fightTypeConquerInfo.drop_crystal
  → FightHandler.manager.GetFightDropCrystalBean(dropCrystal, pos)  (记录 dropperCreatureUUId)
  → lifeTime = FightDropCrystalBean.BASE_LIFE_TIME(30) + 研究加成     (魔晶掉落时长研究 DropCrystalLifeTime 每级+5秒)
  → FightHandler.CreateDropCrystal(fightDropCrystal)                (生成可拾取物)
  → 触发 EventsInfo.GameFightLogic_CreatureDeadDropCrystal          (BUFF 可监听追加掉落)
玩家拾取 → userData.AddCrystal(...) 直接入账
```

> 掉落水晶基础存在时长 = `FightDropCrystalBean.BASE_LIFE_TIME`(30秒)，`DropCrystal` 中显式叠加研究加成 `UserUnlockBean.GetUnlockDropCrystalAddLifeTime()`(强化研究 `UnlockEnum.DropCrystalLifeTime`=200200001，每级+5秒，满级6级+30秒)。显式赋值是为了避免对象池复用残留旧时长。

## 入账与存档链路

- 装备/普通道具：`UserDataBean.AddBackpackItem(itemData)`（特殊道具如水晶内部转 `AddCrystal`）
- 水晶：`UserDataBean.AddCrystal(num)`
- 存档：征服模式统一在 `GameFightLogicConquer.EndGameAndReturnToBase`：
  1. `BuffHandler.manager.ClearAbyssalBlessing()` 清深渊馈赠（单局临时加成）
  2. `GameDataHandler.manager.SaveUserData()` 落盘
  3. `WorldHandler.EnterGameForBaseScene(userData)` 返回基地

## 配置依赖（FightTypeConquerInfoBean）

| 字段 | 含义 |
|------|------|
| `drop_crystal` | 敌人死亡掉落水晶数量（战斗内即时掉落） |
| `reward_crystal` | BOSS 通关领奖魔晶基础数量 |
| `reward_equip_rarity` | 领奖装备品质（稀有度）——只决定出什么稀有度，属性加点数量见 `RarityInfo.equip_attribute_add` |
| `reward_exp` | 普通关卡胜利时给出战阵容生物的经验 |
| `reward_exp_boss` | BOSS 关卡胜利时给出战阵容生物的经验 |

## 征服关卡经验奖励（生物成长经验 levelExp）

> 注意：这是与上文"结算面板经验排行（`FightRecordsBean.exp`）"**完全不同**的另一套经验。结算面板那套统计经验链路仍未接通；本节是给生物**永久成长经验** `CreatureBean.levelExp` 的奖励。

- 征服模式**每关胜利**时，给本场出战阵容生物（`fightData.dlDefenseCreatureData.List`，存档对象引用）累加经验：普通关发 `reward_exp`、BOSS 关发 `reward_exp_boss`。
- 挂钩点：`GameFightLogicConquer.HandleForChangeGameStateSettlement` 中 `isWin` 分支 → `AddLevelExpForLineupCreature(fightDataForConquer, isBossFight)`。每关进入结算时仅触发一次，失败不发。
- 经验直接累加到 `CreatureBean.levelExp`，随 `EndGameAndReturnToBase → SaveUserData` 统一落盘。已满级生物（`IsMaxLevel()`）不再累加经验。
- **经验只是升级门槛，升级本身走"祭坛献祭"**：经验达标后由玩家在基地祭坛献祭祭品掷骰升级（成功才 `level++` 并加属性）。完整升级链路见 [`sacrifice-system`](../sacrifice-system/SKILL.md) Skill。

## 常见开发任务

### 调整 BOSS 通关奖励（装备品质/数量/魔晶）
- 改征服配置表 Excel 源表（`reward_equip_rarity` / `reward_crystal`），在 Unity 编辑器导出 JSON。**禁止只改 JSON**。
- 调装备**属性加点数量**：改稀有度配置表 `excel_rarity_info` 的 `equip_attribute_add`（按稀有度，不在征服表里）。
- 改生成数量逻辑（几件装备/几个魔晶）：改 `RewardSelectBean` 的 `createItemNum` / `createEquipNum` 默认值或生成循环。

### 深渊馈赠对领奖的加成（奖励多多 / 再来一瓶）
- 注入点在 `GameFightLogicConquer.ActionForUIFightSettlementNext`：`new RewardSelectBean()` 后、`InitData(fightData)` **之前** `createItemNum += fightDataForConquer.rewardAddItemNum`（奖励多多，宝箱按 listReward 实时生成会自动多出箱子）；`InitData` **之后** `selectNumMax += rewardAddSelectNum`（再来一瓶），并裁剪 `selectNumMax = Min(selectNumMax, listReward.Count)` 避免多余次数无箱可开。
- 计数器 `rewardAddItemNum / rewardAddSelectNum` 挂在 `FightBeanForConquer`，由两个即时BUFF（`BuffEntityInstantRewardMoreItem` / `BuffEntityInstantRewardMoreSelect`）在选取馈赠时累加。完整机制见 [`abyssal-blessing-system`](../abyssal-blessing-system/SKILL.md) Skill「影响奖励系统的特殊馈赠」一节。

### 调整敌人掉落水晶数量
- 改征服配置 `drop_crystal`（Excel 源表）。

### 新增结算统计维度
1. `FightRecordsCreatureBean` 加字段 + 对应 `FightRecordsBean` 的写入方法
2. 在战斗逻辑/AI 中调用写入方法累计
3. 排序：`UIFightSettlement` 用单个 `OrderBtn` 打开 `UIDialogOrderFilter`（战斗区 `ContentData` 多选战斗维度+按选择顺序定优先级；**固定倒序高值在前，无正/倒序选项**；确认回传 `OrderFilterResultBean`，结算只取 `result.sortTypes`，名字/等级/稀有度筛选不适用）。新增统计维度时：在 `OrderFilterTypeEnum`（`GameStateEnum.cs`）加枚举值 → 弹窗预制体 `ContentData` 下加对应 `UIViewDialogOrderFilterItem` 实例 + `UIDialogOrderFilter` 的 `dataTypes` 数组与 `RegisterSortItem` 登记 → `UIViewDialogOrderFilterItem.GetFilterName` 加内联名文本 → `UIFightSettlement.GetOrderKeySelector` 加该维度排序键 → `ShowOrderFilterDialog` 的 `listFilterType` 放开该项 → `OnConfirmOrderFilter(OrderFilterResultBean result)` 取 `result.sortTypes` 调 `OrderListData(.., false)`。现开放：Damage(伤害50001)/Kill(击杀50002)/DamageReceived(承伤50004)/Exp(经验50003)。治疗(50007)/受疗(50008)目前只在 `UIViewFightSettlementItem` 以进度条展示，尚未接入排序。
4. `UIViewFightSettlementItem` 增加进度条展示

### 接通经验奖励（当前为预留死代码）
- `FightRecordsBean.AddCreatureExp` 与事件 `GameFightLogic_AddExp` 均无调用/触发方 → 结算"经验"维度恒为 0
- 接入：在生物击杀/战斗逻辑里调用 `AddCreatureExp(creatureId, exp)`，并 `TriggerEvent(GameFightLogic_AddExp, exp)`（后者现仅被 `GameHandler` 监听用于终焉议会 `DoomCouncilEntityMoreExp`）

### 测试领奖界面
用 `RewardSelectBean.InitData(null, new RewardSelectTestData(...))` 构造测试数据，绕过战斗数据直接预览领奖界面（见 `test-system`）。

## 关键文件

| 功能 | 路径 |
|------|------|
| 结算 UI | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs |
| 结算 UI 字段 | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlementComponent.cs |
| 结算单项 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItem.cs |
| 结算进度条 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItemProgress.cs |
| 领奖 UI | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelect.cs |
| 领奖 UI 字段 | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelectComponent.cs |
| 奖励数据/生成 | Assets/Scripts/Bean/Game/RewardSelectBean.cs |
| 战斗统计记录 | Assets/Scripts/Bean/Game/FightRecordsBean.cs |
| 掉落水晶实例 | Assets/Scripts/Bean/Game/FightDropCrystalBean.cs |
| 征服配置 Bean | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs |
| 征服配置扩展 | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs |
| 征服结算流程 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 议会结算流程 | Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs |
| 测试结算流程 | Assets/Scripts/Game/Logic/GameFightLogicTest.cs |
| 水晶掉落逻辑 | Assets/Scripts/Game/Fight/FightCreatureEntity.cs（DropCrystal） |
| 事件常量 | Assets/Scripts/Common/EventsInfo.cs（GameFightLogic_*） |
| 入账方法 | Assets/Scripts/Bean/MVC/UserDataBean.cs（AddBackpackItem/AddCrystal） |

## 约束与注意事项

- 结算面板 `UIFightSettlement` 只负责展示统计，**不要在此加发奖逻辑**；其 `OpenUI` 重写会调用 `AudioHandler.Instance.StopMusic()`，在结算界面打开时停止战斗音乐。
- 领奖只在征服 BOSS 通关触发（`ActionForUIFightSettlementNext` 的 `isWin && isBossFight` 分支）；失败/非 BOSS 关/其他模式都不进领奖。
- 配置数据（奖励/掉落数量、品质）变更**必须改 Excel 源表**，再用 Unity 编辑器导出 JSON；仅改 JSON 会被下次导出覆盖。
- `FightTypeConquerInfoBean.cs` 自动生成，**禁止直接修改**；扩展写到 `FightTypeConquerInfoBeanPartial.cs`。
- 存档收口在 `EndGameAndReturnToBase`，先清深渊馈赠再保存 —— 不要在中途调 `ClearAbyssalBlessing`。
- BUFF 追加掉落逻辑（监听 `GameFightLogic_CreatureDeadDropCrystal`）属于 BUFF 系统，走 `buff-system`。
- 所有 C# 方法/属性需 `/// <summary>` 注释并用 `#region` 分类。

## 关联系统

- 战斗整体流程/状态机：`game-fight-system`
- 深渊馈赠（关卡间 BUFF 选择，非物品奖励）：`abyssal-blessing-system`
- 装备/道具/背包：`item-system`
- 征服通关成就统计：`achievement-system`
- BUFF 追加掉落：`buff-system`
- 配置表 Excel：`excel-io`
- UI 通用约束：`ui-framework`
- 领奖界面测试：`test-system`
