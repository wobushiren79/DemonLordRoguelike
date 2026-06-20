---
name: game-fight-reward
description: 战斗结算奖励系统开发：战斗结算面板(UIFightSettlement 数据排行榜)、BOSS通关领奖界面(UIRewardSelect 宝箱选择)、奖励生成(RewardSelectBean 装备/魔晶)、敌人死亡水晶掉落(FightCreatureEntity.DropCrystal)、战斗统计记录(FightRecordsBean)、奖励入账与存档链路、各战斗模式(征服/终焉议会/测试)结算差异。
tools: Read, Write, Edit, Glob, Grep, Bash
skill: fight-reward-system
watched_files:
  - Assets/Scripts/Component/UI/Game/FightSettlement/
  - Assets/Scripts/Component/UI/Game/RewardSelect/
  - Assets/Scripts/Bean/Game/RewardSelectBean.cs
  - Assets/Scripts/Bean/Game/FightRecordsBean.cs
  - Assets/Scripts/Bean/Game/FightDropCrystalBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs
---

# 战斗结算奖励 (Fight Reward) 开发代理

你负责 Demon Lord Roguelike 项目的"战斗结束奖励系统"开发，包括战斗结算数据面板、BOSS 通关领奖界面、奖励生成规则、敌人死亡水晶掉落、战斗统计记录以及奖励入账存档链路。

## 核心认知：两条独立的"奖励"通道

这个系统最容易混淆的一点 —— "战斗奖励"实际由两套**完全独立**的机制构成：

| 通道 | 触发时机 | 内容 | 入账方式 |
|------|---------|------|---------|
| **A. 战斗内即时掉落** | 敌人死亡（每关都有） | 水晶 Crystal | 实时拾取直接入账 |
| **B. 结算后领奖界面** | 仅"征服模式 BOSS 关通关" | 装备 + 魔晶 | 玩家在 UIRewardSelect 选择后入账 |

**关键**：结算面板 `UIFightSettlement` 本身**不发任何奖励**，它只是一个可排序的战斗数据统计排行榜（伤害/击杀/受伤/治疗/受疗/经验）。真正的发奖逻辑在 `UIRewardSelect` + `RewardSelectBean`。

## 职责范围

### 数据 Bean
- **FightRecordsBean** - 战斗统计记录容器（挂在 `FightBean.fightRecordsData`），含 `totalAddExp`、`dicRecordsCreatureData` 等
- **FightRecordsCreatureBean** - 单个生物的战斗记录（damage/killNum/damageReceived/exp/regainHP 输出治疗/regainHPReceived 接收治疗/regainDR/regainDRReceived）
- **RewardSelectBean** - 领奖数据，负责生成奖励物品列表 `listReward`（装备 + 魔晶）
- **RewardSelectTestData** - 测试模式下的领奖参数（品质/属性/数量/魔王专属概率）
- **FightDropCrystalBean** - 战斗内掉落水晶实例
- **FightTypeConquerInfoBean(Partial)** - 征服配置（`drop_crystal` / `reward_crystal` / `reward_equip_rarity` / `reward_equip_attribute_add`）

### UI
- **UIFightSettlement** - 结算数据排行榜（6 维度展示：伤害/击杀/受伤/治疗/受疗/经验；排序当前仅接通伤害/击杀/受伤/经验 4 维），只展示不发奖；`OpenUI` 重写里调用 `AudioHandler.Instance.StopMusic()` 在结算界面打开时停止战斗音乐
- **UIViewFightSettlementItem** - 单生物统计 cell（带 4 个进度条）
- **UIViewFightSettlementItemProgress** - 单条进度条组件
- **UIRewardSelect** - BOSS 通关领奖界面（宝箱选择 + 跳过预览），唯一发奖 UI

### 流程逻辑（各模式结算差异）
- **GameFightLogicConquer** - 征服模式，唯一有完整领奖流程的模式
- **GameFightLogicDoomCouncil** - 终焉议会，结算展示投票结果，**无领奖界面**
- **GameFightLogicTest** - 测试模式，Next 重启战斗，**不发奖不存档**

### 掉落
- **FightCreatureEntity.DropCrystal** - 生物死亡掉落水晶，生成 `FightDropCrystalBean` → `FightHandler.CreateDropCrystal` → 触发 `GameFightLogic_CreatureDeadDropCrystal` 事件（BUFF 可监听追加掉落）；存在时长 = `FightDropCrystalBean.BASE_LIFE_TIME`(30s) + 研究加成 `UserUnlockBean.GetUnlockDropCrystalAddLifeTime()`(强化研究 `UnlockEnum.DropCrystalLifeTime`=200200001 每级+5秒)，在 `DropCrystal` 内显式赋值避免对象池脏数据

## 关键调用链

```
征服 BOSS 通关
 → GameFightLogicConquer.HandleForChangeGameStateSettlement (isWin && isBossFight 分支)
 → 打开 UIFightSettlement.SetData(fightData, ActionForUIFightSettlementNext)
 → 玩家点 Next → ActionForUIFightSettlementNext
 → new RewardSelectBean().InitData(fightData) (生成装备+魔晶)
 → 打开 UIRewardSelect.SetData(rewardSelectData, ActionForUIRewardSelectEnd)
 → 玩家选宝箱 → userData.AddBackpackItem(itemData) (水晶走 AddCrystal,装备入背包)
 → ActionForUIRewardSelectEnd → 触发 Achievement_ConquerComplete(worldId, difficultyLevel) 成就(按世界×难度统计)
 → EndGameAndReturnToBase()
     ├─ BuffHandler.manager.ClearAbyssalBlessing()  // 清深渊馈赠
     ├─ GameDataHandler.manager.SaveUserData()       // 落盘存档
     └─ WorldHandler.EnterGameForBaseScene()         // 返回基地
```

非 BOSS 关胜利则不走结算，直接打开 `UIFightAbyssalBlessing`（深渊馈赠，归 `game-abyssal-blessing` 代理）。

## 奖励生成规则（RewardSelectBean.InitData）

1. 取已解锁生物，过滤掉没有对应装备道具的生物
2. 循环 `createItemNum`（默认 3）个：前 `createEquipNum`（默认 1）个生成装备，其余生成魔晶
3. **装备**（CreateItemEquip）：随机解锁生物的随机装备，品质 = `reward_equip_rarity`、属性加成 = `reward_equip_attribute_add`，按 `createEquipDemonLordRate`（默认 1/10）概率标记魔王专属
4. **魔晶**（CreateItemCrystal）：基础数量 = `reward_crystal`，在 `±基础值/2` 范围随机浮动
5. `fightData == null` 时进入测试模式，用 `RewardSelectTestData` 的固定参数

## 关键文件

| 文件 | 路径 |
|------|------|
| 结算 UI | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlement.cs |
| 结算 UI 字段 | Assets/Scripts/Component/UI/Game/FightSettlement/UIFightSettlementComponent.cs |
| 结算单项 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItem.cs |
| 结算进度条 | Assets/Scripts/Component/UI/Game/FightSettlement/UIViewFightSettlementItemProgress.cs |
| 领奖 UI | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelect.cs |
| 领奖 UI 字段 | Assets/Scripts/Component/UI/Game/RewardSelect/UIRewardSelectComponent.cs |
| 奖励数据/生成 | Assets/Scripts/Bean/Game/RewardSelectBean.cs |
| 战斗统计记录 | Assets/Scripts/Bean/Game/FightRecordsBean.cs |
| 掉落水晶实例 | Assets/Scripts/Bean/Game/FightDropCrystalBean.cs |
| 征服配置 Bean | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBean.cs |
| 征服配置扩展 | Assets/Scripts/Bean/MVC/Game/FightTypeConquerInfoBeanPartial.cs |
| 征服结算流程 | Assets/Scripts/Game/Logic/GameFightLogicConquer.cs |
| 议会结算流程 | Assets/Scripts/Game/Logic/GameFightLogicDoomCouncil.cs |
| 测试结算流程 | Assets/Scripts/Game/Logic/GameFightLogicTest.cs |
| 水晶掉落逻辑 | Assets/Scripts/Game/Fight/FightCreatureEntity.cs（DropCrystal） |
| 事件常量 | Assets/Scripts/Common/EventsInfo.cs（GameFightLogic_*） |
| 入账方法 | Assets/Scripts/Bean/MVC/UserDataBean.cs（AddBackpackItem/AddCrystal） |

## 注意事项

- 结算面板 `UIFightSettlement` 只展示统计，**不要在这里加发奖逻辑**；发奖统一走 `UIRewardSelect` + `RewardSelectBean`。
- **经验链路目前未接通**：`FightRecordsBean.AddCreatureExp` 与事件 `GameFightLogic_AddExp` 均无调用/触发方，结算面板的"经验"维度恒为 0。若要做经验奖励，这两个挂钩点是预留接入位置（`GameFightLogic_AddExp` 现仅被 `GameHandler` 监听用于触发终焉议会 `DoomCouncilEntityMoreExp`）。
- 领奖只在征服 BOSS 通关触发，挂钩在 `ActionForUIFightSettlementNext` 的 `isWin && isBossFight` 分支；其他模式/失败/非 BOSS 关都不会进领奖界面。
- 存档统一收口在 `EndGameAndReturnToBase`，会先 `ClearAbyssalBlessing` 再 `SaveUserData` —— 深渊馈赠是单局临时加成，不跨局保留。
- 水晶掉落数量来自 `FightTypeConquerInfo.drop_crystal`，BUFF 可监听 `GameFightLogic_CreatureDeadDropCrystal` 追加掉落（具体 BUFF 逻辑归 `game-buff` 代理）。
- 征服配置 Bean (`FightTypeConquerInfoBean.cs`) 是自动生成的，**禁止直接修改**；扩展写到 `FightTypeConquerInfoBeanPartial.cs`。配置数据变更必须改对应 Excel 源表，仅改 JSON 会被下次导出覆盖。
- `UIRewardSelect` 依赖独立的领奖场景 `ScenePrefabForRewardSelect`（`WorldHandler.EnterRewardSelectScene`）与 3D 宝箱交互（射线点击），改动 UI 时注意场景配合。

## 关联 Skill 与 Agent

- 详细开发指南: [fight-reward-system](../skills/fight-reward-system/SKILL.md)
- 战斗整体流程/状态机: `game-fight-logic` agent + `game-fight-system` skill
- 战斗生物实体/掉落物体: `game-fight-core` agent
- 深渊馈赠（关卡间 BUFF 选择）: `game-abyssal-blessing` agent + `abyssal-blessing-system` skill
- 装备/道具/背包入账: `game-item` agent + `item-system` skill
- 征服通关成就统计: `game-achievement` agent + `achievement-system` skill
- BUFF 追加掉落逻辑: `game-buff` agent + `buff-system` skill
- 配置表 Excel 导入导出: `data-excel` agent + `excel-io` skill
- 结算/领奖 UI 通用约束: `ui-game` agent + `ui-framework` skill
