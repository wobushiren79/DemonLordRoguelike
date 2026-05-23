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
    │  注册全局事件、判定成就达成、处理手动领奖
    │
    ├── 监听 Achievement_CreatureKill (bool isAttacker)
    ├── 监听 Achievement_ConquerComplete (int difficultyLevel)
    ├── 监听 Achievement_GameTimeChange
    │
AchievementManager (BaseManager)
    │  缓存排序后的成就列表
    │
UserAchievementBean (用户存档)
    │  achievementStates: Dictionary<long, int>
    │  totalKillCount: long
    │  conquerCompleteCountByLevel: Dictionary<int, long>
    │
AchievementInfoBean (配置)
    │  achievement_type / target_value / target_extra
    │  reward_crystal / name / description / sort
```

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
NotReached --[条件达成,Handler自动判定]--> Reached --[玩家手动点击]--> Unlocked
```

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
| `Achievement_GameTimeChange` | (无) | `GameDataHandler.HandleForBaseDataUpdate` (每秒) |
| `Achievement_StateChange` | `long achievementId` | `UserAchievementBean.SetAchievementState` |
| `Achievement_ProgressChange` | `long achievementId` | `AchievementHandler.CheckAchievementsByType` |

## 关键接口

```csharp
// 手动领奖
AchievementHandler.Instance.TryUnlockAchievement(achievementId);

// 主动重新检查所有成就(UI 打开时调用)
AchievementHandler.Instance.CheckAllAchievements();

// 查询某成就当前进度
long progress = AchievementHandler.Instance.GetAchievementProgress(info);

// 用户数据
var userData = GameDataHandler.Instance.manager.GetUserData();
var achievementData = userData.GetUserAchievementData();
achievementData.GetAchievementState(id);
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
5. 在 `AchievementHandler.GetAchievementProgress` 的 switch 中处理新类型
6. 在 `AchievementHandler.InitData` 注册事件监听
7. 在 `AchievementInfo.txt` 添加配置 + 两套多语言文本
8. 如需在统计页签显示：修改 `UIAchievement.BuildStatisticList`

## 注意事项

- 击杀仅统计**进攻方**生物死亡（`CreatureFightTypeEnum.FightAttack`）
- 征服通关在 `RewardSelectEnd` 触发（玩家真的完成全部关卡且领完奖之后），而不是单关结算
- `UserAchievementBean.achievementStates` 字典只存 Reached/Unlocked，未达成状态不入字典（节省存储）
- 成就奖励直接调用 `userData.AddCrystal(num)`（不走背包道具）
- UI 打开时一定要先 `CheckAllAchievements()`，否则后台未触发事件期间的进度变化无法反映

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
