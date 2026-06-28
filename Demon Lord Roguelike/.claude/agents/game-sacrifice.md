---
name: game-sacrifice
description: 生物献祭升级系统开发：CreatureSacrificeLogic 献祭逻辑、CreatureSacrificeBean 献祭数据、UICreatureSacrifice 献祭UI、献祭升级(等级提升/经验门槛/UpLevelForSacrifice)、成功率公式(同id 1/sacrifice_num + 不同id研究率 + 等级差修正 2^(祭品level-目标level))、保底机制(研究驱动 sacrificePityRate)、不同id/保底研究(SacrificeDifferentIdRate/SacrificePityRate)、祭品装备退回、等级上限、UICreatureManager 升级按钮显隐。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs
  - Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs
  - Assets/Scripts/Bean/Game/CreatureBean.cs
  - Assets/Scripts/Bean/Game/CreatureBeanPartial.cs
  - Assets/Scripts/Bean/Game/CreatureAttributeBean.cs
  - Assets/Scripts/Utils/CreatureUtil.cs
  - Assets/Scripts/Component/UI/Game/CreatureSacrifice/
  - Assets/Scripts/Component/UI/Game/CreatureAddAttribute/
  - Assets/Scripts/Component/UI/Game/CreatureManager/
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/LevelInfoBeanPartial.cs
  - Assets/Resources/JsonText/LevelInfo.txt
---

# 献祭升级系统 (Sacrifice System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与生物献祭升级相关的代码开发。详细机制见 `sacrifice-system` Skill。

## 核心流程

玩家在祭坛把若干「祭品生物」献祭给「目标生物」，按成功率掷骰：成功则目标生物升一级并弹出加点界面手动加属性；失败只消耗祭品并累积保底。

```
UICreatureManager(升级按钮) → GameHandler.StartCreatureSacrifice
  → CreatureSacrificeLogic.StartSacrifice(算成功率+掷骰)
    → 失败:动画前立即 SettleSacrificeData(false) 结算并落盘(防中途退出规避结算) → 动画
    → 成功:动画 → OnSacrificeAnimEnd 内 SettleSacrificeData(true) 升级(只改内存不落盘) → 弹 UICreatureAddAttribute 手动加点
  → 成功:加点确认弹窗后 SaveAndEndGame 才落盘(加点界面强退则不写盘、祭品恢复)；失败:数据已落盘,动画后 EndGame
```

## 职责范围

### 献祭逻辑
- **CreatureSacrificeLogic** (BaseGameLogic) - 献祭流程、掷骰、`SettleSacrificeData` 结算(仅失败时立即落盘,成功只改内存)、`OnSacrificeAnimEnd` 动画后表现收尾、祭坛动画。**失败在动画前即结算落盘防退出规避；成功落盘延后到加点确认后的 `SaveAndEndGame`**
- 成功/失败事件：`CreatureSacrifice_SacrificeSuccess`(→`EventForSacrificeSuccess`，Toast textId 61007、`state=1`) / `CreatureSacrifice_SacrificeFail`(→`EventForSacrificeFail`，Toast textId 61008、`state=0`)
- ⚠️ **`UIHandler.ToastHintText(content, state)` 的 state 约定**：`0`=失败(红色 `ui_other_3` 图标)，`1`=成功(绿色 `ui_other_6` 图标)，默认 `0`。正向反馈传 `1`、负向反馈传 `0`，别传错图标

### 升级与数据
- **CreatureBean / CreatureBeanPartial** - `level`/`levelExp`/`sacrificePityRate`(保底)；`UpLevelForSacrifice()`(升级,**返回本次可分配加点数**=`LevelInfo.attribute_point` 当前全等级配置5、`<=0`兜底1，不再自动加属性) / `CanUpLevel()` / `IsMaxLevel()` / `GetNextLevelInfo()`
- **CreatureAttributeBean** - 升级加点 `dicAttributeLevelUp`(public 保证存档)；`AddAttribute` 支持正负增量(减点用)，**已修复首次加该属性丢失第一次加点的 bug**
- **CreatureSacrificeBean** - `targetCreature` + `fodderCreatures`；测试字段 `isTestMode`(不落盘)/`useManualSuccessRate`(覆盖)/`manualSuccessRate`(手动成功率)

### 成功率公式
- **CreatureUtil** - `GetSacrificeSuccessRate`(保底+祭品,截顶) / `GetSacrificeFoddersRate(target, listFodder, sacrificeNum, differentIdRate)`(祭品部分)
  - 同 id 单祭品 = `1/sacrifice_num`；**不同 id = `differentIdRate`(研究 SacrificeDifferentIdRate 等级×5%，未解锁0；已去掉旧的 ×1/10)**；**等级差修正(替代稀有度判定)：×`Mathf.Pow(2, 祭品.level-目标当前level)`，即高1级×2/低1级×0.5/同级×1，同id/不同id均叠加**；总和 + 保底，Clamp01
  - `GetSacrificeSuccessRate` 内部读 `GetUnlockSacrificeDifferentIdRate()` 传入 `differentIdRate`

### 等级配置
- **LevelInfo**(`level_exp` 升级经验 / `sacrifice_num` 祭品基础数量 / `attribute_point` 升级获得加点数,当前全等级配置5 / `CMP_rate` 魔力召唤倍率 / `level_color` 等级字体颜色,1~10级渐进色)；Bean 自动生成，扩展写 Partial。**`level_color` 当前临时放在 `LevelInfoBeanPartial.cs`(含 `LevelInfoCfg.GetLevelColor(level)` 取色,0级/无配置回退白色)，Excel 重新生成 Bean 后须删除临时字段**

### 升级加点（手动分配）
- 献祭**升级成功后弹出 `UICreatureAddAttribute`** 让玩家手动加点(HP/护甲每点+10、攻击/攻速每点+1)，单点增量取 `CreatureUtil.GetAttributePointAddValue(type)`
- `OnSacrificeAnimEnd` 成功分支：`attributePoint = SettleSacrificeData(true)`(升级,只改内存**不落盘**) → 有点数则 `OpenAddAttributeUI(target, point)`（落盘延迟到加点确认弹窗 `SaveAndEndGame`，加点界面强退则不写盘、祭品恢复）；无点数走 `SaveAndEndGame`(落盘)
- `UICreatureAddAttribute`：`SetData(creature, totalPoint, onConfirm)` → 4 个 `UIViewCreatureAddAttributeItem`(HP/DR/ATK/ASPD) 左减右加，实时作用属性并 `RefreshCard`，`RefreshLimmit` 用 `ui_LimmitText` 显示「剩余点数:{0}」(多语言 textId 61005)；**已去掉 exit 退出按钮，改 `ui_BtnConfirm` 确认**：`OnClickForConfirm` 剩余>0 弹 ToastHintText(textId 61004)拦截，剩余=0 弹 `ShowDialogNormal` 二次确认弹窗(textId 61006)，确认后才 `onConfirm`(=SaveAndEndGame)
- `UIViewCreatureAddAttributeItem.RefreshNum`：步进器数字仅显示已分配「点数」(`allocatedCount`，如 +1)，与单点实际增量(HP/DR +10、ATK/ASPD +1)解耦，各属性统一显示点数

### UI
- **UICreatureSacrifice** - 祭品选择、实时成功率显示、开始献祭；祭品选择上限走 `userData.GetUserUnlockData().GetUnlockSacrificeMax()`（基础 5 + `UnlockEnum.SacrificeNum=100100002` 研究等级，满级 15），不要再直接读 `limmitData.sacrificeMax`；成功率进度条按区间分5段变色（`GetSuccessRateColor` 现仅转发 `ColorUtil.GetProgressColor`：0-20红/20-40橙/40-60黄/60-80浅绿/80-100蓝，DOColor 0.5s 渐变；配色为 ColorUtil 单一真实源，与孵化缸进阶BUFF概率共用）
- **UICreatureAddAttribute** - 升级加点界面(`Assets/Scripts/Component/UI/Game/CreatureAddAttribute/`)，献祭升级成功后弹出
- **UICreatureManager** - `RefreshSacrificeButton`：默认隐藏，"解锁祭坛 && CanUpLevel()" 才显示

### 献祭相关研究（设施节点，前置均=开启献祭设施 Altar，level_max=10）
- `UnlockEnum.SacrificeNum = 100100002`（水晶 1000~10000 每级+1000）提升祭品上限；衍生 `GetUnlockSacrificeMax()`。
- `UnlockEnum.SacrificePityRate = 100100003`（水晶 100,500,1000,5000,1万…10万）失败保底每级+5%；衍生 `GetUnlockSacrificeFailPityAddRate()`(等级×0.05)；`SettleSacrificeData` 失败时 `sacrificePityRate = Clamp01(+=)`，**未解锁不累积**。
- `UnlockEnum.SacrificeDifferentIdRate = 100100004`（同上水晶）不同id祭品成功率每级+5%(默认0)；衍生 `GetUnlockSacrificeDifferentIdRate()`(等级×0.05)。
- 配置见 `excel_research_info`/`excel_unlock_info`/`excel_language` id=100100002/100100003/100100004。详见 research-system / sacrifice-system Skill。

### 测试模式（不落盘）
- `TestSceneTypeEnum.CreatureSacrifice`：读取某个真实存档数据，对其中一只生物直接发起献祭，可手动成功率或用存档真实数据，结果不写回真实存档。
- 入口 `LauncherTest.StartForCreatureSacrificeTest(slot, uuid, useManualRate, manualRate)`：`UserDataService` 加载存档 → `SetUserData` → 一次性 `World_EnterGameForBaseScene` 等基地就绪 → `GameHandler.StartCreatureSacrifice`。
- `StartSacrifice` 按 `isTestMode && useManualSuccessRate` 决定手动/公式成功率；`SettleSacrificeData` 在 `isTestMode` 时跳过 `SaveUserData()`(不落盘)。
- 编辑器配置：`GameTestEditor.DrawCreatureSacrificeTest` / `LoadSacrificeTestCreatures`。详见 `test-system` Skill。

### 关键文件

| 文件 | 路径 |
|------|------|
| 献祭逻辑 | Assets/Scripts/Game/Logic/CreatureSacrificeLogic.cs |
| 献祭数据 | Assets/Scripts/Bean/Game/CreatureSacrificeBean.cs |
| 生物升级 | Assets/Scripts/Bean/Game/CreatureBeanPartial.cs |
| 成功率公式 | Assets/Scripts/Utils/CreatureUtil.cs |
| 献祭 UI | Assets/Scripts/Component/UI/Game/CreatureSacrifice/ |
| 升级加点 UI | Assets/Scripts/Component/UI/Game/CreatureAddAttribute/ (UICreatureAddAttribute + UIViewCreatureAddAttributeItem) |
| 单点增量 | Assets/Scripts/Utils/CreatureUtil.cs (`GetAttributePointAddValue`) |
| 升级按钮 | Assets/Scripts/Component/UI/Game/CreatureManager/UICreatureManager.cs |
| 经验发放 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 等级配置 | Assets/Resources/JsonText/LevelInfo.txt · excel_level_info |

## 约束

- **双门槛**：经验门槛控制能否发起；祭品掷骰控制是否真升级。
- **失败只扣祭品**：经验不退、保底累积；成功才扣经验、清保底。
- 祭品装备**成功/失败都退回背包**；目标生物不能作祭品。
- 升级属性走 `creatureAttribute.dicAttributeLevelUp`（`level` 本身不直接算属性）；升级**不再自动加属性**，改为发放 `LevelInfo.attribute_point`(当前全等级配置5) 点数由玩家在 `UICreatureAddAttribute` 手动加点(HP/护甲+10、攻击/攻速+1)。
- 配置改 **Excel(excel_level_info)** 唯一真实源；自动生成 Bean 不直接改，扩展写 Partial。
- 献祭逻辑通过事件通知其他系统；UI 继承 BaseUIComponent。
