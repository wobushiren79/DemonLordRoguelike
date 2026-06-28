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
- 稀有度 BUFF 生成两级收口：`RandomRarity()` 定好稀有度后调用 `CreatureBean.RandomRarityBuffForCreate()`（`CreatureBeanPartial.cs`）按稀有度逐级授予；单档随机走 `BuffUtil.CreateRandomRarityBuff(rarityEnum)`（`Assets/Scripts/Utils/BuffUtil.cs`），与**魔物进阶（UICreatureVat）共用同一口径**，并被测试面板 `UITestBase.OnClickForAddTestCreature` 复用。原 `GashaponItemBean.RandomRarityBuff(RarityEnum)` 已删除；改通用规则应改 `BuffUtil` / `RandomRarityBuffForCreate`，不要在 `GashaponItemBean` 内重新内联（详见 buff-system / utils-system skill）
- **稀有度命中率公式**：每档命中率 = 起始常量 `rarityBaseRate`(当前 10%) + 对应 `*Rate` 概率研究等级(每级+1%)，仅在该档已 `CheckIsUnlock` 时生效。R/SR/SSR 概率研究节点(`100401001`/`100402001`/`100403001`)`level_max` 均 50，每档上限 10%+50%=60%。改起始概率改 `rarityBaseRate` 常量即可
- **稀有度展示概率**：`GashaponItemBean.GetRarityProbabilityList()`（静态）把顺序判定 UR→SSR→SR→R→N 换算成**真实命中概率**（普通 N=剩余补足，合计=1，列表按 普通→R→SR→SSR→UR 排序，仅含已解锁档位+普通），供孕育商店项的可抽生物概率弹窗使用。与 `RandomRarity()` 的实际抽取共用同一套 `GashaponRarity*`/`*Rate` 解锁门控 + 同一 `rarityBaseRate` 起始概率——改概率口径需同步两处
