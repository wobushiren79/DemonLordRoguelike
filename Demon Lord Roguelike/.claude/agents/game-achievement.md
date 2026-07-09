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
- **AchievementInfoBean** - 成就配置 Bean（achievement_type/target_extra/**target_world**/icon_res/sort/**name/details**/remark + **target_values/reward_crystals** 两列逗号分隔字符串）。`target_world`=类型3征服世界id(0=无)；两列承载各级目标/奖励。描述用 `name`/`details` 两列**同指一个文本id**(name→content名字, details→content_1模板含`{Name}`)。旧 target_value/reward_crystal/parent_id/level/level_descriptions **已移除**；`details` 生成 Entity 前由 Partial 桥接字段临时承载(生成后须删)
- **AchievementInfoBeanPartial** - Bean 扩展（GetAchievementType、GetTargetWorldId、**GetLevelCount/GetTargetValues/GetRewardCrystals/GetLevelTargetValue/GetLevelReward/GetLevelDescription/FormatValueByType**；Cfg：GetAllListSorted；**details 桥接字段**）。`GetLevelDescription` 取 `details_language`(框架自动属性=content_1, 优先 `_language` 不手写 GetTextById) + `GetTextReplace` 替换占位符→该级格式化目标值；数值同时挂 `{Name}`(通用)与类型语义占位符(击杀`{KillNum}`/时长`{Time_H}`, 见 `GetValueReplaceKey`)

### 单行多级 ⭐
- 可升级成就**一行=一张卡(含多级)**，逐级领取：必先领低一级才能领高一级，全部领完显示**已完成**
- **无 parent_id/level**。每行两列逗号分隔：`target_values`(各级目标)/`reward_crystals`(各级奖励)，两列长度一致。描述用 `name`/`details` 两列同指一文本id(content=名字, content_1=模板含 `{Name}`)，运行期 `GetTextReplace` 替换为该级格式化目标值
- 一行一成就：击杀1行(6级)、时长1行(10级)、征服按难度各1行(每行3级)，共 **12 行=12 卡**
- **当前激活等级=已领取等级数**(0基)，玩家只能领这一级，领后+1，天然逐级无法跳级
- 存档：`UserAchievementBean.achievementLevelClaimed: Dict<id,已领取等级数>`；整族完成=已领取数≥等级总数
- **AchievementInfo.txt** - 成就数据（JsonText，12 行，两列 target_values/reward_crystals）

### 业务逻辑
- **AchievementHandler** - 单例 Handler；运行期只累加统计数据，`GetCurrentLevelState(info)` 实时算当前激活等级状态，`TryUnlockNextLevel(id)` 领当前级后 `SaveUserData()` 落盘；`GetClaimedLevelCount/GetCurrentLevelIndex/IsCompleted/GetAchievementProgress`
- **AchievementManager** - 配置缓存（`GetAllAchievementsSorted` 12 行，UI 卡片直接用）
- **UserAchievementBean** - 用户存档（**已领取等级数字典** `achievementLevelClaimed`、累计击杀、**按世界×难度**通关次数 `conquerCompleteCountByWorldLevel`）

### 事件
- `Achievement_CreatureKill` - 生物被击杀（在 AIIntentCreatureDead，仅进攻方派发）→ 回调只 `AddKillCount`
- `Achievement_ConquerComplete` - 征服模式完整通关（在 GameFightLogicConquer，参数 `long worldId, int difficultyLevel`）→ 回调只 `AddConquerCompleteCount(worldId, difficultyLevel)`
- 注：两事件仅累加统计；运行期不做达成判定、不实时刷新 UI。达成在打开界面时实时计算。已移除 `Achievement_StateChange` / `Achievement_ProgressChange`（高频空转）与 `Achievement_GameTimeChange`（曾驱动每秒判定，现无消费者）

### UI
- **UIAchievement** - 主界面，双 Tab（成就 / 统计），含 UIAchievementComponent partial
- **UIViewAchievementCard** - 成就卡片，5 列网格 cell；传入**成就配置**，内部用 `GetClaimedLevelCount` 解析"当前激活等级索引"展示：进行中(未达成灰/可领取显示"点击领取"绿(text 4000019)+奖励图标脉冲Animator+卡片流光扫光Shine覆盖层，进度 `ui_TxtProgress`=`当前/目标` + 当前级奖励，等级用 `ui_Level` 图标格子) / **已完成**(全部领完，关蒙版隐藏 `ui_Level` 显示"已完成")。点击领取**当前激活等级**，先用 DOTween 播放 `ui_Content` 弹跳动画（`AnimForUnlock`，节奏放慢，锁屏防重复点击）再回调 `TryUnlockNextLevel(info.id)`+刷新(推进到下一级)，`SetData` 复用 cell 时 `ClearAnim` 复位
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
