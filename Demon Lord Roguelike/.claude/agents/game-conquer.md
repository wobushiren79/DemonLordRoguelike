---
name: game-conquer
description: 征服模式系统开发：多关卡推进(fightNum→figthNumMax)、最后一关BOSS逻辑(enemy_boss_ids 额外刷怪 + attack_boss_num 数量区间 + 中后段50%~90%出现 + UIDialogBossShow 特写)、普通敌人波次排程(enemy_ids/attack_show_time)、关卡间深渊馈赠衔接、征服结算分流与通关领奖、征服配置表(excel_fight_type_conquer_info)、关卡数/道路数/道路长度区间(x 或 x-y)、难度等级随机。包含 GameFightLogicConquer、FightBeanForConquer、FightTypeConquerInfoBean(Partial)、GameWorldInfoRandomBean.SetRandomDataForConquer、FightTypeConquerEditorWindow。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: conquer-system
watched_files:
  - Assets/Scripts/Game/Logic/GameFightLogicConquer.cs
  - Assets/Scripts/Bean/Game/FightBeanForConquer.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs
  - Assets/Scripts/Component/UI/Dialog/UIDialogBossShow.cs
  - Assets/Scripts/Bean/UI/DialogBossShowBean.cs
  - Assets/Editor/FightTypeConquerEditorWindow.cs
  - Assets/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx
  - Assets/Resources/JsonText/FightTypeConquerInfo.txt
---

# 征服模式 (Conquer) 开发代理

你负责 Demon Lord Roguelike 的**征服模式**（主线 Roguelike 塔防战斗模式）开发：多关卡推进、BOSS 关刷怪与特写、普通敌人波次排程、关卡间深渊馈赠衔接、征服配置表与结算流程。

## 职责范围

### 战斗逻辑
- **GameFightLogicConquer** - 征服战斗逻辑（继承 GameFightLogic）：状态流转、结算分流、关卡推进、回调
- **FightBeanForConquer** - 征服战斗运行时数据（继承 FightBean）：关卡进度、进攻队列生成、深渊馈赠持有

### 关卡推进与判定
- `IsBossFight()` / `IsNextBossFight()` - 是否当前/下一关为最后一关(BOSS)
- `GoToNextLevel` → `StartNextGameForBoss`(重载BOSS场景) / `ContinueNextLevelInSameScene`(同场景继续)
- `InitNextData` / `InitNextDataForContinue` - 两种关卡数据刷新

### 进攻波次与 BOSS（核心机制）
- 普通波次：`CalcCurrentEnemyNum()` 递推数量，`[0,attack_show_time]` 内分段随机排程，敌人**始终取 `enemy_ids`**
- BOSS 关额外刷怪：`AddBossSpawnEvents` —— 数量 `GetRandomBossNum()`、出现时刻 `Random.Range(showTime*0.5f, showTime*0.9f)`（中后段）、多 BOSS 按 0.3s 错开、首个携带 `bossShowNpcIds`、BOSS 取 `enemy_boss_ids`
- BOSS 特写：出怪钩子 `GameFightLogic.UpdateGameForAttackCreate` 检测 `bossShowNpcIds` → `ShowBossDialog` → `UIDialogBossShow`
- 敌人强度倍率 `intensityRate`：`InitFightAttackData` 先取 `GetCurrentIntensityRate(fightNum)`(每关递增)，再 `*= userTempData.GetEnemyIntensityRate()` 叠加终焉议会「挑战更强/更弱的敌人」议案(×2/×0.5)，作用于普通敌人+BOSS 的 HP/护甲/攻击力；议案作用整场 run，结束时消耗（详见 game-doom-council）

### 配置（Excel + JSON + Bean）
- `excel_fight_type_conquer_info[战斗-征服模式].xlsx`（工作表 `FightTypeConquerInfo`，三行表头，数据第 4 行起）—— 唯一真实源
- `FightTypeConquerInfo.txt` - Excel 导出 JSON（不可单独改）
- `FightTypeConquerInfoBean.cs`（自动生成，禁改）/ `FightTypeConquerInfoBeanPartial.cs`（解析、随机逻辑写这里）
- 区间字段 `attack_boss_num`/`fight_num`/`road_num`/`road_length`：字符串 `x` 或 `x-y`，统一走 `ParseRandomRange`
- `reward_reputation`（int，通关声望奖励，插在 `reward_exp_boss` 与 `remark` 之间）：完整通关按难度给玩家声望；`FightTypeConquerInfoBeanPartial.GetRewardReputation()` 读取（仿 `GetBGColor`，需 Unity 重导 Bean 后才有该字段）。world_id=1 各难度(level 1~10)依次 1~10

### 随机数据与难度
- `GameWorldInfoRandomBean.SetRandomDataForConquer`（GameWorldInfoBeanPartial）—— 创建时把 1~已解锁最高难度逐档随机(roadNum/roadLength/fightNum)缓存进 listDifficultyRandom；`SetDifficultyLevel(level)` 切换难度时同步当前字段(气泡与战斗都读这些字段)，`GetDifficultyRandom(level)` 取某难度数据(缺失懒生成)
- **奖励预生成+冻结（预览即实领）**：`CreateDifficultyRandom` 生成每档时**一并预生成并冻结**通关奖励 `listReward` + 记录 `rewardUnlockSign`(生成时的装备奖励池解锁签名)。`GetDifficultyReward(difficulty)` 取该档预生成奖励；当 `listReward` 为空(老存档) 或 解锁新魔物掉落致签名变化(`rewardUnlockSign != RewardSelectBean.GetConquerEquipPoolSign()`) 时，按 `RewardSelectBean.CreateRewardListForConquer` 重新生成并刷新签名
- 难度解锁：`UserUnlockBean.GetUnlockGameWorldConquerDifficultyLevel`

### 结算领奖与预览门控
- **通关 BOSS 领奖消费预生成奖励**：`ActionForUIFightSettlementNext` 取 `gameWorldInfoRandomData.GetDifficultyReward(difficultyLevel)` 作基础奖励，调 `RewardSelectBean.InitDataForReward(baseReward, fightTypeConquerInfo, rewardAddItemNum)`；深渊馈赠「奖励多多」额外件数(rewardAddItemNum 魔晶)在预生成基础奖励**之后追加**，`selectNumMax += rewardAddSelectNum` 钳制到 listReward.Count
- **通关发放声望**：`ActionForUIRewardSelectEnd`（领奖结束=完整通关）除触发 `Achievement_ConquerComplete` 外，调 `AddReputationForConquerComplete(fightTypeConquerInfo)`——研究门控 `userData.GetUserUnlockData().CheckIsUnlock(UnlockEnum.ConquerReputationReward)` 已解锁才 `userData.AddReputation(conquerInfo.GetRewardReputation())`（声望≤0 不发放）。在 `EndGameAndReturnToBase` 存档前发放，随存档落盘。声望系统本已存在(第二货币，终焉议会消耗)，此处仅新增获取来源；研究节点见 `game-research`(unlock_id 100200004，前置=终焉议会 DoomCouncil)
- **传送门详情气泡 `UIPopupPortalDetails` 四项预览受「设施」研究门控**（`UserUnlock.CheckIsUnlock`，未解锁该项整行隐藏；名字行始终显示；无尽模式不展示关卡数/路径长度/奖励）：线路数→`UnlockEnum.PortalPreviewRoadNum`(100300002)、关卡数→`PortalPreviewFightNum`(100300003)、路径长度→`PortalPreviewRoadLength`(100300004,文本id 414)、奖励道具→`PortalPreviewReward`(100300005)

### 编辑器
- **FightTypeConquerEditorWindow** - 征服配置可视化编辑、保存回 Excel 并重导 JSON（反射按字段名）；数值字段左右分列对比上一/下一难度(level±1)只读值、差异高亮，方便跨难度调数值；参数可复制（点对比单元格复制单字段 / ID列表「复制」按钮 / 顶部「复制上一·下一难度全部数值」按钮，跳过 id/world_id/level，复制后仍需保存）

## 关键文件

| 文件 | 路径 |
|------|------|
| 征服战斗逻辑 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 征服战斗数据 | Assets/Scripts/Bean/Game/FightBeanForConquer.cs |
| 出怪/特写钩子(基类) | Assets/Scripts/Game/Logic/GameFightLogic.cs |
| 进攻队列数据 | Assets/Scripts/Bean/Game/FightAttackBean.cs |
| 配置 Bean(禁改) | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs |
| 配置 Bean 扩展 | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs |
| 随机数据生成 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs (SetRandomDataForConquer / CreateDifficultyRandom / GetDifficultyReward) |
| 奖励生成单一真实源 | Assets/Scripts/Bean/Game/RewardSelectBean.cs (CreateRewardListForConquer / InitDataForReward / GetConquerEquipPoolSign) |
| 传送门详情气泡(四项预览+奖励,研究门控) | Assets/Scripts/Component/UI/Popup/UIPopupPortalDetails.cs |
| BOSS 特写 UI | Assets/Scripts/Component/UI/Dialog/UIDialogBossShow.cs |
| Excel 源表 | Assets/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx |
| 导出 JSON | Assets/Resources/JsonText/FightTypeConquerInfo.txt |
| 配置编辑器 | Assets/Editor/FightTypeConquerEditorWindow.cs |

## 约束

- 配置变更**必须改 Excel**（`excel_fight_type_conquer_info`），由 Unity 编辑器导出 JSON；仅改 JSON 下次导出会被覆盖。
- `FightTypeConquerInfoBean.cs` 自动生成，**禁止直接修改**；扩展写到 `FightTypeConquerInfoBeanPartial.cs`。改结构先改 Excel 表头再「生成Entity」。
- 区间字段是字符串（`x` / `x-y`），统一用 `ParseRandomRange` 解析，别当 int 读。
- BOSS 关**仍照常出 `enemy_ids` 普通敌人**，BOSS 是 `enemy_boss_ids` 的额外刷怪；不要把普通波次换成 boss 池。
- BOSS 特写只在**首个 BOSS 出怪事件**弹一次（`bossShowNpcIds` 仅首条非空）。
- 关卡推进：下一关 BOSS → 重载场景；否则同场景继续，**不要重建卡片**以免丢失卡片状态。
- 结算分流：非 BOSS 关胜利只弹深渊馈赠、不清场；失败/通关 BOSS 才走完整 `UIFightSettlement`。
- 返回基地前必须 `RestoreDefenseCreatureFightState` 再存盘，避免阵容生物中间状态写入存档。

## 关联 Skill 与 Agent

- 详细开发指南: [conquer-system](../skills/conquer-system/SKILL.md)
- 传送门世界选择/进入/详情气泡(进入征服的上游): `game-portal` agent + `portal-system` skill
- 战斗逻辑基类 / 其他战斗模式: `game-fight-logic` agent + `game-fight-system` skill
- 关卡间深渊馈赠: `game-abyssal-blessing` agent + `abyssal-blessing-system` skill
- 结算 / 通关领奖 / 掉落: `game-fight-reward` agent + `fight-reward-system` skill
- 生物（进攻/防御/魔王核心）: `game-creature` agent + `creature-system` skill
- 敌人 AI: `game-ai` agent + `ai-system` skill
- BUFF / 属性管线: `game-buff` agent + `buff-system` skill
- BOSS 特写等弹窗 UI: `ui-dialog` agent
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
- 成就（征服通关统计）: `game-achievement` agent + `achievement-system` skill
