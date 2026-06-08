---
name: game-sacrifice
description: 生物献祭升级系统开发：CreatureSacrificeLogic 献祭逻辑、CreatureSacrificeBean 献祭数据、UICreatureSacrifice 献祭UI、献祭升级(等级提升/经验门槛/UpLevelForSacrifice)、成功率公式(祭品数量 1/sacrifice_num + 生物id/稀有度惩罚)、保底机制(sacrificePityRate)、祭品装备退回、等级上限、UICreatureManager 升级按钮显隐。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs
  - Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/Game/CreatureAttributeBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Component/UI/Game/CreatureSacrifice/
  - Assets/Scripts/Component/UI/Game/CreatureManager/
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBeanPartial.cs
  - Assets/Resources/JsonText/LevelInfo.txt
---

# 献祭升级系统 (Sacrifice System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与生物献祭升级相关的代码开发。详细机制见 `sacrifice-system` Skill。

## 核心流程

玩家在祭坛把若干「祭品生物」献祭给「目标生物」，按成功率掷骰：成功则目标生物升一级并加属性；失败只消耗祭品并累积保底。

```
UICreatureManager(升级按钮) → GameHandler.StartCreatureSacrifice
  → CreatureSacrificeLogic.StartSacrifice(算成功率+掷骰+动画)
  → SettleSacrifice(退装备/移除祭品 → 成功升级/失败保底 → 存档 → 返回)
```

## 职责范围

### 献祭逻辑
- **CreatureSacrificeLogic** (BaseGameLogic) - 献祭流程、掷骰、`SettleSacrifice` 结算、祭坛动画
- 成功/失败事件：`CreatureSacrifice_SacrificeSuccess` / `CreatureSacrifice_SacrificeFail`

### 升级与数据
- **CreatureBean / CreatureBeanPartial** - `level`/`levelExp`/`sacrificePityRate`(保底)；`UpLevelForSacrifice()`(升级+1ATK) / `CanUpLevel()` / `IsMaxLevel()` / `GetNextLevelInfo()`
- **CreatureAttributeBean** - 升级加点 `dicAttributeLevelUp`(public 保证存档)
- **CreatureSacrificeBean** - `targetCreature` + `fodderCreatures`；测试字段 `isTestMode`(不落盘)/`useManualSuccessRate`(覆盖)/`manualSuccessRate`(手动成功率)

### 成功率公式
- **CreatureUtil** - `GetSacrificeSuccessRate`(保底+祭品,截顶) / `GetSacrificeFoddersRate`(祭品部分)
  - 单祭品基础 = `1/sacrifice_num`；生物 id 不同 ×1/10；稀有度每低一级 ×1/10（可叠加）；总和 + 保底，Clamp01

### 等级配置
- **LevelInfo**(`level_exp` 升级经验 / `sacrifice_num` 祭品基础数量)；Bean 自动生成，扩展写 Partial

### UI
- **UICreatureSacrifice** - 祭品选择、实时成功率显示、开始献祭；祭品选择上限走 `userData.GetUserUnlockData().GetUnlockSacrificeMax()`（基础 5 + `UnlockEnum.SacrificeNum=100100002` 研究等级，满级 15），不要再直接读 `limmitData.sacrificeMax`
- **UICreatureManager** - `RefreshSacrificeButton`：默认隐藏，"解锁祭坛 && CanUpLevel()" 才显示

### 祭品数量研究
- 研究模块「设施」节点 `UnlockEnum.SacrificeNum = 100100002`（前置=开启献祭设施 Altar，`level_max=10`，水晶 1000~10000 每级+1000）提升祭品上限；衍生方法 `UserUnlockBean.GetUnlockSacrificeMax()`。配置见 `excel_research_info`/`excel_unlock_info`/`excel_language` id=100100002。详见 research-system / sacrifice-system Skill。

### 测试模式（不落盘）
- `TestSceneTypeEnum.CreatureSacrifice`：读取某个真实存档数据，对其中一只生物直接发起献祭，可手动成功率或用存档真实数据，结果不写回真实存档。
- 入口 `LauncherTest.StartForCreatureSacrificeTest(slot, uuid, useManualRate, manualRate)`：`UserDataService` 加载存档 → `SetUserData` → 一次性 `World_EnterGameForBaseScene` 等基地就绪 → `GameHandler.StartCreatureSacrifice`。
- `StartSacrifice` 按 `isTestMode && useManualSuccessRate` 决定手动/公式成功率；`SettleSacrifice` 在 `isTestMode` 时跳过 `SaveUserData()`。
- 编辑器配置：`GameTestEditor.DrawCreatureSacrificeTest` / `LoadSacrificeTestCreatures`。详见 `test-system` Skill。

### 关键文件

| 文件 | 路径 |
|------|------|
| 献祭逻辑 | Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs |
| 献祭数据 | Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs |
| 生物升级 | Assets/Scripts/Bean/Game/CreatureBeanPartial.cs |
| 成功率公式 | Assets/Scripts/Utils/CreatureUtil.cs |
| 献祭 UI | Assets/Scripts/Component/UI/Game/CreatureSacrifice/ |
| 升级按钮 | Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs |
| 经验发放 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 等级配置 | Assets/Resources/JsonText/LevelInfo.txt · excel_level_info |

## 约束

- **双门槛**：经验门槛控制能否发起；祭品掷骰控制是否真升级。
- **失败只扣祭品**：经验不退、保底累积；成功才扣经验、清保底。
- 祭品装备**成功/失败都退回背包**；目标生物不能作祭品。
- 升级属性走 `creatureAttribute.dicAttributeLevelUp`（`level` 本身不直接算属性）；当前每级 +1 ATK，后续可优化。
- 配置改 **Excel(excel_level_info)** 唯一真实源；自动生成 Bean 不直接改，扩展写 Partial。
- 献祭逻辑通过事件通知其他系统；UI 继承 BaseUIComponent。
