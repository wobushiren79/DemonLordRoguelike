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

### 单行多级 ⭐
可升级的成就**一条配置行 = 一张卡（含多个等级）**，逐级领取：必须先领取低一级，才能领取高一级；全部等级领完后显示**已完成**。
- **不再有 parent_id/level**。每行用 **两列逗号分隔字符串**承载各级数据：
  - `target_values`：各级目标值，如击杀 `"10,100,1000,10000,100000,1000000"`（顺序即等级 1..N）
  - `reward_crystals`：各级奖励魔晶，与 target_values 一一对应，如 `"10,50,200,500,1000,5000"`
- **描述用 name+details 两列（标准模式）+ 占位符模板**（不再每级一条）：`name[language]`/`details[language_1]` 两列**指向同一文本id**（同一条多语言数据），`name`→`content`(名字，如"生物猎手")、`details`→`content_1`(模板，含 `{Name}` 占位符)。`GetLevelDescription(级)` 取 `details_language`（框架自动生成的 `content_1` 属性，**优先用 `_language` 不手写 GetTextById**），用 `TextHandler.GetTextReplace` 把占位符替换为该级**格式化后的目标值**（时长类换算小时）。例：模板 `"累计击杀 {Name} 只生物"` + 目标100 → `"累计击杀 100 只生物"`。详见 [localization-system] 的 GetTextReplace 与"一个ID承载 content/content_1"。
- **一行一个成就**：类型1(击杀)1行(6级)；类型2(时长)1行(10级)；类型3(征服)按 `难度` 各 1 行(每行 1/10/100 次 = 3级)，共 10 行；类型4(征服某世界·按已通不同难度数)1行(10级)。总计 **13 行 = 13 张卡**。
- **当前激活等级** = "已领取等级数"（0基索引）：玩家只能领取这一级，领取后 +1，天然逐级、无法跳级。
- **UI 卡片数据源** = `AchievementManager.GetAllAchievementsSorted`（12 行直接展示）；卡片内部用 `GetClaimedLevelCount` 解析当前激活等级展示目标/进度/奖励。
- **存档**：`UserAchievementBean.achievementLevelClaimed: Dictionary<long,int>` = 成就id → 已领取等级数。整族完成 = 已领取数 ≥ 等级总数。

### 解锁前置
成就系统本身需要解锁：`UnlockEnum.Achievement = 100500001`。研究节点 `ResearchInfo.id=100500001` 解锁该功能。入口按钮位于 `UIBaseCore` 的功能按钮组，未解锁时隐藏。

## 系统架构

```
AchievementHandler (BaseHandler)
    │  运行期只累加统计数据; 当前激活等级达成(Reached)在打开 UI 时实时计算; 处理逐级领奖并落盘
    │
    ├── 监听 Achievement_CreatureKill (bool isAttacker)  → 仅 AddKillCount
    ├── 监听 Achievement_ConquerComplete (long worldId, int difficultyLevel) → 仅 AddConquerCompleteCount
    │
    │  GetCurrentLevelState(info) 实时算当前激活等级状态 / GetAchievementProgress(info) 取统计进度
    │  GetClaimedLevelCount / IsCompleted / TryUnlockNextLevel
    │
AchievementManager (BaseManager)
    │  缓存排序后的成就列表(12 行)
    │
UserAchievementBean (用户存档)
    │  achievementLevelClaimed: Dictionary<long, int>  // 成就id → 已领取等级数(0..等级总数)
    │  totalKillCount: long
    │  conquerCompleteCountByWorldLevel: Dictionary<long, Dictionary<int, long>>  // 外层worldId→内层难度→次数
    │
AchievementInfoBean (配置, 单行多级)
    │  achievement_type / target_extra / target_world / icon_res / sort / name
    │  target_values / reward_crystals  // 两列逗号分隔, 各级目标/奖励
    │  name / details  // 两列指向同一文本id: name→content(名字), details→content_1(描述模板,含{Name})
```

> **状态模型**：运行期只持续累加统计数据（击杀/通关/时长），**不做任何达成判定、不持久化"达成"状态**。当前激活等级（=已领取数）的 `NotReached`/`Reached` 在打开界面时由 `GetCurrentLevelState` 依据"统计数据 vs 该级目标值"实时计算；只有玩家点击领奖后，`achievementLevelClaimed[id]` +1 并立即 `SaveUserData()` 落盘。

## 数据类型

### AchievementTypeEnum
- `Kill = 1` 击杀生物（累计）
- `PlayTime = 2` 游玩时间（单位秒，UserData.gameTime）
- `ConquerComplete = 3` 征服模式通关（按**世界×难度**区分：target_world=世界id，target_extra=难度等级 1~10）
- `ConquerWorldClear = 4` 征服某世界·按【已通不同难度数】（target_world=世界id；进度=该世界通关次数≥1 的难度种类数，每通一个**新**难度进度+1，与难度顺序无关；target_extra 不用）。判定走 `GetClearedDifficultyCountByWorld(worldId)`

### AchievementStateEnum（针对"当前激活等级"）
- `NotReached = 0` 当前激活等级未达成（显示灰色蒙版）
- `Reached = 1` 当前激活等级已达成可领取（按钮可点）
- `Unlocked = 2` 整族已全部领取完成（卡片显示"已完成"）

### 状态流转（单级）
```
NotReached --[打开UI时按统计数据实时计算]--> Reached --[领取该级,已领取数+1,落盘]--> (推进到下一级 NotReached/Reached, 或全部领完=Unlocked/已完成)
```
- `NotReached` / `Reached`：**不持久化**，每次打开界面对"当前激活等级"实时计算（统计数据 >= 该级 target_values[当前]）
- 持久化的只有 `achievementLevelClaimed`（已领取到第几级）

## 成就数据 (AchievementInfo.txt)

ID 编码规则（每个可升级成就一行，取原一级 id 作行 id）：
- `100001` 击杀类（1 行 6 级：target_values=10/100/1000/10000/100000/1000000）
- `200001` 时长类（1 行 10 级：target_values=3600..36000 秒）
- `30X001` 征服通关（每难度 1 行 3 级：target_values=1/10/100 次；X=难度 1~10，世界id=1）。新增世界时另起 id 段（如世界2用 `32X001` 等避免冲突）。
- `400001` 征服某世界·按已通不同难度数（类型4，1 行 10 级：target_values=1,2,…,10；target_world=1 剑与魔法，文本 id=4004001）。与那 10 条类型3 难度成就**并存**。

> **target_world 列**：类型3专用，0=不限定世界。读取走 `info.GetTargetWorldId()`。

> **target_values / reward_crystals 两列**：逗号分隔字符串，已是自动生成 `AchievementInfoBean.cs` 的 string 字段（早期桥接字段已随「生成 Entity」转正、删除）。解析走 Bean 的 `GetTargetValues/GetRewardCrystals/GetLevelCount/GetLevelTargetValue/GetLevelReward`。
>
> **details[language_1] 列**：描述模板文本id（取 `content_1`，含 `{Name}` 占位符），通常与 `name` 同 id。生成 Entity 后写入自动生成的 `AchievementInfoBean.cs`（`details` + `details_language`）；在此之前由 `AchievementInfoBeanPartial.cs` 的**桥接字段** `public long details;` 临时承载，**生成 Entity 后须删除该桥接字段**。`GetLevelDescription` 取 `details` 的 content_1 模板。
>
> 注：`level_descriptions` 列**已废弃移除**（描述改用 name/details 两列模板）。若生成的 Bean 里还残留该 string 字段，重新「生成 Entity」后会自动消失（属死字段，无引用）。

### 文本 ID 段
- `4000001~4000017` 通用 UI 文本（在 `excel_ui_text`/`Language_UIText_*`；其中 `4000016`=`Lv.{0}/{1}`(等级改用图标格子后卡片已不再用此文本)，`4000017`=已完成/Completed）
- 每个成就**一条** AchievementInfo 文本（该行 `name`/`details` 同指此 id）：`content`=名字，`content_1`=描述模板（含 `{Name}` 占位符）。当前 13 条：击杀 `4001001`、时长 `4002001`、征服难度1~10 `4003101/4003201/.../4003901/4003001`、征服某世界按已通难度数 `4004001`(剑与魔法征服者，模板 `在剑与魔法通关 {Name} 种不同难度`)

> ⚠️ **成就文本源 = `excel_language` 工作簿的 `AchievementInfo` 工作表**（列 `id/content_cn/content_en/content_1_cn/content_1_en/remark`），导出生成 `Language_AchievementInfo_cn/en.txt`。**改文本必须改这个 Excel 工作表**——只改 `.txt` 会在下次导出时被覆盖丢失。同理各配置表的多语言都在 `excel_language` 的同名工作表里。

新增成就时务必同时更新 `Language_UIText_cn.txt` / `Language_UIText_en.txt`。

## 事件常量 (EventsInfo.cs)

| 事件 | 参数 | 触发位置 |
|------|------|----------|
| `Achievement_CreatureKill` | `bool isAttacker` | `AIIntentCreatureDead.IntentEntering` |
| `Achievement_ConquerComplete` | `long worldId, int difficultyLevel` | `GameFightLogicConquer.ActionForUIRewardSelectEnd` |

> 这两个事件**仅用于累加统计数据**（`AddKillCount` / `AddConquerCompleteCount`），不触发任何达成判定与 UI 刷新。
>
> 已移除的事件：`Achievement_StateChange` / `Achievement_ProgressChange`（高频空转）、`Achievement_GameTimeChange`（曾用于驱动每秒达成判定，改为打开 UI 时实时计算后已无消费者）。
>
> UI 不做实时刷新：每次打开界面时各卡片用 `GetCurrentLevelState` 实时算状态；领奖成功后由 `UIAchievement.OnClickForUnlockAchievement` 本地 `RefreshAchievementList()` 刷一次。

## 关键接口

```csharp
// 领取当前激活等级(内部校验 Reached -> 发该级奖 -> 已领取数+1 -> SaveUserData 落盘)
AchievementHandler.Instance.TryUnlockNextLevel(achievementId);

// 实时计算"当前激活等级"状态(整族完成=Unlocked, 否则按统计数据 vs 该级目标值判定)
AchievementStateEnum state = AchievementHandler.Instance.GetCurrentLevelState(info);

// 当前激活等级 / 完成 查询
int claimed   = AchievementHandler.Instance.GetClaimedLevelCount(info);   // 已领取等级数(=当前激活等级0基索引)
bool done     = AchievementHandler.Instance.IsCompleted(info);            // 是否整族完成
long progress = AchievementHandler.Instance.GetAchievementProgress(info); // 统计进度(原始累计值)

// Bean 各级数据(逗号分隔解析, 带缓存)
int n      = info.GetLevelCount();                 // 等级总数
long tgt   = info.GetLevelTargetValue(levelIndex); // 某级目标值(0基)
long rwd   = info.GetLevelReward(levelIndex);      // 某级奖励魔晶
string des = info.GetLevelDescription(levelIndex); // 某级描述(details模板content_1 + GetTextReplace替换{Name}为格式化目标值)
string fv  = info.FormatValueByType(value);        // 按类型格式化数值(时长→小时,其余原值;进度文本与描述统一口径)

// 用户数据(运行期只累加 / 查询统计)
var userData = GameDataHandler.Instance.manager.GetUserData();
var achievementData = userData.GetUserAchievementData();
achievementData.GetClaimedLevelCount(id);      // 已领取等级数
achievementData.SetClaimedLevelCount(id, n);   // 仅领奖成功时调用
achievementData.GetTotalKillCount();
achievementData.GetConquerCompleteCount(worldId, difficultyLevel);  // 按世界×难度
achievementData.GetConquerCompleteCountByWorld(worldId);            // 某世界合计
achievementData.GetClearedDifficultyCountByWorld(worldId);          // 某世界已通不同难度数(类型4 ConquerWorldClear 进度)
achievementData.GetTotalConquerCompleteCount();                     // 全世界合计
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

### UIViewAchievementCard 状态（按"当前激活等级"展示）
卡片传入**成就配置(单行多级)**，内部用 `GetClaimedLevelCount(info)` 得到当前激活等级索引 `currentLevelIndex`：
- **进行中-未达成** → 灰色蒙版 + 锁 + 等级格子(已领取级亮起 LevelIcon) + 进度文本 `ui_TxtProgress`=`当前/目标`(白色) + 当前级奖励
- **进行中-可领取** → 同上但进度文本改显**"点击领取"**(text 4000019, 绿色) + 按钮可点，并叠加两个生动提示（`RefreshClaimableFx(reached)` 统一驱动）：① **奖励图标呼吸脉冲**——`ui_Reward` 上的 `Animator`(controller `Assets/LoadResources/Anim/UI/UIViewAchievementRewardPulse.controller` + clip 同名，循环缩放 1↔1.12)，可领取时 `enabled=true`，否则 `enabled=false` 并复位 `ui_Reward` 缩放；② **卡片流光扫光**——卡片根节点下常驻的 `Shine` 覆盖层(全铺 Image，`color.a=0` 只显扫光不遮内容，材质 `Assets/LoadResources/Materials/UI/Mat_UIViewAchievementCardShine.mat` = `FrameWork/UI/Shader_UI_ImageEffect` 开 `_SHINE_ON`、`_ShineMaskByAlpha=0`)，可领取时 `SetActive(true)`。两引用由 `EnsureClaimableFxRefs()` 惰性缓存(`ui_Reward.GetComponent<Animator>()` / `transform.Find("Shine")`)
- **已完成**（`currentLevelIndex >= GetLevelCount()`）→ 关闭蒙版/锁/奖励，进度区 `ui_TxtProgress` 显示"已完成"(text 4000017)，等级格子全部亮起
- **等级图标格子 `ui_Level`(容器, RectTransform + GridLayoutGroup)**：等级不再用文本角标，改为**按等级总数动态实例化的图标格子**。`RefreshLevelItems()` 以 `ui_LevelItem`(模板, prefab 中 inactive) 为蓝本，复用 `List<RectTransform> listLevelItem` 实例化 `levelCount` 个格子到 `ui_Level` 下；每个格子的子节点 `LevelIcon`(对勾 Image)在该级**已领取**(索引 `< currentLevelIndex`)时 `SetActive(true)` 显示、否则隐藏。用列表复用+隐藏多余，避免单元格被 `ScrollGrid` 池化复用时**重复实例化产生冗余格子**（参考 `UIViewAbyssalBlessingInfoContent` 的复用模式）。text 4000016(`Lv.{0}/{1}`)已不再被卡片使用
- 进度文本格式 `ui_TxtProgress`：仅计数 `{0}/{1}`(text 4000006, 目标=`GetLevelTargetValue(index)`)；图标取 `info.icon_res`，描述气泡取 `info.GetLevelDescription(displayIndex)`，奖励取 `info.GetLevelReward(index)`

> **领取流程**：点击卡片领取的是**当前激活等级**。`OnClickForUnlock` 先用 DOTween 播放**等级图标砸落动画**（`AnimForUnlock`）：取待领取等级 `currentLevelIndex` 对应的 `listLevelItem[currentLevelIndex]`，其 `LevelIcon` 立即以放大态(`IconSlamStartScale`=3x)**突然出现**，再 `DOScale→1`(Ease.InBack) **从大到小"砸"向 LevelItem**，落位后 `LevelItem.DOShakeScale` **抖动一下**；只动目标格子不动 cell 自身 transform，期间 `UIHandler.ShowScreenLock` 锁屏防重复点击。动画结束后回调 `actionForUnlock(info)` → `TryUnlockNextLevel(info.id)`（发该级奖+已领取数+1） + Toast + `RefreshAchievementList`，列表重建后该级 `LevelIcon` 常驻显示、卡片推进到下一级或显示"已完成"。`ClearAnim` 仅 Kill 上一次序列；格子缩放/图标显隐由 `RefreshLevelItems` 每次刷新统一复位，保证池化复用干净。

## 接入新成就类型的流程

1. 在 `AchievementTypeEnum` 新增枚举值
2. 在 `EventsInfo.cs` 新增 `Achievement_*` 事件常量
3. 在源头处 `EventHandler.Instance.TriggerEvent` 派发事件
4. 在 `UserAchievementBean` 添加对应统计字段 + AddXxx/GetXxx 方法
5. 在 `AchievementHandler.GetAchievementProgress` 的 switch 中处理新类型（达成判定靠它返回的进度 vs 当前级 target_values）
6. 在 `AchievementHandler.InitData` 注册事件监听（回调里**只累加统计数据**，不要写达成判定）
7. 在 `AchievementInfo.txt` 添加配置（一行多级：填 target_values/reward_crystals + name/details 同指一个文本id）+ 该条文本（content=名字，content_1=含 `{Name}` 的描述模板）
8. 如需在统计页签显示：修改 `UIAchievement.BuildStatisticList`

## 注意事项

- **运行期零达成判定**：击杀/通关事件回调只 `AddXxx` 累加统计数据；当前激活等级的 `NotReached`/`Reached` 在打开界面时由 `GetCurrentLevelState` 实时计算，不持久化、不需要每秒/每帧检查
- **只持久化已领取等级数**：`achievementLevelClaimed` 字典存 成就id→已领取等级数；领奖成功后 `TryUnlockNextLevel` 会 `SetClaimedLevelCount(id, n+1)` + `SaveUserData()` 立即落盘（连同发放的魔晶）。旧存档的 `achievementStates` 字段已废弃、领取进度从0重算
- **逐级门控天然成立**：当前激活等级 = 已领取等级数，玩家只能领这一级；领取后 +1 自动推进。即使统计数据已远超高级目标，也必须一级一级点
- 击杀仅统计**进攻方**生物死亡（`CreatureFightTypeEnum.FightAttack`）；源头（`AIIntentCreatureDead`）**只为进攻方死亡派发** `Achievement_CreatureKill`，减少战斗高峰事件量
- 征服通关在 `RewardSelectEnd` 触发（玩家真的完成全部关卡且领完奖之后），而不是单关结算
- 成就奖励直接调用 `userData.AddCrystal(num)`（不走背包道具）
- `UserAchievementBean` 已从 UserData 主存档**拆分为独立存档** `UserData_{slot}/UserAchievement_{slot}`（字段标 `[JsonIgnore]`，读写封装在 `UserDataService.Save/Load/Delete` 内部）。仍通过 `userData.GetUserAchievementData()` 取数，调用方无感知
- UI 卡片的状态/进度都通过 `GetCurrentLevelState(info)` / `GetClaimedLevelCount(info)` / `GetAchievementProgress(info)` 取，**不要**直接读存档字典判断
- **图标**：每个成就一个 `icon_res`，整成就所有等级共用。当前三类成就各有专属图标，存于 `Assets/LoadResources/Textures/Achievement/`（由 `AtlasForAchievement.spriteatlas` 打包，图集 tag=`Achievement`，已在 `SpriteAtlasTypeEnum` 注册 `Achievement` 枚举值），`icon_res` 用 `图标名,Achievement` 后缀加载：击杀类=`ui_achievement_kill,Achievement`（骷髅染血剑）、时长类=`ui_achievement_time,Achievement`（金色沙漏）、征服通关类=`ui_achievement_clear,Achievement`（金色奖杯）。当前图标为 32px 像素图；卡片走 `IconHandler.SetUIIcon`（默认 UI 图集，靠 `,Achievement` 后缀经 `ParseIconName` 切到成就图集）
- **新增等级**：给某成就追加更高一级时，直接在该行的 `target_values`/`reward_crystals` 两列各**追加一个逗号项**（两列长度须一致），**描述无需改**（同一模板自动套用新目标值）；无需新增行、无需加文本
- **描述模板占位符**：当前级格式化目标值同时挂在通用 `{Name}` 与**类型语义占位符**下——击杀=`{KillNum}`、时长(小时)=`{Time_H}`、其余=`{Name}`（见 `GetValueReplaceKey`）。模板用哪个都行，推荐用语义占位符（如时长 `累计游玩 {Time_H} 小时`、击杀可 `{Name}` 或 `{KillNum}`）。模板里固定文案（难度数字、"只生物"/"小时"/"次"/`hour(s)`/`time(s)`）直接写死在该成就的 content_1

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
