---
name: game-achievement
description: 成就系统开发：成就配置(击杀/时长/征服通关)、达成判定、手动领奖、统计数据、UIAchievement(双 Tab 网格)。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: achievement-system
watched_files:
  - Assets/Scripts/Bean/MVC/Game/AchievementInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/AchievementInfoBeanPartial.cs
  - Assets/Scripts/Bean/Game/UserAchievementBean.cs
  - Assets/Scripts/Component/Handler/AchievementHandler.cs
  - Assets/Scripts/Component/Manager/AchievementManager.cs
  - Assets/Scripts/Component/UI/Game/Achievement/
  - Assets/Resources/JsonText/AchievementInfo.txt
---

# 成就 (Achievement) 开发代理

你负责 Demon Lord Roguelike 项目的成就系统开发，包括成就配置、达成判定、手动领奖、统计数据展示等。

## 职责范围

### 成就配置
- **AchievementInfoBean** - 成就配置 Bean（achievement_type/target_value/target_extra/**target_world**/reward_crystal/name/description/sort/icon_res/remark）。`target_world`=类型3征服世界id(0=无)，源于 excel 新增列；生成 Entity 前由 Partial 桥接字段临时承载
- **AchievementInfoBeanPartial** - Bean 扩展（GetAchievementType、GetTargetWorldId、GetAllListSorted、target_world 桥接字段 等）
- **AchievementInfo.txt** - 成就数据（JsonText）

### 业务逻辑
- **AchievementHandler** - 单例 Handler；运行期只累加统计数据，`GetAchievementState(info)` 实时算达成状态，`TryUnlockAchievement` 领奖后 `SaveUserData()` 落盘
- **AchievementManager** - 配置缓存
- **UserAchievementBean** - 用户存档（**只存已领取 Unlocked** 的字典、累计击杀、**按世界×难度**通关次数 `conquerCompleteCountByWorldLevel`）

### 事件
- `Achievement_CreatureKill` - 生物被击杀（在 AIIntentCreatureDead，仅进攻方派发）→ 回调只 `AddKillCount`
- `Achievement_ConquerComplete` - 征服模式完整通关（在 GameFightLogicConquer，参数 `long worldId, int difficultyLevel`）→ 回调只 `AddConquerCompleteCount(worldId, difficultyLevel)`
- 注：两事件仅累加统计；运行期不做达成判定、不实时刷新 UI。达成在打开界面时实时计算。已移除 `Achievement_StateChange` / `Achievement_ProgressChange`（高频空转）与 `Achievement_GameTimeChange`（曾驱动每秒判定，现无消费者）

### UI
- **UIAchievement** - 主界面，双 Tab（成就 / 统计），含 UIAchievementComponent partial
- **UIViewAchievementCard** - 成就卡片，5 列网格 cell，3 状态显示
- **UIViewAchievementStatistic** - 统计列表 cell，一行一条

### 解锁前置
- `UnlockEnum.Achievement = 100500001`
- 研究节点 ResearchInfo.id=100500001，挂在终焉议会研究节点旁边
- 入口按钮 `ui_ViewBaseCoreItemFunction_Achievement` 位于 `UIBaseCore`

## 关键文件

| 文件 | 路径 |
|------|------|
| 配置 Bean | `Assets/Scripts/Bean/MVC/Game/AchievementInfoBean.cs` |
| Bean 扩展 | `Assets/Scripts/Bean/MVC/Game/AchievementInfoBeanPartial.cs` |
| 用户存档 | `Assets/Scripts/Bean/Game/UserAchievementBean.cs` |
| Handler | `Assets/Scripts/Component/Handler/AchievementHandler.cs` |
| Manager | `Assets/Scripts/Component/Manager/AchievementManager.cs` |
| 主 UI | `Assets/Scripts/Component/UI/Game/Achievement/UIAchievement.cs` |
| 主 UI 字段 | `Assets/Scripts/Component/UI/Game/Achievement/UIAchievementComponent.cs` |
| 成就卡片 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementCard.cs` |
| 成就卡片字段 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementCardComponent.cs` |
| 统计行 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementStatistic.cs` |
| 统计行字段 | `Assets/Scripts/Component/UI/Game/Achievement/UIViewAchievementStatisticComponent.cs` |
| JsonText | `Assets/Resources/JsonText/AchievementInfo.txt` |
| 多语言-中 | `Assets/Resources/JsonText/Language_UIText_cn.txt` |
| 多语言-英 | `Assets/Resources/JsonText/Language_UIText_en.txt` |
| 解锁配置 | `Assets/Resources/JsonText/UnlockInfo.txt` |
| 研究配置 | `Assets/Resources/JsonText/ResearchInfo.txt` |
| 解锁枚举 | `Assets/Scripts/Enums/GameStateEnum.cs` (UnlockEnum.Achievement) |
| 事件常量 | `Assets/Scripts/Common/EventsInfo.cs` (Achievement_*) |
| 挂钩-击杀 | `Assets/Scripts/AI/Creature/AIIntentCreatureDead.cs` |
| 挂钩-征服通关 | `Assets/Scripts/Game/Logic/GameFightLogicConquer.cs` |
| 挂钩-游戏时间 | `Assets/Scripts/Component/Handler/GameDataHandler.cs` |
| 初始化 | `Assets/Scripts/Game/Launcher/BaseLauncher.cs` (AchievementHandler.Instance.InitData()) |

## 工作流程

### 新增成就（已有类型）
1. 在 `AchievementInfo.txt` 追加一条配置（注意 ID 不冲突、`sort` 字段、`name`/`description` 用新分配的文本 ID）
2. 在 `Language_UIText_cn.txt` / `_en.txt` 追加 name + description 两条文本
3. UI 自动通过 `AchievementInfoCfg.GetAllListSorted()` 加载，无需改代码

### 新增成就类型
按 SKILL 中的"接入新成就类型的流程"操作（7 步）。

### 修改奖励
直接改 `AchievementInfo.txt` 的 `reward_crystal` 字段；不必改代码。

### 测试
1. 入口按钮显隐：通过 ResearchInfo 100500001 解锁
2. 击杀成就：进入战斗击杀进攻方生物
3. 时长成就：游玩超过 1 小时
4. 征服成就：完整通关征服模式（必须走完领奖流程）

## 注意事项

- 击杀仅统计进攻方（`CreatureFightTypeEnum.FightAttack`），玩家的防御方阵亡不计入
- 征服通关挂钩在 `ActionForUIRewardSelectEnd`，避免单局结算就计数
- 添加新成就时，征服成就的 `target_world`+`target_extra` 必须对应实际存在的 `FightTypeConquerInfo`(world_id+level)；判定走 `GetConquerCompleteCount(worldId, difficultyLevel)`
- 征服成就改为按**世界×难度**统计：每个世界一套难度1~maxLevel × 1/10/100；目前仅「剑与魔法」(worldId=1) 有征服配置
- 数据文件 (`AchievementInfo.txt`) 是 JSON 数组，追加新元素时务必保持格式正确（无尾逗号）
