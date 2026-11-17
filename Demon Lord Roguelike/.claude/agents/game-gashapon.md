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

`GameHandler.StartGashaponMachine` 先准备候选孕育逻辑，`PreGame` 只在整批成功提交后将它设为活动实例并播放破壳演出。首次失败继续保留基地逻辑，重置失败保留原批结果。

孕育整批生成后由 `GameDataManager.TryCommitGashaponBatch` 提交魔晶、抽取计数与背包魔物。`UserDataService.TrySaveGashaponBatch` 原子写入同槽位 `GashaponPending_{slot}` 日志作为提交点，快照包括主档、背包生物和成就。读档或再次保存前幂等回放，三份文件完成后清除日志。日志写入前失败回滚本批内存，保存成功后才通知扣款并进入演出。
- **GashaponMachineLogic** - 扭蛋机逻辑，继承 BaseGameLogic；整批生成后，由 `GameDataManager.TryCommitGashaponBatch` 入账并为每只派发 `Achievement_GashaponDraw(creatureId)` 累计职业抽取统计（成就类型5/研究 pre_data「职业抽出99只」条件消费）

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
- **职业独立扭蛋**：除 7 族×3 档种族混合抽（id=X000N）外，29 个可抽职业各有 x1/x5/x10 独立扭蛋（id=creatureId×10+档位，creature_ids 单职业必中，价格=族混合抽×10，图标=职业 class_icon_res），解锁走职业扭蛋研究（300X1NNND，见 research-system）；纯配置驱动，抽取逻辑无需改代码
- **可抽职业速查（29 个）**：各职业攻击方式、基础属性、自带固有能力（暴击/闪避/暴击伤害）、元素异常概率见 [creature-system 参考表](../skills/creature-system/reference/gacha-creature-classes.md)。注意职业级差异有一部分在 `CreatureInfo.creature_buff`（出生自带 BUFF）而非属性列：6001 哥布林刺客 闪避50%/暴击50%/暴伤200%、2001 骷髅战士 闪避20%、1002/2002 暴击20%/暴伤200%、7001 兽人战士 HP<50% 狂暴；`CRT/EVA/CDMG` 基础值 0/0/1.5 且装备不产出。骷髅族全员可攻击身后（`attack_search_back=1`）
- 扭蛋道具和普通道具数据隔离
- 扭蛋机 UI 使用 UIGashapon 前缀命名
- `RandomRarity()` 定好稀有度后调用 `CreatureBean.RandomRarityBuffForCreate()`，按稀有度逐级授予 BUFF。单档随机走 `BuffUtil.CreateRandomRarityBuff(rarityEnum)`，由孕育、测试面板创建魔物和升阶药水复用。基地魔物进阶走 `BuffUtil.CreateAscendRarityBuff` 按素材继承。
- 所有职业都可以获得再补一刀、鞭尸成瘾、碰瓷反杀、越打越嗨。战斗追加攻击排队时，`BuffUtil.CanPerformBuffAttack` 会跳过无有效攻击模块、冲锋自爆和主动自爆的魔物。人类孕育 x1 的冰法前置为 `300100004`。
- SSR 完整奖池共 11 条。「策反死者」的召唤人数和「震撼到场」的击退距离在生成词条时按 1～3 抽定并随魔物存档，单个词条的战斗触发始终使用该数值。
- **稀有度 BUFF 分档设计规则（buff_type 11/12/13，硬约束）**：新增/设计稀有度 BUFF 必须按效果性质归对档位——**R(11)=纯属性 BUFF**（常驻数值加/减益、无触发，类 `BuffEntityAttribute`，可用 HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD/CMP，**无 HPRegeneration 生命回复属性**）；**SR(12)=条件/周期被动触发**（累计伤害/受击/击杀/血量阈值或按周期触发，类 `BuffEntityConditional*`(非死亡)/`Periodic*`/`Pecurrent`）；**SSR(13)=特殊类**（死亡重生/死亡反击/死亡区域效果/克隆增殖/生成改变水晶掉落等质变，类 `BuffEntityConditionalDead*`/`BuffEntityInstant*`）。生成入口 `CreateRandomRarityBuff` 按 `buff_type` 从完整奖池抽取，词条效果分档由配表者维护。高稀有度累积低档（SSR=R+SR+SSR 各 1）。**完整分档表见 buff-system / gashapon-system SKILL「扭蛋/稀有度 BUFF 分档设计规则」（单一真实源）**
- **稀有度命中率公式**：每档命中率 = 稀有度配置表基础概率 `RarityInfo.gashapon_rate`（按档配置：R=25/SR=20/SSR=15，N/UR/L=10，魔王档=0；2026-10 起从写死 `rarityBaseRate` 10% 迁移，常量已删除） + 对应 `*Rate` 概率研究等级(每级+1%)，仅在该档已 `CheckIsUnlock` 时生效。公式已收口 `UserUnlockBean.GetGashaponRarityRateForLevel(rarity, level)`（`RandomRarityItem` 实际抽取/`GetRarityProbabilityList` 展示/研究气泡详情三处共用，改口径只改这一处）。R/SR/SSR 概率研究节点(`100401001`/`100402001`/`100403001`)`level_max` 均 50，满加成 R=75%/SR=70%/SSR=65%。改基础概率改 `excel_rarity_info` 的 `gashapon_rate` 列即可（Bean 字段暂手写于 `RarityInfoBeanPartial` 临时 region，重新生成 Entity 后删除）
- **稀有度展示概率**：`GashaponItemBean.GetRarityProbabilityList()`（静态）把顺序判定 UR→SSR→SR→R→N 换算成**真实命中概率**（普通 N=剩余补足，合计=1，列表按 普通→R→SR→SSR→UR 排序，仅含已解锁档位+普通），供孕育商店项的可抽生物概率弹窗使用。与 `RandomRarity()` 的实际抽取共用同一套 `GashaponRarity*`/`*Rate` 解锁门控，概率数值统一走 `UserUnlockBean.GetGashaponRarityRateForLevel(rarity, level)`（= `RarityInfo.gashapon_rate` + 等级，2026-10 收口，两处调用点+研究气泡详情共用）
