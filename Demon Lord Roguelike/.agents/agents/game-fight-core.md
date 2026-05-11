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
- **FightCreatureEntity** - 战斗生物实体，管理生物在战斗中的完整生命周期
- **FightPrefabEntity** - 战斗预制体实体

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
| 战斗生物实体 | Assets/Scripts/Game/Fight/FightCreatureEntity.cs |
| 预制体实体 | Assets/Scripts/Game/Fight/FightPrefabEntity.cs |
| 场景基类 | Assets/Scripts/Component/Game/Scene/ScenePrefabBase.cs |
| 战斗控制 | Assets/Scripts/Component/Game/Control/ControlForGameFight.cs |
| FightManager | Assets/Scripts/Component/Manager/FightManager.cs |
| FightHandler | Assets/Scripts/Component/Handler/FightHandler.cs |

## 约束

- 战斗实体之间通过事件通信，不要直接耦合
- 属性计算需考虑 BUFF 加成
- 战斗数据 Bean 变更需同步更新序列化逻辑
