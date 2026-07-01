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
- **稀有度 BUFF 分档设计规则（buff_type 11/12/13，硬约束）**：新增/设计稀有度 BUFF 必须按效果性质归对档位——**R(11)=纯属性 BUFF**（常驻数值加/减益、无触发，类 `BuffEntityAttribute`，可用 HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD/CMP，**无 HPRegeneration 生命回复属性**）；**SR(12)=条件/周期被动触发**（累计伤害/受击/击杀/血量阈值或按周期触发，类 `BuffEntityConditional*`(非死亡)/`Periodic*`/`Pecurrent`）；**SSR(13)=特殊类**（死亡重生/死亡反击/死亡区域效果/克隆增殖/生成改变水晶掉落等质变，类 `BuffEntityConditionalDead*`/`BuffEntityInstant*`）。生成入口 `CreateRandomRarityBuff` 只按 buff_type 取池随机、不校验性质，归档正确性靠人工保证。高稀有度累积低档（SSR=R+SR+SSR 各 1）。**完整分档表见 buff-system / gashapon-system SKILL「扭蛋/稀有度 BUFF 分档设计规则」（单一真实源）**
- **稀有度命中率公式**：每档命中率 = 起始常量 `rarityBaseRate`(当前 10%) + 对应 `*Rate` 概率研究等级(每级+1%)，仅在该档已 `CheckIsUnlock` 时生效。R/SR/SSR 概率研究节点(`100401001`/`100402001`/`100403001`)`level_max` 均 50，每档上限 10%+50%=60%。改起始概率改 `rarityBaseRate` 常量即可
- **稀有度展示概率**：`GashaponItemBean.GetRarityProbabilityList()`（静态）把顺序判定 UR→SSR→SR→R→N 换算成**真实命中概率**（普通 N=剩余补足，合计=1，列表按 普通→R→SR→SSR→UR 排序，仅含已解锁档位+普通），供孕育商店项的可抽生物概率弹窗使用。与 `RandomRarity()` 的实际抽取共用同一套 `GashaponRarity*`/`*Rate` 解锁门控 + 同一 `rarityBaseRate` 起始概率——改概率口径需同步两处
