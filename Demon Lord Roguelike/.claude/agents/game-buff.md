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
├── BuffEntityInstant                       # 瞬时触发（SetData后isValid=false）
│   ├── BuffEntityInstantCloneDefenseCreature      # 深渊馈赠「增殖」
│   ├── BuffEntityInstantRewardMoreItem            # 深渊馈赠「奖励多多」
│   └── BuffEntityInstantRewardMoreSelect          # 深渊馈赠「再来一瓶」
├── BuffEntityConditional                   # 条件触发（UpdateBuffTime只增总时长）
│   ├── BuffEntityConditionalAttack         # 攻击/受击事件触发自定义 AttackMode
│   ├── BuffEntityConditionalAttackAgain    # 触发AI立即再攻击一次
│   ├── BuffEntityConditionalAttribute      # 属性变化时触发
│   ├── BuffEntityConditionalDead           # 自身死亡结束时触发
│   ├── BuffEntityConditionalDeadAttack     # 死亡时发起一次攻击
│   ├── BuffEntityConditionalDeadRebirth    # 死亡时重生
│   ├── BuffEntityConditionalDeadAreaHPChange / DeadAreaDRChange  # 死亡时范围改HP/DR
│   ├── BuffEntityConditionalDeadCreateCrystal                    # 死亡时生成水晶
│   ├── BuffEntityConditionalAddDropCrystal                       # 死亡掉落水晶时叠加
│   └── BuffEntityConditionalCreateCrystal                        # 生成水晶
├── BuffEntityPeriodic                      # 周期性触发（无次数）
│   ├── BuffEntityPeriodicAttackAgain       # 周期性再攻击
│   ├── BuffEntityPeriodicMultiInstantAttack # 周期性多次瞬时攻击（闪电落雷：BUFF只管触发→快照全场敌人→有放回抽N目标→第1道立即+后续0.1秒间隔连发(UpdateBuffTime驱动)；每道雷=发射 AttackModeInstantAreaThunder 攻击模块，粒子/AOE/伤害走 AttackMode 框架(半径/单雷命中上限配在攻击模块表 collider_area_size/hit_max，伤害=魔王实时ATK×trigger_value 发射时注入,CRT=0)；class_entity_data="次数,攻击模块ID"；馈赠「闪电」3000300001~005 → 攻击模块300031~300035）
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
SSR(13) 特殊类             —— 死亡重生/死亡反击/死亡区域治疗/克隆增殖/生成改变水晶掉落等质变效果；
                              类 BuffEntityConditionalDead*/BuffEntityInstant* 等
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
- 稀有度 BUFF 生成统一走 `BuffUtil`（`Assets/Scripts/Utils/BuffUtil.cs`），**扭蛋与魔物进阶共用**：`GetRarityBuffType(RarityEnum)`（R/SR/SSR→`CreatureRarity*`，N/UR/L→None）、`CreateRandomRarityBuff(RarityEnum)`（扭蛋通用：取对应 buff_type 池随机 1 条 `new BuffBean(id, isRandom:true)`）、`CreateAscendRarityBuff(newRarity, materials)`（魔物进阶：素材在 newRarity 槽位 BUFF 按 id 聚合，每 id 提供 10%×数量 命中概率，命中则继承并用 `BuffBean.CreateRandomWithFloor` 重随机数值≥素材原值，未命中回退通用随机；UR/L 返回 null）。`GashaponItemBean.RandomRarityBuff` 已改为调用 `CreateRandomRarityBuff`，不要再内联 switch
- `BuffBean.CreateRandomWithFloor(id, floorValue, floorValueRate, createRate=1f)`：沿用扭蛋整数闭区间随机口径，但随机下限抬到 `max(配置min, floor)`，保证重随机结果≥下限（专供魔物进阶继承素材 BUFF 重随机）
