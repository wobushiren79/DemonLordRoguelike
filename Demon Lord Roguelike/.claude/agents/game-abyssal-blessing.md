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
  - 列：`id`(long) `icon_res`(string) `parent_id`(long) `level`(int) `buff_ids`(string,逗号分隔) `name`(language) `details`(language) `remark`(string) `valid`(int)
  - **`valid`**：`1`=有效，`0`=无效。生成器检测到 `valid` 列即自动生成 `valid!=0` 过滤（`GetAllArrayData`/`GetItemData` 均排除），valid==0 的行运行时彻底不存在、不进候选池。⚠️ 新增行必须填 `1`（JSON 缺省 0 会被当无效）。详见 editor-extension-system SKILL「valid 有效性列约定」。
  - **一个等级 = 一行**；同族升级链：lv1 `parent_id=0`，lv2 `parent_id=lv1.id`，lv3 `parent_id=lv2.id`……`level` 从 1 连续递增（`level=0` 为不可升级的常驻馈赠）
  - **三种类型（仅由 `level`+族内行数决定，无需额外字段）**：① 单级可重复 `level=0,parent_id=0`（重复叠加、无角标）；② 单级不可重复 `level=1,parent_id=0` 且族内仅 1 行（选 1 次后消失、无角标，靠 `IsSingleLevelOnce` 隐藏角标）；③ 多级升级链 `level=1..N` 链式（逐级升级、显示 LvN）。「单级不可重复」是「多级」族内只有 1 级的退化形态，天然不可重复、零额外逻辑。
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
- **BuffHandler.RemoveAbyssalBlessingByRootId(rootId)** - 移除某族的所有馈赠及其 BUFF（升级时用）
- **BuffHandler.GetDefenseCoreUUID** - 馈赠 BUFF 的目标/施加者（防守核心）
- **BuffManager.dicAbyssalBlessingBuffsActivie** - 独立的馈赠 BUFF 容器（key=馈赠实例）
- **BuffManager.AddAbyssalBlessingEntity / AddAbyssalBlessingBuff** - 写入容器
- **BuffManager.ClearAbyssalBlessing** - 清空所有馈赠（只在全通关后调）

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
