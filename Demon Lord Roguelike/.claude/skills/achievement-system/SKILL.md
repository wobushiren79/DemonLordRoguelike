---
name: achievement-system
description: Demon Lord Roguelike 游戏的成就系统开发指南。使用此SKILL当需要创建或修改成就配置、成就达成判定、成就解锁(领奖)、成就UI、统计数据(击杀/时长/征服通关)等，包括 AchievementHandler/Manager、UserAchievementBean、UIAchievement(成就/统计双页签)、UIViewAchievementCard、UIViewAchievementStatistic、Achievement_* 事件常量等。
watched_files:
  - Assets/Scripts/Bean/MVC/Game/AchievementInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AchievementInfoBeanPartial.cs
  - Assets/Scripts/Bean/Game/UserAchievementBean.cs
  - Assets/Scripts/Component/Handler/AchievementHandler.cs
  - Assets/Scripts/Component/Manager/AchievementManager.cs
  - Assets/Scripts/Component/UI/Game/Achievement/
  - Assets/Resources/JsonText/AchievementInfo.txt
---

# 成就系统开发指南

## 核心概念

成就系统记录玩家的累计行为（击杀生物、游玩时长、征服通关），达成条件后允许玩家**手动点击领取魔晶奖励**。包含两个页签：成就（5列网格）和统计（一行一条）。

### 解锁前置
成就系统本身需要解锁：`UnlockEnum.Achievement = 100500001`。研究节点 `ResearchInfo.id=100500001` 解锁该功能。入口按钮位于 `UIBaseCore` 的功能按钮组，未解锁时隐藏。

## 系统架构

```
AchievementHandler (BaseHandler)
    │  运行期只累加统计数据; 达成(Reached)在打开 UI 时实时计算; 处理手动领奖并落盘
    │
    ├── 监听 Achievement_CreatureKill (bool isAttacker)  → 仅 AddKillCount
    ├── 监听 Achievement_ConquerComplete (int difficultyLevel) → 仅 AddConquerCompleteCount
    │
    │  GetAchievementState(info) 实时算状态 / GetAchievementProgress(info) 取进度
    │
AchievementManager (BaseManager)
    │  缓存排序后的成就列表
    │
UserAchievementBean (用户存档)
    │  achievementStates: Dictionary<long, int>  // 仅存"已领取(Unlocked=2)", 不存达成
    │  totalKillCount: long
    │  conquerCompleteCountByLevel: Dictionary<int, long>
    │
AchievementInfoBean (配置)
    │  achievement_type / target_value / target_extra
    │  reward_crystal / name / description / sort
```

> **状态模型**：运行期只持续累加统计数据（击杀/通关/时长），**不做任何达成判定、不持久化"达成"状态**。`NotReached`/`Reached` 在打开成就界面时由 `GetAchievementState` 依据"统计数据 vs target_value"实时计算；只有玩家点击领奖后，`Unlocked` 才写入 `achievementStates` 并立即 `SaveUserData()` 落盘。

## 数据类型

### AchievementTypeEnum
- `Kill = 1` 击杀生物（累计）
- `PlayTime = 2` 游玩时间（单位秒，UserData.gameTime）
- `ConquerComplete = 3` 征服模式通关（按难度区分，target_extra=难度等级 1~10）

### AchievementStateEnum
- `NotReached = 0` 未达成（显示灰色蒙版）
- `Reached = 1` 已达成可领取（按钮可点）
- `Unlocked = 2` 已领取奖励（不再可点）

### 状态流转
```
NotReached --[打开UI时按统计数据实时计算]--> Reached --[玩家手动点击领奖,写存档+落盘]--> Unlocked
```
- `NotReached` / `Reached`：**不持久化**，每次打开界面实时计算（统计数据是否 >= target_value）
- `Unlocked`：唯一持久化的状态，仅领奖成功时写入 `achievementStates`

## 成就数据 (AchievementInfo.txt)

ID 编码规则：
- `1000xx` 击杀类（6 条：1/10/100/1000/10000/100000）
- `2000xx` 时长类（10 条：1~10 小时，target_value=秒数）
- `3XX00x` 征服通关（10 难度 × 3 档次=30 条；XX=难度，例：301001=难度1×1次，305002=难度5×10次）

### 文本 ID 段
- `4000001~4000015` 通用 UI 文本
- `4001xxx` 击杀成就 name/description（每条占 2 个 ID）
- `4002xxx` 时长成就
- `4003xxx` 征服成就（难度10 使用 4003001-4003006，难度 1~9 使用 4003<难度>01~4003<难度>06）

新增成就时务必同时更新 `Language_UIText_cn.txt` / `Language_UIText_en.txt`。

## 事件常量 (EventsInfo.cs)

| 事件 | 参数 | 触发位置 |
|------|------|----------|
| `Achievement_CreatureKill` | `bool isAttacker` | `AIIntentCreatureDead.IntentEntering` |
| `Achievement_ConquerComplete` | `int difficultyLevel` | `GameFightLogicConquer.ActionForUIRewardSelectEnd` |

> 这两个事件**仅用于累加统计数据**（`AddKillCount` / `AddConquerCompleteCount`），不触发任何达成判定与 UI 刷新。
>
> 已移除的事件：`Achievement_StateChange` / `Achievement_ProgressChange`（高频空转）、`Achievement_GameTimeChange`（曾用于驱动每秒达成判定，改为打开 UI 时实时计算后已无消费者）。
>
> UI 不做实时刷新：每次打开界面时各卡片用 `GetAchievementState` 实时算状态；领奖成功后由 `UIAchievement.OnClickForUnlockAchievement` 本地 `RefreshAchievementList()` 刷一次。

## 关键接口

```csharp
// 手动领奖(内部校验 Reached -> 发奖 -> 标记 Unlocked -> SaveUserData 落盘)
AchievementHandler.Instance.TryUnlockAchievement(achievementId);

// 实时计算成就状态(已领取读存档, 其余按统计数据 vs target_value 判定)
AchievementStateEnum state = AchievementHandler.Instance.GetAchievementState(info);

// 查询某成就当前进度
long progress = AchievementHandler.Instance.GetAchievementProgress(info);

// 用户数据(运行期只累加 / 查询统计)
var userData = GameDataHandler.Instance.manager.GetUserData();
var achievementData = userData.GetUserAchievementData();
achievementData.IsAchievementUnlocked(id);    // 是否已领取(唯一持久化状态)
achievementData.SetAchievementUnlocked(id);    // 仅领奖成功时调用
achievementData.GetTotalKillCount();
achievementData.GetConquerCompleteCount(difficultyLevel);
```

## UI 结构

```
UIAchievement (BaseUIComponent)
    ├── ui_ViewExit (Button) 返回基地
    ├── ui_RbAchievement (RadioButtonView) 成就 Tab
    ├── ui_RbStatistic (RadioButtonView) 统计 Tab
    ├── ui_TabAchievement (GameObject)
    │   └── ui_ScrollAchievement (ScrollGridVertical)  // 5 列网格
    │       └── tempCell = UIViewAchievementCard
    └── ui_TabStatistic (GameObject)
        └── ui_ScrollStatistic (ScrollGridVertical)    // 1 列列表
            └── tempCell = UIViewAchievementStatistic
```

### 网格 5 列设置
`ScrollGridVertical` 根据 `viewport 宽度 / tempCell 宽度` 自动计算列数。
保证 `viewport.rect.width >= 5 × cellWidth + 4 × cellSpaceX`，列数就会是 5。

### UIViewAchievementCard 状态
- `NotReached` → 灰色蒙版 + "未达成"
- `Reached` → 按钮可点 + "可领取"
- `Unlocked` → 隐藏奖励 + "已解锁"

## 接入新成就类型的流程

1. 在 `AchievementTypeEnum` 新增枚举值
2. 在 `EventsInfo.cs` 新增 `Achievement_*` 事件常量
3. 在源头处 `EventHandler.Instance.TriggerEvent` 派发事件
4. 在 `UserAchievementBean` 添加对应统计字段 + AddXxx/GetXxx 方法
5. 在 `AchievementHandler.GetAchievementProgress` 的 switch 中处理新类型（达成判定就靠它返回的进度 vs target_value）
6. 在 `AchievementHandler.InitData` 注册事件监听（回调里**只累加统计数据**，不要写达成判定）
7. 在 `AchievementInfo.txt` 添加配置 + 两套多语言文本
8. 如需在统计页签显示：修改 `UIAchievement.BuildStatisticList`

## 注意事项

- **运行期零达成判定**：击杀/通关事件回调只 `AddXxx` 累加统计数据；`NotReached`/`Reached` 在打开界面时由 `GetAchievementState` 实时计算，不持久化、不需要每秒/每帧检查
- **只持久化已领取状态**：`achievementStates` 字典仅存 `Unlocked(=2)`；领奖成功后 `TryUnlockAchievement` 会 `SetAchievementUnlocked` + `SaveUserData()` 立即落盘（连同发放的魔晶）。旧存档若残留 `1` 一律按未领取重新计算
- 击杀仅统计**进攻方**生物死亡（`CreatureFightTypeEnum.FightAttack`）；源头（`AIIntentCreatureDead`）**只为进攻方死亡派发** `Achievement_CreatureKill`，减少战斗高峰事件量
- 征服通关在 `RewardSelectEnd` 触发（玩家真的完成全部关卡且领完奖之后），而不是单关结算
- 成就奖励直接调用 `userData.AddCrystal(num)`（不走背包道具）
- `UserAchievementBean` 已从 UserData 主存档**拆分为独立存档** `UserData_{slot}/UserAchievement_{slot}`（字段标 `[JsonIgnore]`，读写封装在 `UserDataService.Save/Load/Delete` 内部）。仍通过 `userData.GetUserAchievementData()` 取数，调用方无感知
- UI 卡片的状态/进度都通过 `AchievementHandler.Instance.GetAchievementState(info)` / `GetAchievementProgress(info)` 取，**不要**直接读存档字典判断达成

## 参考文件

| 模块 | 路径 |
|------|------|
| 配置 Bean | `Assets/Scripts/Bean/MVC/Game/AchievementInfoBean.cs` |
| Bean 扩展 | `Assets/Scripts/Bean/MVC/Game/AchievementInfoBeanPartial.cs` |
| 用户存档 | `Assets/Scripts/Bean/Game/UserAchievementBean.cs` |
| Handler | `Assets/Scripts/Component/Handler/AchievementHandler.cs` |
| Manager | `Assets/Scripts/Component/Manager/AchievementManager.cs` |
| 主 UI | `Assets/Scripts/Component/UI/Game/Achievement/UIAchievement.cs` |
| 成就卡片 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementCard.cs` |
| 统计行 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementStatistic.cs` |
| 数据文件 | `Assets/Resources/JsonText/AchievementInfo.txt` |
