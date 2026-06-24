---
name: game-gashapon
description: 扭蛋系统开发：GashaponMachineLogic 扭蛋机逻辑、扭蛋破坏、扭蛋道具数据。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Logic/GashaponMachineLogic.cs
  - Assets/Scripts/Bean/Game/GashaponMachineBean.cs
  - Assets/Scripts/Bean/Game/GashaponItemBean.cs
  - Assets/Scripts/Component/UI/Game/GashaponMachine/
  - Assets/Scripts/Component/UI/Game/GashaponBreak/
---

# 扭蛋系统 (Gashapon System) 开发代理

你负责 [Scripts/](Assets/Scripts/) 中与扭蛋相关的代码开发。

## 职责范围

### 扭蛋逻辑
- **GashaponMachineLogic** - 扭蛋机逻辑，继承 BaseGameLogic

### 扭蛋数据
- **GashaponMachineBean** - 扭蛋机配置数据
- **GashaponItemBean** - 扭蛋道具数据

### 扭蛋 UI
- **UIGashaponMachine** - 扭蛋机界面
- **UIGashaponBreak** - 扭蛋爆裂界面

### 关键文件

| 文件 | 路径 |
|------|------|
| 扭蛋逻辑 | Assets/Scripts/Game/Logic/GashaponMachineLogic.cs |
| 扭蛋机数据 | Assets/Scripts/Bean/Game/GashaponMachineBean.cs |
| 扭蛋道具数据 | Assets/Scripts/Bean/Game/GashaponItemBean.cs |
| 扭蛋机 UI | Assets/Scripts/Component/UI/Game/GashaponMachine/ |
| 扭蛋爆裂 UI | Assets/Scripts/Component/UI/Game/GashaponBreak/ |

## 约束

- 扭蛋逻辑通过 GashaponMachineLogic 统一管理
- 扭蛋道具和普通道具数据隔离
- 扭蛋机 UI 使用 UIGashapon 前缀命名
- 稀有度 BUFF 生成已收口到 `BuffUtil`：`GashaponItemBean.RandomRarityBuff(RarityEnum)` 改为调用 `BuffUtil.CreateRandomRarityBuff(rarityEnum)`（`Assets/Scripts/Utils/BuffUtil.cs`），行为不变，与**魔物进阶（UICreatureVat）共用同一口径**。改这条通用规则应改 `BuffUtil`，不要在 `GashaponItemBean` 内重新内联 switch（详见 buff-system / utils-system skill）
