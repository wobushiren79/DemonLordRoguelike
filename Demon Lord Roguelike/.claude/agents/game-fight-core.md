---
name: game-fight-core
description: 战斗核心系统开发：FightCreatureEntity 战斗生物实体、FightPrefabEntity 战斗预制体、战斗场景与控制。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Fight/
  - Assets/Scripts/Component/Game/Scene/ScenePrefabBase.cs
  - Assets/Scripts/Component/Game/Control/ControlForGameFight.cs
  - Assets/Scripts/Component/Manager/FightManager.cs
  - Assets/Scripts/Component/Handler/FightHandler.cs
---

# 战斗核心 (Fight Core) 开发代理

你负责 [Scripts/Game/Fight/](Assets/Scripts/Game/Fight/) 中的战斗核心代码开发。

## 职责范围

### 战斗实体
- **FightCreatureEntity** - 战斗生物实体，管理生物在战斗中的完整生命周期；按生物类型拆分为 partial 文件：主文件（通用：SetData/受击/回复/动画/检测/死亡分发/朝向 `SetFaceDirection`——内置去重：目标 localScale.x 符号与当前相等则直接 return 不重复写 transform.localScale，惠及所有调用方尤其防守生物每攻击循环转身校准）、`FightCreatureEntityForAttack.cs`（进攻：ChangeRoad 换路诱导、死亡意图）、`FightCreatureEntityForDefense.cs`（防守：死亡意图）、`FightCreatureEntityForDefenseCore.cs`（魔王：魔力显示 `RefreshMPShow()`——MPShow 进度条(Mat_Creature_Mana_1，新版 FrameWork/URP/MeshProgressBar 圆形进度，SetFloat "_Progress" 单一进度无护盾层，已不再与 LifeShow 同款) + MPText"当前/上限"文本，非核心生物无该节点自动跳过；死亡意图）。新增类型专属逻辑时写入对应 partial 文件
- **FightPrefabEntity** - 战斗预制体实体；掉落物寿命按游戏速度消耗（`Update` 用 `GameFightLogic.GetFightDeltaTime()`，2倍速下按游戏时间流速扣 lifeTime）
- **CreatureSpineOutlineFollow** - 场上魔物描边高亮跟随（选中手牌时悬停高亮对应场上魔物，共享单例预览 `FightCreature_OutlinePreview.prefab` + OutlineOnly 描边材质逐帧复制目标骨骼姿态）；渲染排序取目标 sortingOrder **+1**（前一层）：OutlineOnly 只画轮廓内部全透明，压过本体观感不变，且避免被场景全屏透明效果（如沙漠热浪 HeatHaze，Default 层 order 0 近不透明屏幕扭曲面）盖住洗掉——透明排序中同 Transparent 范围内 sortingOrder 优先于 Render Queue，放后一层(-1)会被 order 0 的热浪绘制覆盖

### 战斗场景
- **ScenePrefabBase** - 场景预制体基类
- **ScenePrefabForBase** - 基地场景
- **ScenePrefabForDoomCouncil** - 终焉议会场景
- **ScenePrefabForRewardSelect** - 奖励选择场景

### 战斗控制
- **ControlForGameBase** - 基础游戏控制
- **ControlForGameFight** - 战斗游戏控制

### 关键 Bean
- **FightBean** - 战斗数据基类
- **FightBeanForConquer** - 征服模式战斗数据
- **FightBeanForDoomCouncil** - 终焉议会战斗数据
- **FightBeanForInfinite** - 无限模式战斗数据
- **FightBeanForTest** - 测试战斗数据
- **FightCreatureBean** - 战斗生物数据
- **FightAttackBean** - 战斗攻击数据
- **FightDropCrystalBean** - 战斗掉落水晶
- **FightRecordsBean** - 战斗记录
- **FightUnderAttackBean** - 受击数据

## 关键文件

| 文件 | 路径 |
|------|------|
| 战斗生物实体(通用) | Assets/Scripts/Game/Fight/FightCreatureEntity.cs |
| 战斗生物实体(进攻专属) | Assets/Scripts/Game/Fight/FightCreatureEntityForAttack.cs |
| 战斗生物实体(防守专属) | Assets/Scripts/Game/Fight/FightCreatureEntityForDefense.cs |
| 战斗生物实体(魔王专属) | Assets/Scripts/Game/Fight/FightCreatureEntityForDefenseCore.cs |
| 预制体实体 | Assets/Scripts/Game/Fight/FightPrefabEntity.cs |
| 魔物描边高亮跟随 | Assets/Scripts/Game/Fight/CreatureSpineOutlineFollow.cs |
| 场景基类 | Assets/Scripts/Component/Game/Scene/ScenePrefabBase.cs |
| 战斗控制 | Assets/Scripts/Component/Game/Control/ControlForGameFight.cs |
| FightManager | Assets/Scripts/Component/Manager/FightManager.cs |
| FightHandler | Assets/Scripts/Component/Handler/FightHandler.cs |

## 弹道渲染接线（DSP GPU Instancing）

`FightManager` 持有 `attackModeInstanceRenderer`（`AttackModeInstanceRenderer`，DSP 式弹道批量渲染器）；`FightHandler.UpdateHandleForAttackModePrefab` 在射线批处理/逻辑推进之后追加**阶段4** `RenderAll(listAttackMode)` 批量绘制在途弹道。按视觉桶签名(`visual_name` + ShowSprite 换图 + 自旋细分子桶)分桶(与 `prefab_name` 原预制渲染独立)，常开但 visual_name 空/未注册桶零副作用。**细节与约束以 attack-mode-system skill / game-attack-mode agent 为准**，本代理只需知道这条接线存在。

## 游戏速度（2倍速）对实体的要求

- **动画**：`FightCreatureEntity.SetAnimTimeScale(timeScale)` 设置 `SkeletonAnimation.timeScale`（与各 PlayAnim 的 animSpeed 相乘叠加）；`SetData` 里自动按 `GameFightLogic.GetCurrentGameSpeed()` 初始化（2倍速中途生成的生物动画也是 2 倍）。全场刷新由 `GameFightLogic.SetGameSpeed → RefreshAllCreatureAnimTimeScale()` 驱动。
- **计时**：实体/弹道/掉落物内一切按时间推进的逻辑必须用 `GameFightLogic.GetFightDeltaTime()`（= `Time.deltaTime × 当前游戏速度`，非战斗场景恒 1 倍）替代 `Time.deltaTime`。

## 约束

- 战斗实体之间通过事件通信，不要直接耦合
- 属性计算需考虑 BUFF 加成
- 战斗数据 Bean 变更需同步更新序列化逻辑
