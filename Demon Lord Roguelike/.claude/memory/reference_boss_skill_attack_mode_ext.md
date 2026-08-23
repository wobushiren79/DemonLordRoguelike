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
命中特效(`effect_hit` → `EffectInfo`)的**前冲距离 = float_data 里 `Speed × LifeTime`（世界单位），跟攻击盒长度无关**。`BaseAttackMode.PlayEffectForHit` 只把 `colliderAreaSize[0]`(前方半长) 当 `size` 传给特效通道(2026-08-16 前是 `ShowEffect`，现经 `SingletonEffectParam.size` 传 `ShowEnduringSingletonEffect`)，而注入逻辑里**只有带 `isSize` 的 "Size" 参数被乘**(缩放粒子大小/粗细)，`Speed`/`LifeTime` 不受 size 影响。所以一个按"1格近战"调好的刀锋(`Effect_Slash_1` id=400001, ~~Speed:10&LifeTime:0.2=距离2~~ 2026-08-23 随 101005 攻击范围缩至前方1格(collider_area_size 1,1,1→0.5,1,0.4)同步改为 `Speed:5&LifeTime:0.2`=距离1)拿去复用到"前方6格"的 BOSS 技能就够不着。修法(2026-07)：给远距离攻击**单配一条 EffectInfo**(id=400002, `Speed:30&LifeTime:0.2`=距离6, res_name 仍复用 Effect_Slash_1 预制)，把 `102001.effect_hit` 指向它，不动仍复用 400001 的 `101001`。2026-08：预制重命名为 `Effect_Slash_1`(与表内其它 Effect_ 前缀一致)，`101001`(魔物4001战之魅魔专用) 的 effect_hit 已改指新粒子 `Effect_Slash_2`(id=400003, **全局单例重播**：与落雷同构，`PlayEffectForHit` 直接调 `EffectHandler.ShowEnduringSingletonEffect(effectId, startPosition)`——2026-08-16 起**所有**攻击模式的 effect_hit 统一走该方法(传击中粒子ID+SingletonEffectParam 结构体，字段为哨兵默认值 0=不设置、有参数才设置，如地面火焰 duration=燃烧时长/冲击波 startSize+startLifetime multiplier)、无 id 分流；manager 的 effectThunderId/effectSlashId/effectFloorFireId 字段已删除，击中粒子 ID 一律配在攻击模块表 effect_hit 列(斩击 101001=400003/落雷 300031~035=900003/火瓶 300101=1800001)，`GetEffectForEnduring` 单例+移动实例+`Stop(StopEmitting)`保活+`Play` 重播，多生物同时出刀=多刀交叠旧刀光不消失；实测(Unity 6.3)确认 Stop(StopEmitting) 后 Play() 从 time0 重启、重触发 burst、不清理旧粒子，机制正确；预制已确认 World 空间模拟+burst 一次性爆发(主粒子/Sparks/Dark 三粒子 simulationSpace=1、rateOverTime=0、非循环)，无需改预制；配置表行仍 show_type=0/show_time=0.5 但被单例路径接管不参与池化，float/int/vector3 数据为空故 Size/StartPosition 配置无效)。**2026-08-22 VFX 注入回归修复（BOSS 刀光不显示的根因）**：325fb71 统一单例通道时丢失了 VFX 参数注入——`Effect_Slash_1`(400001/400002) 是 **VFX Graph 特效**（非 PS，无 ParticleSystem，预制暴露属性 Speed=0/LifeTime=0.1/Direction=2 仅为占位默认值），其前冲距离/朝向/发射点全靠 EffectInfo 配置经 C# `SetFloat/SetInt/SetVector3` 注入；注入丢失后 BOSS 技能(102001→effect_hit=400002)按默认值播放=Speed 0 原地、0.1s 即逝、方向固定向右→玩家看不到。修复：`SingletonEffectParam` 增 `direction`/`size` 字段（哨兵 0/None=不覆盖），`ShowEnduringSingletonEffect` 内新增 `InjectVfxConfigData`（EffectInfo 的 float/int/vector3/vector4 配置写入 VFX 暴露属性，{Direction}/{Size}/{StartPosition} 占位照 ShowEffect 旧逻辑，无 VFX 组件或配置为空跳过），`PlayEffectForHit` 恢复传 direction(attackDirection.x>0?Right:Left)/size(colliderAreaSize[0])。PS 新粒子(400003/900003/1700001/1800001)配置数据为空不受影响。**⚠️透明度坑(2026-08 已修)**：刀光三材质(slash03_AB/magic_orb2_ADD/slash03_ADD)均开**软粒子**(SoftParticles 深度带 0~0.2/0~0.5，URP m_RequireDepthTexture=1 已开)，粒子贴地部分按视线深度差实时淡出——4001 的 `attack_start_position` 原本**为空**(→zero) 发射点 y≈0 整片埋地(面片半高 2.25) → 症状"每次透明度不一样、甚至为 0"。修法分两步：①配置表 4001 行 `attack_start_position` 填 `0,0.5,0`(2026-08-16 从 0.6 调低；Excel+JSON 均已同步；注意此字段同时是近战判定盒中心高度，改后判定盒 Y 从[-1,1]变[-0.5,1.5]，贴地目标仍可命中)；②代码曾加 `targetPos.y += 0.4`(0.5+0.4=y≈0.9) 减轻埋地——2026-08-16 工作区已移除该偏移(统一到 `ShowEnduringSingletonEffect` 后不再保留)，现仅靠配置表 `attack_start_position=0,0.5,0` 出手点抬高；若刀光再现"透明度时高时低甚至为 0"，先查播放高度与软粒子带、考虑恢复该偏移。未动材质。
- **运行链（融入普通攻击循环，非并行）**：逻辑全在**基类** `AIIntentCreatureAttack`（不是新意图！）：`IntentEntering→InitExtraAttack()` 经 `fightCreatureData.creatureData.creatureNpcData?.npcInfo.GetListAttackModeExtInfo()` 读取、按 `ext_type==BossSkill` 筛选并重置各计时器；`IntentUpdate→UpdateExtraAttackTimer()` 每帧**仅累加**各额外攻击CD。**释放融入普通攻击循环**：`AttackCreatureStart`（attackState==0 准备完毕的判定点）调 `GetReadyExtraAttack()` 选第一个CD已到的（**优先级>普通攻击**），`AttackCreatureStartEnd` 用其 `attack_mode_id` 经 `StartCreateAttackMode(self, target, ActionForAttackEnd, customAttackModeId)` 发射并清零该CD、当次不出普通攻击；`IntentLeaving` 清空。CD到了**不会立刻打断**当前攻击，要等下次 `attackState==0` 判定。每攻击循环最多一次攻击 → 多个就绪逐循环出、天然串行（无需 isReleasing 标志）。未来新增 `ext_type` 在 `InitExtraAttack` 筛选处加分支。

放在基类的好处：进攻/防守生物（`AIIntentAttackCreatureAttack`/`AIIntentDefenseCreatureAttack`）都自动获得，**无需新增意图/枚举/工厂**；玩家生物无 `creatureNpcData`(null) 故不受影响。

## ⚠️ 术语澄清（多个 AI 在此踩坑）
游戏内"BOSS" = **敌方强力 NPC**（征服 `enemy_boss_ids` 刷出，创建为 `CreatureFightTypeEnum.FightAttack` 进攻型 → `AIAttackCreatureEntity` → `AIIntentAttackCreatureAttack`，见 `CreatureHandler` 敌人创建路径）。它**不是** `AIDefenseCoreCreatureEntity`——那是玩家防守的"魔王核心/水晶"。给 BOSS 加主动技能要改**攻击意图基类**，绝不是改核心生物 AI。

## 关键文件
- 逻辑：[AIIntentCreatureAttack.cs](Assets/Scripts/AI/Creature/AIIntentCreatureAttack.cs)（`#region 额外攻击`，方法 `InitExtraAttack/UpdateExtraAttack/ReleaseExtraAttack`）
- 解析：[NpcInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/NpcInfoBeanPartial.cs) `GetListAttackModeExtInfo()`、[AttackModeExtInfoBeanPartial.cs](Assets/Scripts/Bean/MVC/Game/AttackModeExtInfoBeanPartial.cs) `GetExtType()`、[AttackModeExtTypeEnum.cs](Assets/Scripts/Enums/AttackModeExtTypeEnum.cs)
- 配置表：`excel_npc_info[NPC信息]`(列 attack_mode_ext)、`excel_attackmode_ext_info[攻击模块扩展信息]`、`excel_attackmode_info[攻击方式]`

新增 `NpcInfo` 列后，自动生成的 `NpcInfoBean.cs` 需在 Unity 跑 ExcelEditorWindow「生成 Entity」才会带上 `attack_mode_ext` 字段（否则 Partial 引用该字段编译不过）。相关：[[feedback_bean_partial]]、[[feedback_excel_id_sorted_insert]]、[[reference_language_excel_source]]
