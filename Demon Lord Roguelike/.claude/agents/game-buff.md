---
name: game-buff
description: BUFF系统开发：属性/瞬时/条件/周期(无限)/周期(有限)5种BUFF实体类型 + 7种前置条件 + 5种堆叠策略；BuffEventDispatcher事件分发、ModifierPipeline属性修改管线、深渊馈赠等级替换。
tools: Read, Write, Edit, Glob, Grep, Bash
watched_files:
  - Assets/Scripts/Game/Buff/
  - Assets/Scripts/Game/Attribute/AttributeModifier.cs
  - Assets/Scripts/Component/Handler/BuffHandler.cs
  - Assets/Scripts/Component/Manager/BuffManager.cs
  - Assets/Scripts/Bean/Game/BuffBean.cs
  - Assets/Scripts/Utils/BuffUtil.cs
  - Assets/Scripts/Bean/Game/BuffEntityBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs
  - Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs
  - Assets/Scripts/Bean/MVC/Game/BuffPreInfoBean.cs
---

# BUFF 系统 (Buff System) 开发代理

你负责 `Assets/Scripts/Game/Buff/` 中的 BUFF 系统开发，包括属性管线、堆叠策略、事件分发与前置条件。

## 职责范围

### BUFF 实体类型体系
```
BuffBaseEntity                              # 抽象基类
├── BuffEntityAttribute                     # 属性BUFF（实现 IAttributeModifierSource）
│   ├── BuffEntityAttributeAttackTime       # 改攻击前摇/动画时间（独立通道，不走管线）
│   │   └── BuffEntityAttributeAttackTimeSingleTarget  # 单体定向攻速BUFF：随机锁定一只防守生物攻速翻倍(攻击时间×0.5)（曾用于馈赠「急性子」，现役无配置、机制留存）
│   ├── BuffEntityAttributeSingleTarget    # 单体定向属性BUFF：随机锁定一只防守生物 ATK/HP/DR 翻倍（曾用于馈赠「大力出奇迹/膘肥体壮/钢铁憨憨」，现役无配置、机制留存）
│   ├── BuffEntityAttributeDynamicRate     # 动态率属性BUFF基类：加成率运行时算(GetDynamicRate)非配置写死，仅走 PercentAdd
│   │   ├── BuffEntityAttributeScaleByDefenseCount  # 通用功能类：属性%随"当前场上存活防守魔物数N"缩放，率=(N-1)×每只率(曾用于馈赠「都是兄弟」，现役无配置、可复用)
│   │   └── BuffEntityAttributeScaleByKillCount     # 通用功能类(兼 IBuffSingleTarget)：选取时随机锁定一只防守生物，属性%随"该只自身累计击杀敌人数"缩放，率=该只killNum×每只率(曾用于馈赠「杀红了眼」，现役无配置、可复用)
│   └── BuffEntityAttributeMulti            # 多属性BUFF：一次随机率同时改多个属性(class_entity_data "ATK:1|HP:-1"=ATK+率/HP等量负率)，实现"一增益、对应属性等比减益"；扭蛋R级双刃(狂战士/快枪手/铜墙铁壁/大块头 A/B/C)。纯属性BUFF走烘焙路径
├── BuffEntityBaseHPChange                  # 周期改血（按目标最大HP%）：value+目标最大HP×rate，正回负扣；子类Area=范围版；中毒1000400001/持续回血12000100001
├── BuffEntityBaseHPChangeByApplierATK      # 周期掉血（按施加者实时ATK）：伤害=施加者当前ATK×|rate|+value，施加者不在场/死亡跳过本次；火骷髅法师「烧伤」1000500001（攻击模式201003命中50%挂，3秒×3次，rate=-0.5）
├── BuffEntityInstant                       # 瞬时触发（SetData后isValid=false）
│   ├── BuffEntityInstantCloneDefenseCreature      # 深渊馈赠「增殖」
│   ├── BuffEntityInstantRewardMoreItem            # 深渊馈赠「奖励多多」
│   └── BuffEntityInstantRewardMoreSelect          # 深渊馈赠「再来一瓶」
├── BuffEntityConditional                   # 条件触发（UpdateBuffTime只增总时长）
│   ├── BuffEntityConditionalAttack         # 攻击/受击事件触发自定义 AttackMode
│   ├── BuffEntityConditionalAttackAgain    # 触发AI立即再攻击一次
│   ├── BuffEntityConditionalThorns         # 荆棘反伤(SSR稀有度「荆棘之躯」:受击时名义承伤×rate反弹给攻击者,上限自身当前HP;反伤走UnderAttack管线,重写EventForUnderAttack自做归属过滤)
│   ├── BuffEntityConditionalLifeSteal      # 伤害吸血(SSR稀有度「吸一吸」:自身造伤×rate转自身HP,走RegainHP正式治疗路径;BUFF来源伤害(反伤/再攻击)同样触发,AOE每目标各触发)
│   ├── BuffEntityConditionalInvincible     # 上场无敌(SSR稀有度「真男人」:挂上即无敌trigger_value秒(5~10)、color_body染金,到期清旗标移除;驱动FightCreatureBean.isInvincible,SetData/ClearData/UpdateBuffTime三处自理旗标与刷新颜色)
│   ├── BuffEntityConditionalAttribute      # 属性变化时触发
│   ├── BuffEntityConditionalDead           # 自身死亡结束时触发
│   ├── BuffEntityConditionalDeadAttack     # 死亡时发起一次攻击
│   ├── BuffEntityConditionalDeadRebirth    # 死亡时重生
│   ├── BuffEntityConditionalDeadAreaHPChange / DeadAreaDRChange  # 死亡时范围改HP/DR
│   ├── BuffEntityConditionalDeadCreateCrystal                    # 死亡时生成水晶
│   ├── BuffEntityConditionalAddDropCrystal                       # 死亡掉落水晶时叠加
│   ├── BuffEntityConditionalCreateCrystal                        # 生成水晶
│   └── BuffEntityConditionalDemonLordMPRegain                    # 给魔王(防守核心)回复魔力(SR稀有度「左膀右臂」击杀1个/「舍己为王」累计承伤100,回复trigger_value点(10~20),ChangeMP钳制上限+RefreshMPShow)
├── BuffEntityPeriodic                      # 周期性触发（无次数）
│   ├── BuffEntityPeriodicAttackAgain       # 周期性再攻击
│   ├── BuffEntityPeriodicAttackMultiInstant # 周期性多次瞬时攻击（深渊馈赠「闪电」落雷：BUFF只管触发→快照全场敌人→不放回抽N个主目标(一轮内多道雷主目标互不重复，敌人少于雷数只发同等数量；溅射不受限——同一目标可被多道雷重复溅射、被溅射过的仍可作主目标)→第1道立即+后续0.1秒间隔连发(UpdateBuffTime驱动)；每道雷=发射 AttackModeInstantAreaThunder 攻击模块，粒子/AOE/伤害走 AttackMode 框架(半径/单雷命中上限配在攻击模块表 collider_area_size/hit_max，AOE多目标伤害按命中次序依次减半保底1，单次攻击内局部去重，伤害=魔王实时ATK×trigger_value 发射时注入,CRT=0)；class_entity_data="次数,攻击模块ID"；馈赠「闪电」3000300001~005 → 攻击模块300031~300035）
│   ├── BuffEntityPeriodicAttackRoad       # 周期性随机道路冲撞（深渊馈赠「失控的矿车」：随机选 N 条道路各从最左端驶出 1 辆矿车沿路向右碾压；车数>路数时每路先各 1 辆、多出随机重复分配，同路第 2 辆起 0.5 秒间隔错开(UpdateBuffTime 驱动批次队列)；伤害=魔王实时ATK×trigger_value 发车时注入、CRT=0，逐目标减半由攻击模块处理；class_entity_data="车数,攻击模块ID"；馈赠 2000003001~005 → BUFF 3000400001~005 → 5 级共用攻击模块 300041=AttackModeRangedPiercingRoad）
│   ├── BuffEntityPeriodicAttackBoomerang  # 周期性回旋镖（深渊馈赠「死亡回旋」：从魔王位置+CreatureInfo.attack_start_position偏移发射回旋镖，锁定点=目标位置(高度取魔王发射点高度)；不放回抽目标，一轮内多镖目标互不重复、敌人少于镖数只发同等数量；第1镖立即发射，后续0.2秒间隔队列发射(照闪电UpdateBuffTime间隔模式)；去程命中后伤害逐目标减半，越过目标一格后返回魔王身边；速度曲线=时间参数化：去程前半程全速后半程线性减速至0(折返点静止)、返程从0匀加速至ReturnMaxSpeedRate(1.5)倍；弹体自旋=-720度/秒绕Z轴(InitAttackModeShow写spinSpeed/spinAxis走DSP自旋子桶，每发随机相位)；class_entity_data="镖数,攻击模块ID"；馈赠 2000004001~005 → BUFF 3000500001~005 → 5 级共用攻击模块 300051=AttackModeRangedBoomerang）
│   ├── BuffEntityPeriodicAttackBounceAxe  # 周期性弹跳斧头（深渊馈赠「跳跳斧」：目标=随机一排的最远敌人(按 roadIndex 分组、同路 x 降序)，不放回抽取(随机路取最远,首目标互不重复,敌人少于斧数只扔同等数量)；第1斧立即+后续0.2秒间隔队列发射；伤害=魔王实时ATK×trigger_value 注入、CRT=0；弹跳/追踪/伤害减半由 AttackModeRangedArcBounce 处理，弹跳次数经 StartAttack 前写 bounceMax 注入；class_entity_data="斧数,攻击模块ID,弹跳次数"；馈赠 2000005001~005 → BUFF 3000600001~005 → 5 级共用攻击模块 300061=AttackModeRangedArcBounce，弹跳半径配在其 collider_area_size 第1项）
│   ├── BuffEntityPeriodicAttackOrbit      # 常驻环绕书本（深渊馈赠「知识的力量」：随机选 1 只最前排(x 最大)存活魔物为宿主，环绕 N 本书(半径0.75、高度取魔王位置+攻击起始偏移、转速Lv1~5=0.6/1.2/1.8/2.4/3弧度/秒、均分角)；宿主死亡/有更前排魔物→改选(并列随机1只、并列不换防抖动)，无己方魔物→全销毁、有再生成；书本=常驻攻击模块 AttackModeOrbit 不自毁、触碰命中每只书对同一敌人0.5秒冷却、伤害=魔王实时ATK×trigger_value(0.4)命中瞬间取、CRT=0；关卡切换被回收→整套重建；trigger_time=9999 周期触发永不发生；class_entity_data="书本数,攻击模块ID,转速"；馈赠 2000006001~005 → BUFF 3000700001~005 → 5 级共用攻击模块 300071=AttackModeOrbit）
│   ├── BuffEntityPeriodicAttackRebound    # 常驻回弹菱块维持（深渊馈赠「回弹菱块」：每 trigger_time(1秒) 自检——本BUFF发射且仍存活(isValid)的菱块不足 class_entity_data[0] 颗即补足，满编无事；菱块=永久弹(不销毁、不累积、总数恒定=弹数)，关卡切换被 ClearAttackModePrefab 清掉后自动补回，升级替换/清空时 ClearData 销毁全部存量弹；发射=魔王位置+攻击起始偏移，前向锥 ±75° 随机角，无敌人也照常发射；伤害=魔王实时ATK×trigger_value(1)注入(保底1)、CRT=0；反弹(±5°偏转)/同目标1秒冷却由 AttackModeRangedRebound 处理，弹速按级配在攻击模块行 speed_move=2~4；class_entity_data="弹数,攻击模块ID"；馈赠 2000007001~005 → BUFF 3000800001~005 → 攻击模块 300081~300085=AttackModeRangedRebound，视觉 AttackModeVisual_Pingpang_1，图标 ui_abyssalblessing_pingpang）
│   ├── BuffEntityPeriodicAttackShockwave  # 周期性冲击波（深渊馈赠「第六次冲击」：每 trigger_time(10秒) 从魔王位置+CreatureInfo攻击起始偏移(+攻击模块start_pos_offset)处发出一道圆环冲击波 AttackModeShockwaveRing，半径扩张至覆盖整条道路(最大半径=道路右缘+余量−圆心x，攻击模块按当场路长计算)，扫到的敌人受伤害并被击退0.5(交 AI 击退意图 StartKnockback：方向固定 +x 沿道路向后推、固定0.2s推完、攻击循环打断、结束回闲置重索敌)；伤害=魔王实时ATK×trigger_value(0.1~0.3)发射时注入(保底1)、CRT=0，场上无敌人本轮不触发；class_entity_data="攻击模块ID"；馈赠 2000008001~005 → BUFF 3001100001~005 → 5 级共用攻击模块 300091=AttackModeShockwaveRing，视觉 Effect_Shockwave_1 走 EffectHandler.ShowShockwaveEffect(半径/时长同步)，图标 ui_abyssalblessing_boom）
│   ├── BuffEntityPeriodicAttackFireBottle  # 周期地形火焰瓶（深渊馈赠「瓶装炼狱火」：每 trigger_time(10秒) 快照存活敌人不放回抽 N 个主目标(同轮不重复、敌人少于瓶数只丢同等数量)、第1瓶立即+0.2秒间隔连发；每瓶=发射 AttackModeRangedArcGround 抛物线飞向固定落点不追踪(飞行段弹体自旋-720°/s绕-Z轴)，落地燃放半径1.2地形火焰持续5秒每1秒跳伤；伤害=魔王实时ATK×trigger_value(0.1)、CRT=0、不递减(多片叠加多次跳伤)，无敌人不触发；class_entity_data="瓶数,攻击模块ID"；馈赠 2000009001~005 → BUFF 3001200001~005 → 共用攻击模块 300101=AttackModeRangedArcGround，视觉 AttackModeVisual_FireBottle_1+燃烧段 ShowFloorFireEffect(Effect_FloorFire_1)，图标 ui_abyssalblessing_fire）
│   └── BuffEntityPeriodicPickupCrystal     # 周期性拾取水晶
└── BuffEntityPecurrent                     # 周期性触发（有次数 = trigger_num）
```

### BUFF 前置条件
```
BuffBasePreEntity (含 BuffPreEventRole 用于事件归属过滤)
├── BuffPreEntityForAttackDamage           # 累计造成伤害   EventRole=Attacker
├── BuffPreEntityForUnderAttackDamage      # 累计受到伤害   EventRole=Attacked
├── BuffPreEntityForHPRateLess             # HP 低于百分比 EventRole=Attacked
├── BuffPreEntityForKillNum                # 击杀数量       EventRole=None
├── BuffPreEntityForRegainHPReceived       # 累计被治疗HP   EventRole=Attacked (走 RegainHP 事件)
├── BuffPreEntityForRegainHPCast           # 累计施放治疗HP EventRole=Attacker (走 RegainHP 事件)
└── BuffPreEntityForOnFieldTime            # 在场存活时间秒 EventRole=None (纯时间驱动,读timeUpdateTotal,仅Gaming状态累积)
```
> 时间驱动条件配套实体 `BuffEntityConditionalAttributeTime`(继承 BuffEntityConditionalAttribute)：UpdateBuffTime 未达标逐帧调 HandleForEvent 跨阈值刷属性；class_entity_events 留空。

### 扭蛋/稀有度 BUFF 分档规则（buff_type 11/12/13）
稀有度 BUFF 池按稀有度分三档，每档对「效果性质」有硬约束（`BuffUtil.CreateRandomRarityBuff` 只按 buff_type 取池随机、不校验性质，归档正确性靠人工保证）：
```
R  (11) 纯属性 BUFF        —— 常驻数值加/减益、无触发条件；类 BuffEntityAttribute / 多属性双刃 BuffEntityAttributeMulti
                              可用属性 HP/DR/ATK/ASPD/MSPD/CRT/EVA/RCD/CMP（CRT/EVA rate走Flat；另有MP/MPR/MPF魔法向）
                              多属性双刃(BuffEntityAttributeMulti): class_entity_data "属性:倍率|属性:倍率"(如 ATK:1|HP:-1),各属性率=trigger_value_rate×倍率共享同一次随机(ATK+30%⇒HP-30%);纯属性→走烘焙,IsBuffEntityAttributeOnly 判定;id 段 11 0007~0010 0000X
                              注意:无 HPRegeneration 生命回复属性(实际枚举 index11=MPF魔法回复),游戏无被动回血刻
SR (12) 条件/周期被动触发   —— 累计伤害/受击/击杀/血量阈值/累计治疗/在场时间或按周期触发；
                              类 BuffEntityConditional*(非死亡,含时间驱动 BuffEntityConditionalAttributeTime)/Periodic*/Pecurrent，条件走 pre_info+BuffPreEntityFor*
SSR(13) 特殊类             —— 死亡重生/死亡反击/死亡区域治疗/克隆增殖/生成改变水晶掉落/荆棘反伤/伤害吸血/上场无敌等质变效果；
                              类 BuffEntityConditionalDead*/BuffEntityInstant*/BuffEntityConditionalThorns/LifeSteal/Invincible 等
高稀有度累积低档：SSR生物=R+SR+SSR各1、SR生物=R+SR各1（RandomRarityBuffForCreate 逐级授予）
```
> 详细分档表与设计自检见 buff-system SKILL「扭蛋/稀有度 BUFF 分档设计规则」，为单一真实源。

### 堆叠策略 BuffStackMode (BuffInfoBean.stack_mode)
```
0 Refresh           刷新次数/计时+施加者，不叠层（默认）
1 Stack             stackCount+1（受 stack_max 限制），变化时刷属性
2 Independent       完全独立实例，分别计时（多源 DOT）
3 Ignore            完全忽略新BUFF
4 ReplaceStrongest  仅当新 trigger_value 更大时替换旧实例
```

### 属性修改管线 (AttributeModifier.cs)
```
ModifierChannel: Flat → PercentAdd → PercentMul → Override
公式: v = (base + flatSum) * (1 + pctAddSum) * pctMulProduct  (Override时强覆盖取最高priority)
IAttributeModifierSource: BUFF/装备/天赋等实现该接口参与管线
```

### 事件分发 (BuffEventBinding.cs)
```
BuffEventDispatcher.dicBindings  # 事件名 → IBuffEventBinding 字典
默认已注册:
  GameFightLogic_UnderAttack_Dead       → EventForUnderAttackDead
  GameFightLogic_UnderAttack            → EventForUnderAttack（含前置 EventRole 过滤）
  GameFightLogic_RegainHP               → EventForRegainHP（回血事件,借用FightUnderAttackBean;含前置 EventRole 过滤,仅真实回血>0派发）
  GameFightLogic_CreatureDeadDropCrystal→ EventForCreatureDeadDropCrystal
  GameFightLogic_CreatureDeadStart      → EventForCreatureDeadStart
  GameFightLogic_CreatureDeadEnd        → EventForCreatureDeadEnd
新增事件：dicBindings 加一行 + BuffBaseEntity 加 virtual 方法，无需改基类 switch
```

### 关键文件

| 文件 | 路径 |
|------|------|
| BUFF 基类 | Assets/Scripts/Game/Buff/BuffEntity/BuffBaseEntity.cs |
| 事件分发 | Assets/Scripts/Game/Buff/BuffEventBinding.cs（IBuffEventBinding 接口已抽到 Interface/） |
| 属性修改管线 | Assets/Scripts/Game/Attribute/AttributeModifier.cs（**通用属性管线，已移出 Buff/**；含 IAttributeModifierSource 接口，BUFF/装备/天赋共用，非 BUFF 专属） |
| BUFF接口 | Assets/Scripts/Game/Buff/Interface/（IBuffSingleTarget 单体定向 / IBuffEventBinding 事件绑定，均以 IBuff 打头） |
| HP/DR 共享基类 | Assets/Scripts/Game/Buff/BuffEntity/BuffEntityBase*Change*.cs |
| 前置条件基类 | Assets/Scripts/Game/Buff/BuffPre/BuffBasePreEntity.cs |
| BuffHandler | Assets/Scripts/Component/Handler/BuffHandler.cs |
| BuffManager | Assets/Scripts/Component/Manager/BuffManager.cs |
| BuffBean | Assets/Scripts/Bean/Game/BuffBean.cs（含静态工厂 `CreateRandomWithFloor` 带下限随机） |
| 稀有度 BUFF 生成工具 | Assets/Scripts/Utils/BuffUtil.cs（`CreateRandomRarityBuff`/`CreateAscendRarityBuff`/`GetRarityBuffType`/`GetCreatureAscendBuffChances` 进阶概率展示） |
| BuffEntityBean | Assets/Scripts/Bean/Game/BuffEntityBean.cs |
| BuffInfoBean | Assets/Scripts/Bean/MVC/Game/BuffInfoBean.cs（自动生成，禁改） |
| BuffInfoBean 扩展 | Assets/Scripts/Bean/MVC/Game/BuffInfoBeanPartial.cs（含 BuffStackMode 枚举） |

## 约束

- 新增 BUFF 类型选择正确的基类（Attribute/Instant/Conditional/Periodic/Pecurrent）
- 属性 BUFF 必须实现 `CollectModifiers`，由 `ModifierPipeline.Apply` 统一计算；不再直接累乘
- 属性类型使用 `CreatureAttributeTypeEnum` 枚举；`CRT`/`EVA` 的 rate 走 Flat（其值本身是百分比）
- 前置条件以 `BuffPreEntityFor` 开头命名，**必须重写 `GetEventRole()`**，否则会被 UnderAttack 事件错误过滤
- 条件 BUFF 通过 `BuffInfoBean.class_entity_events` 声明事件名（必须在 `BuffEventDispatcher.dicBindings` 已注册）
- 事件订阅/注销由基类 `SetData`/`ClearData` 自动调用 `BuffEventDispatcher.Register/Unregister` 完成，子类不要手动订阅
- 修改 BUFF 配置字段时改 `BuffInfoBean.cs` 是禁止的（自动生成），所有扩展逻辑写在 `BuffInfoBeanPartial.cs`
- BUFF 池化复用：`ClearData` 内会注销事件并置空 `buffEntityData`；事件回调需用 `isValid` + null 守卫
- Instant 类型识别走 `BuffInfoBean.IsInstantBuffEntity()`（基于 Type 继承检查，按实例缓存），**不要用类名前缀判断**
- 深渊馈赠等级BUFF：通过 `buff_parent_id` + `buff_level` 实现替换升级；新增时 `BuffHandler.AddAbyssalBlessing` 会自动移除旧等级；**必须在防守核心创建后调用**（战斗中或 `GameFightLogic.PreGameForAfterCreateDefenseCore` 钩子），此前调用 `LogWarning` 跳过不添加
- 死亡流程：`RemoveFightCreatureBuffs` 前应先 `TriggerEvent(GameFightLogic_CreatureDeadEnd)`，让 `BuffEntityConditionalDead` 有机会完成触发
- 添加 BUFF 必须经过 `BuffHandler.AddFightCreatureBuff`（处理 createRate、stacking、事件通知），不要直接写 `manager.dicFightCreatureBuffsActivie`
- 攻击时间修正走专用通道 `BuffHandler.ChangeAttackTimeDataForBuff`（看 `BuffEntityAttributeAttackTime`），不接入属性管线；该方法除生物自身战斗BUFF外，还扫描深渊馈赠池中实现 `IBuffSingleTarget` 的攻速BUFF，按锁定 `SingleTargetCreatureUUId` 单体生效
- 单体定向深渊馈赠（随机一只防守生物 ATK/HP/DR/攻速 翻倍）：`BuffEntityAttributeSingleTarget`/`BuffEntityAttributeAttackTimeSingleTarget` 实现 `IBuffSingleTarget`（`SetData` 随机锁定一只防守生物 UUID）；属性类在 `FightCreatureBean.CollectFromBuffList`、攻速类在 `ChangeAttackTimeDataForBuff` 按 `SingleTargetCreatureUUId` 过滤。**复制魔物(增殖 `BuffEntityInstantCloneDefenseCreature`)不继承单体定向**：克隆体是新 UUID 不匹配锁定 UUID，只继承全体性馈赠(靠 trigger_creature_type)。随机锁定走 `FightBean.GetRandomDefenseCreatureUUId()`(fightData 实例方法)；卡片展示用 `AbyssalBlessingUtil.IsAbyssalBlessingTargetCreature`(`Assets/Scripts/Utils/`) 统一判定口径。**只改运行时 dicAttribute/攻击时间，绝不改 `dlDefenseCreatureData` 里 CreatureBean 的 creatureAttribute（与存档共享引用，会污染存档）**。详见 abyssal-blessing-system SKILL
- 动态率深渊馈赠（曾用于都是兄弟/杀红了眼，现役无配置、机制留存）：抽象基类 `BuffEntityAttributeDynamicRate : BuffEntityAttribute` 重写 `CollectModifiers`+`ChangeData` 用 `GetDynamicRate()` 替代配置固定 `trigger_value_rate`（仅走 PercentAdd，用于 ATK/DR/HP）；子类为**通用功能类**（按缩放来源命名、不绑馈赠名，可被其它同功能馈赠复用）：`BuffEntityAttributeScaleByDefenseCount`(属性随"当前场上存活防守魔物数"缩放，rate=(N-1)×rate，曾用于都是兄弟) / `BuffEntityAttributeScaleByKillCount`(属性随"本局累计击杀数"缩放，rate=`fightRecordsData.totalKillNumForDef`×rate，曾用于杀红了眼)。rate 变化需重算 `dicAttribute` 才生效（事件驱动）：魔物死亡/敌人击杀由 `GameFightLogic.EventForGameFightLogicCreatureDeadEnd` 广播 `RefreshAllDefenseCreatureAttribute()`（且重算放在 `CheckGameEnd()` 之前）；魔物放置/增殖由 `CreatureHandler.CreateDefenseCreatureEntity` 末尾**推送新事件** `EventsInfo.GameFightLogic_DefenseCreatureCreate`（参数 FightCreatureEntity）→ `GameFightLogic.EventForDefenseCreatureCreate` 监听后广播（CreatureHandler 只生成、推事件，重算职责归 GameFightLogic）。守卫用泛型 `BuffHandler.HasDynamicRateAbyssalBlessing()`（通用：馈赠池含指定类型/子类 BUFF 才广播）避免普通对局开销。详见 abyssal-blessing-system SKILL
- `BuffHandler.AddAbyssalBlessing` 末尾 `TriggerEvent(Buff_AbyssalBlessingChange)`，由 `GameFightLogic.EventForAbyssalBlessingChange` 监听并刷新防守核心 + 全部防守生物 `RefreshBaseAttribute`（事件驱动，BuffHandler 不直接刷新）：属性类馈赠只有重算 `dicAttribute` 才生效，征服「普通关→普通关」走 `ContinueNextLevelInSameScene` 保留现场不重算，若馈赠变化时不刷新会出现「普通关选了不生效、切BOSS关重载场景才生效」的BUG。改动馈赠添加链路勿删此事件触发
- 稀有度 BUFF 生成统一走 `BuffUtil`（`Assets/Scripts/Utils/BuffUtil.cs`），**扭蛋与魔物进阶共用**：`GetRarityBuffType(RarityEnum)`（R/SR/SSR→`CreatureRarity*`，N/UR/L→None）、`CreateRandomRarityBuff(RarityEnum)`（扭蛋通用：取对应 buff_type 池随机 1 条 `new BuffBean(id, isRandom:true)`）、`CreateAscendRarityBuff(newRarity, materials)`（魔物进阶：素材在 newRarity 槽位 BUFF 按 id 聚合，每 id 提供 25%×数量 命中概率，命中则继承并用 `BuffBean.CreateRandomWithFloor` 重随机数值≥素材原值，未命中回退通用随机；UR/L 返回 null）。`GashaponItemBean.RandomRarityBuff` 已改为调用 `CreateRandomRarityBuff`，不要再内联 switch
- `BuffBean.CreateRandomWithFloor(id, floorValue, floorValueRate, createRate=1f)`：沿用扭蛋整数闭区间随机口径，但随机下限抬到 `max(配置min, floor)`，保证重随机结果≥下限（专供魔物进阶继承素材 BUFF 重随机）
