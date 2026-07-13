---
name: reference_boss_skill_attack_mode_ext
description: 额外攻击(攻击模块扩展 attack_mode_ext,旧称BOSS技能,命名通用不限于BOSS)按间隔自动释放机制：NpcInfo.attack_mode_ext → AttackModeExtInfo → 基类攻击意图消费；含"BOSS=进攻型敌人非防守核心"的术语澄清
metadata:
  type: reference
---

# 额外攻击（攻击模块扩展 attack_mode_ext）

> 命名说明：机制叫"额外攻击"（ExtraAttack，通用、不限于 BOSS）；`ext_type` 区分类型，目前仅 `1`=BOSS技能(`AttackModeExtTypeEnum.BossSkill`)按间隔释放。曾用名"BOSS技能"已改为通用的"额外攻击"。

带 `attack_mode_ext` 的 NPC（典型为敌方 BOSS，如 `1010020001` 持盾战士-Boss）进入攻击状态后，在普通攻击之外**按各额外攻击自己的间隔额外释放**攻击模块。数据链与运行链：

- **配置链**：`NpcInfo.attack_mode_ext`（逗号分隔的 `AttackModeExtInfo` id）→ `AttackModeExtInfo`（`ext_type=1`=BOSS技能=`AttackModeExtTypeEnum.BossSkill`；`trigger_interval` 释放间隔秒；`attack_mode_id` 指向 `AttackModeInfo`）→ `AttackModeInfo`（真正的攻击模块）。几何：`AttackModeMeleeArea` + `collider_area_type=24(AreaBoxFront)`，`collider_area_size`=(X前方半长,Y高度半长,Z横向/上下半长)，1格=1世界单位，盒覆盖前方 [0, 2*X]。例：`102001`(BOSS技能)="3,1,0.25"=前方6格(同lane)；`101001`(普通镰刀)="0.5,1,1"=前方1格+上下±1格。

## ⚠️ 命中特效的"射程"与攻击范围各管各（易脱节）
命中特效(`effect_hit` → `EffectInfo`)的**前冲距离 = float_data 里 `Speed × LifeTime`（世界单位），跟攻击盒长度无关**。`BaseAttackMode.PlayEffectForHit` 只把 `colliderAreaSize[0]`(前方半长) 当 `size` 传给 `EffectHandler.ShowEffect`，而 `ShowEffect` 里**只有带 `isSize` 的 "Size" 参数被乘**(缩放粒子大小/粗细)，`Speed`/`LifeTime` 不受 size 影响。所以一个按"1格近战"调好的刀锋(`EffectSlash_1` id=400001, `Speed:10&LifeTime:0.2`=距离2)拿去复用到"前方6格"的 BOSS 技能就够不着。修法(2026-07)：给远距离攻击**单配一条 EffectInfo**(id=400002, `Speed:30&LifeTime:0.2`=距离6, res_name 仍复用 EffectSlash_1 预制)，把 `102001.effect_hit` 指向它，不动仍复用 400001 的 `101001`。
- **运行链（融入普通攻击循环，非并行）**：逻辑全在**基类** `AIIntentCreatureAttack`（不是新意图！）：`IntentEntering→InitExtraAttack()` 经 `fightCreatureData.creatureData.creatureNpcData?.npcInfo.GetListAttackModeExtInfo()` 读取、按 `ext_type==BossSkill` 筛选并重置各计时器；`IntentUpdate→UpdateExtraAttackTimer()` 每帧**仅累加**各额外攻击CD。**释放融入普通攻击循环**：`AttackCreatureStart`（attackState==0 准备完毕的判定点）调 `GetReadyExtraAttack()` 选第一个CD已到的（**优先级>普通攻击**），`AttackCreatureStartEnd` 用其 `attack_mode_id` 经 `StartCreateAttackMode(self, target, ActionForAttackEnd, customAttackModeId)` 发射并清零该CD、当次不出普通攻击；`IntentLeaving` 清空。CD到了**不会立刻打断**当前攻击，要等下次 `attackState==0` 判定。每攻击循环最多一次攻击 → 多个就绪逐循环出、天然串行（无需 isReleasing 标志）。未来新增 `ext_type` 在 `InitExtraAttack` 筛选处加分支。

放在基类的好处：进攻/防守生物（`AIIntentAttackCreatureAttack`/`AIIntentDefenseCreatureAttack`）都自动获得，**无需新增意图/枚举/工厂**；玩家生物无 `creatureNpcData`(null) 故不受影响。

## ⚠️ 术语澄清（多个 AI 在此踩坑）
游戏内"BOSS" = **敌方强力 NPC**（征服 `enemy_boss_ids` 刷出，创建为 `CreatureFightTypeEnum.FightAttack` 进攻型 → `AIAttackCreatureEntity` → `AIIntentAttackCreatureAttack`，见 `CreatureHandler` 敌人创建路径）。它**不是** `AIDefenseCoreCreatureEntity`——那是玩家防守的"魔王核心/水晶"。给 BOSS 加主动技能要改**攻击意图基类**，绝不是改核心生物 AI。

## 关键文件
- 逻辑：[AIIntentCreatureAttack.cs](Assets/Scripts/AI/Creature/AIIntentCreatureAttack.cs)（`#region 额外攻击`，方法 `InitExtraAttack/UpdateExtraAttack/ReleaseExtraAttack`）
- 解析：[NpcInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs) `GetListAttackModeExtInfo()`、[AttackModeExtInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/AttackModeExtInfoBeanPartial.cs) `GetExtType()`、[AttackModeExtTypeEnum.cs](Assets/Scripts/Enums/AttackModeExtTypeEnum.cs)
- 配置表：`excel_npc_info[NPC信息]`(列 attack_mode_ext)、`excel_attackmode_ext_info[攻击模块扩展信息]`、`excel_attackmode_info[攻击方式]`

新增 `NpcInfo` 列后，自动生成的 `NpcInfoBean.cs` 需在 Unity 跑 ExcelEditorWindow「生成 Entity」才会带上 `attack_mode_ext` 字段（否则 Partial 引用该字段编译不过）。相关：[[feedback_bean_partial]]、[[feedback_excel_id_sorted_insert]]、[[reference_language_excel_source]]
