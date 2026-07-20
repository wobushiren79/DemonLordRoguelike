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

> **结算行的生物卡片**：`UIViewFightSettlementItem` 预制体内嵌完整 `UIViewCreatureCardItem`（x=-508, scale 1.2），由 `SetCardData(creatureData)` → 卡片自身 `SetData(creatureData, CardUseStateEnum.Show)` 驱动，表现与其他场景卡片完全一致（图标/稀有度/等级/MP/职业图标 + 悬浮弹 `UIPopupCreatureCardDetails`）。名字仍用结算行自己的 `ui_Name_TextMeshProUGUI`（Details/Name 节点）——**字段名不能改成 `ui_Name`**，否则与卡片内部 "Name" 节点撞名，AutoLinkUI 会优先绑到卡片里那个（且在默认隐藏的 NameBg 下），行内名字就不显示了。

> **预览即实领（传送门预生成奖励）**：B 通道的奖励**不是通关时才现生成**，而是在**创建传送门时按难度预生成并冻结**，存到 `GameWorldDifficultyRandomBean.listReward`（详见 `conquer-system`）。传送门详情 `UIPopupPortalDetails` 展示的就是这份预生成奖励，通关 BOSS 领奖时**直接消费同一份**（`gameWorldInfoRandomData.GetDifficultyReward(difficulty)`），保证"预览所见 = 实际所领"。`RewardSelectBean` 退化为奖励生成的**单一真实源**：既被传送门预生成调用，也被通关领奖复用，规则完全一致。

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
RewardSelectBean (奖励生成单一真实源)
    │  生成规则统一吃 FightTypeConquerInfoBean(不再吃 FightBean)
    │  通关领奖: InitDataForReward(预生成基础奖励, conquerInfo, 额外件数) → listReward
    │  传送门预生成/预览: CreateRewardListForConquer(conquerInfo) → List<ItemBean>
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

### RewardSelectBean（奖励数据 + 生成逻辑，奖励生成单一真实源）
- `listReward: List<ItemBean>` 生成的奖励物品列表
- `selectNum / selectNumMax` 已选/可选次数
- `createItemNum`（默认3）/ `createEquipNum`（默认1）/ `createEquipDemonLordRate`（默认0.1）
- **API（全部以 `FightTypeConquerInfoBean` 为奖励配置源，私有 `CreateItemEquip`/`CreateItemCrystal`/`InitRewardList` 均吃 `conquerInfo` 不再吃 `FightBean`）**：
  - `InitData(FightBean fightData, RewardSelectTestData testData = null)`：原签名不变。内部从 `(fightData as FightBeanForConquer)?.fightTypeConquerInfo` 取 `conquerInfo`；测试模式 `InitData(null, testData)` 行为不变。
  - `InitData(FightTypeConquerInfoBean conquerInfo)`：由征服配置直接生成（传送门预生成/预览用，与通关领奖同规则）。
  - `static List<ItemBean> CreateRewardListForConquer(FightTypeConquerInfoBean conquerInfo)`：生成一份奖励列表（传送门创建时预生成奖励即调此）。
  - `InitDataForReward(List<ItemBean> baseReward, FightTypeConquerInfoBean conquerInfo, int extraItemNum)`：用预生成的 `baseReward` 充当 `listReward`（预览=实领）；`baseReward` 为空则容错按 `conquerInfo` 即时生成；其后按 `extraItemNum` 追加装备道具（深渊馈赠「奖励多多」，`CreateItemEquip` 与首件同规则、生成不出装备时兜底魔晶）。**通关领奖走此入口。**
  - `static int GetConquerEquipPoolSign()` / `static List<long> GetUnlockCreatureModelIdsForEquip()`：装备奖励池「解锁签名」= 可生成装备的已解锁生物模型数量。解锁新魔物掉落（生物分支 `EquipReward*` 研究）后签名变化，传送门预生成奖励据此判定是否需重新生成。
  - `static ItemBean CreateEquipItemForReward(long itemId, int rarity, int userType = 0, int addAttributeOverride = -1)`：**装备奖励生成的单一真实源**（从 `CreateItemEquip` 抽出的核心两行）。按指定 `itemId`+`rarity` 生成一个装备道具，属性条数=品质、每条加点数取 `addAttributeOverride`（`<0` 时按 `RarityInfoCfg.GetItemData(rarity).equip_attribute_add`）。私有 `CreateItemEquip` 现复用它（传入已算好的 `addAttribute`/`userType`，行为不变）。供 GM/测试按「指定道具id+稀有度」直接发货（不走解锁生物模型池、不含魔王专属概率）——GM「添加道具」即遍历所有道具×每种稀有度调此。

### RewardSelectTestData（测试模式参数）
仅测试模式（`fightData == null`）使用：`rarity / addAttribute / crystalNum / createEquipNum / createItemNum / selectNumMax / createEquipDemonLordRate`。

## 奖励生成规则（RewardSelectBean，单一真实源）

> `RewardSelectBean` 现为奖励生成的**单一真实源**：传送门创建时预生成、传送门详情预览、通关 BOSS 领奖三处共用同一套规则（私有 `CreateItemEquip`/`CreateItemCrystal`/`InitRewardList` 统一吃 `FightTypeConquerInfoBean`）。

```
InitRewardList(conquerInfo, testData)   // 各 InitData* 入口最终都收口到这里
  1. GetUnlockCreatureModelIdsForEquip()：取已解锁生物，过滤掉无对应装备道具的生物
  2. （测试入口 InitData(null,testData) 先用 testData 覆盖数量参数）
  3. for i in createItemNum:
       if i < createEquipNum:  CreateItemEquip(conquerInfo, ...)   // 优先生成装备
       else:                   CreateItemCrystal(conquerInfo, ...) // 其余生成魔晶
```

**通关领奖消费链（预览即实领）**：`GameFightLogicConquer.ActionForUIFightSettlementNext`（BOSS 关）→ `baseReward = gameWorldInfoRandomData.GetDifficultyReward(difficultyLevel)`（取传送门预生成并冻结的奖励）→ `rewardSelectData.InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)`。其中：
- `baseReward` 直接成为 `listReward`，与传送门详情 `UIPopupPortalDetails` 预览的是同一份。
- 深渊馈赠「奖励多多」额外件数 `rewardAddItemNum` 在基础奖励**之后**追加（追加内容为装备道具，与首件同规则、生成不出装备时兜底魔晶）。
- 再 `selectNumMax += rewardAddSelectNum`（「再来一瓶」），并钳制 `selectNumMax = Min(selectNumMax, listReward.Count)`。

### 装备生成 CreateItemEquip
- 随机挑一个解锁生物 → 取该生物的随机装备道具（无道具则容错改生成魔晶）
- **正常模式**：品质 `rarityItem = fightTypeConquerInfo.reward_equip_rarity`，属性加点数量 `addAttribute = RarityInfoCfg.GetItemData(rarityItem).equip_attribute_add`（由稀有度配置表决定，征服表只控制出什么稀有度）；按 `createEquipDemonLordRate` 概率设为魔王专属 `ItemUserTypeEnum.DemonLord`
- **测试模式**：用 `testData.rarity / addAttribute / createEquipDemonLordRate`
- ⚠️ **道具稀有度白名单过滤（reward_rarity）**：现流程**先**算好目标 `rarityItem`，**再**按道具的 `ItemsInfoBean.IsMatchRewardRarity(rarityItem)` 过滤生物装备池——道具 `reward_rarity` 配了(如 `5,6`)就只在对应稀有度奖励里出现，空=全稀有度适配。过滤后池为空则回退发魔晶（与"无相关道具"一致）。字段/编辑器详见 [item-system](../item-system/SKILL.md)。这也是取随机道具的时机**从"先取件再定稀有度"调整为"先定稀有度再过滤取件"**的原因。
- 末尾算好 `rarityItem/userType/addAttribute` 后收口到 `CreateEquipItemForReward(id, rarityItem, userType, addAttribute)`（内部 `new ItemBean(id, 1, rarityItem, userType)` → `InitRandomAttributeForCreate(addAttribute)`）——该静态方法即装备生成单一真实源，GM/测试也复用。

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
- 声望（第二货币，与魔晶并列，终焉议会消耗）：`UserDataBean.AddReputation(long)`。**完整通关征服**（打完 BOSS、领奖结束 `ActionForUIRewardSelectEnd`）时由 `GameFightLogicConquer.AddReputationForConquerComplete` 按难度发放，受研究 `UnlockEnum.ConquerReputationReward` 门控：已解锁才 `AddReputation(conquerInfo.GetRewardReputation())`（声望值取征服难度表 `reward_reputation`；≤0 不发放）。在 `EndGameAndReturnToBase` 存档前发放，随存档落盘。声望系统本已存在，此处仅新增获取来源；研究节点见 [`research-system`](../research-system/SKILL.md)。
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
| `reward_reputation` | **完整通关声望奖励**：打完 BOSS、领奖结束后按难度给玩家「声望(reputation)」，受研究 `UnlockEnum.ConquerReputationReward` 门控（world_id=1 各难度依次 1~10） |

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
- 注入点在 `GameFightLogicConquer.ActionForUIFightSettlementNext`（BOSS 关分支）。因奖励改为传送门预生成、领奖即"消费"这份冻结奖励，注入方式与旧版不同：
  - **奖励多多（rewardAddItemNum）**：作为额外件数传给 `InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)`，在预生成基础奖励**之后**追加（追加内容为装备道具，与首件同规则、生成不出装备时兜底魔晶）。不再通过预生成前 `createItemNum +=` 实现。
  - **再来一瓶（rewardAddSelectNum）**：`InitDataForReward` **之后** `selectNumMax += rewardAddSelectNum`，并钳制 `selectNumMax = Min(selectNumMax, listReward.Count)` 避免多余次数无箱可开。
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
用 `RewardSelectBean.InitData(null, new RewardSelectTestData(...))` 构造测试数据（此入口签名/行为不变），绕过战斗数据直接预览领奖界面（见 `test-system`）。

### 修改传送门预生成奖励 / 预览
- 奖励的预生成与冻结在 `GameWorldInfoBeanPartial.CreateDifficultyRandom`，存到 `GameWorldDifficultyRandomBean.listReward`（并记录 `rewardUnlockSign`）；取用走 `GameWorldInfoRandomBean.GetDifficultyReward(difficulty)`。生成调的就是本 Skill 的 `RewardSelectBean.CreateRewardListForConquer(conquerInfo)`。完整数据/流程归 [`conquer-system`](../conquer-system/SKILL.md)。
- **重新生成时机**：`listReward` 为空（老存档）或装备奖励池签名变化（`rewardUnlockSign != GetConquerEquipPoolSign()`，即解锁了新魔物掉落）时，`GetDifficultyReward` 会按 `FightTypeConquerInfo` 重新生成并刷新签名。
- 传送门详情 `UIPopupPortalDetails` 的"奖励道具"区直接展示这份预生成奖励（以 `ui_UIViewItem` 为模板的缓存池实时生成 cell），受"设施"研究门控 `UnlockEnum.PortalPreviewReward`(100300005)；无尽模式不展示奖励。门控与 UI 细节归 `conquer-system` / `research-system`。

## 关键文件

| 功能 | 路径 |
|------|------|
| 结算 UI | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs |
| 结算 UI 字段 | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlementComponent.cs |
| 结算单项 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItem.cs |
| 结算进度条 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItemProgress.cs |
| 领奖 UI | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelect.cs |
| 领奖 UI 字段 | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelectComponent.cs |
| 奖励数据/生成（单一真实源） | Assets/Scripts/Bean/Game/RewardSelectBean.cs |
| 传送门预生成奖励/取用 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs（CreateDifficultyRandom / GetDifficultyReward / GameWorldDifficultyRandomBean.listReward+rewardUnlockSign） |
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

- 传送门奖励预生成/冻结、传送门详情预览(UIPopupPortalDetails)、难度随机数据：`conquer-system`
- 设施研究门控（传送门预览各项解锁 `PortalPreview*`）、魔物掉落装备解锁(`EquipReward*`)：`research-system`
- 战斗整体流程/状态机：`game-fight-system`
- 深渊馈赠（关卡间 BUFF 选择，非物品奖励）：`abyssal-blessing-system`
- 装备/道具/背包：`item-system`
- 征服通关成就统计：`achievement-system`
- BUFF 追加掉落：`buff-system`
- 配置表 Excel：`excel-io`
- UI 通用约束：`ui-framework`
- 领奖界面测试：`test-system`
