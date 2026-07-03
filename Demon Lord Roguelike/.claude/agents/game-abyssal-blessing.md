---
name: game-abyssal-blessing
description: 深渊馈赠系统开发：征服模式关卡间馈赠选择、馈赠 BUFF 添加、深渊馈赠配置表(excel_abyssal_blessing_info)新增、parent_id/level 同族等级链升级替换、AbyssalBlessingInfoBean 配置、AbyssalBlessingInfoCfg.GetFamilyRootId 族根回溯、UIFightAbyssalBlessing 选择界面(RollCandidates 按族取下一级)、UIViewAbyssalBlessingInfoContent 常驻列表、UIPopupAbyssalBlessingInfo 详情气泡、Buff_AbyssalBlessingChange 事件。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: abyssal-blessing-system
watched_files:
  - Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/
  - Assets/Scripts/Component/UI/Common/AbyssalBlessing/
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs
  - Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfoComponent.cs
  - Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx
  - Assets/Resources/JsonText/AbyssalBlessingInfo.txt
---

# 深渊馈赠 (Abyssal Blessing) 开发代理

你负责 `Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/`、`Assets/Scripts/Component/UI/Common/AbyssalBlessing/`、`Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs` 及相关配置的深渊馈赠系统开发。

## 职责范围

### 数据
- **AbyssalBlessingInfoBean** - 馈赠配置 Bean（自动生成，禁改）
- **AbyssalBlessingInfoBeanPartial** - 馈赠配置扩展（写自定义逻辑）
- **AbyssalBlessingEntityBean** - 馈赠运行时实例（含 UUID）

### 配置（Excel + JSON）
- `excel_abyssal_blessing_info[深渊馈赠信息].xlsx` - 唯一真实源（数据写在 **AbyssalBlessingInfo** 工作表，三行表头：字段名/类型/中文说明）
  - 列：`id`(long) `icon_res`(string) `parent_id`(long) `level`(int) `buff_ids`(string,逗号分隔) `name`(language) `details`(language) `remark`(string) `valid`(int) `max_count`(int)
  - **`max_count`**：一局最多可获得（选取）次数，仅对可重复馈赠(`level<=0`)生效；`0`/留空=不限，`N>0`=最多 N 次（`1`=一次性）。升级链(`level>0`)留 0。**新增此列后须在 Unity 跑「生成 Entity」让 Bean 多出字段、再「导出 JSON」**。
  - **`valid`**：`1`=有效，`0`=无效。生成器检测到 `valid` 列即自动生成 `valid!=0` 过滤（`GetAllArrayData`/`GetItemData` 均排除），valid==0 的行运行时彻底不存在、不进候选池。⚠️ 新增行必须填 `1`（JSON 缺省 0 会被当无效）。详见 editor-extension-system SKILL「valid 有效性列约定」。
  - **一个等级 = 一行**；同族升级链：lv1 `parent_id=0`，lv2 `parent_id=lv1.id`，lv3 `parent_id=lv2.id`……`level` 从 1 连续递增（`level=0` 为不参与升级链的可重复馈赠）
  - **两个正交维度**：`level`/`parent_id` 管"强度升级链"，`max_count` 管"一局可获得次数"。类型：① 可重复·不限 `level=0,max_count=0`（重复叠加、无角标，如增殖）；② 可重复·限N次 `level=0,max_count=N`（一局最多N次；`N=1` 即"一次性"，如大力出奇迹等）；③ 多级升级链 `level=1..N` 链式（逐级升级、显示 LvN，`max_count` 留 0）。**「一次性」用 `level=0+max_count=1`，不再用废弃的 `level=1` 单行族**。次数门控落点 `UIFightAbyssalBlessing.IsCandidateEligible` + `BuffHandler.GetAbyssalBlessingPickCount`。
  - id 约定 10 位（如 `2000001005`，末 3 位=等级序号）
- `AbyssalBlessingInfo.txt` - Excel 导出 JSON（不可单独改）
- `Language_AbyssalBlessingInfo_{cn,en}.txt` - 多语言

### UI 组件
- **UIFightAbyssalBlessing** - 征服模式关卡间馈赠选择界面（随机 3 选 1）
- **UIViewFightAbyssalBlessingItem** - 候选项（带等级 BUFF 预览）
- **UIViewAbyssalBlessingInfoContent** - 战斗界面常驻已选馈赠列表
- **UIViewAbyssalBlessingInfoContentItem** - 已选馈赠列表项
- **UIPopupAbyssalBlessingInfo** - 馈赠详情气泡

### 流程入口
- **GameFightLogicConquer.ActionForUIFightSettlementNext** - 关卡间触发选择
- **GameFightLogicConquer.ActionForUIFightAbyssalBlessingSelect/Skip** - 选择/跳过回调
- **GameFightLogicConquer.ActionForUIRewardSelectEnd** - 全通关后清空馈赠
- **FightBeanForConquer.AddAbyssalBlessing** - 添加馈赠到征服数据

### 升级链（核心机制，配置表自身负责）
- **AbyssalBlessingInfoCfg.GetFamilyRootId(id)** - 沿 `parent_id` 回溯到族根（parent_id==0），防循环 64 层 + 缓存
- **AbyssalBlessingInfoCfg.GetFamilyMaxLevel(rootId)** - 族内最大 level（带缓存，level==0 不计入）
- **AbyssalBlessingInfoCfg.IsSingleLevelOnce(info)** - 单级不可重复判定（`level==1` 且 `GetFamilyMaxLevel(族根)==1`），仅用于 UI 隐藏等级角标
- **AbyssalBlessingInfoBean.IsLevelUp()** - `level > 0`
- 升级链**由馈赠表 `parent_id`/`level` 定义，与 BUFF 的 buff_parent_id/buff_level 无关**（旧设计已废弃）

### BUFF 联动
- **BuffHandler.AddAbyssalBlessing** - 添加馈赠：`GetFamilyRootId` → `RemoveAbyssalBlessingByRootId`(移除同族旧级) → 解析 `buff_ids` 加到防守核心 → 触发事件
- **BuffHandler.GetAbyssalBlessingOwnedLevel(rootId)** - 查询某族当前拥有等级（传**族根 id**，0=未拥有）
- **BuffHandler.GetAbyssalBlessingPickCount(rootId)** - 查询某族一局已选取次数（数容器内同族实例数；用于可重复馈赠 `max_count` 候选门控）
- **BuffHandler.RemoveAbyssalBlessingByRootId(rootId)** - 移除某族的所有馈赠及其 BUFF（升级时用）
- **BuffHandler.GetDefenseCoreUUID** - 馈赠 BUFF 的目标/施加者（防守核心）
- **BuffManager.dicAbyssalBlessingBuffsActivie** - 独立的馈赠 BUFF 容器（key=馈赠实例）
- **BuffManager.AddAbyssalBlessingEntity / AddAbyssalBlessingBuff** - 写入容器
- **BuffManager.ClearAbyssalBlessing** - 清空所有馈赠（只在全通关后调）
- **单体定向馈赠（随机一只防守生物属性/攻速翻倍）**：`level=0`，当前均 `max_count=1`（整局限 1 次；改 `max_count` 即可调一局可获次数、与 BUFF 逻辑独立），效果只作用于随机一只防守生物（非全体/非核心）。当前 4 个：大力出奇迹(1000004001/ATK翻倍)、膘肥体壮(1000005001/HP翻倍)、钢铁憨憨(1000006001/DR翻倍)、急性子(1000007001/攻速翻倍即攻击间隔减半)。BUFF 实体 `BuffEntityAttributeSingleTarget`/`BuffEntityAttributeAttackTimeSingleTarget` 实现 `IBuffSingleTarget`(仅暴露 `SingleTargetCreatureUUId`)，`SetData` 时 `fightData.GetRandomDefenseCreatureUUId()`(实例方法在 `FightBean`) 随机锁定一只 UUID；属性类在 `FightCreatureBean.CollectFromBuffList`、攻速类在 `BuffHandler.ChangeAttackTimeDataForBuff` 按 `SingleTargetCreatureUUId` 过滤。**绝不能改 `dlDefenseCreatureData` 里 CreatureBean 的 creatureAttribute（与存档共享引用，会污染永久存档）**，只改运行时 dicAttribute/攻击时间。图标 `ui_abyssalblessing_11~14`。
  - **复制魔物(增殖)不继承单体定向**：`BuffEntityInstantCloneDefenseCreature` 克隆出的魔物是**新 UUID**，与单体定向馈赠锁定的原魔物 UUID 不匹配，故不显示也不继承；克隆体只继承「作用于全体防守生物」的馈赠(靠 `trigger_creature_type` 过滤、与 UUID 无关，新魔物 `RefreshBaseAttribute` 时自动生效)。
  - **战斗卡片展示**：`UIViewCreatureCardItemForFight` 用 `AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature(buff, creatureData, FightDefense)`(在 `Assets/Scripts/Utils/AbyssalBlessingUtil.cs`；trigger_creature_type + 单体定向 UUID + 仅属性/攻速BUFF 三连) 取「实际作用于本魔物」的馈赠图标展示——含全体防守加成，排除敌方/核心/掉落/奖励/复制类。详见 abyssal-blessing-system / buff-system / creature-card-system SKILL。
- **动态数值馈赠（当前用于 都是兄弟/杀红了眼）**：加成率随战况**每次重算属性时动态算**（非配置写死）。BUFF 继承抽象基类 `BuffEntityAttributeDynamicRate : BuffEntityAttribute`（重写 `CollectModifiers`+`ChangeData` 用 `GetDynamicRate()` 替代固定率、仅走 PercentAdd，用于 ATK/DR/HP；`trigger_creature_type=1` 不含核心）；子类为**通用功能类**（按缩放来源命名、不绑馈赠名，可被其它同功能馈赠复用）。作用范围两类不同：**都是兄弟作用全体防守魔物**；**杀红了眼作用随机锁定的单只防守魔物**（兼实现 `IBuffSingleTarget`）。6 族（每属性一族，均 `level=1~5` 升级链、`parent_id` 链式、`max_count=0`）：
  - 都是兄弟·攻击/护甲/生命：`BuffEntityAttributeScaleByDefenseCount`(data=ATK/DR/HP；通用"随当前场上存活防守魔物数缩放"，当前用于本馈赠)，`rate=(场上存活防守魔物数N-1)×trigger_value_rate`（N 数 `dlDefenseCreatureEntity.List` 中 `!IsDead()`，N≤1 为 0）。馈赠 id `2000008001~005`/`2000009001~005`/`2000010001~005`，BUFF id `3001500001~005`/`3001600001~005`/`3001700001~005`，图标 `ui_abyssalblessing_120/121/122`。
  - 杀红了眼·攻击/护甲/生命：`BuffEntityAttributeScaleByKillCount`(data=ATK/DR/HP；**单体定向**，兼实现 `IBuffSingleTarget`，选取时 `SetData` 用 `GetRandomDefenseCreatureUUId()` 随机锁定一只防守生物，随"该只自身累计击杀数缩放"，当前用于本馈赠)，`rate=fightRecordsData.GetRecordsForCreatureData(锁定UUID,false)?.killNum×trigger_value_rate`（仅魔物击杀；killNum 按 `creatureUUId` 持久累积，该只阵亡后下一关重上场 UUID 不变、加成保留，征服 run 内跨关卡累积）。「只作用锁定那只」的过滤由 `FightCreatureBean.CollectFromBuffList` 的 `IBuffSingleTarget` 落点自动完成，`trigger_creature_type=1` 仅作防守类兜底过滤。馈赠 id `2000011001~005`/`2000012001~005`/`2000013001~005`，BUFF id `3001800001~005`/`3001900001~005`/`3002000001~005`，图标 `ui_abyssalblessing_123/124/125`。
  - 每级 `trigger_value_rate=0.01~0.05`（每只 +1%~5%）；语言 BUFF 用 `{Percentage}` 占位、馈赠逐级写死。**当前 id 进度**：深渊馈赠 BUFF 段最大 `3002000005`（族号到 20）、馈赠多级 id 最大 `2000013005`（族号到 13）、彩色图标编号到 125（下一个 126）。
  - **广播重算（rate 变化才生效，事件驱动）**：泛型守卫 `BuffHandler.HasDynamicRateAbyssalBlessing()`（通用：馈赠池含指定类型/子类 BUFF 才广播，避免普通对局开销）+ 入口 `GameFightLogic.RefreshAllDefenseCreatureAttribute()`（public，刷新防守核心+全体防守魔物 `RefreshBaseAttribute`，由原 `EventForAbyssalBlessingChange` 循环抽出）。两处广播：① `GameFightLogic.EventForGameFightLogicCreatureDeadEnd`（死亡→都是兄弟N减/杀红了眼击杀数增，重算放在 `CheckGameEnd()` 之前）；② `CreatureHandler.CreateDefenseCreatureEntity` 末尾**推送新事件** `EventsInfo.GameFightLogic_DefenseCreatureCreate`（参数 FightCreatureEntity）→ `GameFightLogic.EventForDefenseCreatureCreate` 监听后按守卫广播（放置/增殖；CreatureHandler 只生成、推事件，重算职责归 GameFightLogic）。继承 `BuffEntityAttribute`，天然被卡片展示/`GetAttribute(true)` 判定通过，无需改判定（都是兄弟全体显示；杀红了眼因兼 `IBuffSingleTarget`，仅在锁定那只魔物卡上显示图标）。

### 事件
- **EventsInfo.Buff_AbyssalBlessingChange** - 馈赠变化（参数 AbyssalBlessingEntityBean）

### 图标资源
- **专用图集** `AtlasForAbyssalBlessing.spriteatlas` 存放所有馈赠图标，所有馈赠相关 UI 图标必须放入此图集
- **枚举映射** `SpriteAtlasTypeEnum.AbyssalBlessing`（`Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs`）
- **加载入口** `IconHandler.Instance.SetAbyssalBlessingIcon(iconName, image)` —— 馈赠图标统一走此方法，禁止用 `SetUIIcon`

## 关键流程

```
关卡结算 → 非最后一关 → 打开 UIFightAbyssalBlessing.SetData
                          ↓
          RollCandidates(SHOW_NUM=3)：按 GetFamilyRootId 分族 →
          每族取"当前等级+1"那一行(未拥有取族根) → 洗牌取前 3
                          ↓
                       玩家选择 3 选 1（或跳过）
                          ↓
          FightBeanForConquer.AddAbyssalBlessing(info)
                          ↓
          new AbyssalBlessingEntityBean(info)   // 构造函数自动生成 UUID
                          ↓
          BuffHandler.AddAbyssalBlessing(entity)
            ↓
            GetFamilyRootId → RemoveAbyssalBlessingByRootId(移除同族旧级)
            ↓
            解析 buff_ids(逗号分隔) → 添加到防守核心
            ↓
            触发 Buff_AbyssalBlessingChange 事件
            ↓
          UIViewAbyssalBlessingInfoContent 刷新

关卡全通关 → 领奖结束 → BuffHandler.manager.ClearAbyssalBlessing()
```

## 等级链替换机制（重点）

升级链**由馈赠配置表自身的 `parent_id` + `level` 定义**（链表式，每个等级一条独立配置行，`buff_ids` 只决定该级数值）：
1. 选择界面 `RollCandidates` 用 `GetAbyssalBlessingOwnedLevel(rootId)` 取当前等级，只展示 `level == owned+1` 那一行（玩家看到的即"将获得"的等级）
2. `BuffHandler.AddAbyssalBlessing` 添加时：`GetFamilyRootId(id)` → `RemoveAbyssalBlessingByRootId`(整条移除同族旧级) → 逐个解析 `buff_ids` 加到防守核心
3. `parent_id` 链断裂（某级缺失或指向错误）→ `RollCandidates` 取不到下一级，该族卡住
4. ⚠️ 与 BUFF 的 `buff_parent_id`/`buff_level` **无关**，那是旧设计已废弃

## 关键文件

| 文件 | 路径 |
|------|------|
| 馈赠配置 Bean | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBean.cs（自动生成，禁改） |
| 馈赠配置扩展 | Assets/Scripts/Bean/MVC/Game/AbyssalBlessingInfoBeanPartial.cs |
| 馈赠运行时实例 | Assets/Scripts/Bean/Game/AbyssalBlessingEntityBean.cs |
| Excel 源表 | Assets/Data/Excel/excel_abyssal_blessing_info[深渊馈赠信息].xlsx |
| 导出 JSON | Assets/Resources/JsonText/AbyssalBlessingInfo.txt |
| 选择界面 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIFightAbyssalBlessing.cs |
| 候选项 | Assets/Scripts/Component/UI/Game/FightAbyssalBlessing/UIViewFightAbyssalBlessingItem.cs |
| 常驻列表 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContent.cs |
| 常驻项 | Assets/Scripts/Component/UI/Common/AbyssalBlessing/UIViewAbyssalBlessingInfoContentItem.cs |
| 详情气泡 | Assets/Scripts/Component/UI/Popup/UIPopupAbyssalBlessingInfo.cs |
| 图集 | Assets/LoadResources/Textures/SpriteAtlas/AtlasForAbyssalBlessing.spriteatlas |
| 图集枚举 | Assets/FrameWork/Scripts/Enums/BaseGameEnum.cs（SpriteAtlasTypeEnum.AbyssalBlessing） |
| 图标加载入口 | Assets/Scripts/Component/Handler/IconHandler.cs（SetAbyssalBlessingIcon） |
| BUFF 集成 | Assets/Scripts/Component/Handler/BuffHandler.cs（深渊馈赠BUFF region） |
| BUFF 容器 | Assets/Scripts/Component/Manager/BuffManager.cs |
| 征服流程 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 数据持有 | Assets/Scripts/Bean/Game/FightBeanForConquer.cs |
| 动态率馈赠BUFF（当前用于 都是兄弟/杀红了眼） | Assets/Scripts/Game/Buff/BuffEntity/Attribute/BuffEntityAttributeDynamicRate.cs（基类）/ BuffEntityAttributeScaleByDefenseCount.cs（通用"随场上魔物数缩放"，当前用于都是兄弟）/ BuffEntityAttributeScaleByKillCount.cs（通用"随累计击杀数缩放"，当前用于杀红了眼） |
| 动态馈赠广播重算 | Assets/Scripts/Game/Logic/GameFightLogic.cs（RefreshAllDefenseCreatureAttribute 全体重算 + 死亡事件在 CheckGameEnd 前按守卫广播 + EventForDefenseCreatureCreate 监听放置事件按守卫广播）/ Assets/Scripts/Component/Handler/BuffHandler.cs（O(1) 缓存守卫 HasDynamicRateAbyssalBlessing 读 BuffManager.hasDynamicRateAbyssalBlessing；缓存在 AddAbyssalBlessing 选取动态率馈赠时单调置 true、ClearAbyssalBlessing 复位）/ Assets/Scripts/Component/Handler/CreatureHandler.cs（CreateDefenseCreatureEntity 末尾只推送 GameFightLogic_DefenseCreatureCreate 事件，不直接重算） |

## 约束

- 配置变更**必须改 Excel**（`excel_abyssal_blessing_info`），由 Unity 编辑器导出 JSON。仅改 JSON 会在下次导出被覆盖。
- `AbyssalBlessingInfoBean.cs` 是自动生成的，**禁止直接修改**；扩展写到 `AbyssalBlessingInfoBeanPartial.cs`。
- 添加馈赠必须经过 `BuffHandler.AddAbyssalBlessing`，**不要直接写 `manager.dicAbyssalBlessingBuffsActivie`**（会跳过同族替换 + 事件通知）。
- 升级链由**馈赠表 `parent_id`+`level`** 定义：`parent_id` 链表式逐级指向上一级 id（lv2→lv1，lv3→lv2），**不是都指向根**；`level` 从 1 连续递增。链断裂该族会卡住。
- 馈赠 BUFF 目标固定为**防守核心**（CreatureFightTypeEnum.FightDefenseCore），施加者也是核心 UUID。
- `ClearAbyssalBlessing` **只能在征服全通关 + 领奖结束后调用**，中途调用会丢失玩家选择。
- `GetAbyssalBlessingOwnedLevel` 必须传**族根 id**（`GetFamilyRootId` 取得），不是任意等级的 id。
- 配置数据写在 Excel 的 **`AbyssalBlessingInfo`** 工作表（不存在 `Sheet1`/`Sort Title`）；改完 Excel 必须用 Unity 编辑器导出 JSON。
- BUFF 具体实体类型 / 触发逻辑 / 属性管线请走 `game-buff` 代理 + `buff-system` SKILL。
- 馈赠图标必须放入 `AtlasForAbyssalBlessing.spriteatlas`，加载只能走 `IconHandler.Instance.SetAbyssalBlessingIcon`；用 `SetUIIcon` 会去 UI 图集查找导致丢图。

## 关联 Skill 与 Agent

- 详细开发指南: [abyssal-blessing-system](../skills/abyssal-blessing-system/SKILL.md)
- BUFF 实体开发: `game-buff` agent + `buff-system` skill
- 征服模式战斗流程: `game-fight-logic` agent + `game-fight-system` skill
- 选择界面 UI 通用约束: `ui-game` agent
- 详情气泡 UI 通用约束: `ui-popup` agent
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
