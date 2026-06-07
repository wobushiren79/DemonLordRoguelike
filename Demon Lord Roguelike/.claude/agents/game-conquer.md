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

### 配置（Excel + JSON + Bean）
- `excel_fight_type_conquer_info[战斗-征服模式].xlsx`（工作表 `FightTypeConquerInfo`，三行表头，数据第 4 行起）—— 唯一真实源
- `FightTypeConquerInfo.txt` - Excel 导出 JSON（不可单独改）
- `FightTypeConquerInfoBean.cs`（自动生成，禁改）/ `FightTypeConquerInfoBeanPartial.cs`（解析、随机逻辑写这里）
- 区间字段 `attack_boss_num`/`fight_num`/`road_num`/`road_length`：字符串 `x` 或 `x-y`，统一走 `ParseRandomRange`

### 随机数据与难度
- `GameWorldInfoRandomBean.SetRandomDataForConquer`（GameWorldInfoBeanPartial）—— 难度 + roadNum/roadLength/fightNum 随机
- 难度解锁：`UserUnlockBean.GetUnlockGameWorldConquerDifficultyLevel`

### 编辑器
- **FightTypeConquerEditorWindow** - 征服配置可视化编辑、保存回 Excel 并重导 JSON（反射按字段名）

## 关键文件

| 文件 | 路径 |
|------|------|
| 征服战斗逻辑 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 征服战斗数据 | Assets/Scripts/Bean/Game/FightBeanForConquer.cs |
| 出怪/特写钩子(基类) | Assets/Scripts/Game/Logic/GameFightLogic.cs |
| 进攻队列数据 | Assets/Scripts/Bean/Game/FightAttackBean.cs |
| 配置 Bean(禁改) | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs |
| 配置 Bean 扩展 | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs |
| 随机数据生成 | Assets/Scripts/Bean/MVC/Game/GameWorldInfoBeanPartial.cs |
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
- 战斗逻辑基类 / 其他战斗模式: `game-fight-logic` agent + `game-fight-system` skill
- 关卡间深渊馈赠: `game-abyssal-blessing` agent + `abyssal-blessing-system` skill
- 结算 / 通关领奖 / 掉落: `game-fight-reward` agent + `fight-reward-system` skill
- 生物（进攻/防御/魔王核心）: `game-creature` agent + `creature-system` skill
- 敌人 AI: `game-ai` agent + `ai-system` skill
- BUFF / 属性管线: `game-buff` agent + `buff-system` skill
- BOSS 特写等弹窗 UI: `ui-dialog` agent
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
- 成就（征服通关统计）: `game-achievement` agent + `achievement-system` skill
